/*
 * Wiegand API Raspberry Pi
 * By Kyle Mallory
 * 12/01/2013
 * Based on previous code by Daniel Smith (www.pagemac.com) and Ben Kent (www.pidoorman.com)
 * Depends on the wiringPi library by Gordon Henterson: https://projects.drogon.net/raspberry-pi/wiringpi/
 *
 * The Wiegand interface has two data lines, DATA0 and DATA1.  These lines are normall held
 * high at 5V.  When a 0 is sent, DATA0 drops to 0V for a few us.  When a 1 is sent, DATA1 drops
 * to 0V for a few us. There are a few ms between the pulses.
 *   *************
 *   * IMPORTANT *
 *   *************
 *   The Raspberry Pi GPIO pins are 3.3V, NOT 5V. Please take appropriate precautions to bring the
 *   5V Data 0 and Data 1 voltges down. I used a 330 ohm resistor and 3V3 Zenner diode for each
 *   connection. FAILURE TO DO THIS WILL PROBABLY BLOW UP THE RASPBERRY PI!
 */
#include <stdio.h>
#include <stdlib.h>
#include <wiringPi.h>
#include <time.h>
#include <unistd.h>
#include <memory.h>

#define D0_PIN 29
#define D1_PIN 28

#define WIEGANDMAXDATA 34
#define WIEGANDTIMEOUT 3000000

static unsigned char __wiegandData[WIEGANDMAXDATA];    // can capture upto 32 bytes of data -- FIXME: Make this dynamically allocated in init?
static unsigned long __wiegandBitCount;            // number of bits currently captured
static struct timespec __wiegandBitTime;        // timestamp of the last bit received (used for timeouts)

void data0Pulse(void) {
	if (__wiegandBitCount / 8 < WIEGANDMAXDATA) {
		__wiegandData[__wiegandBitCount / 8] <<= 1;
		__wiegandBitCount++;
	}
	clock_gettime(CLOCK_MONOTONIC, &__wiegandBitTime);
}

void data1Pulse(void) {
	if (__wiegandBitCount / 8 < WIEGANDMAXDATA) {
		__wiegandData[__wiegandBitCount / 8] <<= 1;
		__wiegandData[__wiegandBitCount / 8] |= 1;
		__wiegandBitCount++;
	}
	clock_gettime(CLOCK_MONOTONIC, &__wiegandBitTime);
}

int wiegandInit(int d0pin, int d1pin) {
	// Setup wiringPi
	wiringPiSetup() ;
	pinMode(d0pin, INPUT);
	pinMode(d1pin, INPUT);

	wiringPiISR(d0pin, INT_EDGE_FALLING, data0Pulse);
	wiringPiISR(d1pin, INT_EDGE_FALLING, data1Pulse);
}

void wiegandReset() {
	memset((void *)__wiegandData, 0, WIEGANDMAXDATA);
	__wiegandBitCount = 0;
}

int wiegandGetPendingBitCount() {
	struct timespec now, delta;
	clock_gettime(CLOCK_MONOTONIC, &now);
	delta.tv_sec = now.tv_sec - __wiegandBitTime.tv_sec;
	delta.tv_nsec = now.tv_nsec - __wiegandBitTime.tv_nsec;

	if ((delta.tv_sec > 1) || (delta.tv_nsec > WIEGANDTIMEOUT))
		return __wiegandBitCount;

	return 0;
}

/*
 * wiegandReadData is a simple, non-blocking method to retrieve the last code
 * processed by the API.
 * data : is a pointer to a block of memory where the decoded data will be stored.
 * dataMaxLen : is the maximum number of -bytes- that can be read and stored in data.
 * Result : returns the number of -bits- in the current message, 0 if there is no
 * data available to be read, or -1 if there was an error.
 * Notes : this function clears the read data when called. On subsequent calls,
 * without subsequent data, this will return 0.
 */
int wiegandReadData(void* data, int dataMaxLen) {
	if (wiegandGetPendingBitCount() > 0) {
		int bitCount = __wiegandBitCount;
		int byteCount = (__wiegandBitCount / 8) + 1;
		memcpy(data, (void *)__wiegandData, ((byteCount > dataMaxLen) ? dataMaxLen : byteCount));

		wiegandReset();
		return bitCount;
	}
	return 0;
}

extern "C"
void initW26() {
	setbuf(stdout, NULL);
	wiegandInit(D0_PIN, D1_PIN);
}

static char output[9];

extern "C"
char *readW26() {
	int bitLen = wiegandGetPendingBitCount();
	if (bitLen == 0) {
		usleep(5000);

		output[0] = '\0';
	} 
	else {
		char data[100];

		bitLen = wiegandReadData((void *)data, 100);

		if (bitLen == 26) {
			// Shift data so keys make sense
			data[3] = (data[2] << 1) + (data[3] >> 7);
			data[2] = (data[1] << 1) + (data[2] >> 7);
			data[1] = (data[0] << 1) + (data[1] >> 7);
			data[0] = data[0] >> 7;

			for (int i = 0; i < 4; i++)
				sprintf(output + i * 2, "%02X", (int)data[i]);
		}
		else if (bitLen == 4 || bitLen == 8)
		{
			if (bitLen == 8)
				data[0] = data[0] & 0xF;
			
			if (data[0] == 11)
				sprintf(output, "#");
			if (data[0] == 10)
				sprintf(output, "*");
			else if (data[0] > -1 && data[0] < 10)
				sprintf(output, "%d", (int)data[0]);
		}
	}

	return output;
}

