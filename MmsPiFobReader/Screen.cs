using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
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
        byte[] currentFrame;
        byte[] pendingFrame;
        object frameLock = new object();

        public Screen()
        {
            // We're taking over the screen
            DisableConsole();

            // Handle SigTerm
            Console.CancelKeyPress += EnableConsole;
            AssemblyLoadContext.Default.Unloading += EnableConsole;
        }

        public void Mutate(Action<IImageProcessingContext<Bgr565>> operation, bool draw = true)
        {
            buffer.Mutate(operation);

            if (draw)
                Draw();
        }

        private static void DisableConsole()
        {
#if LINUX
			File.WriteAllText("/sys/class/vtconsole/vtcon1/bind", "0");
#endif
        }

        private static void EnableConsole(AssemblyLoadContext obj)
        {
            EnableConsole(null, null);
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

            lock (frameLock)
            {
                if (currentFrame == null)
                    currentFrame = bytes.ToArray();
                else
                {
                    pendingFrame = bytes.ToArray();
                }
            }

            var thread = new Thread(DrawThread);
            thread.Start();
#else
            var file = new FileStream("C:\\temp\\example.bmp", FileMode.Create);

            buffer.SaveAsBmp(file);
#endif
        }

#if LINUX
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