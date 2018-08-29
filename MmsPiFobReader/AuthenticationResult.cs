using System;

namespace MmsPiFobReader
{
	public class AuthenticationResult
    {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public bool Admin { get; set; }
		public DateTime Joined { get; set; }
		public DateTime Expiration { get; set; }
		public bool AccessGranted { get; set; }
    }
}
