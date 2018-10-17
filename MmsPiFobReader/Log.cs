using System;
using System.Collections.Generic;
using System.Text;

namespace MmsPiFobReader
{
	static class Log
	{
		public static void Exception(Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}
}
