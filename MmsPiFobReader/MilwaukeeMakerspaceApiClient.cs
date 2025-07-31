using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Rssdp;

namespace MmsPiFobReader
{
	class MilwaukeeMakerspaceApiClient : IController
	{
		private ReaderStatus status;
		private HttpClient client;

		public MilwaukeeMakerspaceApiClient(ReaderStatus statusIn)
		{
			status = statusIn;
			status.Ip = GetLocalIp4Address();

			// SSDP Not working? Use override file
			if (File.Exists("server.txt")) {
				var dirtyServers = File.ReadAllText("server.txt").Split('\n');
				var cleanServers = new List<string>();

				foreach (var dirtyServer in dirtyServers) {
					var cleanServer = dirtyServer.Trim();

					if (!string.IsNullOrEmpty(cleanServer) && !cleanServers.Contains(cleanServer))
						cleanServers.Add(cleanServer);
				}

				status.Server = cleanServers.ToArray();
			}

			client = GetClient();

			status.Controller = "Server";
			status.Warning = "";
		}

		public void Dispose()
		{
			client?.Dispose();
			client = null;
		}

		public ReaderResult Initialize()
		{
			var request = new StringContent(JsonConvert.SerializeObject(status));
			var result = client.PostAsync($"reader/initialize", request).Result;

			result.EnsureSuccessStatusCode();

			return JsonConvert.DeserializeObject<ReaderResult>(result.Content.ReadAsStringAsync().Result);
		}

		public AuthenticationResult Authenticate(string key)
		{
			var action = new ActionRequest {
				Id = status.Id,
				Key = key,
				Type = "Login",
				Action = ""
			};

			var request = new StringContent(JsonConvert.SerializeObject(action));
			var result = client.PostAsync($"reader/action", request).Result;

			result.EnsureSuccessStatusCode();

			return JsonConvert.DeserializeObject<AuthenticationResult>(result.Content.ReadAsStringAsync().Result);
		}

		public void Logout(string key)
		{
			var action = new ActionRequest {
				Id = status.Id,
				Key = key,
				Type = "Logout",
				Action = ""
			};

			var request = new StringContent(JsonConvert.SerializeObject(action));
			var result = client.PostAsync($"reader/action", request).Result;

			result.EnsureSuccessStatusCode();
		}

		public void Action(string key, string details)
		{
			var action = new ActionRequest {
				Id = status.Id,
				Key = key,
				Type = "Action",
				Action = details
			};

			var request = new StringContent(JsonConvert.SerializeObject(action));
			var result = client.PostAsync($"reader/action", request).Result;

			result.EnsureSuccessStatusCode();
		}

		public void Charge(string key, string details, string description, decimal amount)
		{
			var action = new ActionRequest {
				Id = status.Id,
				Key = key,
				Type = "Charge",
				Action = details,
				Description = description,
				Amount = amount.ToString(),
			};

			var request = new StringContent(JsonConvert.SerializeObject(action));
			var result = client.PostAsync($"reader/action", request).Result;

			result.EnsureSuccessStatusCode();
		}

		public void DownloadSnapshot()
		{
			var result = client.GetAsync($"reader/snapshot").Result;

			result.EnsureSuccessStatusCode();

			using (var fs = new FileStream(LocalController.FileName, FileMode.Create))
				result.Content.CopyTo(fs, null, CancellationToken.None);
		}

		private HttpClient GetClient()
		{
			var found = false;

			// Seems redundant, but if any of these are null, foreach would throw
			if (status?.Server?.Length > 0) {
				foreach (var server in status.Server) {
					var cleanServer = server.Trim();

					if (string.IsNullOrEmpty(cleanServer))
						break;

					var client = new HttpClient();
					client.BaseAddress = new Uri($"http://{cleanServer}/");
					client.Timeout = new TimeSpan(0, 0, 5);

					try {
						// Check if server is responding
						_ = client.GetStringAsync($"/").Result;

						return client;
					}
					catch {
						// Let it try the next one
					}
				}
			}

			if (!found) {
				SearchForServer();

				var client = new HttpClient();
				client.BaseAddress = new Uri($"http://{status.Server[0]}/");
				client.Timeout = new TimeSpan(0, 0, 5);

				// Check if server is responding
				_ = client.GetStringAsync($"/").Result;

				return client;
			}

			throw new Exception("Could not find server.");
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
					if (status.Server?.Length > 0)
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
				status.Server = [e.DiscoveredDevice.DescriptionLocation.Host];
		}

		private static string GetLocalIp4Address()
		{
			try {
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
			}
			catch {
				// Do Nothing
			}

			return "127.0.0.1";
		}
	}
}
