using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using SDL2;

namespace MmsPiFobReader
{
	static class ReaderHardware
	{
		public static HardwareType Platform { get; private set; }
		public static ScreenType Screen { get; private set; }
		public static bool InvertScreen;

		private static bool modbus_mode = false;

		private static GpioController gpio;

		private static int address0Pin = 0;
		private static int address1Pin = 0;
		private static int address2Pin = 0;
		private static int address3Pin = 0;
		private static int address4Pin = 0;
		private static int address5Pin = 0;
		private static int address6Pin = 0;
		private static int address7Pin = 0;

		private static int triggerPin = 0;

		private static int ledPin = 0;
		private static int beeperPin = 0;

		private static int transmitEnablePin = 0;

		public static void Initialize()
		{
			// Default to SDL interface for cross platform desktop support
			Platform = HardwareType.PC;
			Screen = ScreenType.SDLWindow480x360;

			// Check for supported embedded platforms
			if (File.Exists("/proc/device-tree/model")) {
				var model = File.ReadAllText("/proc/device-tree/model");

				if (model.Contains("Orange Pi"))
					Platform = HardwareType.OrangePi;
				else if (model.Contains("Raspberry Pi"))
					Platform = HardwareType.RaspberryPi;
			}

			// Check for supported embedded screens
			if (File.Exists("/sys/class/graphics/fb1/virtual_size")) {
				var size = File.ReadAllText("/sys/class/graphics/fb1/virtual_size");

				switch (size) {
					case "480,320":
						Screen = ScreenType.FB480x360;
						break;
					case "800,480":
						Screen = ScreenType.FB800x480;
						break;
				}
			}
			else if (File.Exists("/sys/class/graphics/fb0/virtual_size")) {
				var size = File.ReadAllText("/sys/class/graphics/fb0/virtual_size");

				switch (size) {
					case "480,320":
						Screen = ScreenType.FB480x360;
						break;
					case "800,480":
						Screen = ScreenType.FB800x480;
						break;
				}
			}
			// Check for supported embedded screens
			if (File.Exists("/sys/class/graphics/fb1/virtual_size")) {
				var size = File.ReadAllText("/sys/class/graphics/fb1/virtual_size");

				switch (size) {
					case "480,320":
						Screen = ScreenType.FB480x360;
						break;
					case "800,480":
						Screen = ScreenType.FB800x480;
						break;
				}
			}
			else if (File.Exists("/sys/class/graphics/fb0/virtual_size")) {
				var size = File.ReadAllText("/sys/class/graphics/fb0/virtual_size");

				switch (size) {
					case "480,320":
						Screen = ScreenType.FB480x360;
						break;
					case "800,480":
						Screen = ScreenType.FB800x480;
						break;
				}
			}

			switch (Platform) {
				case HardwareType.OrangePi:
					RS232Port.Initalize("/dev/ttyS3");

					address0Pin = 11;
					address1Pin = 6;
					address2Pin = 0;
					address3Pin = 3;
					address4Pin = 7;
					address5Pin = 8;
					address6Pin = 10;
					address7Pin = 20;
					triggerPin = 9;
					ledPin = 200;
					beeperPin = 201;
					transmitEnablePin = 68;
					break;
				case HardwareType.RaspberryPi:
					RS232Port.Initalize("/dev/serial0");

					address0Pin = 3;
					address1Pin = 4;
					address2Pin = 27;
					address3Pin = 22;
					address4Pin = 5;
					address5Pin = 6;
					address6Pin = 19;
					address7Pin = 26;
					triggerPin = 13;
					ledPin = 12;
					beeperPin = 16;
					transmitEnablePin = 23;
					break;
			}

			switch (Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					W26Pipe.Initalize();

					var driver = new LibGpiodDriver(0);
					gpio = new GpioController(PinNumberingScheme.Logical, driver);

					gpio.OpenPin(address0Pin, PinMode.Output);
					gpio.OpenPin(address1Pin, PinMode.Output);
					gpio.OpenPin(address2Pin, PinMode.Output);
					gpio.OpenPin(address3Pin, PinMode.Output);
					gpio.OpenPin(address4Pin, PinMode.Output);
					gpio.OpenPin(address5Pin, PinMode.Output);
					gpio.OpenPin(address6Pin, PinMode.Output);
					gpio.OpenPin(address7Pin, PinMode.Output);
					gpio.OpenPin(triggerPin, PinMode.Output);
					gpio.OpenPin(ledPin, PinMode.Output);
					gpio.OpenPin(beeperPin, PinMode.Output);
					gpio.OpenPin(transmitEnablePin, PinMode.Output);
					break;
			}

			Logout();
		}

