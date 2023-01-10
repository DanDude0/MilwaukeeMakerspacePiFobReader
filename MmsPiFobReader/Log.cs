using System;
using System.Collections.Generic;
using System.Text;

namespace MmsPiFobReader
{
	static class Log
	{
		public static void Message(string message)
		{
			Console.WriteLine(message);
		}

		public static void Exception(Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}
}
