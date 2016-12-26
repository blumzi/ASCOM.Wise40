//instalation version July 18, 2010
//connections on the VGA 15 pin connector and on arduino board:
//                            Func                Arduino       VGA Conncetor
//                            -=-=-=-==-       =-=-=- -=-=   =-=-=-=-=--==-=-=-
//                            common               Pin 53                  Pin 12          
//                            Stepper P2        Pin 51                  Pin 5
//                            Stepper P1        Pin 47                  Pin 6
//
//  VGA connector scheme: (looking into face of connector)
//      \    1   2   3   4   5   6   7   8   /
//        \    9  10  11  12  13 14 15  /


#include <Stepper.h>
#define STEPS 1600
#define NORMSPEED 37
// constants
int P2=51; // stepper pin
int P1=47; // stepper pin
int common=53; // stepper pin
int gatePin=30; // PhotoInteruppter pin
int highPin=34; // Vdd for photo interupter
int lowPin=26; // Gnd for photo interupter
Stepper stepper1(STEPS, P2, P1);
int cmd = 0;// for incoming serial data
int fullmotorturn=1600;
int contstep1=100;// manual steps - large step
int contstep2=50; // manual steps - medium step
int contstep3=5; // manual steps - small step
int NUM_OF_LOOKING_MOVES=30; // number of small step when looking for close detent
// dynamic vars
int moveSteps=0;
boolean PhotoInterEn=false;
boolean terminate=false;
boolean smallMoves=false;
boolean stillLooking;
int direct;
int i;
	
void setup() {
        stepper1.setSpeed(NORMSPEED);//define  the stepper speed
  	Serial.begin(9600);	// opens serial port, sets data rate to 9600 bps
        Serial.flush();
        pinMode(highPin,OUTPUT);
        pinMode(lowPin,OUTPUT);
        pinMode(gatePin,INPUT);
        pinMode(common,OUTPUT);
        digitalWrite(highPin,HIGH);
        digitalWrite(lowPin,LOW);
        digitalWrite(common,HIGH);
        
}
void loop() {
        terminate=0;
        PhotoInterEn=false;
        moveSteps=0;
        smallMoves=false;
	if (Serial.available() > 0) {
		cmd = Serial.read();// read the incoming byte:
        
          if (cmd== '1') { moveSteps=-1*fullmotorturn;}
          if (cmd== '2') { moveSteps=-2*fullmotorturn;}
          if (cmd== '3') { moveSteps=-3*fullmotorturn;}
          if (cmd== '4') { moveSteps=-4*fullmotorturn;}
          if (cmd== 'A') { moveSteps=fullmotorturn;}
          if (cmd== 'B') { moveSteps=2*fullmotorturn;}
          if (cmd== 'C') { moveSteps=3*fullmotorturn;}
          if (cmd== 'a') { moveSteps=-contstep1; smallMoves=true;}
          if (cmd== 'b') { moveSteps=contstep1; smallMoves=true;}
          if (cmd== 'c') {  moveSteps=-contstep2; smallMoves=true;}
          if (cmd== 'd') {  moveSteps=contstep2; smallMoves=true;}
          if (cmd== 'e') { moveSteps=-contstep3; smallMoves=true;}
          if (cmd== 'f') {  moveSteps=contstep3; smallMoves=true;}
          if (cmd=='I') { // look for detent on close surrounding
                stillLooking=true;
                direct=1;
                i=0;
                while (stillLooking==true and i<NUM_OF_LOOKING_MOVES) {
                  stepper1.step(direct*contstep3);
                  i=i+1;
                   if (digitalRead(gatePin)==LOW) {stillLooking=false; }
                   }
                   i=0;
                  while (stillLooking==true and i<2*NUM_OF_LOOKING_MOVES) {
                  direct=-1;
                  stepper1.step(direct*contstep3);
                  i=i+1;
                   if (digitalRead(gatePin)==LOW) {stillLooking=false; }
                   }
                   delay(500);
                   if (digitalRead(gatePin)==HIGH) { // on some cases detent is reported on delay, so doing another check after the delay, and moving back 2 steps if needed
                     stepper1.step(-2*direct*contstep3);
                   }
          }              
          if (cmd=='W') { // operating detent check and report
            PhotoInterEn=true;
            delay(100);
            }
          stepper1.step(moveSteps);
          if (moveSteps!=0 and smallMoves==false) {
          Serial.print("P");
          Serial.flush();
          terminate=true;
          }
  if ( PhotoInterEn==true) {
          if (digitalRead(gatePin)==LOW) {
           Serial.print("D");
           terminate=true;
      }    else { // gatePin==HIGH (not in detent)
            Serial.print("d");
            Serial.print("R");
        }
  }
  if (terminate==true) {
        Serial.print("R");
        delay(500);
        Serial.flush();
  }
}
}

