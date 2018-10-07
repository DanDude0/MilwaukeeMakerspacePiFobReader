using System;
using System.Runtime.InteropServices;

namespace MmsPiFobReader
{
	static class WiringPi
	{
		[DllImport("libwiringPi.so")]
		public static extern void pinMode(int pin, int mode);

		[DllImport("libwiringPi.so")]
		public static extern void digitalWrite(int pin, int value);
	}
}
