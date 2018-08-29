using System;
using System.Runtime.InteropServices;

namespace MmsPiFobReader
{
	static class ReadW26
	{
		public static void Initalize()
		{
#if LINUX
			initW26();
#endif
		}

		[DllImport ("libReadW26.so")]
		private static extern void initW26();

		public static string Read()
		{
#if LINUX
			var pointer = readW26();

			return Marshal.PtrToStringAnsi(pointer);
#else
			return ((char)Console.Read()).ToString();
#endif
		}

		[DllImport ("libReadW26.so")]
		private static extern IntPtr readW26();
	}
}
