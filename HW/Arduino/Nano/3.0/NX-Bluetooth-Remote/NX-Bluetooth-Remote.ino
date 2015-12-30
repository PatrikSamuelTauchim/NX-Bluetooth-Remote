#include <SoftwareSerial.h>

// Pins
int shutterPin = A0;                 // shutter connected to pin A0
int focusPin = A1;                 // focus connected to pin A1
int ledPin = 13;                 // LED connected to digital pin 13
int btPin = 2;                 // Power to bluetooth module
// software serial: RX = digital pin 4, TX = digital pin 3
SoftwareSerial softSerial(4, 3);

int sensorValue = 0;        // value read from ADC

int MinDelay = 500; // Minimum delay between consecutive shots in miliseconds
int MinHold = 50; // Minimum shutter hold time

void setup() {
  pinMode(ledPin, OUTPUT);//BUILD IN LED (D3)
  pinMode(btPin, OUTPUT);//POWER ON BT (D2))
  digitalWrite(btPin, HIGH);//POWER ON BT (D2))

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
    int Count = softSerial.parseInt();
    int Delay = softSerial.parseInt();
    int Hold = softSerial.parseInt();

    if (softSerial.read() == ';') {
      DoCommand(Count, Delay, Hold);
    }
  }

  while (Serial.available() > 0) {
    int Count = Serial.parseInt();
    int Delay = Serial.parseInt();
    int Hold = Serial.parseInt();

    if (Serial.read() == ';') {
      DoCommand(Count, Delay, Hold);
    }
  }
}

void DoCommand(int Count, int Delay, int Hold) {
  Serial.print("Got command to take ");
  Serial.print(Count);
  Serial.print(" shots with delay of ");
  Serial.print(Delay);
  Serial.print("ms. I will hold shutter for ");
  Serial.print(Hold);
  Serial.println(" ms between them.");
  int ShotsRemaining = Count;
  while (ShotsRemaining > 0) {
    ledOn();
    if (Delay < MinDelay)
    {
      Delay = MinDelay;
      Serial.println("Setting minimum delay to " + MinDelay);
    }
    if (Hold < MinHold)
    {
      Hold = MinHold;
      Serial.println("Setting minimum shutter hold time to " + MinHold);
    }
    TakeShot(Hold);
    ShotsRemaining -= 1;
    Serial.print("Shots remaining: ");
    Serial.println(ShotsRemaining);
    if (ShotsRemaining > 0) {
      delay(Delay);
    }
    ledOff();
  }
  Serial.print("Finished ");
  Serial.print(Count);
  Serial.print(" shots with delay ");
  Serial.println(Delay);
}

// Check if camera is turned ON
bool isCameraOn() {
  sensorValue = analogRead(shutterPin);
  if (sensorValue >= 0)
  {
    Serial.print("A0 = ");
    Serial.println(sensorValue);
    return true;
  }
  else
  {
    Serial.print("A0 = ");
    Serial.println(sensorValue);
    return false;
  }
}

void TakeShot(int Hold) {
  pinMode(shutterPin, INPUT);
  digitalWrite(ledPin, HIGH);
  delay(Hold);
  pinMode(shutterPin, OUTPUT);
  digitalWrite(ledPin, LOW);
}

void ledOn() {
  digitalWrite(ledPin, HIGH);   // turn the LED on
}

void ledOff() {
  digitalWrite(ledPin, LOW);   // turn the LED off
}

