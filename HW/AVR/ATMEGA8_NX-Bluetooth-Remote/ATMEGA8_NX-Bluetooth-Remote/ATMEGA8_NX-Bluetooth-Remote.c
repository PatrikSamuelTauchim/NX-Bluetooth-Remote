/*
 * ATMEGA8_NX-Bluetooth-Remote.c
 *
 * Created: 9/29/2015 6:09:34 PM
 * Author: Patrik Samuel Tauchim
 * + Some code snippets found around the Internet - Thanks to the authors!
 *PINOUT:
 *PB0: LED+
 *PC0: Shutter
 *PC1: Focus
 *PD0 + PD1: RX/TX to Bluetooth module/PC/...
 */

#include <avr/io.h>
#include <avr/delay.h>
#include <avr/interrupt.h>
#define F_CPU 8000000UL // 8MHz
#define USART_BAUDRATE 9600
#define BAUD_PRESCALE (((F_CPU / (USART_BAUDRATE * 16UL))) - 1)

void adc_init(void);            //Function to initialize/configure the ADC
uint16_t read_adc(uint8_t channel);    //Function to read an arbitrary analogic channel/pin
uint16_t adc_value;            //Variable used to store the value read from the ADC
char buffer[5];                //Output of the itoa function
uint8_t i=0;                //Variable for the for() loop

volatile int analog_value;

ISR(ADC_vect)//Dont ask me what it does
{
	analog_value = ADCH;
}

int main (void)
{
	char ReceivedByte;
	adc_init();
	int resend_count;
	int resend_limit;
	resend_count=0;   //TODO: How many times we already asked for resend
	resend_limit=10; //TODO:  How many times to ask for last command again, if we received some crap
	int AT_mode_on =0; //TODO: Switch to send AT commands to BT module, not actual commands for camera
	int i = 0;

	UCSRB = (1 << RXEN) | (1 << TXEN);   // Turn on the transmission and reception circuitry
	UCSRC = (1 << URSEL) | (1 << UCSZ0) | (1 << UCSZ1); // Use 8-bit character sizes

	UBRRH = (BAUD_PRESCALE >> 8); // Load upper 8-bits of the baud rate value into the high byte of the UBRR register
	UBRRL = BAUD_PRESCALE; // Load lower 8-bits of the baud rate value into the low byte of the UBRR register
	ReplyString("Hello"); // Say Hello!

for (;;) // Loop forever
{
	while ((UCSRA & (1 << RXC)) == 0) {}; // Do nothing until data have been received and is ready to be read from UDR
	ReceivedByte = UDR; // Fetch the received byte value into the variable "ByteReceived"
	
	if (AT_mode_on == 0)
	{
		if (ReceivedByte==48)// "0"
		{
			resend_count=0;
			ReplyString("OK");
			if (isCameraConnected()==1)
			{
				ReplyString("Camera is ON");
			}
			else
			{
				ReplyString("Camera is OFF");
			}
		}
		else if (ReceivedByte==49)// "1" => Shutter
		{
			resend_count=0;
			SingleShot();
			ReplyString("Single shot");
		}
		else if (ReceivedByte==50)// "2" => Focus
		{
			SingleFocus();
			ReplyString("Single focus");
		}
		//LOCAL DEBUG ONLY
		else if (ReceivedByte==51)// "3" => Sends "AT" to check if BT module is paired - useless over BT since when module its paired, it doesnt recives AT commands
		{
			resend_count=0;
			_delay_ms(2000);
			SendAT("AT");
			_delay_ms(2000);
		}
		else
		//TODO (basically works, but app doesnt support it yet)
		//UNKNOWN COMMAND OR COMMUNICATION ERROR HANDLING
		//There should not be much communication errors, but I have some on my prototype while communication over long wires. Over Bluetooth it works fine.
		{
			/*ReplyString("Unrecognised command");
			SendChar(resend_count+48);
			SendChar(10);
			//ASK FOR COMMAND RESEND
			if (resend_count<=resend_limit)
			{
				ReplyString("RESEND");
				resend_count++;
			}
			else
			{
				ReplyString("UNRECOGNISED: ");//FAILED TOO MANY TIMES - DO NOT SEND THIS COMMAND AGAIN OR TROUBLESHOOT COMMUNICATION ISSUES and HAVE FUN! :-)
				resend_count=0;
			}*/
			SendChar(ReceivedByte);//send back what we received - probably some crap :-!
			SendChar(10);
		}
	}
	/*else
	// TODO
	// AT MODE - listens for whole strings - While NOT paired, We can set AT commands to Bluetooth module, like set NAME, PIN, etc.
	// To "AT" reply should be "OK" while not paired, so we can check pairing status. :-)
	// This code is probably not working
	{
	SendChar(ReceivedByte);
		if (ReceivedByte == "13")
		{
			ReplyString(data);
			i = 0;
			data[0] = 0;
		}
		else
		{
			data[i++] = ReceivedByte;
		}
		unsigned char string[20];
		int ii = 0;

		//receive the characters until ENTER is pressed (ASCII for ENTER = 13)
		if(ReceivedByte != 13)
		{
			//and store the received characters into the array string[] one-by-one
			string[ii] = ReceivedByte;
			ii++;
			FlashLed();
		}
		else if (string!=0)
		{
			ReplyString(string);
			ii=0;
			string[0]=0;
		}
		else
			ReplyString("0");
	}*/
}
}
// When NX1000 camera (and maybe other models) is turned ON, there is around 2,5V on data pins. When it is OFF, there is 0V
// We measure voltage on PC0 for 10 times and making average. If average is more than 100 ( around 0,5V) we consider camera is ON, otherwise it should be less (0V)
// TODO: Handle floating (disconnected) cable -> HW
int isCameraConnected()
{
	int ii;
	int total;
	int average;
	while (ii<10) //Measure voltage on PB0 10 times
	{
		_delay_ms(1000);
		adc_value = read_adc(i);
		total +=adc_value;
		
		itoa(adc_value, buffer, 10);
		ReplyString(buffer);//DEBUG - send result
		SendChar(10);
		
		FlashLed();
		ii++;
	}
	average = total/=10;
	itoa(average, buffer, 10);
	ReplyString(buffer); //send average
	SendChar(10);
	if (average < 100)
	{
		return 0;//not connected
	}
	else
	{
		return 1;//connected
	}
}

