using System;
using System.IO;
using System.Threading;
using SDL2;

namespace MmsPiFobReader
{
	static class ReaderHardware
	{
		public static HardwareType Type { get; private set; }

		public static void Initialize()
		{
			// Default to SDL interface for cross platform desktop support
			Type = HardwareType.SDL;

			// Check for supported embedded platforms
			if (File.Exists("/proc/device-tree/model")) {
				var model = File.ReadAllText("/proc/device-tree/model");

				if (model.Contains("Orange Pi"))
					Type = HardwareType.OrangePi;
				else if (model.Contains("Raspberry Pi"))
					Type = HardwareType.RaspberryPi;
			}

			switch (Type) {
				case HardwareType.OrangePi:
					W26SysFs.Initalize();
					WiringPi.wiringPiSetup();
					WiringPi.pinMode(9, 1); // Equipment Trigger, 1
					WiringPi.pinMode(7, 1); // Equipment Trigger, 2
					WiringPi.pinMode(15, 1); // Equipment Trigger, 3
					WiringPi.pinMode(16, 1); // Equipment Trigger, 4
					WiringPi.pinMode(0, 1); // Equipment Trigger, 5
					WiringPi.pinMode(1, 1); // Equipment Trigger, 6
					WiringPi.pinMode(2, 1); // Equipment Trigger, 7
					WiringPi.pinMode(3, 1); // Equipment Trigger, 8
					WiringPi.pinMode(4, 1); // Equipment Trigger, 9
					WiringPi.pinMode(5, 1); // Equipment Trigger, 10
					WiringPi.pinMode(6, 1); // Equipment Trigger, 11
					WiringPi.pinMode(21, 1); // Equipment Trigger, 12
					WiringPi.pinMode(22, 1); // Equipment Trigger, Logged in enable, 13
					WiringPi.pinMode(23, 1); // Equipment Trigger, Logged in enable, 14
					WiringPi.pinMode(24, 1); // Equipment Trigger, Logged in disable, 15
					WiringPi.pinMode(25, 1); // Equipment Trigger, Logged in disable, 16
					WiringPi.pinMode(26, 1); // LED 
					WiringPi.pinMode(27, 1); // Beeper
					break;
				case HardwareType.RaspberryPi:
					W26SysFs.Initalize();
					WiringPi.wiringPiSetup();
					WiringPi.pinMode(9, 1); // Equipment Trigger, 1
					WiringPi.pinMode(7, 1); // Equipment Trigger, 2
					WiringPi.pinMode(15, 1); // Equipment Trigger, 3
					WiringPi.pinMode(16, 1); // Equipment Trigger, 4
					WiringPi.pinMode(0, 1); // Equipment Trigger, 5
					WiringPi.pinMode(1, 1); // Equipment Trigger, 6
					WiringPi.pinMode(2, 1); // Equipment Trigger, 7
					WiringPi.pinMode(3, 1); // Equipment Trigger, 8
					WiringPi.pinMode(4, 1); // Equipment Trigger, 9
					WiringPi.pinMode(5, 1); // Equipment Trigger, 10
					WiringPi.pinMode(6, 1); // Equipment Trigger, 11
					WiringPi.pinMode(21, 1); // Equipment Trigger, 12
					WiringPi.pinMode(22, 1); // Equipment Trigger, Logged in enable, 13
					WiringPi.pinMode(23, 1); // Equipment Trigger, Logged in enable, 14
					WiringPi.pinMode(24, 1); // Equipment Trigger, Logged in disable, 15
					WiringPi.pinMode(25, 1); // Equipment Trigger, Logged in disable, 16
					WiringPi.pinMode(26, 1); // LED 
					WiringPi.pinMode(27, 1); // Beeper
					break;
			}

			Logout();
		}

		public static string Read()
		{
			switch (Type) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					return W26SysFs.Read();
				default:
					Thread.Sleep(5);

					SDL2.SDL.SDL_PollEvent(out var pollEvent);

					if (pollEvent.type == SDL.SDL_EventType.SDL_KEYDOWN) {
						var keycode = pollEvent.key.keysym.sym;

						switch (keycode) {
							case SDL.SDL_Keycode.SDLK_HASH:
							case SDL.SDL_Keycode.SDLK_RETURN:
							case SDL.SDL_Keycode.SDLK_RETURN2:
								return "#";
							case SDL.SDL_Keycode.SDLK_ASTERISK:
							case SDL.SDL_Keycode.SDLK_BACKSPACE:
								return "*";
							case SDL.SDL_Keycode.SDLK_ESCAPE:
								Environment.Exit(0);
								break;
						}

						var character = (char)keycode;

						// Numerals
						if (character > 47 && character < 58)
							return character.ToString();
					}
					else if (pollEvent.type == SDL.SDL_EventType.SDL_WINDOWEVENT) {
						if (pollEvent.window.windowEvent ==
							SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
							Environment.Exit(0);
					}

					return "";
			}
		}

