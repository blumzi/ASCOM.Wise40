from Tkinter import *
import serial
import time
import sys
import csv
import os
comPort = "COM3"
bitRate = 9600

class App:
	def __init__(self,master):
		self.frame = Frame(master)
		self.on_off_components = ['shutter','halogen1','halogen2','fe_a','mirror']
		#self.position_components = ['collimator','aperture','upper_grism','lower_grism']
		self.position_components = ['aperture','upper_grism','lower_grism']
		self.components = self.on_off_components + self.position_components
		self.componentPins = {}
		self.currentState = {}
		self.dataLookup = {}
		self.reverseDataLookup = {}
		self.guiData = {}
		self.guiSettings = {}
		for i,comp in enumerate(self.components):
			self.dataLookup[comp] = {}
			self.reverseDataLookup[comp] = {}
			self.guiData[comp] = {'var':StringVar(),'ops':{}}
			self.guiSettings[comp] = {}
			self.guiSettings[comp]['col'] = i 	
			self.guiSettings[comp]['row'] = 0
			self.guiSettings[comp]['numUsedRows'] = 1
		self.componentPins["aperture"] = {"pulse":1,"position_coarse":2,"direction":3,"position_fine":4}
		self.componentPins["upper_grism"] = {"pulse":5,"position_coarse":6,"direction":7,"position_fine":8}
		self.componentPins["lower_grism"] = {"pulse":9,"position_coarse":10,"direction":11,"position_fine":12}
		#self.componentPins["collimator"] = {"pulse":13,"position":14,"direction":15}
		self.componentPins["shutter"] = 16
		self.componentPins["halogen1"] = 17
		self.componentPins["halogen2"] = 18
		self.componentPins["fe_a"] = 19
		self.componentPins["mirror"] = 20
		
		self.arduino = Arduino()
		self.create_gui_from_file()
		
		print "App initialized"

		
		
		
	def create_current_state_string(self):
		state = [];
		for i in range(0,21):
			state.append(0)
		for comp in ["shutter","halogen1","halogen2","fe_a","mirror"]:
			print self.componentPins[comp]
			state[self.componentPins[comp]] = self.dataLookup[comp][self.currentState[comp]]
		stateString = ""
		for a in state:
			stateString += str(a)
		print "PIN STATE FOLLOWS"
		print stateString
		return stateString
	def destroy_app(self):
		self.set_current_state()
		self.save_current_state()
		self.frame.quit()
	def set_current_state(self):
		for comp in self.components:
			currentValue = self.guiData[comp]['var'].get()
			if(self.currentState[comp] != currentValue):
				self.guiData[comp]['ops'][self.reverseDataLookup[comp][self.currentState[comp]]]['button'].config(fg="black")
				self.guiData[comp]['ops'][currentValue]['button'].config(fg="red")
				self.currentState[comp] = currentValue
	def get_changed_components(self):
		changedComponents = []
		for comp in self.components:
			if(self.currentState[comp] != self.guiData[comp]['var'].get()):
				changedComponents.append(comp)
		return changedComponents
	def save_current_state(self):
		with open('TEMP_LAST_STATE','w') as saveFile:
			saveFile.write("#TYPE,VALUE#")
			for comp in self.components:
				saveFile.write("\n")
				saveFile.write(comp + "," + self.currentState[comp])
		saveFile.close()
		os.remove('FOSC_LAST_STATE.csv')
		os.rename('TEMP_LAST_STATE','FOSC_LAST_STATE.csv')

	def create_gui_from_file(self):
		with open('FOSC_GUI_SETTINGS.csv','r') as settingsFile:
			settingsReader = csv.reader(settingsFile,skipinitialspace=True)
			for line in settingsReader:
				if(line[0][0] == '#'):
					continue
				self.guiData[line[0]]['ops'][line[1]]={}
				self.guiData[line[0]]['ops'][line[1]]['value']=line[1]
				self.dataLookup[line[0]][line[1]] = line[2]
				self.reverseDataLookup[line[0]][line[2]] = line[1]
		self.create_gui()
		self.initialize_GUI()
		
	def initialize_GUI(self):
		with open('FOSC_LAST_STATE_DEFAULT.csv','r') as settingsFile:
			settingsReader = csv.reader(settingsFile,skipinitialspace=True)
			for line in settingsReader:
				if(line[0][0] != '#'):
					print line
					self.guiData[line[0]]['ops'][line[1]]['button'].select()
					self.currentState[line[0]] = line[1]
					self.guiData[line[0]]['ops'][line[1]]['button'].config(fg="red")

					#self.guiData[line[0]]['ops'][self.dataLookup[line[0]][line[1]]]['button'].select()
					#self.currentState[line[0]] = self.dataLookup[line[0]][line[1]]
					#self.guiData[line[0]]['ops'][self.dataLookup[line[0]][line[1]]]['button'].config(fg="red")
					
		for component in self.position_components:
			state = self.establish_current_state(component)
		self.set_current_state()

	def establish_current_state(self,component):
		self.send_move_command(component,"f")
		pos = self.send_move_command(component,"b")
		#self.guiData[component]['ops'][pos]['button'].select()
		#self.currentState[component] = pos
		#self.guiData[component]['ops'][pos]['button'].config(fg="red")

		
	def create_gui(self):
		maxRow = 0
		maxColumn = 0
		for comp in self.components:
			Label(text=comp.upper()).grid(row=self.guiSettings[comp]['row'],column=self.guiSettings[comp]['col'])
			for setting in sorted(self.guiData[comp]['ops'].keys()):
				self.guiData[comp]['ops'][setting]['button'] = Radiobutton(text=setting,variable=self.guiData[comp]['var'],value=self.guiData[comp]['ops'][setting]['value'])
				self.guiData[comp]['ops'][setting]['button'].grid(row=(self.guiSettings[comp]['row']+self.guiSettings[comp]['numUsedRows']),column=self.guiSettings[comp]['col'])
				self.guiSettings[comp]['numUsedRows']+=1
				if(self.guiSettings[comp]['numUsedRows'] > maxRow):
					maxRow = self.guiSettings[comp]['numUsedRows']
				if(self.guiSettings[comp]['col'] > maxColumn):
					maxColumn = self.guiSettings[comp]['col']
		self.quitButton = Button(text="QUIT", fg="red", command=self.destroy_app)
		self.quitButton.grid(column=maxColumn,row=maxRow)
		self.submitButton = Button(text="Send",fg='blue',command=self.send_command);
		self.submitButton.grid(column=0,row=maxRow)

	def send_command(self):
		changedComponents = self.get_changed_components()
		for comp in changedComponents:
			if comp in self.on_off_components:
				self.send_set_command(comp,self.dataLookup[comp][self.currentState[comp]])
			elif comp in self.position_components:
				#old value minus new value
				move = int(self.dataLookup[comp][self.guiData[comp]['var'].get()]) - int(self.dataLookup[comp][self.currentState[comp]])
				direction = "f"
				if(move < 0):
					direction = "b"
					move = move * -1
				print "Move: "+str(move)+" "+direction
				while(move > 0):
					self.send_move_command(comp,direction)
					move = move - 1
		self.set_current_state()
		self.save_current_state()
		
	def send_set_command(self,component,state):
		cmd = "set,"+component+","+state
		self.arduino.write(cmd)
		conf = self.arduino.read().lower().split(",",1)
		if(conf[0] == "received" and conf[1] == cmd):
			print "Set command received"
			conf = self.arduino.read().lower().split(",",1)
			if(conf[0] == "completed" and conf[1] == cmd):
				print "Set command completed"
				conf = self.arduino.read().lower()
				if(conf == "ready"):
					print "Arduino is ready to receive a new command"
					return 1
		return 0
	
	def send_move_command(self,component,dir):
		cmd = "move,"+component+","+dir
		self.arduino.write(cmd)
		self.arduino.read()
		conf = self.arduino.read().split(",")
		
		print conf
		pos = conf[3]
		
		self.guiData[component]['ops'][self.reverseDataLookup[component][pos]]['button'].select()
		self.currentState[component] = pos
		return pos
	
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
			return ""
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
