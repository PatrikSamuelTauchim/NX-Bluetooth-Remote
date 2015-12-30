# NX-Bluetooth-Remote
DIY hardware and software developed to control Samsung NX1000 Camera remotely, using bluetooth (SPP) compatible device (Phone/PC/...) or by wire using USB.
* Probably compatible with other NX cameras, but resistor of different value (in cable) AND/OR firmware modification may be needed.

Price: DIY under 20€

**Features:**
* Remote shutter - without touching and shaking camera
* Multi-platform - Using Serial port over blueTooth you can control Samsung NX camera from any Bluetooth (SPP) equiped device - phone, tablet, PC,..
* External power supply - Using external power supply, such as Li-Ion battery, you can save battery of your camera
* Low Power - Bluetooth require less power to operate compared to WiFi, so you also save battery of your mobile device used as controler
* Shooting in full quality (RAW) - original app only supports JPEG
* Custom shooting modes - time-lapse,...

**Parts:**
* [Avaliable on wiki](https://github.com/PatrikSamuelTauchim/NX-Bluetooth-Remote/wiki)

**Schematics:**
* TODO - Comming soon, once wiring is more finalized.

**Development status:**
* 20.10.2015 - Working prototype based on ATMEGA8 dev board - better HW ordered already. Check wiki for picture :-)
* 13.12.2015 - Arduino Nano based prototype. Basically final version with part list published. Check wiki for new pictures :-)
* 30.12.2015 - Android App published. Arduino Nano protype got some wiring  changes, new software with new command format support and basic feature set (take X photos, with Y delay and Z shutter hold time).
