String inputString = "";
boolean stringComplete = false;

const int APERTURE_WAIT = 1000;
const int COLLIMATOR_WAIT = 1525;
const int UPPER_GRISM_WAIT = 900;
const int LOWER_GRISM_WAIT = 1000;
unsigned long wait = 0;

const int APERTURE_PULSE = 22;
const int APERTURE_POSITION_COARSE = A0;
const int APERTURE_POSITION_FINE = A4;
const int APERTURE_DIRECTION = 24;
const int UPPER_GRISM_PULSE = 26;
const int UPPER_GRISM_POSITION_COARSE = A1;
const int UPPER_GRISM_POSITION_FINE = A5;
const int UPPER_GRISM_DIRECTION = 28;
const int LOWER_GRISM_PULSE = 30;
const int LOWER_GRISM_POSITION_COARSE = A3;
const int LOWER_GRISM_POSITION_FINE = A6;
const int LOWER_GRISM_DIRECTION = 32;
const int COLLIMATOR_PULSE = 34;
const int COLLIMATOR_POSITION = A2;
const int COLLIMATOR_DIRECTION = 36;
const int SHUTTER = 37;
const int HALOGEN_1 = 38;
const int MIRROR = 39;
const int FE_A = 40;
const int STEADY_COMPONENTS[] = {SHUTTER, HALOGEN_1, FE_A, MIRROR};
const int NUM_STEADY_COMPONENTS = 4;
const int UNDULATING_COMPONENTS[] = {APERTURE_PULSE,APERTURE_DIRECTION,UPPER_GRISM_PULSE,UPPER_GRISM_DIRECTION,LOWER_GRISM_PULSE,LOWER_GRISM_DIRECTION,COLLIMATOR_PULSE,COLLIMATOR_DIRECTION};
const int NUM_UNDULATING_COMPONENTS = 8;
const int INPUT_COMPONENTS[] = {APERTURE_POSITION_COARSE, UPPER_GRISM_POSITION_COARSE, LOWER_GRISM_POSITION_COARSE, COLLIMATOR_POSITION, APERTURE_POSITION_FINE, UPPER_GRISM_POSITION_FINE, LOWER_GRISM_POSITION_FINE};
const int NUM_INPUT_COMPONENTS = 7;

int CRIT_COUNT_UPPER_GRISM[] = {925,1240,1449,1643,1843,2102,2317,2516,2698,2907};
//int CRIT_COUNT_APERTURE[] = {1379,1773,2061,2428,2688,3093,3460,3703,4187,4570}; //original of matt
int CRIT_COUNT_APERTURE[] = {1192,1525,1805,2080,2357,2680,3010,3300,3650,3980};

int CRIT_COUNT_LOWER_GRISM[] = {835,944,1211,1399,1698,1956,2184,2387,2548,2871};
int LAST_MOVED_UPPER_GRISM = 0;
int LAST_MOVED_LOWER_GRISM = 0;
int LAST_MOVED_APERTURE = 0;
int LAST_MOVED = 0;
int MAX_CRIT_COUNT = 10;
String component = "";
String command = "";
boolean dir = true; //true = forward, false = backward
int interruptPin = 0;
int fineInterruptPin = 0;
int stepsToDo = 0;
int stepsDone = 0;
int directionPin = 0;
int pulsePin = 0;
int steadyPin = 0;
boolean hasPassedInterrupt = false;

boolean inProcess = false;
unsigned long currentMicros = 0;
unsigned long lastOpMicros = 0;
boolean lastOp = 0;
unsigned long currentRep = 0;
boolean crit1,crit2,crit3,crit4;
long critCounter,backCounter;


