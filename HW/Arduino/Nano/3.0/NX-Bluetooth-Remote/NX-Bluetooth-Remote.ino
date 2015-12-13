// These constants won't change.  They're used to give names
// to the pins used:

int shutterPin = A0;                 // shutter connected to pin A0
int focusPin = A1;                 // focus connected to pin A1
int ledPin = 13;                 // LED connected to digital pin 13
int btPin = 2;                 // Power to bluetooth module

int sensorValue = 0;        // value read from ADC

void setup() {
  // initialize serial communications at 9600 bps:
  Serial.begin(9600);
  pinMode(ledPin, OUTPUT);//BUILD IN LED (D3)
  pinMode(btPin, OUTPUT);//POWER ON BT (D2))
  digitalWrite(btPin, HIGH);//POWER ON BT (D2))
  Serial.println("Start");
}

void loop() {
  /*if (isCameraOn)//TODO
    Serial.println("Camera is ON");
    else
    Serial.println("Camera is OFF");
    delay(10);*/

  while (Serial.available() > 0) {
    Serial.println("Serial.available");

    // look for the next valid integer in the incoming serial stream:
    int Count = Serial.parseInt();
    //Serial.println(Count);
    // do it again:
    int Delay = Serial.parseInt();
    //Serial.println(Delay);

    // look for the newline. That's the end of your
    // sentence:
    if (Serial.read() == ';') {
      Serial.print("Number of shots: ");
      Serial.print(Count);
      Serial.print(" Delay between shots: ");
      Serial.println(Delay);
      DoCommand(Count, Delay);
    }
  }
}

void DoCommand(int Count, int Delay) {
  int ShotsRemaining = Count;
  while (ShotsRemaining > 0) {
    ledOn();
    TakeShot();
    ShotsRemaining -= 1;
    Serial.print("Shots remaining: ");
    Serial.println(ShotsRemaining);
    if (ShotsRemaining > 0) {
      if (Delay < 100)//minimum delay
        Delay=100;
      else
        delay(Delay);
    }
    ledOff();
  }
  Serial.print("Finished ");
  Serial.print(Count);
  Serial.print(" shots with delay ");
  Serial.println(Delay);
}

bool isCameraOn() {
  sensorValue = analogRead(shutterPin);
  if (sensorValue >= 0)
  {
    digitalWrite(ledPin, HIGH);   // turn the LED on (HIGH is the voltage level)
    Serial.print("A0 = ");
    Serial.println(sensorValue);
    return true;
  }
  else
  {
    digitalWrite(ledPin, LOW);    // turn the LED off by making the voltage LOW
    Serial.print("A0 = ");
    Serial.println(sensorValue);
    return false;
  }
}
void TakeShot() {
  pinMode(shutterPin, INPUT);
  digitalWrite(ledPin, HIGH);
  delay(100);
  pinMode(shutterPin, OUTPUT);
  digitalWrite(ledPin, LOW);
}

void ledOn() {
  digitalWrite(ledPin, HIGH);   // turn the LED on
}

void ledOff() {
  digitalWrite(ledPin, LOW);   // turn the LED off
}

