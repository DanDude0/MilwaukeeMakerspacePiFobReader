using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MmsPiFobReader
{
	class Screen
	{
		Image<Bgr565> buffer = new Image<Bgr565>(480, 320);
		byte[] currentFrame = new byte[480 * 320 * 2];
		byte[] pendingFrame = new byte[480 * 320 * 2];
		byte[] rotationFrame = new byte[480 * 320 * 2];
		FileStream frameBuffer;
		object frameLock = new object();
		IntPtr window;
		IntPtr renderer;
		IntPtr texture;
		Thread drawThread;
		bool active;
		bool frameReady;

		public Screen()
		{
			switch (ReaderHardware.Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					frameBuffer = new FileStream("/dev/fb0", FileMode.Append);

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
					window = SDL.SDL_CreateWindow("MmsPiFobReader", 50, 50, 480, 320, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);
					renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
					SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
					SDL.SDL_RenderClear(renderer);
					SDL.SDL_RenderPresent(renderer);
					SDL.SDL_ShowCursor(SDL.SDL_DISABLE);
					texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGB565, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 480, 320);
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

			active = true;
			drawThread = new Thread(DrawThread);
			drawThread.Start();
		}

		private void EnableConsole(AssemblyLoadContext obj)
		{
			EnableConsole(null, null);
		}

		private void EnableConsole(object sender, EventArgs e)
		{
			active = false;
			drawThread.Interrupt();

			File.AppendAllText("/sys/class/vtconsole/vtcon1/bind", "1");
			Process.Start("setupcon");
		}

		private unsafe void Draw()
		{

			lock (frameLock) {
				if (ReaderHardware.InvertScreen) {
					buffer.CopyPixelDataTo(rotationFrame);

					for (int row = 0; row < 320; row += 1) {
						for (int col = 0; col < 960; col += 2) {
							pendingFrame[(319 - row) * 960 + (958 - col)] = rotationFrame[row * 960 + col];
							pendingFrame[(319 - row) * 960 + (958 - col) + 1] = rotationFrame[row * 960 + col + 1];
						}
					}
				}
				else
					buffer.CopyPixelDataTo(pendingFrame);

				frameReady = true;
			}

			switch (ReaderHardware.Platform) {
				case HardwareType.OrangePi:
				case HardwareType.RaspberryPi:
					// This is a sort of double buffering to make up for low device frame rate.
					drawThread.Interrupt();

					break;
				default:
					fixed (byte* pointer = pendingFrame) {
						SDL.SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)pointer, 480 * 2);
					}

					SDL.SDL_RenderClear(renderer);
					SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
					SDL.SDL_RenderPresent(renderer);
					break;
			}
		}

		private void DrawThread()
		{
			while (active) {
				try {
					if (frameReady == false)
						Thread.Sleep(10000);
					else {
						lock (frameLock) {
							var oldFrame = currentFrame;
							currentFrame = pendingFrame;
							pendingFrame = oldFrame;
							frameReady = false;
						}
					}

					frameBuffer.Seek(0, SeekOrigin.Begin);
					frameBuffer.Write(currentFrame);
				}
				catch (ThreadInterruptedException) {
					// Continue to either redraw or exit.
				}
			}
		}
	}
}