void setup()
{
  Serial.begin(9600);
  setAllPinModes();
  Serial.println("SETUP COMPLETE");
}
void loop()
{
  currentMicros = micros();
  if(stringComplete){
	command = "";
	String args = "";
	parseCommand(inputString,command,args);
	stringComplete = false;
	inputString = "";
	command.toLowerCase();
	if(command == "move")
	  moveInit(args);
	else if(command == "set")
	  setPin(args);
	else if(command == "q")
	  endProcess();
	else if(command == "move_steps")
	  moveStepsInit(args);
	/*if(component == "aperture"){
	  Serial.println("DON'T DO THAT");
	  endProcess();
	}*/
  }
  else if(inProcess){
	runPaw();
  }
}
void runPaw(){
  boolean updateAtEnd = false;
  if(currentMicros - lastOpMicros >= wait){
	if(lastOp)
	  digitalWrite(pulsePin,HIGH);
	else
	  digitalWrite(pulsePin,LOW);
	lastOp = !lastOp;
	lastOpMicros = micros();
	currentRep++;
  }
  boolean currentInterruptState = analogPinState(interruptPin);
  
// crit1 - move back to zero (reverse last moved
// crit2 - moving
  if (command == "move_steps"){
    if(component == "collimator"){
      if(!crit1){
        if(currentInterruptState == 1){
		critCounter++;
	}
        else{
        	if(critCounter >= 100){
		  //Serial.println("Zeroed: "+String(component));
		  crit1 = true;
		}
	  }
      }
      else if (!crit2){
        //Serial.println(stepsToDo);
        if (stepsToDo == 0){
           crit2 = true;
          //Serial.println("Moved to position");
           inProcess = false;
           Serial.println("finished,move_steps,collimator");
        }
        stepsToDo -= 1;
      }
    }
    else if (component != "collimator"){
      if (!crit1){
        if (abs(LAST_MOVED) == currentRep || LAST_MOVED == 0){
          crit1 = true;
        
          if (stepsToDo <= 0){
            dir = false;
            digitalWrite(directionPin, LOW);
          }
          else{
            dir = true;
            digitalWrite(directionPin, HIGH);
          }
          currentRep = 0;
        }
      }
      else if (!crit2){
        if (currentRep == abs(stepsToDo)){
          crit2 = true;
          inProcess = 0;
          if (component == "upper_grism")
            LAST_MOVED_UPPER_GRISM = stepsToDo;
          if (component == "lower_grism")
            LAST_MOVED_LOWER_GRISM = stepsToDo;
          if (component == "aperture")
            LAST_MOVED_APERTURE = stepsToDo;
          Serial.println("finished,move_steps,"+component);
        }
      }
    }
  }
  else if (command == "move"){
	if(!crit1){
	  if(currentInterruptState == 1){
		//Serial.println("IN 1, crit: "+String(critCounter));
		critCounter++;
	  }
	  else{
		//Serial.println("1:"+String(critCounter));
		if(critCounter>=5000){
		 // Serial.println("Finished 1 critCount:"+String(critCounter));
		  crit1 = true;
		}
		critCounter = 0;
	  }
	}
	else{
	  if(!crit2){
		if(currentInterruptState == 0){
		 // Serial.println("IN 2, crit: "+String(critCounter));
		  critCounter++;
		}
		else{
	   //   Serial.println("Finished 2 critCount:"+String(critCounter));
		  crit2 = true;
		  critCounter = 0;
		}
	  }
	  else{
		if(!crit3){
		  if(currentInterruptState == 1){
			critCounter++;

		  }
		  else{
			if(!dir){
			  inProcess = 0;
			  reportPosition(critCounter);
			  critCounter = 0;
			}
			else{
			  digitalWrite(directionPin,LOW);
			  crit3 = true;
			}
		  }
		}
		else{
			  backCounter ++;
			  if (backCounter >= 500 && currentInterruptState == 0){
				inProcess = 0;
				reportPosition(critCounter);
				//Serial.println(critCounter);
				//Serial.println(command);
				critCounter = 0;
				//Serial.println(backCounter);
			  }

		}
	  }
	}
  }
}
void reportPosition(int crit){
  int *critList;
  if(component == "lower_grism")
	critList = &CRIT_COUNT_LOWER_GRISM[0];
  else if(component == "upper_grism")
	critList = &CRIT_COUNT_UPPER_GRISM[0];
  else if(component == "aperture")
	critList = &CRIT_COUNT_APERTURE[0];
  int closestPos = 0;
  int closestPosDist = -1;
  for(int i = 0; i < MAX_CRIT_COUNT; i++){
	int currentDist = abs(crit-critList[i]);
 //   Serial.println("crit: "+String(crit)+" currentDist:"+String(currentDist)+" closestPosDist:"+String(closestPosDist));
	if(currentDist < closestPosDist || closestPosDist == -1){
	  closestPosDist = currentDist;
	  closestPos = i;
	}
  }
  closestPos++;
  Serial.println("finished,move,"+String(component)+","+String(closestPos)+","+String(crit));
}
boolean moveStepsInit(String args){
  crit1 = false;
  crit2 = false;
  critCounter = 0;
  currentRep = 0;
  
  boolean parsePass = moveStepsParse(args);
  boolean gotSettings = getComponentSettings(component);
  if(!parsePass || !gotSettings){
	Serial.println("Failed setup");
	return false;
  }
  if(LAST_MOVED >= 0){
    dir = false; //backward
    digitalWrite(directionPin,LOW);
  }
  else{
    dir = true;
    digitalWrite(directionPin,HIGH);
  }
  
  lastOpMicros = currentMicros;
  lastOp = 0;
  inProcess = true;
  
  Serial.println("received,move_steps,"+component);
  return true;
}
boolean moveInit(String args){    
  crit1 = false;
  crit2 = false;
  crit3 = false;
  crit4 = false;
  critCounter = 0;
  backCounter = 0;
  
  boolean parsePass = moveParse(args);
  boolean gotSettings = getComponentSettings(component);
  if(!parsePass || !gotSettings){
	Serial.println("Failed setup");
	return false;
  }

 // String output = "Move COMPONENT:"+component+" WAIT:"+String(wait)+" STEPS:"+String(stepsToDo)+" INTERRUPT_PIN:"+String(interruptPin)+" DIRECTION:"+String(dir)+" DIRECTION_PIN:"+String(directionPin);
  String output = "received,move,"+component;
  inProcess = true;
  currentRep = 0;
  stepsDone = 0;
  Serial.println(output);
	
  if(dir)
	digitalWrite(directionPin,HIGH);
  else
	digitalWrite(directionPin,LOW);
 
  lastOpMicros = currentMicros;
  lastOp = 0;
  return true;
}

