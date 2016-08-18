#include <SoftwareSerial.h>

// HW PINOUT
const int shutterPin = A0;       // shutter connected to pin A0
const int focusPin = A1;         // focus connected to pin A1
const int battPin = A2;          // Battery positive pin - to measure battery voltage
const int ledPin = 13;           // LED connected to digital pin 13 (fixed on board)
const int btPin = 2;             // Power to bluetooth module
const int txPin = 3;             // TX pin for software serial
const int rxPin = 4;             // RX pin for software serial

// USER CONFIGURABLE
int camDisplayOffT = 30000;      // Display goes off after 30 seconds / 30 000 miliseconds
int camSleepT = 1800;         // Camera goes to sleep after 30 minutes / 1 800 seconds
int MinDelay = 500;              // Minimum delay between consecutive shots in miliseconds
int MinHold = 50;                // Minimum shutter hold time - value under 50ms could not be registered by camera.
bool Debug = false;              // Debug and reply messages are always sent to both serial port, not only to source one. (TODO)

//
int sensorValue = 0;        // value read from ADC

SoftwareSerial softSerial(rxPin, txPin);

void setup() {
  pinMode(ledPin, OUTPUT);   //BUILD IN LED (D3)
  pinMode(btPin, OUTPUT);    //POWER ON BT (D2))
  digitalWrite(btPin, HIGH); //POWER ON BT (D2))

  // Open serial communications and wait for port to open:
  Serial.begin(9600);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }
  // Start software serial port
  softSerial.begin(9600);
  Serial.println("Hello");
}

void loop() {
  softSerial.listen();

  while (softSerial.available() > 0) {
    long StartDelay = softSerial.parseInt();
    int Count = softSerial.parseInt();
    long Delay = softSerial.parseInt();
    int Hold = softSerial.parseInt();

    if (softSerial.read() == ';') {
      DoCommand(StartDelay, Count, Delay, Hold);
    }
  }

  while (Serial.available() > 0) {
    long StartDelay = Serial.parseInt();
    int Count = Serial.parseInt();
    long Delay = Serial.parseInt();
    int Hold = Serial.parseInt();

    if (Serial.read() == ';') {
      DoCommand(StartDelay, Count, Delay, Hold);
    }
  }
}

void DoCommand(long StartDelay, int Count, long Delay, int Hold) {
  if (Debug) {
    Serial.print(StartDelay);
    Serial.println("ms");
    Serial.print(Count);
    Serial.println("shot(s) ");
    Serial.print(Delay);
    Serial.println("ms");
    Serial.print(Hold);
    Serial.println("ms");
  }

  if (Count == 0) {
    DoInternal(Delay, Hold);
  }
  // Single shot
  else if (Count == 1) {
    if (Hold < MinHold)
    {
      Hold = MinHold;
    }
    delay(StartDelay);
    if (camDisplayOffT >> 0) //If display sleep is enabled, wake display before shot.
      Focus();
    TakeShot(Hold);
    Serial.println("Single shot taken.");
    softSerial.println("Single shot taken.");
  }
  // Series of shot
  else if (Count >> 1)
  {
    if (Delay < MinDelay)
    {
      Delay = MinDelay;
    }
    if (Hold < MinHold)
    {
      Hold = MinHold;
    }
    int ShotsRemaining = Count;
    delay(StartDelay);
    if (camDisplayOffT >> 0) //If display sleep is enabled, wake display before first shot.
      Focus();
    while (ShotsRemaining > 0) {
      TakeShot(Hold);
      ShotsRemaining -= 1;
      Serial.print("Shots remaining: ");
      Serial.println(ShotsRemaining);
      softSerial.print("Shots remaining: ");
      softSerial.println(ShotsRemaining);

      // Wait only if we have to take another shot(s)
      if (ShotsRemaining > 0) {
        // If delay between shots is longer than camera display off timer (minus 1 second), wake display before shot
        if (Delay >= camDisplayOffT - 1000) {
          delay(Delay - 50); // wait time - focus time (50ms)
          Focus();
        }
        // delay is short enough, so just wait
        else {
          delay(Delay);
        }
      }
    }
  }
  Serial.println("Done");
}

//Internal commands for configuration, diagnostics, debugging and so.
void DoInternal(long Delay, int Hold) {
  // Return battery charge
  if (Delay == 0 && Hold == 0) {
    int charge = batteryCharge();
    Serial.print(" Battery charge is: ");
    Serial.println(charge);
  }
  // Turn Bluetooth ON
  if (Delay == 0 && Hold == 1) {
    digitalWrite(btPin, HIGH); //POWER ON BT (D2))
    Serial.println("Bluetooth powered ON");
  }
  // Turn Bluetooth OFF
  if (Delay == 0 && Hold == 2) {
    digitalWrite(btPin, LOW); //POWER OFF BT (D2))
    Serial.println("Bluetooth powered OFF");
  }
  // Return camera ON/OFF state
  if (Delay == 0 && Hold == 3) {
    if (isCameraOn()) {
      Serial.println("Camera is ON");
    }
    else
      Serial.println("Camera is OFF");
  }
  // Short focus. For example to wake camera.
  if (Delay == 0 && Hold == 4) {
    Focus();
  }
  // Debug ON/OFF
  if (Delay == 0 && Hold == 5) {
    if (Debug) {
      Debug = false;
      Serial.println("Debug disabled");
      softSerial.println("Debug disabled");
    }
    else
    {
      Debug = true;
      Serial.println("Debug enabled");
      softSerial.println("Debug enabled");
    }
  }
  // Timing test
  if (Delay == 0 && Hold == 6) {
    int c = 0;
    while (c < 60)
    {
      Serial.println(c);
      delay(1000);
      c++;
    }
  }
}

// Check if camera is turned ON
bool isCameraOn() {
  sensorValue = analogRead(focusPin);
  if (sensorValue >= 512)
  {
    if (Debug)
      Serial.println(sensorValue);
    return true;
  }
  else
  {
    if (Debug)
      Serial.println(sensorValue);
    return false;
  }
}

// battery charge TODO
int batteryCharge() {
  //5V = 1023
  int measurement;
  int value;
  while (measurement < 5) {
    sensorValue = analogRead(battPin);
    value += sensorValue;
    measurement++;
    delay(20);
  }
  int charge = (value / 5);
  return charge;
}

void TakeShot(int Hold) {
  pinMode(shutterPin, INPUT);
  digitalWrite(ledPin, HIGH);
  delay(Hold);
  pinMode(shutterPin, OUTPUT);
  digitalWrite(ledPin, LOW);
}

void Focus() {
  pinMode(focusPin, INPUT);
  delay(50);
  pinMode(focusPin, OUTPUT);
}

void ledOn() {
  digitalWrite(ledPin, HIGH);   // turn the LED on
}

void ledOff() {
  digitalWrite(ledPin, LOW);   // turn the LED off
}

