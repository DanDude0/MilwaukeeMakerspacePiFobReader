using System;

namespace MmsPiFobReader
{
	public class ReaderResult
	{
		public string Name { get; set; }
		public int Timeout { get; set; }
		public bool Enabled { get; set; }
		public string Group { get; set; }
		public string Settings { get; set; }
		public DateTime ServerUTC { get; set; }
	}
}
