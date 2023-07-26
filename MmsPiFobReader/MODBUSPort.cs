using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Gpio;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MmsPiFobReader
{
	static class MODBUSPort
	{
		private static SerialPort serialPort;
		private static GpioController gpio;
		private static byte[] buffer;
		private static int transmitEnablePin = 0;

		public static void Initalize(string device, GpioController gpio, int transmitEnablePin)
		{
			serialPort = new SerialPort(device, 115200, Parity.Even, 8, StopBits.One);
			MODBUSPort.transmitEnablePin = transmitEnablePin;
			MODBUSPort.gpio = gpio;
			serialPort.WriteTimeout = 2000;
			serialPort.ReadTimeout = 500;
			serialPort.Open();
			buffer = new byte[256];
		}

		public static byte[] ModBusMessage(byte[] outgoing, int timeout_ms)
		{
			if (gpio == null) {
				Log.Message("MODBUSPort tried to send message when it was not initialized");

				return null;
			}

			byte[] response = new byte[256]; // maximum modbus packet length
			try {
				gpio.Write(transmitEnablePin, PinValue.High);
				Thread.Sleep(1);
				//gpio.Write(receiveEnablePin, PinValue.High);
				serialPort.Write(outgoing, 0, outgoing.Length);
				Thread.Sleep(2);
			}
			catch {
				// serial port error
				return null;
			}

			gpio.Write(transmitEnablePin, PinValue.Low);
			//gpio.Write(receiveEnablePin, PinValue.Low);

			int readCount = 0;
			long ticksBegin = DateTime.Now.Ticks;
			serialPort.ReadTimeout = 100;
			long ticksNow = DateTime.Now.Ticks;
			TimeSpan elapsedSpan;
			do {
				try {
					readCount += serialPort.Read(response, readCount, 256 - readCount);
				}
				catch (Exception e) {
					if (e.GetType() != typeof(TimeoutException)) {
						// serial port error
						return null;
					}
				}

				if (readCount > 3) {
					byte[] crc = CRC16_MODBUS.fn_makeCRC16_byte(response, 2);
					if (crc[0] == response[response.Length - 2] && crc[1] == response[response.Length - 1]) {
						// good message
						return response[0..readCount];
					}
				}
				ticksNow = DateTime.Now.Ticks;
				elapsedSpan = new TimeSpan(ticksNow - ticksBegin);
			} while (elapsedSpan.TotalMilliseconds < timeout_ms);

			return null;
		}
	}
}
