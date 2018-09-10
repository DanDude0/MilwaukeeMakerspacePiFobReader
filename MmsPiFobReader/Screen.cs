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
		Image<Bgr565> buffer = new Image<Bgr565>(480, 320);

#if RPI
        FileStream frameBuffer = new FileStream("/dev/fb1", FileMode.Append);
		byte[] currentFrame;
		byte[] pendingFrame;
		object frameLock = new object();
#else
		IntPtr window;
#endif

		public Screen()
		{
#if RPI
			// We're taking over the screen
			DisableConsole();

			// Handle SigTerm
			Console.CancelKeyPress += EnableConsole;
			AssemblyLoadContext.Default.Unloading += EnableConsole;
#else
			// Make a desktop window to draw the screen contents to
			SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING);
			window = SDL.SDL_CreateWindow("MmsPiFobReader", 50, 50, 480, 320, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
			var renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
			SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
			SDL.SDL_RenderClear(renderer);
			SDL.SDL_RenderPresent(renderer);
#endif
		}

		public void Mutate(Action<IImageProcessingContext<Bgr565>> operation, bool draw = true)
		{
			buffer.Mutate(operation);

			if (draw)
				Draw();
		}

		private static void DisableConsole()
		{
			File.WriteAllText("/sys/class/vtconsole/vtcon1/bind", "0");
		}

		private static void EnableConsole(AssemblyLoadContext obj)
		{
			EnableConsole(null, null);
		}

		private static void EnableConsole(object sender, EventArgs e)
		{
			File.WriteAllText("/sys/class/vtconsole/vtcon1/bind", "1");
			Process.Start("setupcon");
		}

		private void Draw()
		{
			var bytes = MemoryMarshal.AsBytes(buffer.GetPixelSpan()).ToArray();

#if RPI
            lock (frameLock)
            {
                if (currentFrame == null)
                    currentFrame = bytes;
                else
                {
                    pendingFrame = bytes;
                }
            }

            var thread = new Thread(DrawThread);
            thread.Start();
#else
			// This code is seriously bloated and could almost certainly be faster, but it just needs to be good enough for debugging.
			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var pointer = handle.AddrOfPinnedObject();
			var bitmapSurface = SDL.SDL_CreateRGBSurfaceFrom(pointer, 480, 320, 16, 960, 0x1F << 11, 0x3F << 5, 0x1F, 0);
			var windowSurface = SDL.SDL_GetWindowSurface(window);

			SDL.SDL_BlitSurface(bitmapSurface, IntPtr.Zero, windowSurface, IntPtr.Zero);
			SDL.SDL_FreeSurface(bitmapSurface);
			SDL.SDL_UpdateWindowSurface(window);
#endif
		}

#if RPI
        private void DrawThread()
        {
            while (true)
            {
                frameBuffer.Seek(0, SeekOrigin.Begin);
                frameBuffer.Write(currentFrame);

                lock (frameLock)
                {
                    currentFrame = pendingFrame;
                    pendingFrame = null;

                    if (currentFrame == null)
                        return;
                }
            }
        }
#endif
	}
}