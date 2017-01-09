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

//
// Glossary:
//  The following terms are used:
//
//	position:
//		- there are 8 positions on the filter wheel, distanced 200
//			steps (full motor revolution) one from the other.
//		- on the 8-filter wheel there are filters (and RFID tags) at every position, on
//			the 4-filter wheel only one in two positions has a filter (and RFID tag).
//		- at each position the optical detector gate-pin goes LOW
//
// tag:
//	- RFID tags are located on the filter wheel, to identify the filter positions.
//	- the RFID reader will attempt to read the tags ONLY when the optical slit is detected.
//	- an RFID tag has 10 HEX characters, e.g. "7F0007F75E".
//	- an empty RFID tag indicates the reader could not read a tag.  This can occur if:
//		- there is no tag, e.g. in odd positions on a 4-filter wheel
//		- the existing tag is faulty
//
// revolution: 
//		- 200 steps
//
//	speed:
//		- the speed at which the stepper will rotate (STEPPER_NORMAL_SPEED)
//
//	direction:
//		- the stepper direction: CW (positive number of steps) or CCW (negative number of steps)
//
//  Transport layer:
//	  packet format:[STX][PAYLOAD][CRC 2][ETX]
//		- STX - one byte, ASCII:2 (start-of-text)
//		- ETX - one byte, ASCII:3 (end-of-text)
//		- PAYLOAD - Even number of bytes (padded with NULL, if odd), ASCII printable characters ONLY
//		- CRC - two bytes
//
//  Data layer:	command[:argument]
//		- Commands may have optional arguments, separated by a colon (:)
//
//	Commands (always initiated by the PC):
//
//	1. Get position
//		command: get-position, argument: none
//		reply:   position:<tag> or position:no-tag
//		operation:
//			- If optical slot is NOT detected, search for nearest position (up to one motor revolution, first CW then CCW)
//			- When optical slot is detected, read RFID tag.
//			- If a <tag> was read, reply: position:<tag>
//			- If no tag could be read, reply: position:no-tag
//
//	2. Move CW or CCW
//		command: move:{CW|CCW}[n]
//		reply:   position:<tag> or position:no-tag
//		operation:
//			- The arduino will rotate the motor n (default: 1) full revolution(s), in the specified direction
//			- The arduino will perform exactly the same procedure as for the 'get-position' command
//

#include <Stepper.h>
#include "Id12la.h"
#include "Ascii.h"
#define STEPPER_STEPS_PER_WHEEL8	1600
#define STEPPER_STEPS_PER_REV		 200
#define STEPPER_NORMAL_SPEED		  37
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

#define CW(steps) (steps)		// clock-wise direction
#define CCW(steps) (-(steps))	// counter-clock-wise direction

Stepper stepper(STEPPER_STEPS_PER_REV, STEPPER_PIN_1, STEPPER_PIN_2);
const int largeStep  = 100; // manual steps - large step
const int mediumStep =  50; // manual steps - medium step
const int smallStep  =   5; // manual steps - small step
const int searchSteps = 30; // number of small step when looking for close detent

							// dynamic vars
int moveSteps = 0;
boolean PhotoInterEn = false;
boolean terminate = false;
boolean smallMoves = false;
boolean stillLooking;
int direct;
int i;
char inBuf[256], outBuf[256];

#if _ARDUINO_ == UNO
Id12la tagReader(10, 11, 13);
#elif _ARDUINO_ == MEGA
Id12la tagReader(13);	// TBD: RFID reset pin on MEGA
#endif

//
// reads a packet from the PC, on the Serial port
// blocks until a good packet is received (bad packets are silently discarded)
//
String readPacket() {
	char *p;

again:
	// clean the inBuf
	for (p = inBuf; (unsigned)(p - inBuf) < sizeof(inBuf); p++)
		*p = 0;

	// skip characters until we get a STX
	Serial.find(Ascii::STX);

	// store all characters up to ETX (or no more space in inBuf)
	for (p = inBuf; (unsigned)(p - inBuf) < sizeof(inBuf); p++)
		if ((*p = Serial.read()) == Ascii::ETX)
			break;

	if (*p != Ascii::ETX)
		goto again;	// reached limit of inBuf without getting an ETX, start all over

	// TODO: checksum

	*p = 0;
	return String(inBuf);
}

//
// Sends a packet to the PC
//
void sendPacket(String payload) {
	Serial.write(Ascii::STX);
	Serial.print(payload);
	Serial.write(Ascii::ETX);
}

//
// Search for the optical slit.
//  maxSteps:  max number of steps to move in each direction
//  stepsPerMove: how much to move each time
//
bool searchForOpticalSlit(int maxSteps = STEPPER_STEPS_PER_REV, int stepsPerMove = 1) {
	int count;

	for (count = 1; count < maxSteps; count++) {
		if (photoSlitDetected())
			return true;
		stepper.step(CW(stepsPerMove));
	}
	stepper.step(CCW(maxSteps));	// go back
	for (count = 1; count < maxSteps; count++) {
		if (photoSlitDetected())
			return true;
		stepper.step(CCW(stepsPerMove));
	}
	return false;
}

void setup() {
	stepper.setSpeed(STEPPER_NORMAL_SPEED);//define  the stepper speed

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
	
	searchForOpticalSlit();
}

//
// The photo detector gate-pin drops to ground when
//  it finds the positioning slit, and according to Ezra it
//  is accurate, i.e. drops to ground if and only if it detected
//  the slit.
//
bool photoSlitDetected() {
	return digitalRead(PHOTO_GATE_PIN) == LOW;
}

String doGetPosition() {
	String tag;

	searchForOpticalSlit();
	if ((tag = tagReader.readTag()) == NULL)
		tag = String("no-tag");

	return String("position:") + tag;
}

String doMove(int nPos) {
	stepper.step(nPos * STEPPER_STEPS_PER_REV);
	return doGetPosition();
}

void loop() {
	String command, reply;

	command = readPacket();

	if (command.startsWith("get-position", 0)) {
		reply = doGetPosition();
	} else if (command.startsWith("moveCW:", 0)) {
		int nPos = command.substring(strlen("moveCW:")).toInt();
		if (nPos < 1 || nPos > 7)
			reply = "bad-param";
		else
			reply = doMove(nPos);
	}
	else if (command.startsWith("moveCCW:", 0)) {
		int nPos = command.substring(strlen("moveCCW:")).toInt();
		if (nPos < 1 || nPos > 7)
			reply = "bad-param";
		else
			reply = doMove(-nPos);
	}
	if (reply.length() > 0)
		sendPacket(reply);
}