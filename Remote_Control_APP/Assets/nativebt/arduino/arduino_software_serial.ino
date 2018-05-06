#include <SoftwareSerial.h>
SoftwareSerial BTserial(2, 3); // RX | TX
// Connect the HC-05 TX to the Arduino RX on pin 2. 
// Connect the HC-05 RX to the Arduino TX on pin 3 through a voltage divider. 
// I have not used a voltage divider...
 
void setup() 
{
    Serial.begin(9600); 
    // HC-06 default serial speed is 9600
    BTserial.begin(9600);  
}
 
void loop()
{ 
    // Keep reading from HC-05 and send to Arduino Serial Monitor
    if (BTserial.available())
    {  
        Serial.write(BTserial.read());
    }
 
    // Keep reading from Arduino Serial Monitor and send to HC-05
    if (Serial.available())
    {
        BTserial.write(Serial.read());
    } 
}
