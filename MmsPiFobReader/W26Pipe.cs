using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MmsPiFobReader
{
	static class W26Pipe
	{
		private const string pipePath = "/tmp/MmsPiFobReaderKeypad";
		private const int O_RDRW = 00000002;
		private const int O_NONBLOCK = 00004000;
		private const int POLLIN = 1;

		private static Pollfd[] pfd;
		private static IntPtr r;
		private static int ret;
		private static int fd;
		private static string output;
		private static int cursor;
		private static int end;
		private static int size;
		private static byte[] buffer;

		public static void Initalize()
		{
			if (!File.Exists(pipePath))
				if (mkfifo(pipePath, 0666) != 0)
					throw new Exception("Could not create fifo buffer.");

			pfd = new Pollfd[1];
			r = Marshal.AllocHGlobal(8);
			fd = open(pipePath, O_RDRW | O_NONBLOCK);

			pfd[0].fd = fd;
			pfd[0].events = POLLIN;
			buffer = new byte[256];
		}

		public static string Read()
		{
			size = end - cursor;

			if (size < 1) {
				ret = poll(pfd, 1, 5);

				if (ret > 0) {
					ret = (int)read(fd, r, 256);

					if (ret > 0) {
						Marshal.Copy(r, buffer, 0, ret);

						cursor = 0;
						end = ret;
						size = ret;
					}
				}

				// Nothing to read
				return "";
			}

			if (buffer[cursor + 1] == '\n') {
				// Keypress stacked up front
				output = Encoding.ASCII.GetString(buffer, cursor, 1);
				cursor += 2;
			}
			else {
				// Fob stacked up front
				output = Encoding.ASCII.GetString(buffer, cursor, 8);
				cursor += 9;
			}

			return output;
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
		private static extern long read(int fd, IntPtr buf, int count);

		[DllImport("libc", SetLastError = true)]
		private static extern int mkfifo([MarshalAs(UnmanagedType.LPStr)]
			string pathname, uint mode);
	}
}
