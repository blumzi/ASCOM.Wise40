from Tkinter import *
import serial
import time
import sys
import csv
import os
comPort = "COM3"
bitRate = 9600

#guiData looks like this
#							guiData
#							[component] - the components of FOSC
#'col'-Column in GUI	'set'-Holds component specific settings 'ordered_settings'-Order settings are displayed in GUI 	'real_value'-known value IRL		'button_value'- GUI button value		'gui_offset_value'		'entry_value' gui entry object
#						 	|
#					 	[specific position] - like position_1,on
#						/					|											\						
#	'numeric_pos'-value for FOSC		'button'-GUI button					'offset'-calibration value    
#										
class App:
	def __init__(self,master):
		self.frame = Frame(master)
		self.on_off_components = ['shutter','halogen1','halogen2','fe_a','mirror']
		self.position_components = ['collimator','aperture','upper_grism','lower_grism']
		#self.position_components = ['aperture','upper_grism','lower_grism']
		self.components = self.on_off_components + self.position_components
		
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
			self.guiData[comp]['ordered_settings'] = []
			self.guiData[comp]['button_value'] = StringVar()
			self.guiData[comp]['entry_value'] = StringVar()

		self.currentState = {}
		
		self.create_gui_from_file()

	def create_gui_from_file(self):
		with open('FOSC_GUI_SETTINGS.csv','r') as settingsFile:
			settingsReader = csv.reader(settingsFile,skipinitialspace=True)
			for line in settingsReader:
				if line[0][0]=='#':
					continue
				self.guiData[line[0]]['set'][line[1]] = {'numeric_pos':line[2],'offset':line[3]}
				self.guiData[line[0]]['ordered_settings'].append(line[1]) #doing it like this avoids 
								#any annoying sorting errors in displaying numbers over two digits, displays
								#the settings in the order in which they are defined in FOSC_GUI_SETTINGS.csv

		self.create_gui()

	def create_gui(self):
		maxRow = 0
		maxColumn = 0
		for comp in self.components:
			Label(text=comp.upper()).grid(row=self.guiData[comp]['row'],column=self.guiData[comp]['col'])
			self.guiData[comp]['row']+=1
			for setting in self.guiData[comp]['ordered_settings']:
				self.guiData[comp]['set'][setting]['button'] = Radiobutton(text=setting,variable=self.guiData[comp]['button_value'],value=self.guiData[comp]['set'][setting]['numeric_pos'])
				self.guiData[comp]['set'][setting]['button'].grid(row=(self.guiData[comp]['row']),column=self.guiData[comp]['col'])
				self.guiData[comp]['row']+=1
			self.guiData[comp]['entry'] = Entry(textvariable=self.guiData[comp]['entry_value'])
			self.guiData[comp]['entry'].grid(row = self.guiData[comp]['row'], column = self.guiData[comp]['col'])
			self.guiData[comp]['entry'].config(width = 6)
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
		with open('FOSC_LAST_STATE_DEFAULT.csv') as settingsFile:
			settingsReader = csv.reader(settingsFile,skipinitialspace=True)
			for line in settingsReader:
				if(line[0][0]!='#'):
					print line
					self.guiData[line[0]]['set'][line[1]]['button'].select()
					self.guiData[line[0]]['set'][line[1]]['button'].config(fg="red")
					self.currentState[line[0]] = line[1]
	def save_current_state(self):
		with open('TEMP_LAST_STATE','w') as saveFile:
			saveFile.write("#TYPE,VALUE#")
			for comp in self.components:
				saveFile.write("\n") 	
				toWrite = comp + "," + self.currentState[comp]
				print toWrite
				saveFile.write(toWrite)
		saveFile.close()
		os.remove('FOSC_LAST_STATE.csv')
		os.rename('TEMP_LAST_STATE','FOSC_LAST_STATE.csv')

	def destroy_app(self):
		#self.set_current_state()
		self.save_current_state()
		self.frame.quit()
	def send_command(self):
		#self.set_current_state()
		self.save_current_state()	
root = Tk()

app = App(root)

root.mainloop()
root.destroy()
