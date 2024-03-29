using System;

namespace MmsPiFobReader
{
	internal interface IController : IDisposable
	{
		public ReaderResult Initialize();
		public AuthenticationResult Authenticate(string key);
		public void Logout(string key);
		public void Action(string key, string details);
		public void Charge(string key, string details, string description, decimal amount);
	}
}
