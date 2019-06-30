using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MmsPiFobReader
{
	static class W26SysFs
	{
		private static string d0Pin;
		private static string d1Pin;

		private static object bufferLock = new object();
		private static int readBuffer = 0;
		private static string inputBuffer = null;

		public static void Initalize()
		{
			switch (ReaderHardware.Type) {
				case HardwareType.OrangePi:
					d0Pin = "199";
					d1Pin = "198";
					break;
				case HardwareType.RaspberryPi:
					d0Pin = "21";
					d1Pin = "20";
					break;
			}

			if (!Directory.Exists($"/sys/class/gpio/gpio{d0Pin}"))
				File.WriteAllText("/sys/class/gpio/export", d0Pin);

			File.WriteAllText($"/sys/class/gpio/gpio{d0Pin}/direction", "in");
			File.WriteAllText($"/sys/class/gpio/gpio{d0Pin}/edge", "falling");

			if (!Directory.Exists($"/sys/class/gpio/gpio{d1Pin}"))
				File.WriteAllText("/sys/class/gpio/export", d1Pin);
			File.WriteAllText($"/sys/class/gpio/gpio{d1Pin}/direction", "in");
			File.WriteAllText($"/sys/class/gpio/gpio{d1Pin}/edge", "falling");

			var thread = new Thread(InputThread);
			thread.Start();
		}

		public static string Read()
		{
			Thread.Sleep(5);
			var buffer = "";

			lock (bufferLock) {
				buffer = inputBuffer;
				inputBuffer = "";
			}

			return buffer;
		}

		private static void WriteToDevice(string path, string contents)
		{
			using (var stream = new FileStream(path, FileMode.Append)) {
				stream.Seek(0, SeekOrigin.Begin);
				stream.Write(Encoding.ASCII.GetBytes(contents));
			}
		}

		private static void InputThread()
		{
			var pfd = new Pollfd[2];
			var r0 = Marshal.AllocHGlobal(1);
			var r1 = Marshal.AllocHGlobal(1);
			byte d0 = 0;
			byte d1 = 0;
			var ret = 0;
			var bitLength = 0;
			var fd0 = open($"/sys/class/gpio/gpio{d0Pin}/value", 0);
			var fd1 = open($"/sys/class/gpio/gpio{d1Pin}/value", 0);

			pfd[0].fd = fd0;
			pfd[0].events = 2;
			pfd[1].fd = fd1;
			pfd[1].events = 2;

			while (true) {
				lseek(fd0, 0, 0);
				lseek(fd1, 0, 0);

				ret = poll(pfd, 2, 5);

				read(fd0, r0, 1);
				read(fd1, r1, 1);

				if (ret != 0) {
					readBuffer <<= 1;
					bitLength += 1;

					d0 = Marshal.ReadByte(r0);
					d1 = Marshal.ReadByte(r1);

					if (d0 == '0')
						continue;
					else if (d1 == '0')

						readBuffer += 1;
				}
				else {
					if (bitLength > 0) {
						lock (bufferLock) {
							if (bitLength == 26) {
								// Shift data so keys make sense
								readBuffer >>= 1;

								inputBuffer = readBuffer.ToString("X8");
							}
							else if (bitLength == 4 || bitLength == 8) {
								if (bitLength == 8)
									readBuffer &= 0xF;

								if (readBuffer == 11)
									inputBuffer += "#";
								if (readBuffer == 10)
									inputBuffer += "*";
								else if (readBuffer > -1 && readBuffer < 10)
									inputBuffer += readBuffer.ToString();
							}

							readBuffer = 0;
							bitLength = 0;
						}
					}
				}

			}
		}

#pragma warning disable 649
		private struct Pollfd
		{
			public int fd;
			public short events;
			public short revents;
		}
#pragma warning restore 649

		[DllImport("libc", SetLastError = true)]
		private static extern int poll([In, Out] Pollfd[] ufds, int nfds, int timeout);

		[DllImport("libc", SetLastError = true)]
		private static extern int open([MarshalAs(UnmanagedType.LPStr)]
				string pathname, int flags);

		[DllImport("libc", SetLastError = true)]
		private static extern int lseek(int fd, int offset, int whence);

		[DllImport("libc", SetLastError = true)]
		private static extern long read(int fd, IntPtr buf, int count);
	}
}
