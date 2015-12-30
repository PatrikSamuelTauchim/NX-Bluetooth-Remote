# NX-Bluetooth-Remote
DIY hardware and software developed to control Samsung NX1000 Camera remotely, using bluetooth (SPP) compatible device (Phone/PC/...) or by wire using USB.
* Probably compatible with other NX cameras, but resistor of different value (in cable) AND/OR firmware modification may be needed.

Price: DIY under 15€

**Features done **
* Remote shutter - Allows to take one shot
* Continuous shooting - take specific amount of shots with defined delay between them
* Android App for wireless control via Bluetooth

**Features planed:**
* Desktop app for Windows - For now, serial terminal is enough to send commands manually. Just connect USB cable from Arduino module.

**HW features**
* Battery operation from Li-Ion cell and charging from USB, which can be also used for control.
* Matchbox size

**Parts:**
* [Avaliable on wiki](https://github.com/PatrikSamuelTauchim/NX-Bluetooth-Remote/wiki)

**Schematics:**
* TODO - Comming soon, once wiring is more finalized.

**Development status:**
* 20.10.2015 - Working prototype based on ATMEGA8 dev board - better HW ordered already. Check wiki for picture :-)
* 13.12.2015 - Arduino Nano based prototype. Basically final version with part list published. Check wiki for new pictures :-)
* 30.12.2015 - Android App published. Arduino Nano protype got some wiring  changes, new software with new command format support and basic feature set (take X photos, with Y delay and Z shutter hold time).
