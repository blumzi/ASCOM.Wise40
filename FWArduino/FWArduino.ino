//instalation version July 18, 2010
//connections on the VGA 15 pin connector and on arduino board:
//                            Func              Arduino       VGA Conncetor
//                            -=-=-=-==-      =-=-=- -=-=   =-=-=-=-=--==-=-=-
//                            common            Pin 53                  Pin 12          
//                            Stepper P2        Pin 51                  Pin 5
//                            Stepper P1        Pin 47                  Pin 6
//
//  VGA connector scheme: (looking into face of connector)
//      \    1   2   3   4   5   6   7   8   /
//        \    9  10  11  12  13 14 15  /


#include <Stepper.h>
#include "Id12la.h"
#define STEPPER_STEPS 1600
#define NORMSPEED 37
// constants
#if _ARDUINO_ == UNO
#define STEPPER_PIN_1		47
#define STEPPER_PIN_2		51
#define STEPPER_COMMON_PIN	53
#define PHOTO_GATE_PIN		 8
#define PHOTO_HIGH_PIN		34
#define PHOTO_LOW_PIN		26
#elif _ARDUINO_ == MEGA
#define STEPPER_PIN_1		51
#define STEPPER_PIN_2		47
#define STEPPER_COMMON_PIN	53

#define PHOTO_GATE_PIN		30
#define PHOTO_HIGH_PIN		34
#define PHOTO_LOW_PIN		26
#endif
Stepper stepper(STEPPER_STEPS, STEPPER_PIN_1, STEPPER_PIN_2);
const int largeStep = 100;// manual steps - large step
const int mediumStep = 50; // manual steps - medium step
const int smallStep = 5; // manual steps - small step
const int searchSteps = 30; // number of small step when looking for close detent

							// dynamic vars
int moveSteps = 0;
boolean PhotoInterEn = false;
boolean terminate = false;
boolean smallMoves = false;
boolean stillLooking;
int direct;
int i;

#if _ARDUINO_ == UNO
Id12la tagReader(10, 11, 13);
#elif _ARDUINO_ == MEGA
Id12la tagReader(13);	// TBD: RFID reset pin on MEGA
#endif

void setup() {
	stepper.setSpeed(NORMSPEED);//define  the stepper speed

	Serial.begin(57600);	// opens serial port, sets data rate to 9600 bps
	Serial.flush();

	pinMode(PHOTO_HIGH_PIN, OUTPUT);
	pinMode(PHOTO_LOW_PIN, OUTPUT);
	pinMode(STEPPER_COMMON_PIN, OUTPUT);
	pinMode(PHOTO_GATE_PIN, INPUT_PULLUP);

	digitalWrite(PHOTO_HIGH_PIN, HIGH);
	digitalWrite(PHOTO_LOW_PIN, LOW);
	digitalWrite(STEPPER_COMMON_PIN, HIGH);

	Serial.println("filter wheel ready ...");
}

bool photoSlitInPosition() {
	return digitalRead(PHOTO_GATE_PIN) == LOW;
}

void loop() {
	int cmd = 0;// for incoming serial data

	terminate = 0;
	PhotoInterEn = false;
	moveSteps = 0;
	smallMoves = false;

	if (Serial.available() > 0) {

		switch (cmd = Serial.read()) {
		case 'T':
			if (photoSlitInPosition()) {
				char *tag = tagReader.readTag();
				if (tag != NULL)
					Serial.println(tag);
			}
			break;
		case '1':
			moveSteps = -1 * STEPPER_STEPS;
			break;
		case '2':
			moveSteps = -2 * STEPPER_STEPS;
			break;
		case '3':
			moveSteps = -3 * STEPPER_STEPS;
			break;
		case '4':
			moveSteps = -4 * STEPPER_STEPS;
			break;
		case 'A':
			moveSteps = STEPPER_STEPS;
			break;
		case 'B':
			moveSteps = 2 * STEPPER_STEPS;
			break;
		case 'C':
			moveSteps = 3 * STEPPER_STEPS;
			break;
		case 'a':
			moveSteps = -largeStep;
			smallMoves = true;
			break;
		case 'b':
			moveSteps = largeStep;
			smallMoves = true;
			break;
		case 'c':
			moveSteps = -mediumStep;
			smallMoves = true;
			break;
		case 'd':
			moveSteps = mediumStep;
			smallMoves = true;
			break;
		case 'e':
			moveSteps = -smallStep;
			smallMoves = true;
			break;
		case 'f':
			moveSteps = smallStep;
			smallMoves = true;
			break;
		case 'I':
			// look for detent on close surrounding
			stillLooking = true;
			direct = 1;
			i = 0;
			while (stillLooking && i < searchSteps) {
				stepper.step(direct*smallStep);
				i++;
				if (photoSlitInPosition())
					stillLooking = false;
			}

			i = 0;
			while (stillLooking && i < 2 * searchSteps) {
				direct = -1;
				stepper.step(direct*smallStep);
				i++;
				if (photoSlitInPosition())
					stillLooking = false;
			}
			delay(500);
			if (!photoSlitInPosition()) { // on some cases detent is reported on delay, so doing another check after the delay, and moving back 2 steps if needed
				stepper.step(-2 * direct*smallStep);
			}
			break;
		}

		if (cmd == 'W') { // operating detent check and report
			PhotoInterEn = true;
			delay(100);
		}
		stepper.step(moveSteps);
		if (moveSteps != 0 && smallMoves == false) {
			Serial.print("P");
			Serial.flush();
			terminate = true;
		}
		if (PhotoInterEn == true) {
			Serial.print(photoSlitInPosition() ? "D" : "d");
			terminate = true;
		}
		if (terminate == true) {
			Serial.print("R");
			delay(500);
			Serial.flush();
		}
	}
}