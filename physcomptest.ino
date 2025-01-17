#include <HCSR04.h>

UltraSonicDistanceSensor distanceSensor(2, 3); //initialisation class HCSR04 (trig pin , echo pin)


float dist = 0;

void setup() {
  Serial.begin(9600);
  // put your setup code here, to run once:
}

void loop() {
  // put your main code here, to run repeatedly:
  dist = distanceSensor.measureDistanceCm(); 
  if(dist > 100 or dist <= 0) {
    return;
  }
  Serial.println( dist ); //return current distance (cm) in serial
delay(20);
// delay(2000);
}
