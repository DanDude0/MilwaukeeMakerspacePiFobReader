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
					WiringPi.pinMode(22, 1); // Equipment Trigger, Logged in enable
					WiringPi.pinMode(23, 1); // Equipment Trigger, Logged in enable
					WiringPi.pinMode(24, 1); // Equipment Trigger, Logged in disable
					WiringPi.pinMode(25, 1); // Equipment Trigger, Logged in disable
					WiringPi.pinMode(26, 1); // LED 
					WiringPi.pinMode(27, 1); // Beeper
					break;
				case HardwareType.RaspberryPi:
					W26SysFs.Initalize();
					WiringPi.wiringPiSetup();
					WiringPi.pinMode(22, 1); // Equipment Trigger, Logged in enable
					WiringPi.pinMode(23, 1); // Equipment Trigger, Logged in enable
					WiringPi.pinMode(24, 1); // Equipment Trigger, Logged in disable
					WiringPi.pinMode(25, 1); // Equipment Trigger, Logged in disable
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
