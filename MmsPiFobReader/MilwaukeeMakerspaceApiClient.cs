using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
			var host = SearchForServer().Result;

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

		private SsdpDeviceLocator deviceLocator = null;
		private string hostname = null;

		private async Task<string> SearchForServer()
		{
			var ip4 = GetLocalIp4Address();

			try{
				deviceLocator = new SsdpDeviceLocator(ip4);
				deviceLocator.StartListeningForNotifications();
				deviceLocator.DeviceAvailable += FoundDevice;
				var task = deviceLocator.SearchAsync(new TimeSpan(0, 0, 10));

				while (deviceLocator.IsSearching) {
					if (hostname != null)
						return hostname;

					await Task.Delay(500);
				}

			}
			finally
			{
				deviceLocator.StopListeningForNotifications();
			}

			throw new Exception("Could not locate server");
		}

		private void FoundDevice(object sender, DeviceAvailableEventArgs e)
		{
			if (e.DiscoveredDevice.Usn.Contains("6111f321-2cee-455e-b203-4abfaf14b516"))
				hostname = e.DiscoveredDevice.DescriptionLocation.Host;
		}

		private static string GetLocalIp4Address()
		{
			var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

			foreach (var network in networkInterfaces)
			{
				if (network.OperationalStatus != OperationalStatus.Up)
					continue;

				var properties = network.GetIPProperties();

				if (properties.GatewayAddresses.Count == 0)
					continue;

				foreach (var address in properties.UnicastAddresses)
				{
					if (address.Address.AddressFamily != AddressFamily.InterNetwork)
						continue;

					if (IPAddress.IsLoopback(address.Address))
						continue;

					return address.Address.ToString();
				}
			}

			throw new Exception("No IP Address Found for SSDP");
		}
	}
}
