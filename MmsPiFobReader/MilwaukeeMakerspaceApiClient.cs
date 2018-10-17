using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Rssdp;

namespace MmsPiFobReader
{
	class MilwaukeeMakerspaceApiClient
	{
		string server;

		public MilwaukeeMakerspaceApiClient()
		{
			// SSDP Not working? Use override file
			if (File.Exists("server.txt"))
				server = File.ReadAllText("server.txt");
			else
				SearchForServer();

			var client = GetClient();

			var unused = client.GetStringAsync($"/").Result;
		}

		public ReaderResult ReaderLookup(int id)
		{
			var client = GetClient();
			var result = client.GetStringAsync($"reader/lookup/{id}").Result;

			return JsonConvert.DeserializeObject<ReaderResult>(result);
		}

		public AuthenticationResult Authenticate(int id, string key)
		{
			var client = GetClient();
			var result = client.GetStringAsync($"authenticate/json/{id}/{key}").Result;

			return JsonConvert.DeserializeObject<AuthenticationResult>(result);
		}

		public void Logout(int id)
		{
			var client = GetClient();

			client.GetAsync($"authenticate/logout/{id}/").Result.EnsureSuccessStatusCode();
		}

		private HttpClient GetClient()
		{
			var client = new HttpClient();
			client.BaseAddress = new Uri($"http://{server}/");
			client.Timeout = new TimeSpan(0, 0, 5);

			return client;
		}

		private void SearchForServer()
		{
			string ip4;

			// Have to bind to all addresses on Linux, or broadcasts don't work!
			if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
				ip4 = GetLocalIp4Address();
			}
			else {
				ip4 = IPAddress.Any.ToString();
			}

			SsdpDeviceLocator deviceLocator = null;

			try {
				deviceLocator = new SsdpDeviceLocator(ip4);
				deviceLocator.StartListeningForNotifications();
				deviceLocator.DeviceAvailable += FoundDevice;
				var unused = deviceLocator.SearchAsync("uuid:6111f321-2cee-455e-b203-4abfaf14b516", new TimeSpan(0, 0, 5));
				deviceLocator.StartListeningForNotifications();

				for (int i = 0; i < 20; i += 1) {
					if (!string.IsNullOrEmpty(server))
						// We found a server, let the constructor continue
						return;

					Thread.Sleep(500);
				}
			}
			finally {
				deviceLocator?.StopListeningForNotifications();
				deviceLocator?.Dispose();
			}

			throw new Exception("Could not locate server");
		}

		private void FoundDevice(object sender, DeviceAvailableEventArgs e)
		{
			if (e.DiscoveredDevice.Usn.Contains("6111f321-2cee-455e-b203-4abfaf14b516"))
				server = e.DiscoveredDevice.DescriptionLocation.Host;
		}

		private static string GetLocalIp4Address()
		{
			var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

			foreach (var network in networkInterfaces) {
				if (network.OperationalStatus != OperationalStatus.Up)
					continue;

				var properties = network.GetIPProperties();

				if (properties.GatewayAddresses.Count == 0)
					continue;

				foreach (var address in properties.UnicastAddresses) {
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
