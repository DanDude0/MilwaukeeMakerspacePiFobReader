using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace MmsPiFobReader
{
	class Screen
	{
#if LINUX
		FileStream frameBuffer = new FileStream("/dev/fb1", FileMode.Append);
#endif
		Image<Bgr565> buffer = new Image<Bgr565>(480, 320);

		public Screen()
		{
			// We're taking over the screen
			DisableConsole();

			// Handle SigTerm
			Console.CancelKeyPress += EnableConsole;
		}

		public void Mutate(Action<IImageProcessingContext<Bgr565>> operation)
		{
			buffer.Mutate(operation);
			Draw();
		}

		private static void DisableConsole()
		{
#if LINUX
			File.WriteAllText("/sys/class/vtconsole/vtcon1/bind", "0");
#endif
		}

		private static void EnableConsole(object sender, EventArgs e)
		{
#if LINUX
			File.WriteAllText("/sys/class/vtconsole/vtcon1/bind", "1");
			Process.Start("setupcon");
#endif
		}

		private void Draw()
		{
#if LINUX
			var bytes = MemoryMarshal.AsBytes(buffer.GetPixelSpan());

			frameBuffer.Seek(0, SeekOrigin.Begin);
			frameBuffer.Write(bytes);
#else
			var file = new FileStream("C:\\temp\\example.bmp", FileMode.Create);

			buffer.SaveAsBmp(file);
#endif
		}
	}
}
