using System;
using System.Runtime.InteropServices;

namespace MmsPiFobReader
{
	// TODO, Port C++ W26 library to C# remove dependancy
	static class W26WiringPi
	{
		public static void Initalize()
		{
			initW26();
		}

		[DllImport("libReadW26.so")]
		private static extern void initW26();

		public static string Read()
		{
			var pointer = readW26();

			return Marshal.PtrToStringAnsi(pointer);
		}

		[DllImport("libReadW26.so")]
		private static extern IntPtr readW26();
	}
}