		public static void Login()
		{
			switch (Type) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					WiringPi.digitalWrite(22, 1);
					WiringPi.digitalWrite(23, 1);
					WiringPi.digitalWrite(24, 0);
					WiringPi.digitalWrite(25, 0);
					WiringPi.digitalWrite(26, 1);
					WiringPi.digitalWrite(27, 0);
					break;
			}
		}

		public static void Logout()
		{
			switch (Type) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					warningThread?.Join();

					WiringPi.digitalWrite(22, 0);
					WiringPi.digitalWrite(23, 0);
					WiringPi.digitalWrite(24, 1);
					WiringPi.digitalWrite(25, 1);
					WiringPi.digitalWrite(26, 0);
					WiringPi.digitalWrite(27, 0);
					break;
			}
		}

		public static void Output(int i)
		{
			switch (Type) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					WiringPi.digitalWrite(9, 0); // Equipment Trigger, 1
					WiringPi.digitalWrite(7, 0); // Equipment Trigger, 2
					WiringPi.digitalWrite(15, 0); // Equipment Trigger, 3
					WiringPi.digitalWrite(16, 0); // Equipment Trigger, 4
					WiringPi.digitalWrite(0, 0); // Equipment Trigger, 5
					WiringPi.digitalWrite(1, 0); // Equipment Trigger, 6
					WiringPi.digitalWrite(2, 0); // Equipment Trigger, 7
					WiringPi.digitalWrite(3, 0); // Equipment Trigger, 8
					WiringPi.digitalWrite(4, 0); // Equipment Trigger, 9
					WiringPi.digitalWrite(5, 0); // Equipment Trigger, 10
					WiringPi.digitalWrite(6, 0); // Equipment Trigger, 11
					WiringPi.digitalWrite(21, 0); // Equipment Trigger, 12
					WiringPi.digitalWrite(22, 0); // Equipment Trigger, 13
					WiringPi.digitalWrite(23, 0); // Equipment Trigger, 14
					WiringPi.digitalWrite(24, 0); // Equipment Trigger, 15
					WiringPi.digitalWrite(25, 0); // Equipment Trigger, 16

					switch (i) {
						case 1:
							WiringPi.digitalWrite(9, 1); // Equipment Trigger, 1
							break;
						case 2:
							WiringPi.digitalWrite(7, 1); // Equipment Trigger, 2
							break;
						case 3:
							WiringPi.digitalWrite(15, 1); // Equipment Trigger, 3
							break;
						case 4:
							WiringPi.digitalWrite(16, 1); // Equipment Trigger, 4
							break;
						case 5:
							WiringPi.digitalWrite(0, 1); // Equipment Trigger, 5
							break;
						case 6:
							WiringPi.digitalWrite(1, 1); // Equipment Trigger, 6
							break;
						case 7:
							WiringPi.digitalWrite(2, 1); // Equipment Trigger, 7
							break;
						case 8:
							WiringPi.digitalWrite(3, 1); // Equipment Trigger, 8
							break;
						case 9:
							WiringPi.digitalWrite(4, 1); // Equipment Trigger, 9
							break;
						case 10:
							WiringPi.digitalWrite(5, 1); // Equipment Trigger, 10
							break;
						case 11:
							WiringPi.digitalWrite(6, 1); // Equipment Trigger, 11
							break;
						case 12:
							WiringPi.digitalWrite(21, 1); // Equipment Trigger, 12
							break;
						case 13:
							WiringPi.digitalWrite(22, 1); // Equipment Trigger, 13
							break;
						case 14:
							WiringPi.digitalWrite(23, 1); // Equipment Trigger, 14
							break;
						case 15:
							WiringPi.digitalWrite(24, 1); // Equipment Trigger, 15
							break;
						case 16:
							WiringPi.digitalWrite(25, 1); // Equipment Trigger, 16
							break;
					}
					break;
			}
		}

		public static void Warn(int seconds)
		{
			switch (Type) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					if (seconds < 60 && seconds > 1)
						WiringPi.digitalWrite(26, seconds % 2);

					if (seconds > 45 || seconds < 1)
						return;
					else if (seconds > 30)
						WarningLength = 15;
					else {
						WarningLength = 510 - (int)(Math.Log(seconds) * 147);
					}

					warningThread = new Thread(WarnThread);
					warningThread.Start();
					break;
			}
		}

		private static Thread warningThread;
		private static int WarningLength;

		private static void WarnThread()
		{
			WiringPi.digitalWrite(27, 1);

			Thread.Sleep(WarningLength);

			WiringPi.digitalWrite(27, 0);
		}
	}
}
