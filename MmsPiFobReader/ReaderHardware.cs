using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SDL2;

namespace MmsPiFobReader
{
	public static class ReaderHardware
	{
		static ReaderHardware()
		{
#if RPI
			ReadW26.Initalize();
			WiringPi.pinMode(22, 1); // Equipment Trigger, Logged in enable
			WiringPi.pinMode(23, 1); // Equipment Trigger, Logged in enable
			WiringPi.pinMode(24, 1); // Equipment Trigger, Logged in disable
			WiringPi.pinMode(25, 1); // Equipment Trigger, Logged in disable
			WiringPi.pinMode(26, 1); // LED 
			WiringPi.pinMode(27, 1); // Beeper 
#endif
		}

		public static string Read()
		{
#if RPI
			return ReadW26.Read();
#else
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
#endif
		}

		public static void Login()
		{
#if RPI
			WiringPi.digitalWrite(22, 1);
			WiringPi.digitalWrite(23, 1);
			WiringPi.digitalWrite(24, 0);
			WiringPi.digitalWrite(25, 0);
			WiringPi.digitalWrite(26, 1);
			WiringPi.digitalWrite(27, 0);
#endif
		}

		public static void Logout()
		{
#if RPI
			warningThread?.Join();

			WiringPi.digitalWrite(24, 0);
			WiringPi.digitalWrite(25, 0);
			WiringPi.digitalWrite(24, 1);
			WiringPi.digitalWrite(25, 1);
			WiringPi.digitalWrite(26, 0);
			WiringPi.digitalWrite(27, 0);
#endif
		}

		public static void Warn(int seconds)
		{
#if RPI
			if (seconds < 60 && seconds > 1)
				WiringPi.digitalWrite(26, seconds % 2);

			if (seconds > 45 || seconds < 1)
				return;
			else if (seconds > 30)
				WarningLength = 10;
			else {
				WarningLength = 510 - (int)(Math.Log(seconds) * 147);
			}

			warningThread = new Thread(WarnThread);
			warningThread.Start();
#endif
		}

#if RPI
		private static Thread warningThread;
		private static int WarningLength;

		private static void WarnThread()
		{
			WiringPi.digitalWrite(27, 1);

			Thread.Sleep(WarningLength);

			WiringPi.digitalWrite(27, 0);
		}
#endif
	}
}