boolean analogPinState(int mappedPin){
  if(mappedPin == 0 || mappedPin == -1)
	return 0;
  int initialAnalogState = analogRead(mappedPin);
  //Serial.println(initialAnalogState);
  if(initialAnalogState < 700)
	return 0;
  else
	return 1;

}
boolean setPin(String args){
  //Serial.println("SET");   
  for(int i = 0; i <=1; i++){
	if(args == "")
	  return false;
	int commaIndex = args.indexOf(',');
	if(i==0){
	  component = args.substring(0,commaIndex);
	  component.toLowerCase();
	  args = args.substring(commaIndex+1,args.length());
	  //Serial.println("COMPONENT: "+component);
	}
	else if(i==1){
	  int given = args.substring(0,commaIndex).toInt();
	  int changeTo = LOW;
	  if(given == 1)
		changeTo = HIGH;
	  //Serial.println("CHANGE TO: "+String(changeTo));
	  int steadyPin;
	  if(component == "shutter")
		steadyPin = SHUTTER;
	  else if(component == "halogen_1")
		steadyPin = HALOGEN_1;
	  else if(component == "fe_a")
		steadyPin = FE_A;
	  else if(component == "mirror")
		steadyPin = MIRROR;
	  Serial.println("set,"+component+","+String(changeTo));
	  digitalWrite(steadyPin,changeTo);
	}
  }
}           
boolean moveStepsParse(String args)
{
  for(int i = 0; i <= 1; i++){
	if(args == "")
	  return false;
	int commaIndex = args.indexOf(',');
	if(i==0){
	  component = args.substring(0,commaIndex);
	  component.toLowerCase();
	  args = args.substring(commaIndex+1,args.length());
	}
	else if(i==1){
	  stepsToDo = args.substring(0,commaIndex).toInt();
	  return true;
	}
  }
}
boolean moveParse(String args)
{
  for(int i = 0; i <= 2; i++){
	if(args == "")
	  return false;
	int commaIndex = args.indexOf(',');
	if(i==0){
	  component = args.substring(0,commaIndex);
	  component.toLowerCase();
	  args = args.substring(commaIndex+1,args.length());
	}
	else if(i==1){
	  String dirString = args.substring(0,commaIndex);
	  dirString.toLowerCase();
	  args = args.substring(commaIndex+1,args.length());
	  
	  if(dirString == "b" || dirString == "backward" || dirString == "back")
		dir = false;
	  else if(dirString == "f" || dirString == "forward")
		dir = true;
	  else
		return false;
	}
	else if (i==2){
	  stepsToDo = args.substring(0,commaIndex).toInt();
	  return true;
	}
  }
  return false;
}
void parseCommand(String input,String &cmd, String &args)
{
  int commaIndex = input.indexOf(',');
  cmd = input.substring(0,commaIndex);
  args = input.substring(commaIndex+1,input.length());
}
void setAllPinModes(){
  for(int i = 0; i < NUM_INPUT_COMPONENTS; i++)
	pinMode(INPUT_COMPONENTS[i],INPUT);
  for(int i = 0; i < NUM_UNDULATING_COMPONENTS; i++)
	pinMode(UNDULATING_COMPONENTS[i],OUTPUT);
  for(int i = 0; i < NUM_STEADY_COMPONENTS; i++)
	pinMode(STEADY_COMPONENTS[i],OUTPUT);
}

