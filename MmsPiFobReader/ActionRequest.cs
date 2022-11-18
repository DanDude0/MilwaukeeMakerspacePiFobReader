using System;

namespace MmsPiFobReader
{
	public class ActionRequest
	{
		public int Id { get; set; }
		public string Key { get; set; }
		public string Type { get; set; }
		public string Action { get; set; }
		public string Description { get; set; }
		public string Amount { get; set; }
	}
}
