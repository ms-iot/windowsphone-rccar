// Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the BSD 2 - Clause License.
// See License.txt in the project root for license information.

// Main.cpp : Defines the entry point for the console application.
//


#include "stdafx.h"
#include "arduino.h"

//define pins
const int OUT_RIGHT = 10;
const int OUT_UP = 6;
const int OUT_LEFT = 11;
const int OUT_DOWN = 5;
const int OUT_READY = 2;

//define command buffers
char * command;
char * subcommand;

int _tmain(int argc, _TCHAR* argv[])
{
	return RunArduinoSketch();
}


//Process the incoming command from Windows Phone.
void processCommand(char* command) 
{

	if (command[0] == '4'){
		//shutdown command
		digitalWrite(OUT_READY, LOW);		
		system("shutdown /s /t 0");
	}

	UINT8 val;
	// CHECK L/R Command
	if (command[0] == '0')
	{
		//use bytes 2,3,4 of command if you want speed, but l/r doesnt need it
		analogWrite(OUT_RIGHT, 0);
		analogWrite(OUT_LEFT, 255);
	}
	else if (command[0] == '1')
	{
		//use bytes 2,3,4 of command if you want speed, but l/r doesnt need it
		analogWrite(OUT_RIGHT, 255);
		analogWrite(OUT_LEFT, 0);
	}
	else
	{
		digitalWrite(OUT_RIGHT, LOW);
		digitalWrite(OUT_LEFT, LOW);
	}

	// CHECK U/D Command
	if (command[4] == '0')
	{
		//interpret intensity message
		memcpy(subcommand, &command[5], 3);
		val = (UINT8)atoi(subcommand);
		analogWrite(OUT_UP, 0);
		analogWrite(OUT_DOWN, val);
	}
	else if (command[4] == '1')
	{
		//interpret intensity message
		memcpy(subcommand, &command[5], 3);
		val = (UINT8)atoi(subcommand);
		analogWrite(OUT_UP, val);
		analogWrite(OUT_DOWN, 0);
	}
	else
	{
		digitalWrite(OUT_UP, LOW);
		digitalWrite(OUT_DOWN, LOW);
	}

}


/* Setup Arduino function */
void setup()
{
	Serial.begin(9600);

	//set motor pins to output mode
	pinMode(OUT_UP, OUTPUT);
	pinMode(OUT_RIGHT, OUTPUT);
	pinMode(OUT_DOWN, OUTPUT);
	pinMode(OUT_LEFT, OUTPUT);

	//set pin outputs to low
	digitalWrite(OUT_UP, LOW);
	digitalWrite(OUT_RIGHT, LOW);
	digitalWrite(OUT_DOWN, LOW);
	digitalWrite(OUT_LEFT, LOW);

	//turn on ready light
	pinMode(OUT_READY, OUTPUT);
	digitalWrite(OUT_READY, HIGH);

	/*
	Message structure:
	1 Byte for L/R Command
	3 Bytes for L/R Speed
	1 Byte for F/B Command
	3 Bytes for F/B Speed
	1 Null
	____________________
	9 bytes
	*/
	command = (char *)malloc(9);
	subcommand = (char *)malloc(4);
	subcommand[4] = 0;
}

//Loop Arduino function
void loop() 
{
	//read packets from BT
	if (Serial.available()) {
		int commandSize = (int)Serial.read();
		int commandPos = 0;
		while (commandPos < commandSize) {
			if (Serial.available()) {
				command[commandPos] = (char)Serial.read();
				commandPos++;
			}
		}
		command[commandPos] = 0;
		processCommand(command);
	}

}