void FlashLed() // flash led for 0,5 seconds
{
	DDRB |=1; //Set PB0 as output
	PORTB |= 1; // Set high
	_delay_ms(50); // wait 0,5 seconds
	PORTB &= ~1; // Set low
}

void adc_init(void)// Init ADC - I found this code laying somewhere 
{
	ADCSRA |= ((1<<ADPS2)|(1<<ADPS1)|(1<<ADPS0));    //16Mhz/128 = 125Khz the ADC reference clock
	ADMUX |= (1<<REFS0);                //Voltage reference from Avcc (5v)
	ADCSRA |= (1<<ADEN);                //Turn on ADC
	ADCSRA |= (1<<ADSC);                //Do an initial conversion because this one is the slowest and to ensure that everything is up and running
}

uint16_t read_adc(uint8_t channel)// Read ADC - I also found this code laying somewhere 
{
	ADMUX &= 0xF0;                    //Clear the older channel that was read
	ADMUX |= channel;                //Defines the new ADC channel to be read
	ADCSRA |= (1<<ADSC);                //Starts a new conversion
	while(ADCSRA & (1<<ADSC));            //Wait until the conversion is done
	return ADCW;                    //Returns the ADC value of the chosen channel
}

	void ReplyString(char* StringPtr)
	{
		while (*StringPtr != 0x00)
		{
			SendChar(*StringPtr);
			StringPtr++;
		}
		SendChar(10);
	}

	void SendAT(char* StringPtr)
	{
		while (*StringPtr != 0x00)
		{
			SendChar(*StringPtr);
			StringPtr++;
		}
	}

	void SendChar(char ToSend)
	{
		UDR = ToSend; // Send byte
		FlashLed();
	}

	void SingleShot()//BLACK WIRE (for me)
	{
		/* OLD CODE
		*DDRC |=1; // Set PC0 as output - low
		*_delay_ms(10); // wait
		*DDRC &=0; // Set PC0 as input - floating
		*/
		DDRC |= (1<<0);
		_delay_ms(4000); // wait
		DDRC &= ~(1<<0);
	}

	void SingleFocus()//GREEN WIRE (for me)
	{
		DDRC |= (1<<1);  //Set PC1 as output - low
		_delay_ms(4000); // wait 4 SECONDS
		DDRC &= ~(1<<1); // Set PC1 as input - floating
	}