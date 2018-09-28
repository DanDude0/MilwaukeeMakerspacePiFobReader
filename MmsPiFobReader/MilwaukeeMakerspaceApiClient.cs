using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rssdp;

namespace MmsPiFobReader
{
    class MilwaukeeMakerspaceApiClient
    {
        HttpClient client;

		public MilwaukeeMakerspaceApiClient()
        {
            var host = "10.1.1.15"; //SearchForServer().Result;

            client = new HttpClient();
            client.BaseAddress = new Uri($"http://{host}/");
            client.Timeout = new TimeSpan(0, 0, 1);

            var unused = client.GetStringAsync($"/").Result;
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

		public async Task<string> SearchForServer()
		{
			using (var deviceLocator = new SsdpDeviceLocator())
			{
				var foundDevices = await deviceLocator.SearchAsync("uuid:6111f321-2cee-455e-b203-4abfaf14b516", new TimeSpan(0, 0, 15));

				foreach (var foundDevice in foundDevices)
				{
					return foundDevice.DescriptionLocation.Authority;
				}
			}

			throw new Exception("Could not locate server");
		}
	}
}