		/// <summary>
		/// Read user input from whatever devices we can.
		/// </summary>
		/// <returns>Must never return null string, use an empty string if you must.</returns>
		public static string Read()
		{
			switch (Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					var output = W26Pipe.Read();

					if (string.IsNullOrEmpty(output) && !modbus_mode) {
						output = RS232Port.Read();
					}

					if (!string.IsNullOrEmpty(output))
						Log.Message($"Received raw input [{output.Length}]: {Convert.ToBase64String(System.Text.Encoding.Latin1.GetBytes(output))} '{output}'");

					return output;
				default:
					Thread.Sleep(5);

					SDL2.SDL.SDL_PollEvent(out var pollEvent);

					if (pollEvent.type == SDL.SDL_EventType.SDL_KEYDOWN) {
						var keycode = pollEvent.key.keysym.sym;

						switch (keycode) {
							case SDL.SDL_Keycode.SDLK_HASH:
							case SDL.SDL_Keycode.SDLK_RETURN:
							case SDL.SDL_Keycode.SDLK_RETURN2:
								return "B";
							case SDL.SDL_Keycode.SDLK_ASTERISK:
							case SDL.SDL_Keycode.SDLK_BACKSPACE:
								return "A";
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
			switch (Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					// For historical reasons, if we've not in cabinet mode, addresses 7,6,5 are treated as additional triggers.
					gpio.Write(new PinValuePair[] {
						new PinValuePair(address7Pin, PinValue.High),
						new PinValuePair(address6Pin, PinValue.High),
						new PinValuePair(address5Pin, PinValue.High),
						new PinValuePair(triggerPin, PinValue.High),
						new PinValuePair(ledPin, PinValue.High),
						new PinValuePair(beeperPin, PinValue.Low),
						new PinValuePair(transmitEnablePin, PinValue.High)
					});
					break;
			}
		}

		public static void Logout()
		{
			switch (Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					warningThread?.Join();

					Output(0);
					break;
			}
		}

		public static void Output(int i)
		{
			switch (Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					// Make sure this turns off first, before we 0 the address pins
					gpio.Write(triggerPin, PinValue.Low);

					gpio.Write(new PinValuePair[] {
						new PinValuePair(address0Pin, PinValue.Low),
						new PinValuePair(address1Pin, PinValue.Low),
						new PinValuePair(address2Pin, PinValue.Low),
						new PinValuePair(address3Pin, PinValue.Low),
						new PinValuePair(address4Pin, PinValue.Low),
						new PinValuePair(address5Pin, PinValue.Low),
						new PinValuePair(address6Pin, PinValue.Low),
						new PinValuePair(address7Pin, PinValue.Low),
						new PinValuePair(ledPin, PinValue.Low),
						new PinValuePair(beeperPin, PinValue.Low),
					});

					if ((i & 1) > 0)
						gpio.Write(address0Pin, PinValue.High);

					if ((i & 2) > 0)
						gpio.Write(address1Pin, PinValue.High);

					if ((i & 4) > 0)
						gpio.Write(address2Pin, PinValue.High);

					if ((i & 8) > 0)
						gpio.Write(address3Pin, PinValue.High);

					if ((i & 16) > 0)
						gpio.Write(address4Pin, PinValue.High);

					if ((i & 32) > 0)
						gpio.Write(address5Pin, PinValue.High);

					if ((i & 64) > 0)
						gpio.Write(address6Pin, PinValue.High);

					if ((i & 128) > 0)
						gpio.Write(address7Pin, PinValue.High);

					// Make sure this turns on last, after we set the address pins
					if (i > 0)
						gpio.Write(triggerPin, PinValue.High);

					break;
			}
		}

		public static void Warn(int seconds)
		{
			switch (Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					if (seconds < 60 && seconds > 1)
						gpio.Write(ledPin, seconds % 2);

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

		public static void SetModBusMode()
		{
			RS232Port.Close();
			switch (Platform) {
				case HardwareType.OrangePi:
					MODBUSPort.Initalize("/dev/ttyS3", gpio, transmitEnablePin);
					break;
				case HardwareType.RaspberryPi:
					MODBUSPort.Initalize("/dev/serial0", gpio, transmitEnablePin);
					break;
			}
			modbus_mode = true;
		}

		public static bool ModbusOutput(int board_id)
		{
			byte[] msg = new byte[8];
			msg[0] = (byte)board_id; // dest id,
			msg[1] = 5; // 0x05 = function (write single coil)
			msg[2] = 0; // coil addr high byte
			msg[3] = 0; // coil addr low byte
			msg[4] = 255; // 0xFF value high byte (0xFF in high byte = set)
			msg[5] = 0; // 0x00 value low byte, (0xFF in low byte = clear)
			byte[] crc = CRC16_MODBUS.fn_makeCRC16_byte(msg, 2); 
			msg[6] = crc[0]; 
			msg[7] = crc[1];
			byte[] response = MODBUSPort.ModBusMessage(msg, 1000);
			if (response == null) {
				return false;
			}
			return Enumerable.SequenceEqual(msg, response);
		}

		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
		public static int ModbusInput(int board_id, int input_ch)
		{
			if (input_ch < 1 || input_ch > 2) { return -1; }
			input_ch--;
			byte[] msg = new byte[8];
			msg[0] = (byte)board_id;
			msg[1] = 2;
			msg[2] = 0;
			msg[3] = 0;
			msg[4] = 0;
			msg[5] = 2;
			byte[] crc = CRC16_MODBUS.fn_makeCRC16_byte(msg, 2);
			msg[6] = crc[0];
			msg[7] = crc[1];
			byte[] response = MODBUSPort.ModBusMessage(msg, 1000);
			if (response == null) { return -1; }
			if (response.Length != 6) { return -1; }
			byte inputs = response[3];
			if (((inputs >> input_ch) & 1) != 0) {
				return 1;
			}
			else {
				return 0;
			}
		}


		private static Thread warningThread;
		private static int WarningLength;

		private static void WarnThread()
		{
			gpio.Write(beeperPin, PinValue.High);

			Thread.Sleep(WarningLength);

			gpio.Write(beeperPin, PinValue.Low);
		}
	}
}
