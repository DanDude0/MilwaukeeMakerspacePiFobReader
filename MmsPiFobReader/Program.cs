using System;
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
		const string ServiceMenuMagicCode = "14725369#";

		static MilwaukeeMakerspaceApiClient server;
		static int id;
		static DateTime expiration = DateTime.MinValue;
		static AuthenticationResult user;
		static ReaderResult reader;
		static JObject settings;
		static bool cabinetMode;
		static bool inputCleared;

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
						Draw.Prompt("Enter PIN or swipe fob");
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
				else if (input.Length == 1) {
					switch (input[0]) {
						case '*':
							Draw.Prompt("Enter PIN or swipe fob");
							userEntryBuffer = "";
							break;
						case '#':
							ProcessCommand($"{userEntryBuffer}#");
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
			}
		}

		static void Connect()
		{
			id = 0;
			server = null;
			reader = null;

			// Use a background thread to do the connect to avoid stalling UI
			var connectingThread = new Thread(ConnectThread);
			connectingThread.Start();

			// Spin the foreground thread to collect and throw away any user input while we are connecting.
			var inputBuffer = "";

			while (reader == null) {
				var input = ReaderHardware.Read();

				if (input == "*")
					inputBuffer = "";
				else
					inputBuffer += input;

				if (inputBuffer == ServiceMenuMagicCode) {
					inputBuffer = "";

					EnterServiceMenu(false);
				}
			}

			inputCleared = true;
			Draw.Heading(reader.Name);
			Draw.Status(-1, false);
			Logout();

			if (user != null)
				Draw.User(user);
			else if (reader.Enabled)
				Draw.Prompt("Enter PIN or swipe fob");
			else
				Draw.Prompt("Login has been disabled");
		}

		static void ConnectThread()
		{
			while (true) {
				var random = new Random();

				var message = loadingMessages[random.Next(loadingMessages.Length - 1)];

				Draw.Loading(message);

				try {
					id = int.Parse(File.ReadAllText("readerid.txt"));
				}
				catch (Exception ex) {
					Log.Exception(ex);

					Draw.Fatal("Reader ID is not set");
				}

				try {
					if (id != 0)
						server = new MilwaukeeMakerspaceApiClient();
				}
				catch (Exception ex) {
					Log.Exception(ex);

					Draw.Fatal("Cannot reach server");
				}

				try {
					if (server != null) {
						reader = server.ReaderLookup(id);
						settings = JObject.Parse(reader.Settings);
						cabinetMode = settings?["mode"]?.ToString() == "cabinet";

						// Exit the loop after we've setup everything
						break;
					}
				}
				catch (Exception ex) {
					Log.Exception(ex);

					Draw.Fatal("Server does not recognise reader ID");
				}

				Thread.Sleep(2000);
			}
		}

		static void ProcessCommand(string command)
		{
			// Empty Input
			if (command == ServiceMenuMagicCode) {
				EnterServiceMenu(true);
			}
			else if (command == "#") {
				// Do Nothing
			}
			// Force Logout
			else if (command == "0#") {
				expiration = DateTime.Now - new TimeSpan(0, 0, 1);

				Logout();
			}
			// Login / Extend
			else {
				Login(command);
			}
		}

		static void Login(string key)
		{
			Draw.Prompt("Authenticating. . .");

			AuthenticationResult newUser;

			try {
				newUser = server.Authenticate(id, key);
			}
			catch (Exception ex) {
				Log.Exception(ex);

				switch (ex.InnerException) {
					case HttpRequestException e when e.Message == "Response status code does not indicate success: 500 (Internal Server Error).":
						Draw.Prompt("Invalid key");
						break;
					default:
						Connect();
						break;
				}

				return;
			}

			if (!newUser.AccessGranted) {
				Draw.Prompt("Expired membership");

				return;
			}

			string tool = null;

			if (cabinetMode) {
				tool = EnterCabinetMenu();

				Draw.Heading(reader.Name);
			}

			inputCleared = true;
			expiration = DateTime.Now + new TimeSpan(0, 0, reader.Timeout);
			user = newUser;
			Draw.Status(reader.Timeout, false);

			if (cabinetMode) {
				Draw.Prompt($"Selected: {tool}");
			}
			else {
				Draw.User(user);
				ReaderHardware.Login();
			}
		}

		static void Logout()
		{
			inputCleared = true;
			user = null;
			Draw.Status(-1, false);
			Draw.Prompt("Enter PIN or swipe fob");

			if (cabinetMode)
				ReaderHardware.Output(0);
			else
				ReaderHardware.Logout();

			try {
				server.Logout(id);
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

		static string EnterCabinetMenu()
		{
			var draw = true;
			var inputBuffer = "";
			var items = settings?["items"] as JArray;

			if (items == null) {
				Draw.Prompt("Cannot Read Item List");

				return "Cannot Read Item List";
			}

			Draw.MenuOverride = true;

			while (true) {
				if (draw) {
					var menu = $"Select a tool:\n";

					foreach (var item in items) {
						menu += $"[{item?["id"]}] {item?["name"]}\n";
					}

					if (inputBuffer.Length > 0)
						menu += $"\nSelected: {inputBuffer}";

					Draw.Service(menu);

					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case '*':
						if (inputBuffer.Length > 0)
							inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);

						draw = true;
						break;
					case '#':
						id = int.Parse(inputBuffer);

						var item = items?[id - 1]?["name"]?.ToString();

						ReaderHardware.Output(id);
						server.Action(id, inputBuffer);
						Draw.MenuOverride = false;

						return item;
					default:
						inputBuffer += input;
						draw = true;
						break;
				}
			}
		}

		static void EnterServiceMenu(bool reconnectOnExit)
		{
			Draw.MenuOverride = true;

			var version = File.GetCreationTime("MmsPiFobReader.dll");
			var hardware = "SDL";

			if (File.Exists("/proc/device-tree/model"))
				hardware = File.ReadAllText("/proc/device-tree/model");

			var ip = MilwaukeeMakerspaceApiClient.GetLocalIp4Address();
			var draw = true;

			while (true) {
				var serverAddress = server?.Server;

				if (serverAddress == null && File.Exists("server.txt"))
					serverAddress = File.ReadAllText("server.txt").Replace("\n", "");

				if (draw) {
					Draw.Service($@"Version: {version}
Hardware: {hardware}
IP Address: {ip}
Reader Id: {id}
Server: {serverAddress}

[1] Set Reader Id
[2] Set Server
[3] Reboot Reader
[4] Shutdown Reader
[5] Exit Reader Application");

					draw = false;
				}

				var input = ReaderHardware.Read();

				if (input.Length != 1)
					continue;

				switch (input[0]) {
					case '*':
						Draw.MenuOverride = false;

						if (reconnectOnExit)
							Connect();

						return;
					case '1':
						EnterReaderId();
						break;
					case '2':
						EnterServer();
						break;
					case '3':
						Process.Start("reboot");
						Environment.Exit(0);
						break;
					case '4':
						Process.Start("shutdown", "-hP 0");
						Environment.Exit(0);
						break;
					case '5':
						Process.Start("systemctl", "stop MmsPiFobReader");
						Environment.Exit(0);
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
					case '*':
						return;
					case '#':
						id = int.Parse(inputBuffer);
						File.WriteAllText("readerid.txt", inputBuffer);
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
					case '*':
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
					case '#':
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
	}
}
