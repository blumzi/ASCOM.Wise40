import tkinter
import serial
import time
import sys
import csv
import os
#com_port = "COM4"
com_port = "COM1"
bit_rate = 9600

class Element(object):
	def __init__(self, var, value, title):
		self.title = title
		self.value = value
		self.element_var = var
		self.element = None
		self.setValue(value)
	
	def setValue(self, new_value):
		self.element_var.set(new_value)
		
	def getValue(self):
		return self.element_var.get()
		
	def pack(self, row, column):
		if self.element != None:
			self.element.grid(row=row, column=column)

class Position(Element):
	def __init__(self, var, value, title):
		super(Position, self).__init__(var, value, title)
		self.element = Radiobutton(text=title, variable=self.element_var, value=value)

class Offset(Element):
	def __init__(self, var, value, title='offset'):
		super(Offset, self).__init__(var, value, title)
		self.element = Entry(textvariable=self.element_var)
		self.element.config(width=6)
		
	def set(self, val):
		self.element.setValue(val)
		
class Component(object):
	def __init__(self, name, column):
		self.name = name
		self.positions = {}
		self.buttons = {}
		self.selected_pos = None
		self.true_pos = None
		self.radio_var = StringVar()

		self.column = column
		self.row = 1

		self.label = Label(text=name.upper())
		self.label.grid(row=0, column=self.column)

	def addElement(self, value, title):
		self.positions[title] = Position(self.radio_var, value, title)
		self.positions[title].pack(row=self.row, column=self.column)
		self.row += 1

	def selectedPosition(self):
		return int(self.radio_var.get())

	def truePosition(self):
		return self.positions[self.true_pos].value

	def selectElement(self, title):
		self.positions[title].element.select()
		self.selected_pos = title

	def setElement(self, new_title):
		for title in self.positions:
			position = self.positions[title]
			position.element.config(fg='black')
		self.positions[new_title].element.config(fg='red')
		self.true_pos = new_title
		
	def setElementByValue(self, value):
		for title in self.positions:
			position = self.positions[title]
			if value == position.value:
				self.setElement(position.title)
				break

	def isChanged(self):
		return True if self.truePosition() != self.selectedPosition() else False

class WheelComponent(Component):
	def __init__(self, name, column):
		super(WheelComponent, self).__init__(name, column)
		self.offset_var = StringVar()
		self.offset = None
		self.true_offset = 0
		
	def addOffset(self):
		self.offset = Offset(self.offset_var, 0)
		self.offset.pack(self.row, self.column)
		
	def getOffset(self):
		return self.offset.getValue()
		
	def setOffset(self, new_value):
		self.offset.setValue(new_value)
		self.true_offset = new_value
		
	def getNextMoveCommand(self):
		move = int(self.selectedPosition()) - int(self.truePosition())
		if move == 0:
			return None
		direction = "f"
		if(move < 0):
			direction = "b"
			move *= -1
		command = 'move,'+self.name+','+direction
		return command
	
	def getOffsetCommand(self):
		command = 'move_steps,'+self.name+','+str(self.getOffset())
		print ("offset command: " + command)
		return command
	
	def sendCommand(self, connection):
		move_command = self.getNextMoveCommand()
		self.offset.element.config(fg='black')
		while not None == move_command:
			connection.write(move_command)
			receipt = connection.read()
			if not "received,move" in receipt:
				print("Out of sync. Try move again")
				return 
			finished = connection.read()
			finished = finished.rstrip().split(",")
			if not finished[0] == "finished" or not finished[1] == "move":
				print("Out of sync. Try move again")
				return
			new_pos = int(finished[3])
			self.setElementByValue(new_pos)
			move_command = self.getNextMoveCommand()
		
		offset_command = self.getOffsetCommand()
		connection.write(offset_command)
		receipt = connection.read()
		if not "received,move_steps" in receipt:
			print("Failed to move calibration steps")
			return
		finished = connection.read()
		if not "finished,move_steps" in finished:
			print("Failed to move calibration steps")
			return
		self.true_offset = self.getOffset()
		self.offset.element.config(fg='red')
	
	def startUp(self, connection):
		move_command = 'move,'+self.name+','+'f'
		connection.write(move_command)
		receipt = connection.read()
		if not "received,move" in receipt:
			print("Out of sync. Try move again")
			return 
		finished = connection.read()
		finished = finished.rstrip().split(",")
		if not finished[0] == "finished" or not finished[1] == "move" or not len(finished) == 4:
			print("Out of sync. Try move again")
			return
		
		new_pos = int(finished[3])
		self.setElementByValue(new_pos)
		self.true_offset = 0
		
	
	def isChanged(self):
		print ("true: " + str(self.true_offset))
		print ("reported: " + str(self.getOffset()))
		if self.truePosition() != self.selectedPosition() or self.true_offset != self.getOffset():
			return True
		return False
		
class BinaryComponent(Component):
	def __init__(self, name, column):
		super(BinaryComponent, self).__init__(name, column)

	def getCommand(self):
		command = "set,"+self.name+","+str(self.selectedPosition())
		return command
		
	def sendCommand(self, connection):
		cmd = self.getCommand()
		print(cmd)
		connection.write(cmd)
		receipt = connection.read()
		print("Received: " + receipt)
		receipt = receipt.rstrip().split(",")
		if not "set" == receipt[0]:
			print("Out of sync")
			return False
		self.setElementByValue(int(receipt[2]))
		
