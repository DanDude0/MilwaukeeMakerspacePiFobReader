using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using SDL2;
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
		Image<Bgra32> buffer = new Image<Bgra32>(800, 480);
		byte[] currentFrame;
		byte[] pendingFrame;
		FileStream frameBuffer;
		object frameLock = new object();
		IntPtr window;
		IntPtr renderer;
		IntPtr texture;
		Thread drawThread;

		public Screen()
		{
			switch (ReaderHardware.Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					if (ReaderHardware.Platform == HardwareType.OrangePi)
						frameBuffer = new FileStream("/dev/fb0", FileMode.Append);
					else if (ReaderHardware.Platform == HardwareType.RaspberryPi)
						frameBuffer = new FileStream("/dev/fb1", FileMode.Append);

					// We're taking over the screen
					DisableConsole();

					// Handle SigTerm
					Console.CancelKeyPress += EnableConsole;
					AssemblyLoadContext.Default.Unloading += EnableConsole;
					break;
				default:
					// Make a desktop window to draw the screen contents to
					SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING);
					//SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP |
					window = SDL.SDL_CreateWindow("MmsPiFobReader", 50, 50, 800, 480, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);
					renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
					SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
					SDL.SDL_RenderClear(renderer);
					SDL.SDL_RenderPresent(renderer);
					SDL.SDL_ShowCursor(SDL.SDL_DISABLE);
					texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 800, 480);
					break;
			}
		}

		public void Mutate(Action<IImageProcessingContext> operation, bool draw = true)
		{
			buffer.Mutate(operation);

			if (draw)
				Draw();
		}

		private void DisableConsole()
		{
			File.AppendAllText("/sys/class/vtconsole/vtcon1/bind", "0");

			drawThread = new Thread(DrawThread);
			drawThread.Start();
		}

		private void EnableConsole(AssemblyLoadContext obj)
		{
			EnableConsole(null, null);
		}

		private void EnableConsole(object sender, EventArgs e)
		{
			drawThread.Abort();

			File.AppendAllText("/sys/class/vtconsole/vtcon1/bind", "1");
			Process.Start("setupcon");
		}

		private unsafe void Draw()
		{
			var span = buffer.GetPixelSpan();

			byte[] bytes = MemoryMarshal.AsBytes(span).ToArray();

			switch (ReaderHardware.Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					// This is a sort of double buffering to make up for low device frame rate.
					lock (frameLock) {
						if (currentFrame == null) {
							currentFrame = bytes;
						}
						else {
							pendingFrame = bytes;
						}
					}
					drawThread.Interrupt();

					break;
				default:
					fixed (byte *pointer = bytes) {
						SDL.SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)pointer, 800 * 4);
					}

					SDL.SDL_RenderClear(renderer);
					SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
					SDL.SDL_RenderPresent(renderer);
					break;
			}
		}

		private void DrawThread()
		{
			while (true) {
				try {
					if (currentFrame == null)
						Thread.Sleep(10000);

					frameBuffer.Seek(0, SeekOrigin.Begin);
					frameBuffer.Write(currentFrame);

					lock (frameLock) {
						currentFrame = pendingFrame;
						pendingFrame = null;
					}
				}
				catch (ThreadInterruptedException) {
					// Continue
				}
			}
		}
	}
}
