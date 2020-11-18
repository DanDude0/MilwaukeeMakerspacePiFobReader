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
		static bool cabinetMode;
		static Dictionary<int, string> cabinetItems;
		static bool warningBeep;
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
				else if (input.Length > 10 && input[0] == 0x2) {
					// Detect and chop off start/stop bytes from an RS232 reader
					input = input.Substring(1, 10);

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
					Console.WriteLine($"Received input unknown [{input.Length}]: {input} {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input))}");
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
			Logout();
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
					}
					catch {
						// Wait some more, and loop around
						Thread.Sleep(20000);
					}
				}

				try {
					if (controller != null) {
						config = controller.Initialize();

						cabinetMode = false;
						warningBeep = true;
						cabinetItems?.Clear();

						try {
							var settings = JObject.Parse(config.Settings);

							cabinetMode = settings?["mode"]?.ToString() == "cabinet";
							warningBeep = (bool?)settings?["warn"] ?? true;

							if (cabinetMode) {
								var itemsList = settings?["items"] as JArray;

								if (itemsList == null) {
									Draw.Fatal("Cannot Read Item List");

									continue;
								}

								cabinetItems = new Dictionary<int, string>(itemsList.Count);

								foreach (var item in itemsList) {
									cabinetItems.Add(int.Parse(item?["id"].ToString()), item?["name"].ToString());
								}
							}
						}
						catch {
							// Do Nothing
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
			// Empty Input
			if (command == ServiceMenuMagicCode) {
				EnterServiceMenu();

				Connect();
			}
			else if (command == "") {
				// Do Nothing
			}
			// Force Logout
			else if (command == "0") {
				expiration = DateTime.Now - new TimeSpan(0, 0, 1);

				Logout();
			}
			// Login / Extend
			else {
				Login(command);
			}
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

			if (cabinetMode) {
				EnterCabinetMenu();
			}
			else {
				expiration = DateTime.Now + new TimeSpan(0, 0, config.Timeout);

				Draw.Status(config.Timeout, false);
				Draw.User(user);
				ReaderHardware.Login();
			}
		}

		static void Logout()
		{
			inputCleared = true;
			user = null;
			Draw.Status(-1, false);
			ClearEntry();

			ReaderHardware.Logout();

			try {
				controller.Logout(key);
			}
			catch (Exception ex) {
				Log.Exception(ex);

				switch (ex.InnerException) {
					case HttpRequestException e when e.Message == "Response status code does not indicate success: 500 (Internal Server Error).":
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

		static void EnterServiceMenu()
		{
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
[5] Update Reader	[6] Reboot Reader
[7] Shutdown Reader   [8] Exit App
[9] Download Snapshot");


					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case 'A':
						Draw.MenuOverride = false;

						Draw.Loading("Reconnecting");

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
						}
						else {
							trigger = true;
							ReaderHardware.Login();
						}

						break;
					case '5':
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
						Process.Start("reboot");
						Process.Start("systemctl", "stop MmsPiW26Interface");
						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
						break;
					case '7':
						Process.Start("shutdown", "-hP 0");
						Process.Start("systemctl", "stop MmsPiW26Interface");
						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
						break;
					case '8':
						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
						break;
					case '9':
						Draw.Loading("Downloading Database Snapshot");
						//TODO: If we can talk to the server, push attempt history back up BEFORE we overwrite it.
						try {
							server.DownloadSnapshot();

							Draw.Service("Snapshot updated");

							Thread.Sleep(2000);
						}
						catch {
							Draw.Service("Could not download snapshot");

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
