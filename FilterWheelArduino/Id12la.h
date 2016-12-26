// determine board type
#if defined(__AVR_ATmega328P__) || defined(__AVR_ATmega168__)
# define _ARDUINO_ UNO
#elif defined(__AVR_ATmega32U4__) || defined(__AVR_ATmega16U4__)
# define _ARDUINO_ MICRO
#elif defined(__AVR_ATmega1280__) || defined(__AVR_ATmega2560__)
# define _ARDUINO_ MEGA
#else
# Cannot detect the ARDUINO board type
#endif

#if _ARDUINO_ == UNO
#include <SoftwareSerial.h>
#endif

#define DEBUG

#ifdef DEBUG
# define debug(x) Serial.print(x)
# define debugln(x) Serial.println(x)
#else
# define debug(x)
# define debugln(x)
#endif

#define BINARY(p)  (((ascii2bin[*(p)] & 0xf) << 4) | (ascii2bin[*((p)+1)]))

class Id12la {
  private:
    static const int STX = 2;
    static const int ETX = 3;
    static const int NL = 10;
    static const int CR = 13;

    static const int TagPayloadBytes = 10;
    static const int TagAllBytes = TagPayloadBytes + 7; // [STX][Payload(10)][CHK(2)][NL][CR][ETX][NULL]

    uint8_t resetPin;
    uint8_t ascii2bin['F'];

#if _ARDUINO_ == UNO
    uint8_t rxPin, txPin;
    SoftwareSerial *reader;
#endif
    uint8_t buf[TagAllBytes];
    uint8_t tag[TagPayloadBytes + 1];
    int nbytes;

    void initAscii() {
      ascii2bin['0'] = 0x0; ascii2bin['1'] = 0x1; ascii2bin['2'] = 0x2; ascii2bin['3'] = 0x3;
      ascii2bin['4'] = 0x4; ascii2bin['5'] = 0x5; ascii2bin['6'] = 0x6; ascii2bin['7'] = 0x7;
      ascii2bin['8'] = 0x8; ascii2bin['9'] = 0x9; ascii2bin['A'] = 0xa; ascii2bin['B'] = 0xb;
      ascii2bin['C'] = 0xc; ascii2bin['D'] = 0xd; ascii2bin['E'] = 0xe; ascii2bin['F'] = 0xf;
    }

#if _ARDUINO_ == UNO
  private:
    void init(uint8_t rxPin, uint8_t txPin, uint8_t resetPin) {
      this->rxPin = rxPin;
      this->txPin = txPin;
      this->resetPin = resetPin;

      pinMode(this->resetPin, OUTPUT);
      digitalWrite(this->resetPin, LOW);
      initAscii();
    }

  public:
    Id12la(uint8_t rxPin, uint8_t txPin, uint8_t resetPin) {
      reader = new SoftwareSerial(rxPin, txPin);
      reader->begin(9600);
      while (!reader)
        ;
      this->rxPin = rxPin;
      this->txPin = txPin;
      init(rxPin, txPin, resetPin);
    }
#elif _ARDUINO_ == MEGA
    //
    // On the MEGA we use Serial1 (rx=19, tx=18) to talk
    //  to the RFID reader
    //
  private:
    void init(uint8_t resetPin) {
      this->resetPin = resetPin;

      pinMode(this->resetPin, OUTPUT);
      digitalWrite(this->resetPin, LOW);
      initAscii();
    }

  public:
    Id12la(uint8_t resetPin) {
      Serial1.begin(9600);
      while (Serial1)
        ;
      init(resetPin);
    }
#endif

  private:
    void resetReader() {
      digitalWrite(this->resetPin, LOW);
      digitalWrite(this->resetPin, HIGH);
      delay(150);
    }

    void clearBufs() {
      uint8_t *p;

      for (p = buf; (unsigned int) (p - buf) < sizeof(buf); p++)
        *p = 0;
      for (p = tag; (unsigned int) (p - tag) < sizeof(tag); p++)
        *p = 0;
    }

    bool dataArrivedSafely() {
      int computedSum, sentSum;
      uint8_t *p;

      if (nbytes != TagAllBytes)
        return false;

      for (p = buf; (unsigned int) (p - buf) < sizeof(buf); p++) {
        //debug("buf["); debug(p - buf); debug("] = "); Serial.print(*p, HEX); debug(", "); Serial.println(*p, BIN);
      }

      if (buf[0] != STX || buf[13] != CR || buf[14] != NL || buf[15] != ETX)
        return false;

      for (computedSum = 0, p = buf + 1; p - (buf + 1) < TagPayloadBytes; p += 2)
        computedSum ^= BINARY(p);
      sentSum = BINARY(p);

      if (computedSum != sentSum)
        return false;
      return true;
    }

    int availableBytes() {
      return
#if _ARDUINO_ == UNO
        reader->available();
#elif _ARDUINO_ == MEGA
        Serial1.available();
#endif
    }

    uint8_t readByte() {
      uint8_t b =
#if _ARDUINO_ == UNO
        reader->read();
#elif _ARDUINO_ == MEGA
        Serial1.read();
#endif
      //debug("byte: "); Serial.print(b, HEX); debug(", "); Serial.println(b, BIN);
      return b;
    }

  public:
    char* readTag() {
      int i;

      while (availableBytes())                // discard any pending bytes from SerialRFID
        readByte();
      clearBufs();                            // clear read buffer and tag areas
      resetReader();                          // force reader to take a reading
      delay(250);                             // wait for reader to produce all the bytes

      nbytes = availableBytes();
      //debug("nbytes: "); debugln(nbytes);
      if (nbytes == 0)
        return NULL;

      readByte();                             // when getting out-of-reset, we get a FF byte

      for (i = 0; i < nbytes - 1; i++)
        buf[i] = readByte();
      if (! dataArrivedSafely())
        return NULL;

      for (i = 0; i < TagPayloadBytes; i++)   // extract payload
        this->tag[i] = this->buf[i + 1];
      this->tag[i] = 0;                       // NULL-terminate tag

      //debug("readTag: tag: ");
      //debugln((char*)tag);
      return (char *)tag;
    }
};