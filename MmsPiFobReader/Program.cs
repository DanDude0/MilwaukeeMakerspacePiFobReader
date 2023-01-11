using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace MmsPiFobReader
{
	class Program
	{
		const string ServiceMenuMagicCode = "14725369";

		// Set at Initialization
		static readonly ReaderStatus status = new ReaderStatus();
		static MilwaukeeMakerspaceApiClient server;
		static IController controller;
		static ReaderResult config;

		// Populated from config settings
		static ReaderMode mode;
		static Dictionary<int, string> cabinetItems;
		static bool warningBeep;
		static string chargePrompt;
		static decimal chargeRate;
		static ChargeUnit chargeUnit;
		static DateTime chargeStart = DateTime.MinValue;
		static bool inputCleared;

		// Set on login
		static string key;
		static AuthenticationResult user;
		static DateTime expiration = DateTime.MinValue;

		// With all due respect to Sim City!
		static string[] loadingMessages =
		{
			"Adding Hidden Agendas",
			"Adjusting Bell Curves",
			"Aesthesizing Industrial Areas",
			"Aligning Covariance Matrices",
			"Applying Feng Shui Shaders",
			"Applying Theatre Soda Layer",
			"Asserting Packed Exemplars",
			"Attempting to Lock Back-Buffer",
			"Binding Sapling Root System",
			"Breeding Fauna",
			"Building Data Trees",
			"Bureacritizing Bureaucracies",
			"Calculating Inverse Probability Matrices",
			"Calculating Llama Expectoration Trajectory",
			"Calibrating Blue Skies",
			"Charging Ozone Layer",
			"Coalescing Cloud Formations",
			"Cohorting Exemplars",
			"Collecting Meteor Particles",
			"Compounding Inert Tessellations",
			"Compressing Fish Files",
			"Computing Optimal Bin Packing",
			"Concatenating Sub-Contractors",
			"Containing Existential Buffer",
			"Debarking Ark Ramp",
			"Debunching Unionized Commercial Services",
			"Deciding What Message to Display Next",
			"Decomposing Singular Values",
			"Decrementing Tectonic Plates",
			"Deleting Ferry Routes",
			"Depixelating Inner Mountain Surface Back Faces",
			"Depositing Slush Funds",
			"Destabilizing Economic Indicators",
			"Determining Width of Blast Fronts",
			"Deunionizing Bulldozers",
			"Dicing Models",
			"Diluting Livestock Nutrition Variables",
			"Downloading Satellite Terrain Data",
			"Exposing Flash Variables to Streak System",
			"Extracting Resources",
			"Factoring Pay Scale",
			"Fixing Election Outcome Matrix",
			"Flood-Filling Ground Water",
			"Flushing Pipe Network",
			"Gathering Particle Sources",
			"Generating Jobs",
			"Gesticulating Mimes",
			"Graphing Whale Migration",
			"Hiding Willio Webnet Mask",
			"Implementing Impeachment Routine",
			"Increasing Accuracy of RCI Simulators",
			"Increasing Magmafacation",
			"Initializing My Sim Tracking Mechanism",
			"Initializing Rhinoceros Breeding Timetable",
			"Initializing Robotic Click-Path AI",
			"Inserting Sublimated Messages",
			"Integrating Curves",
			"Integrating Illumination Form Factors",
			"Integrating Population Graphs",
			"Iterating Cellular Automata",
			"Lecturing Errant Subsystems",
			"Mixing Genetic Pool",
			"Modeling Object Components",
			"Mopping Occupant Leaks",
			"Normalizing Power",
			"Obfuscating Quigley Matrix",
			"Overconstraining Dirty Industry Calculations",
			"Partitioning City Grid Singularities",
			"Perturbing Matrices",
			"Pixalating Nude Patch",
			"Polishing Water Highlights",
			"Populating Lot Templates",
			"Preparing Sprites for Random Walks",
			"Prioritizing Landmarks",
			"Projecting Law Enforcement Pastry Intake",
			"Realigning Alternate Time Frames",
			"Reconfiguring User Mental Processes",
			"Relaxing Splines",
			"Removing Road Network Speed Bumps",
			"Removing Texture Gradients",
			"Removing Vehicle Avoidance Behavior",
			"Resolving GUID Conflict",
			"Reticulating Splines",
			"Retracting Phong Shader",
			"Retrieving from Back Store",
			"Reverse Engineering Image Consultant",
			"Routing Neural Network Infanstructure",
			"Scattering Rhino Food Sources",
			"Scrubbing Terrain",
			"Searching for Llamas",
			"Seeding Architecture Simulation Parameters",
			"Sequencing Particles",
			"Setting Advisor Moods",
			"Setting Inner Deity Indicators",
			"Setting Universal Physical Constants",
			"Sonically Enhancing Occupant-Free Timber",
			"Speculating Stock Market Indices",
			"Splatting Transforms",
			"Stratifying Ground Layers",
			"Sub-Sampling Water Data",
			"Synthesizing Gravity",
			"Synthesizing Wavelets",
			"Time-Compressing Simulator Clock",
			"Unable to Reveal Current Activity",
			"Weathering Buildings",
			"Zeroing Crime Network"
		};

		static void Main(string[] args)
		{
			ReaderHardware.Initialize();
			Draw.Loading("");
			var userEntryBuffer = "";
			var lastEntry = DateTime.MinValue;
			var seconds = -1;

			Connect();

			// Main activity loop
			while (true) {
				var newSeconds = (int)Math.Floor(
						(expiration - DateTime.Now).TotalSeconds);

				if (newSeconds > -5 && newSeconds != seconds) {
					if (warningBeep)
						ReaderHardware.Warn(newSeconds);

					Draw.Status(newSeconds);
				}

				seconds = newSeconds;

				// This blocks for 5ms waiting for user input
				var input = ReaderHardware.Read();

				if (!string.IsNullOrEmpty(input)) {
					inputCleared = false;
					lastEntry = DateTime.Now;
				}

				// We're not logged in
				if (seconds <= 0) {
					// Transition from logged in state.
					if (user != null)
						Logout();

					if (!inputCleared && DateTime.Now - lastEntry > new TimeSpan(0, 0, 30)) {
						ClearEntry();
						userEntryBuffer = "";
						inputCleared = true;
					}
				}
				// We're Logged in
				else {
					if (!inputCleared && DateTime.Now - lastEntry > new TimeSpan(0, 0, 30)) {
						Draw.User(user);
						userEntryBuffer = "";
						inputCleared = true;
					}
				}

				if (input.Length == 8) {
					ProcessCommand($"W26#{input}");
					userEntryBuffer = "";
				}
				else if (input.Length == 10) {
					ProcessCommand(input);
					userEntryBuffer = "";
				}
				else if (input.Length == 1) {
					switch (input[0]) {
						case 'A':
							ClearEntry();
							userEntryBuffer = "";
							break;
						case 'B':
							ProcessCommand(userEntryBuffer);
							userEntryBuffer = "";
							break;
						default:
							userEntryBuffer += input[0];
							var count = userEntryBuffer.Length;

							if (count > 10)
								count = 10;

							Draw.Entry("".PadLeft(count, '*'));
							break;
					}
				}
				else if (input.Length > 0) {
					Log.Message($"Received input unknown [{input.Length}]: {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input))} '{input}'");
				}
			}
		}

		static void Connect()
		{
			UpdateStatus();

			status.Id = 0;
			server?.Dispose();
			controller?.Dispose();
			server = null;
			controller = null;
			config = null;

			// Use a background thread to do the connect to avoid stalling UI
			var connectingThread = new Thread(ConnectThread);
			connectingThread.Start();

			// Spin the foreground thread to collect and throw away any user input while we are connecting. Also allow people to access the service menu if they need to.
			var inputBuffer = "";

			while (config == null) {
				var input = ReaderHardware.Read();

				if (input == "A")
					inputBuffer = "";
				else if (input == "B") {
					if (inputBuffer == ServiceMenuMagicCode) {
						inputBuffer = "";

						EnterServiceMenu();
					}
					else
						inputBuffer = "";
				}
				else
					inputBuffer += input;
			}

			inputCleared = true;

			Draw.Heading(config.Name, status.Warning);
			Draw.Status(-1, false);
			ForceLogout();
			ClearEntry();
		}

		static void ConnectThread()
		{
			while (true) {
				var random = new Random();

				var message = loadingMessages[random.Next(loadingMessages.Length - 1)];

				UpdateStatus();
				Draw.Loading(message);

				try {
					status.Id = int.Parse(File.ReadAllText("readerid.txt"));
				}
				catch (Exception ex) {
					Log.Exception(ex);

					Draw.Fatal("Reader ID is not set");

					Thread.Sleep(60000);
				}

				try {
					if (status.Id != 0) {
						server?.Dispose();
						server = new MilwaukeeMakerspaceApiClient(status);
						controller?.Dispose();
						controller = server;
					}
				}
				catch (Exception ex) {
					Log.Exception(ex);

					Draw.Fatal($"Cannot reach server\nReader IP: {status.Ip}");

					Thread.Sleep(2000);

					try {
						// Fall back to a local snapshot database if one exists.
						controller?.Dispose();
						controller = new LocalController(status);
						Log.Message("Falling back to local snapshot database.");
					}
					catch {
						// Wait some more, and loop around
						Thread.Sleep(20000);
					}
				}

				try {
					if (controller != null) {
						config = controller.Initialize();

						mode = ReaderMode.Access;
						warningBeep = true;
						cabinetItems?.Clear();

						try {
							var settings = JObject.Parse(config.Settings);

							switch (settings?["mode"]?.ToString()) {
								case "cabinet":
									mode = ReaderMode.Cabinet;
									break;
								case "sensor":
									mode = ReaderMode.Sensor;
									break;
								case "charge":
									mode = ReaderMode.Charge;
									break;
								default:
									mode = ReaderMode.Access;
									break;
							}

							warningBeep = (bool?)settings?["warn"] ?? true;

							switch (mode) {
								case ReaderMode.Cabinet:
									var itemsList = settings?["items"] as JArray;

									if (itemsList == null) {
										Draw.Fatal("Cannot Read Item List");

										continue;
									}

									cabinetItems = new Dictionary<int, string>(itemsList.Count);

									foreach (var item in itemsList) {
										cabinetItems.Add(int.Parse(item?["id"].ToString()), item?["name"].ToString());
									}
									break;
								case ReaderMode.Charge:
									chargePrompt = settings?["prompt"].ToString();

									var rate = settings?["rate"].ToString();

									decimal.TryParse(rate, out var cleanRate);

									chargeRate = cleanRate;

									if (cleanRate < 0.01m || cleanRate > 1000m) {
										Draw.Fatal("'Rate' is not set to a valid dollar value. Must be between $0.01 and $1000");

										continue;
									}

									switch (settings?["unit"]?.ToString()) {
										case "hour":
											chargeUnit = ChargeUnit.Hour;
											break;
										default:
											chargeUnit = ChargeUnit.Fixed;
											break;
									}

									break;
							}
						}
						catch  (Exception ex) {
							// Not a fatal error, try to keep going on the initialization.
							Log.Message("Error parsing reader settings.");
							Log.Exception(ex);
						}

						// Exit the loop after we've setup everything
						break;
					}
				}
				catch (Exception ex) {
					Log.Exception(ex);

					Draw.Fatal("Server does not recognise reader ID");

					Thread.Sleep(10000);
				}
			}
		}

		static void ClearEntry()
		{
			if (user != null)
				Draw.User(user);
			else if (config.Enabled)
				Draw.Prompt("Enter PIN or swipe fob");
			else
				Draw.Prompt("Login has been disabled");
		}

		static void ProcessCommand(string command)
		{
			if (command == ServiceMenuMagicCode) {
				EnterServiceMenu();

				Connect();
			}
			// Empty Input
			else if (command == "") {
				// Do Nothing
			}
			// Force Logout
			else if (command == "0") {
				ForceLogout();
			}
			// Login / Extend
			else {
				Login(command);
			}
		}

		static void ForceLogout()
		{
			expiration = DateTime.Now - new TimeSpan(0, 0, 1);

			Logout();
		}

		static void Login(string keyIn)
		{
			Draw.Prompt("Authenticating. . .");

			AuthenticationResult newUser;

			try {
				newUser = controller.Authenticate(keyIn);
			}
			catch (Exception ex) {
				Log.Exception(ex);

				if (ex.Message.Contains("403 (Forbidden)") || ex.Message.Contains("Invalid key"))
					Draw.Prompt("Invalid key");
				else
					Connect();

				return;
			}

			if (!newUser.AccessGranted) {
				Draw.Prompt("Expired membership");

				return;
			}

			inputCleared = true;
			key = keyIn;
			user = newUser;

			if (mode == ReaderMode.Cabinet) {
				EnterCabinetMenu();
			}
			else {
				if (mode == ReaderMode.Charge) {
					if (chargeStart == DateTime.MinValue) {
						if (EnterChargePrompt()) {
							if (chargeUnit == ChargeUnit.Fixed)
								controller.Charge(key, $"Accepted Fixed Charge of: '${chargeRate.ToString("0.00")}'", $"Fixed Charge from '{config.Name}'", chargeRate);
							else
								chargeStart = DateTime.Now;
						}
						else {
							Draw.Prompt("Charge Refused");

							return;
						}
					}
				}

				expiration = DateTime.Now + new TimeSpan(0, 0, config.Timeout);

				Draw.Heading(config.Name, status.Warning);
				Draw.Status(config.Timeout, false);
				Draw.User(user);
				ReaderHardware.Login();
				Log.Message("Login");
			}
		}

		static void Logout()
		{
			inputCleared = true;
			user = null;
			Draw.Heading(config.Name, status.Warning);
			Draw.Status(-1, false);
			ClearEntry();

			ReaderHardware.Logout();
			Log.Message("Logout");

			try {
				if (mode == ReaderMode.Charge
					&& chargeStart > DateTime.MinValue
					&& chargeUnit == ChargeUnit.Hour) {
					var endDate = DateTime.Now;

					var chargeTimespan = endDate - chargeStart;
					var chargeAmount = Math.Round((decimal)chargeTimespan.TotalHours * chargeRate, 2, MidpointRounding.ToPositiveInfinity);

					controller.Charge(key, $"Accepted Usage Charge of: '${chargeAmount.ToString("0.00")}' for '{chargeTimespan.ToString("hh\\:mm\\:ss")}'", $"Charged '{chargeTimespan.ToString("hh\\:mm\\:ss")}' time on '{config.Name}'", chargeAmount);

					chargeStart = DateTime.MinValue;
				}

				controller.Logout(key);
			}
			catch (Exception ex) {
				Log.Exception(ex);

				switch (ex.InnerException) {
					case HttpRequestException e when e.Message == "Response status code does not indicate success: 500 (Internal Server Error).":
						Log.Message("Breaking for server error");
						break;
					default:
						Connect();
						break;
				}

				return;
			}
		}

		static void EnterCabinetMenu()
		{
			var draw = true;
			var inputBuffer = "";

			Log.Message("Entering Cabinet Menu");

			if (cabinetItems == null) {
				Draw.MenuOverride = false;
				Draw.Fatal("Could not load cabinet items");

				Thread.Sleep(2000);

				return;
			}

			Draw.MenuOverride = true;

			while (true) {
				if (draw) {
					var menu = "";

					foreach (var item in cabinetItems) {
						menu += $"[{item.Key}] {item.Value}\n";
					}

					var entry = $"\nSelect a tool: {inputBuffer}";

					Draw.Cabinet(menu, entry);

					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case 'A':
						if (inputBuffer.Length > 0)
							inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);

						draw = true;
						break;
					case 'B':
						int.TryParse(inputBuffer, out var code);

						if (cabinetItems.ContainsKey(code)) {
							var item = cabinetItems[code];

							Draw.MenuOverride = false;
							Draw.Heading(config.Name, status.Warning);
							Draw.Prompt($"Selected: {item}");
							controller.Action(key, item);
							ReaderHardware.Output(code);
							Thread.Sleep(1000);
							ReaderHardware.Output(0);
							return;
						}
						else {
							inputBuffer = "";
							draw = true;
						}
						break;
					default:
						inputBuffer += input;
						draw = true;
						break;
				}
			}
		}

		static bool EnterChargePrompt()
		{
			Draw.MenuOverride = true;
			Draw.FullScreenPrompt(chargePrompt);

			while (true) {
				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case 'A':
						Draw.MenuOverride = false;
						Log.Message("Rejected Charge");

						return false;
					case 'B':
						Draw.MenuOverride = false;
						Log.Message("Accepted Charge");

						return true;
				}
			}
		}

		static void EnterServiceMenu()
		{
			Log.Message("Entering Service Menu");
			UpdateStatus();

			var draw = true;
			var trigger = false;

			while (true) {
				if (draw) {
					Draw.MenuOverride = true;
					Draw.Service($@"Version: {status.Version}
Uptime: {status.Uptime}
Hardware: {status.Hardware}
IP Address: {status.Ip}     Reader Id: {status.Id}
Server: {status.Server}     Controller: {status.Controller}
Snapshot: {status.LocalSnapshot}

[1] Set Reader Id	[2] Set Server
[3] Test Cabinet   [4] Toggle Trigger
[5] Update Reader	[6] Push Log Dump
[9] Next Page");

					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case 'A':
						Draw.MenuOverride = false;

						Draw.Loading("Reconnecting");
						Log.Message("Exiting Service Menu");

						return;
					case '1':
						EnterReaderId();
						break;
					case '2':
						EnterServer();
						break;
					case '3':
						EnterCabinetMenu();
						break;
					case '4':
						if (trigger) {
							trigger = false;
							ReaderHardware.Logout();
							Log.Message("Toggle Trigger - Off");
						}
						else {
							trigger = true;
							ReaderHardware.Login();
							Log.Message("Toggle Trigger - On");
						}

						break;
					case '5':
						Draw.Loading("Updating Software, please wait.");
						Log.Message("Updating Software");

						switch (ReaderHardware.Platform) {
							case HardwareType.OrangePi:
								Process.Start("bash", "-c \"cd /tmp; rm -f installOPi.sh; wget https://raw.githubusercontent.com/DanDude0/MilwaukeeMakerspacePiFobReader/master/installOPi.sh; chmod +x installOPi.sh; sudo ./installOPi.sh\"");
								break;
							case HardwareType.RaspberryPi:
								Process.Start("bash", "-c \"cd /tmp; rm -f installRPi.sh; wget https://raw.githubusercontent.com/DanDude0/MilwaukeeMakerspacePiFobReader/master/installRPi.sh; chmod +x installRPi.sh; sudo ./installRPi.sh\"");
								break;
						}

						Process.Start("systemctl", "stop MmsPiW26Interface");
						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
						break;
					case '6':
						PushLogDump();
						break;
					case '9':
						EnterServiceMenu2();
						break;
				}

				draw = true;
			}
		}

		static void EnterServiceMenu2()
		{
			Log.Message("Entering Service Menu 2nd Page");

			var draw = true;

			while (true) {
				if (draw) {
					Draw.MenuOverride = true;
					Draw.Service($@"[1] Reboot Reader   [2] Shutdown Reader
[3] Exit App
[4] Download Offline Database Snapshot
[5] Delete Offline Database Snapshot
[0] Previous Page");

					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case '0':
						Log.Message("Exiting Service Menu 2nd Page");

						return;
					case '1':
						Log.Message("Rebooting");

						Process.Start("reboot");
						Process.Start("systemctl", "stop MmsPiW26Interface");
						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
						break;
					case '2':
						Log.Message("Shutdown");

						Process.Start("shutdown", "-hP 0");
						Process.Start("systemctl", "stop MmsPiW26Interface");
						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
						break;
					case '3':
						Log.Message("Exiting");

						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
						break;
					case '4':
						Draw.Loading("Downloading Database Snapshot");
						Log.Message("Downloading Database Snapshot");
						//TODO: If we can talk to the server, push attempt history back up BEFORE we overwrite it.
						try {
							server.DownloadSnapshot();

							Draw.Service("Snapshot updated");
							Log.Message("Snapshot updated");

							Thread.Sleep(2000);
						}
						catch (Exception ex) {
							Draw.Service("Could not download snapshot");
							Log.Message("Could not download snapshot");
							Log.Exception(ex);

							Thread.Sleep(2000);
						}

						UpdateStatus();
						break;
					case '5':
						Draw.Loading("Deleting Database Snapshot");
						Log.Message("Deleting Database Snapshot");
						//TODO: If we can talk to the server, push attempt history back up BEFORE we overwrite it.
						try {
							File.Delete(LocalController.FileName);

							Draw.Service("Snapshot deleted");
							Log.Message("Snapshot deleted");

							Thread.Sleep(2000);
						}
						catch (Exception ex) {
							Draw.Service("Could not delete snapshot");
							Log.Message("Could not delete snapshot");
							Log.Exception(ex);

							Thread.Sleep(2000);
						}

						UpdateStatus();
						break;
				}

				draw = true;
			}
		}

		static void EnterReaderId()
		{
			var draw = true;
			var inputBuffer = "";

			while (true) {
				if (draw) {
					Draw.Service($@"Enter Reader Id Using Keypad

Reader Id: {inputBuffer}

[ENT] Save
[ESC] Cancel
");

					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case 'A':
						return;
					case 'B':
						int.TryParse(inputBuffer, out var id);

						if (id > 0) {
							status.Id = id;
							File.WriteAllText("readerid.txt", inputBuffer);
							Log.Message($"Set reader id to: {id}");
						}

						return;
					default:
						inputBuffer += input;
						draw = true;
						break;
				}
			}
		}

		static void EnterServer()
		{
			var draw = true;
			var inputBuffer = "";
			var segments = new string[] { "_", "x", "x", "x", "x" };
			int currentSegment = 0;

			while (true) {
				var complete = $"{segments[0]}.{segments[1]}.{segments[2]}.{segments[3]}:{segments[4]}";

				if (draw) {
					Draw.Service($@"Enter Server Using Keypad

Server: {complete}

[ENT] Next / Save
[ESC] Previous / Cancel 
");

					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				draw = true;

				switch (input[0]) {
					case 'A':
						inputBuffer = "";

						if (segments[currentSegment] != "x" && segments[currentSegment] != "_") {
							if (currentSegment < 4)
								segments[currentSegment + 1] = "x";

							segments[currentSegment] = "_";
						}
						else if (currentSegment > 0) {
							currentSegment -= 1;
							segments[currentSegment + 1] = "x";
							segments[currentSegment] = "_";
						}
						else
							return;
						break;
					case 'B':
						inputBuffer = "";

						if (segments[currentSegment] == "x" && segments[currentSegment] == "_") {
							segments[currentSegment] = "0";
						}

						if (currentSegment < 4) {
							currentSegment += 1;
							segments[currentSegment] = "_";
						}
						else {
							File.WriteAllText("server.txt", complete);
							Log.Message($"Set server to: {complete}");
							return;
						}
						break;
					default:
						inputBuffer += input;

						segments[currentSegment] = inputBuffer;
						break;
				}
			}
		}

		static void UpdateStatus()
		{
			status.Version = File.GetCreationTime("MmsPiFobReader.dll").ToString();
			status.Hardware = "SDL";
			status.LocalSnapshot = "No";

			if (File.Exists("/proc/device-tree/model"))
				status.Hardware = File.ReadAllText("/proc/device-tree/model");

			if (File.Exists("/etc/os-release"))
				status.Os = File.ReadAllText("/etc/os-release");

			if (File.Exists("/etc/armbian-release"))
				status.Os = File.ReadAllText("/etc/armbian-release");

			if (File.Exists(LocalController.FileName))
				status.LocalSnapshot = "Yes, " + GetCommandOutput("ls -l", LocalController.FileName);

			status.Kernel = GetCommandOutput("uname", "-a");
			status.Uptime = GetCommandOutput("uptime", "");
		}

		static void PushLogDump()
		{
			//TODO
		}

		static string GetCommandOutput(string command, string arguements)
		{
			try {
				var cliProcess = new Process() {
					StartInfo = new ProcessStartInfo(command, arguements) {
						UseShellExecute = false,
						RedirectStandardOutput = true
					}
				};
				cliProcess.Start();
				var cliOut = cliProcess.StandardOutput.ReadToEnd();
				cliProcess.WaitForExit(100);
				cliProcess.Close();

				return cliOut.Trim();
			}
			catch {
				return "";
			}
		}
	}
}
