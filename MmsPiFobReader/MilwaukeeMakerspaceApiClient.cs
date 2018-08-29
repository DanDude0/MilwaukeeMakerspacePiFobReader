using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace MmsPiFobReader
{
	class MilwaukeeMakerspaceApiClient
	{
		HttpClient client;

		public MilwaukeeMakerspaceApiClient()
		{
			client = new HttpClient();
			client.BaseAddress = new Uri("http://10.1.1.15/");
			client.Timeout = new TimeSpan(0, 0, 5);

			try
			{
				var unused = client.GetStringAsync($"/").Result;
			}
			catch
			{
				// Try from My House if we're not in the building
				client = new HttpClient();
				client.BaseAddress = new Uri("http://192.168.86.32:9588/");
				client.Timeout = new TimeSpan(0, 0, 5);

				var unused2 = client.GetStringAsync($"/").Result;
			}
		}

		public ReaderResult ReaderLookup(int id)
		{
			var result = client.GetStringAsync($"reader/lookup/{id}").Result;

			return JsonConvert.DeserializeObject<ReaderResult>(result);
		}

		public AuthenticationResult Authenticate(int id, string key)
		{
			var result = client.GetStringAsync($"authenticate/json/{id}/{key}").Result;

			return JsonConvert.DeserializeObject<AuthenticationResult>(result);
		}
	}
}
