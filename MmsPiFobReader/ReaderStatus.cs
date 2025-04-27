namespace MmsPiFobReader
{
	public class ReaderStatus
	{
		public int Id { get; set; }
		public string Version { get; set; }
		public string Ip { get; set; }
		public string Hardware { get; set; }
		public string Kernel { get; set; }
		public string Os { get; set; }
		public string[] Server { get; set; }
		public string Controller { get; set; }
		public string LocalSnapshot { get; set; }
		public string Uptime { get; set; }
		public string Warning { get; set; }
	}
}
