from Tkinter import *
import serial
import time
import sys
import csv
import os
comPort = "COM4"
bitRate = 9600

#guiData looks like this
#							guiData
#							[component] - the components of FOSC
#'col'-Column in GUI	'set'-Holds component specific settings 						'ordered_settings'-Order settings are displayed in GUI 	'real_value'-known value IRL		'button_value'- GUI button value	'label'-GUI Label(at top)	'gui_offset_value'		'entry_value' entry variable	'entry' entry object	'offset_label_value' 'offset_label'
#						 	|													\
#					 	[specific position] - like position_1,on				'reverse_lookup'	
#						/			|							\						\
#	'numeric_pos'-value for FOSC 'button'-GUI button	'offset'-calibration value     [specific_position]-Name value
																							
#										
class App:
	def __init__(self,master):
		self.frame = Frame(master)
		self.master = master
		self.on_off_components = ['shutter','halogen1','halogen2','fe_a','mirror']
		self.position_components = ['collimator','aperture','upper_grism','lower_grism']
		self.components = self.on_off_components + self.position_components
		
		self.write_queue = []
		
		#Hardcoded because this should never change
		self.componentPins = {}
		self.componentPins['aperture'] = {'pulse':1,'position_coarse':2,'direction':3,'position_fine':4}
		self.componentPins['upper_grism'] = {'pulse':5,'position_coarse':6,'direction':7,'position_fine':8}
		self.componentPins['lower_grism'] = {'pulse':9,'position_coarse':10,'direction':11,'position_fine':12}
		self.componentPins['collimator'] = {'pulse':13,'position':14,'direction':15}
		self.componentPins['shutter'] = 16
		self.componentPins['halogen1'] = 17
		self.componentPins['halogen2'] = 18
		self.componentPins['fe_a'] = 19
		self.componentPins['mirror'] = 20
		
		self.guiData = {}
		for i,comp in enumerate(self.components):
			self.guiData[comp] = {}
			self.guiData[comp]['col'] = i
			self.guiData[comp]['row'] = 0
			self.guiData[comp]['set'] = {} #position etc SETtings
			self.guiData[comp]['set']['reverse_lookup'] = {} #find name given numeric position
			self.guiData[comp]['ordered_settings'] = []
			self.guiData[comp]['button_value'] = StringVar()
			if comp in self.position_components:
				self.guiData[comp]['entry_value'] = StringVar()

		self.currentState = {}
		self.create_gui_from_file()
		self.initialize_gui_with_saved_state()
		
		print "App initialized"
		self.arduino = Arduino()
	
	def create_gui_from_file(self):
		with open('FOSC_GUI_SETTINGS.csv','r') as settingsFile:
			settingsReader = csv.reader(settingsFile,skipinitialspace=True)
			for line in settingsReader:
				if line[0][0]=='#':
					continue
				self.guiData[line[0]]['set'][line[1]] = {'numeric_pos':line[2],'offset':line[3]}
				self.guiData[line[0]]['set']['reverse_lookup'][line[2]] = line[1]
				self.guiData[line[0]]['ordered_settings'].append(line[1]) #doing it like this avoids 
								#any annoying sorting errors in displaying numbers over two digits, displays
								#the settings in the order in which they are defined in FOSC_GUI_SETTINGS.csv

		self.create_gui()

	def create_gui(self):
		maxRow = 0
		maxColumn = 0
		for comp in self.components:
			self.guiData[comp]['label']=Label(text=comp.upper())
			self.guiData[comp]['label'].grid(row=self.guiData[comp]['row'],column=self.guiData[comp]['col'])
			self.guiData[comp]['label'].config(fg="red")
			self.guiData[comp]['row']+=1
			for setting in self.guiData[comp]['ordered_settings']:
				self.guiData[comp]['set'][setting]['button'] = Radiobutton(text=setting,variable=self.guiData[comp]['button_value'],value=self.guiData[comp]['set'][setting]['numeric_pos'])
				self.guiData[comp]['set'][setting]['button'].grid(row=(self.guiData[comp]['row']),column=self.guiData[comp]['col'])
				self.guiData[comp]['row']+=1
			if(comp in self.position_components):
				self.guiData[comp]['offset_label_value'] = StringVar()
				self.guiData[comp]['offset_label'] = Label(textvariable=self.guiData[comp]['offset_label_value'])
				self.guiData[comp]['offset_label'].grid(row = self.guiData[comp]['row'], column = self.guiData[comp]['col'])
				self.guiData[comp]['offset_label'].config(width = 6)
				self.guiData[comp]['offset_label_value'].set("0")
				self.guiData[comp]['row']+=1
				
				self.guiData[comp]['entry'] = Entry(textvariable=self.guiData[comp]['entry_value'])
				self.guiData[comp]['entry'].grid(row = self.guiData[comp]['row'], column = self.guiData[comp]['col'])
				self.guiData[comp]['entry'].config(width = 6)
				self.guiData[comp]['entry_value'].set("0")
				self.guiData[comp]['row']+=1
			if(self.guiData[comp]['row'] > maxRow):
				maxRow = self.guiData[comp]['row']
			if(self.guiData[comp]['col'] > maxColumn):
				maxColumn = self.guiData[comp]['col']
		self.quitButton = Button(text='QUIT', fg='red', command=self.destroy_app)
		self.quitButton.grid(column=maxColumn,row=maxRow)
		self.submitButton = Button(text='Send',fg='blue',command=self.send_command);
		self.submitButton.grid(column=0,row=maxRow)
		self.initialize_gui_with_saved_state()

	def initialize_gui_with_saved_state(self):
		with open('FOSC_LAST_STATE.csv') as settingsFile:
			settingsReader = csv.reader(settingsFile,skipinitialspace=True)
			for line in settingsReader:
				if(line[0][0]!='#'):
					print line
					self.guiData[line[0]]['set'][line[1]]['button'].select()
					self.guiData[line[0]]['set'][line[1]]['button'].config(fg="red")
					self.currentState[line[0]] = {'pos':line[1],'offset':0}
					if line[0] in self.position_components:
						print line[0],line[2]
						self.currentState[line[0]]['offset'] = line[2]
						self.guiData[line[0]]["offset_label_value"].set(line[2])
						self.guiData[line[0]]["entry_value"].set(line[2])
		
	def save_current_state(self):
		with open('TEMP_LAST_STATE','w') as saveFile:
			saveFile.write("#TYPE,VALUE#")
			for comp in self.on_off_components:
				saveFile.write("\n")
				print self.currentState[comp]
				toWrite = comp + "," + self.currentState[comp]['pos']
				saveFile.write(toWrite)
				
				
			for comp in self.position_components:
				saveFile.write("\n")
				toWrite = comp + "," + self.currentState[comp]['pos']+","+str(self.currentState[comp]['offset'])
				saveFile.write(toWrite)
		saveFile.close()
		os.remove('FOSC_LAST_STATE.csv')
		os.rename('TEMP_LAST_STATE','FOSC_LAST_STATE.csv')
		
	def get_changed_components(self):
		changedComponents = []
		for comp in self.components:
			guiValue = self.guiData[comp]['button_value'].get()
			posName = self.currentState[comp]['pos']
			if(guiValue != self.guiData[comp]['set'][posName]['numeric_pos']):
				changedComponents.append(comp)
		return changedComponents
	def get_changed_offsets(self):
		changedComponents = []
		for comp in self.position_components:
			guiValue = self.guiData[comp]['entry_value'].get()
			if(guiValue != self.currentState[comp]['offset']):
				changedComponents.append(comp)
		print "CHANGED OFFSETS:",changedComponents
		return changedComponents
		
	def set_current_state(self,changedComponents,changedOffsets):
		for comp in changedComponents:
			textPos=""
			pos = 0
			if(comp in self.position_components):
				textPos = self.currentState[comp]['pos']
				pos = self.guiData[comp]['set'][textPos]['numeric_pos']
				self.guiData[comp]['button_value'].set(pos)
			elif(comp in self.on_off_components):
				textPos = self.currentState[comp]['pos']
				pos = self.guiData[comp]['set'][textPos]['numeric_pos']
				self.guiData[comp]['button_value'].set(pos)
			self.clear_column(comp)
			self.guiData[comp]['set'][textPos]['button'].config(fg="red")
			#print "CHANGED COLOR: "+textGuiValue
		for comp in changedOffsets:
			offsetValue = self.guiData[comp]['entry_value'].get()
			print offsetValue,self.currentState[comp]['offset'],self.guiData[comp]['offset_label_value'].get()
			self.currentState[comp]['offset']=offsetValue
			self.guiData[comp]['offset_label_value'].set(offsetValue)
			print offsetValue,self.currentState[comp]['offset'],self.guiData[comp]['offset_label_value'].get()
	def clear_column(self, component):
		print "test"
		for position in self.guiData[component]['set'].keys():
			if(position != "reverse_lookup"):
				self.guiData[component]['set'][position]['button'].config(fg="black")
	def destroy_app(self):
		self.save_current_state()
		self.frame.quit()
	def send_command(self):
		changedComponents = self.get_changed_components()
		changedOffsets = self.get_changed_offsets()
		for comp in changedComponents:
			buttonValue = self.guiData[comp]['button_value'].get()
			nameValue = self.guiData[comp]['set']['reverse_lookup'][buttonValue]
			foscValue = self.guiData[comp]['set'][nameValue]['numeric_pos']
			if comp in self.on_off_components:
				newPos = self.send_set_command(comp,foscValue)
				newPosText = self.guiData[comp]['set']['reverse_lookup'][newPos]
				self.currentState[comp]["pos"]=newPosText
			elif comp in self.position_components:
				move = int(foscValue) - int(self.guiData[comp]['set'][self.currentState[comp]['pos']]['numeric_pos'])
				print "MOVE:",move
				dir = 'f'
				if(move < 0):
					dir = 'b'
					move = abs(move)
				print "DIR:",dir
				if(abs(move)>5):
					move = abs(move)
					if(dir == 'f'):
						dir = 'b'
					else:
						dir = 'f'
					move = 10 - move
				print "MOVE-AFTER:",move
				print "DIR-AFTER:",dir
				while(move > 0):
					self.currentState[comp]['pos'] = self.send_move_command(comp,dir)
					move = move - 1
				print "END ALL MOVES"
		for comp in changedOffsets:
			print comp
			newOffset = self.guiData[comp]['entry_value'].get()
			self.send_move_steps_command(comp,newOffset)
			
		self.set_current_state(changedComponents,changedOffsets)
		self.save_current_state()
	def send_move_steps_command(self,component,steps):
		cmd = "move_steps,"+component+","+steps
		self.arduino.write(cmd)
		conf = self.arduino.read().lower().split(",",1)
		if(conf[0]=="received" and conf[1]==cmd):
			#print "Move_steps command received"
			conf = self.arduino.read().lower().split(",")
			if(conf[0]=="finished" and conf[1]=="move_steps" and conf[2]==component):
				#print "Move_steps command finished"
				#self.currentState[component]['offset'] = steps
				return 1
		return 0
	def send_set_command(self,component,value):
		cmd = "set,"+component+","+value
		self.arduino.write(cmd)
		conf = self.arduino.read().lower().split(",",1)
		if(conf[0]=="received" and conf[1]==cmd):
			#print "Set command received"
			conf = self.arduino.read().lower().split(",")
			if(conf[0]=="completed"):
				#print "Set command completed"
				return conf[3]
	def send_move_command(self,component,dir):
		cmd = "move,"+component+","+dir
		self.arduino.write(cmd)
		conf = self.arduino.read().lower().split(",",1)
		if(conf[0]=="received" and conf[1]==cmd):
			#print "Move command received"
			conf = self.arduino.read().lower().split(",")
			if(conf[0] == "finished"):
				pos = conf[3]
				posName = self.guiData[component]['set']['reverse_lookup'][pos]
				#print "MOVE COMPLETE"
				return posName
		
class Arduino:
	
	def __init__(self):
		self.serCon = serial.Serial(comPort,bitRate)
		if(self.serCon.isOpen):
			print "Serial port:"+self.serCon.name+" opened"
		else:
			sys.exit("Failed to open port")
		time.sleep(3)
		firstInput = self.read()
		if(firstInput == "SETUP COMPLETE"):
			print "Arduino is up"
		else:
			sys.exit("Arduino failed to boot properly")
	def non_blocking_read(self):
		if(self.serCon.inWaiting() != 0):
			input = self.serCon.readline().strip()
			print "FROM ARDUINO: "+input
			self.serCon.flush
			return input.strip()
		else:
			return None
	def read(self):
		input = self.serCon.readline().strip()
		print "FROM ARDUINO: "+input
		self.serCon.flush
		return input.strip()
	def is_open(self):
		if(self.serCon.isOpen):
			return True
		return False
		
	def write(self,command):
		if(self.is_open()):
			print "TO ARDUINO: "+command
			self.serCon.write(command+"\n")
		else:
			print "Serial connection is closed, could not write"

root = Tk()

app = App(root)

root.mainloop()
root.destroy()