void serialEvent(){
  while(Serial.available() && stringComplete != true){
	char inChar = (char)Serial.read();
	if(inChar == '\n'){
	  stringComplete = true;
	}
	else{
	  inputString += inChar;
	}
  }
}
void endProcess(){
  Serial.println("Manually ended process");
  digitalWrite(directionPin,LOW);
  inProcess = 0;
}

boolean getComponentSettings(String comp)
{
	  if(comp == "collimator"){
		interruptPin = COLLIMATOR_POSITION;
		fineInterruptPin = 0;
		wait = COLLIMATOR_WAIT;
		directionPin = COLLIMATOR_DIRECTION;
		pulsePin = COLLIMATOR_PULSE;
		return 1;
	  }
	  if(comp == "aperture"){
		interruptPin = APERTURE_POSITION_COARSE;
		fineInterruptPin = APERTURE_POSITION_FINE;
		wait = APERTURE_WAIT;
		directionPin = APERTURE_DIRECTION;
		pulsePin = APERTURE_PULSE;
                LAST_MOVED = LAST_MOVED_APERTURE;
                LAST_MOVED_APERTURE = 0;
		return 1;
	  }
	  if(comp == "upper_grism"){
		interruptPin = UPPER_GRISM_POSITION_COARSE;
		fineInterruptPin = UPPER_GRISM_POSITION_FINE;
		wait = UPPER_GRISM_WAIT;
		directionPin = UPPER_GRISM_DIRECTION;
		pulsePin = UPPER_GRISM_PULSE;
                LAST_MOVED = LAST_MOVED_UPPER_GRISM;
                LAST_MOVED_UPPER_GRISM = 0;
                
		return 1;
	  }
	  if(comp == "lower_grism"){
		interruptPin = LOWER_GRISM_POSITION_COARSE;
		fineInterruptPin = LOWER_GRISM_POSITION_FINE;
		wait = LOWER_GRISM_WAIT;
		directionPin = LOWER_GRISM_DIRECTION;
		pulsePin = LOWER_GRISM_PULSE;
                LAST_MOVED = LAST_MOVED_LOWER_GRISM;
                LAST_MOVED_LOWER_GRISM = 0;
		return 1;
	  }
	  return 0;
}