class App:
	def __init__(self,master):
		self.frame = tkinter.Frame(master)
		self.binary_components = ['shutter','halogen_1','he_ar','mirror']
		self.wheel_components = ['collimator','aperture','upper_grism','lower_grism']
		self.arduino = Arduino()

		self.components = {}
		for i,component in enumerate(self.binary_components + self.wheel_components):
			if component in self.wheel_components:
				self.components[component] = WheelComponent(component, i)
			else:
				self.components[component] = BinaryComponent(component, i)
		self.createGuiFromFile()

	def createGuiFromFile(self):
		with open('FOSC_GUI_SETTINGS.csv','r') as settings_file:
			settings_reader = csv.reader(settings_file,skipinitialspace=True)
			for line in settings_reader:
				if line[0][0]=='#':
					continue
				component, title, value = line[0], line[1], int(line[2])
				if component in self.components:
					self.components[component].addElement(value, title)
					
		for component in self.wheel_components:
			self.components[component].addOffset()
		max_row = reduce(lambda x,y: x if x.row > y.row else y, self.components.values()).row

		self.quit_button = Button(text='QUIT', fg='red', command=self.destroyApp)
		self.quit_button.grid(column=len(self.components.keys()), row=max_row)
		self.submit_button = Button(text='Send',fg='blue',command=self.sendAllCommands);
		self.submit_button.grid(column=0,row=max_row)

		self.initializeGuiWithSavedState()

	def initializeGuiWithSavedState(self):
		with open('FOSC_LAST_STATE.csv') as settings_file:
			settings_reader = csv.reader(settings_file,skipinitialspace=True)
			for line in settings_reader:
				if(line[0][0]!='#'):
					component, title = line[0], line[1]
					offset = 0
					if (len(line) >= 3):
						offset = line[2]
					self.components[component].selectElement(title)
					self.components[component].setElement(title)
					if component in self.wheel_components:
						self.components[component].setOffset(offset);
		#self.startUp()

	def startUp(self):
		for component in self.wheel_components:
			self.components[component].startUp(self.arduino)
		self.sendAllCommands()
		
	def saveCurrentState(self):
		with open('TEMP_LAST_STATE','w') as save_file:
			save_file.write("#TYPE,VALUE,OFFSET#")
			for component in self.components:
				offset = 0
				if component in self.wheel_components:
					offset = self.components[component].getOffset()
				save_file.write("\n") 	
				to_write = component + "," + self.components[component].true_pos + "," + str(offset)
				save_file.write(to_write)
		os.remove('FOSC_LAST_STATE.csv')
		os.rename('TEMP_LAST_STATE','FOSC_LAST_STATE.csv')

	def getChangedComponents(self):
		changed_components = []
		for component in self.components.values():
			if component.isChanged():		
				changed_components.append(component)
		return changed_components


	def destroyApp(self):
		self.saveCurrentState()
		self.frame.quit()

	def sendAllCommands(self):
		changed_components = self.getChangedComponents()
		
		print(map( lambda x: x.name , changed_components))
		for component in changed_components:
			component.sendCommand(self.arduino)

		self.saveCurrentState()
		print("DONE")
	
	'''
	def sendSetCommand(self,component,state):
		print component,state
		cmd = "set,"+component+","+state
		print cmd
		#self.arduino.write(cmd)
		#conf = self.arduino.read().lower().split(",",1)
		#if(conf[0] == "received" and conf[1] == cmd):
		#	print "Set command received"
		#	conf = self.arduino.read().lower().split(",",1)
		#	if(conf[0] == "completed" and conf[1] == cmd):
		#		print "Set command completed"
		#		conf = self.arduino.read().lower()
		#		if(conf == "ready"):
		#			print "Arduino is ready to receive a new command"
		#			return 1
		return 0

	def sendMoveCommand(self,component,dir):
		cmd = "move,"+component+","+dir
		print cmd
		pos = self.currentState[component]
		num_pos = self.guiData[component]['set'][self.currentState[component]]['numeric_pos']
		if(dir=='f'):
			num_pos+=1
		else:
			num_pos-=1
		#self.arduino.write(cmd)
		#self.arduino.read()
		#conf = self.arduino.read().split(",")
		#
		#print conf
		#pos = conf[3]
		#
		#self.guiData[component]['ops'][self.reverseDataLookup[component][pos]]['button'].select()
		#self.currentState[component] = pos
		return pos
	'''
		
class Arduino:
	
	def __init__(self):
		self.serCon = serial.Serial(com_port,bit_rate)
		if(self.serCon.isOpen):
			print("Serial port:"+self.serCon.name+" opened")
		else:
			sys.exit("Failed to open port")
		time.sleep(3)
		firstInput = self.read()
		if(firstInput == "SETUP COMPLETE"):
			print("Arduino is up")
		else:
			sys.exit("Arduino failed to boot properly")
	def non_blocking_read(self):
		if(self.serCon.inWaiting() != 0):
			input = self.serCon.readline().strip()
			print("FROM ARDUINO: "+input)
			self.serCon.flush
			return input.strip()
		else:
			return ""
	def read(self):
		input = self.serCon.readline().strip()
		print("FROM ARDUINO: "+input)
		self.serCon.flush
		return input.strip()
	def is_open(self):
		if(self.serCon.isOpen):
			return True
		return False
		
	def write(self,command):
		if(self.is_open()):
			print("TO ARDUINO: "+command)
			self.serCon.write(command+"\n")
		else:
			print("Serial connection is closed, could not write")
root = tkinter.Tk()

app = App(root)

root.mainloop()
root.destroy()
