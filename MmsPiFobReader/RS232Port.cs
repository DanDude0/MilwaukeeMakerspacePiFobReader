using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MmsPiFobReader
{
	static class RS232Port
	{
		private static SerialPort serialPort;
		private static string output;
		private static int ret;
		private static int cursor;
		private static int end;
		private static int size;
		private static byte[] buffer;

		public static void Initalize(string device)
		{
			serialPort = new SerialPort(device, 9600, Parity.None, 8, StopBits.One);
			serialPort.Open();
			buffer = new byte[256];
		}

		public static string Read()
		{
			size = end - cursor;

			// Fill empty buffer if we have data, reset cursor
			if (size < 1 && serialPort.BytesToRead != 0) {
				ret = serialPort.Read(buffer, 0, 256);

				cursor = 0;
				end = ret;
				size = ret;

				Console.WriteLine($"Received raw RS232 read [{size}]: {Convert.ToBase64String(buffer.AsSpan(0,size))}");
			}

			// Detect start/stop bytes from an RS232 reader
			if (size > 0 && buffer[cursor] == 0x2 && buffer[cursor + 13] == 0x3) {
				// Fob id stacked up front
				// chop off start/stop bytes and CrLf from an RS232 reader
				output = Encoding.ASCII.GetString(buffer, cursor + 1, 10);
				cursor += 14;

				return output;
			}
			else if (size > 0) {
				// Advance the read cursor, try again next time.
				cursor += 1;
			}

			// Nothing to read
			return "";
		}
	}
}
