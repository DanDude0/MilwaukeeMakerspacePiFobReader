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
    class Program
    {
        static Screen screen = new Screen();
        static Font hugeFont = new Font(SystemFonts.Find("Arial"), 100f, FontStyle.Regular);
        static Font bigFont = new Font(SystemFonts.Find("Arial"), 54f, FontStyle.Regular);
        static Font littleFont = new Font(SystemFonts.Find("Arial"), 40f, FontStyle.Regular);
        static Font tinyFont = new Font(SystemFonts.Find("Arial"), 27f, FontStyle.Regular);
        static Bgr565 black = new Bgr565(0, 0, 0);
        static Bgr565 white = new Bgr565(1, 1, 1);
        static Bgr565 red = new Bgr565(1, 0.1f, 0.1f);
        static Bgr565 green = new Bgr565(0.1f, 1, 0.1f);
        static Bgr565 blue = new Bgr565(0.3f, 0.3f, 1);
        static Bgr565 grey = new Bgr565(0.5f, 0.5f, 0.5f);
        static Image<Bgr565> logo200 = Image.Load<Bgr565>("mms200x226.png");
        static Image<Bgr565> logo150 = Image.Load<Bgr565>("mms150x170.png");

        static MilwaukeeMakerspaceApiClient server;
        static int id;
        static DateTime expiration = DateTime.MinValue;
        static AuthenticationResult user;
        static ReaderResult reader;

        static void Main(string[] args)
        {
            try
            {
                id = int.Parse(File.ReadAllText("readerid.txt"));
            }
            catch
            {
                DrawFatal("Reader ID is not set");

                return;
            }

            try
            {
                server = new MilwaukeeMakerspaceApiClient();
            }
            catch
            {
                DrawFatal("Cannot reach server");

                return;
            }

            try
            {
                reader = server.ReaderLookup(id);
            }
            catch
            {
                DrawFatal("Server does not recognise reader ID");

                return;
            }

            DrawHeading(reader.Name);
            DrawStatus(-1);

            if (reader.Enabled)
            {
                DrawPrompt("Enter PIN or swipe fob");
            }
            else
            {
                DrawPrompt("Login has been disabled");
            }
            var userEntryBuffer = "";
            var lastEntry = DateTime.MinValue;
            var seconds = -1;
            var clear = false;
            ReaderHardware.Logout();

            // Main activity loop
            while (true)
            {
                var newSeconds = (int)Math.Floor(
                        (expiration - DateTime.Now).TotalSeconds);

                if (newSeconds > -5 && newSeconds != seconds)
                {
                    ReaderHardware.Warn(newSeconds);
                    DrawStatus(newSeconds);
                }

                seconds = newSeconds;

                // This blocks for 5ms waiting for user input
                var input = ReaderHardware.Read();

                if (!string.IsNullOrEmpty(input))
                {
                    clear = false;
                    lastEntry = DateTime.Now;
                }

                // We're not logged in
                if (seconds <= 0)
                {
                    // Transition from logged in state.
                    if (user != null)
                    {
                        DrawStatus(seconds);
                        clear = false;
                        user = null;
                        ReaderHardware.Logout();
                    }

                    if (!clear && DateTime.Now - lastEntry > new TimeSpan(0, 0, 30))
                    {
                        DrawPrompt("Enter PIN or swipe fob");
                        userEntryBuffer = "";
                        clear = true;
                    }
                }
                // We're Logged in
                else
                {
                    if (!clear && DateTime.Now - lastEntry > new TimeSpan(0, 0, 30))
                    {
                        DrawUser();
                        userEntryBuffer = "";
                        clear = true;
                    }
                }

                if (input.Length == 8)
                {
                    Authenticate($"W26#{input}");
                    userEntryBuffer = "";
                }
                else if (input.Length == 1)
                {
                    switch (input[0])
                    {
                        case '*':
                            DrawPrompt("Enter PIN or swipe fob");
                            userEntryBuffer = "";
                            break;
                        case '#':
                            Authenticate($"{userEntryBuffer}#");
                            userEntryBuffer = "";
                            break;
                        default:
                            userEntryBuffer += input[0];
                            DrawEntry("".PadLeft(userEntryBuffer.Length, '*'));
                            break;
                    }
                }
            }
        }

        static void Authenticate(string key)
        {
            // Force Logout
            if (key == "0#")
            {
                expiration = DateTime.Now - new TimeSpan(0, 0, 1);

                DrawStatus(-1, false);
                DrawPrompt("Enter PIN or swipe fob");
            }
            // Login / Extend
            else
            {
                DrawPrompt("Authenticating. . .");

                AuthenticationResult newUser;

                try
                {
                    newUser = server.Authenticate(id, key);
                }
                catch
                {
                    DrawPrompt("Invalid key");

                    return;
                }

                if (!newUser.AccessGranted)
                {
                    DrawPrompt("Expired membership");

                    return;
                }

                user = newUser;
                expiration = DateTime.Now + new TimeSpan(0, 0, reader.Timeout);

                DrawStatus(reader.Timeout, false);
                DrawUser();
                ReaderHardware.Login();
            }
        }

        static void DrawHeading(string name)
        {
            screen.Mutate(s => s
                .Fill(black)
                .DrawImage(logo150, 1, new Point(0, 0))
                .DrawText(new TextGraphicsOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 480 - 160,
                    VerticalAlignment = VerticalAlignment.Top,
                },
                    name,
                    littleFont,
                    red,
                    new PointF(160, 0)
                )
                .DrawPolygon(
                    grey,
                    2,
                    new PointF(0, 210),
                    new PointF(480, 210),
                    new PointF(480, 319),
                    new PointF(0, 319)
                )
            );
        }

        static void DrawStatus(int seconds, bool draw = true)
        {
            Bgr565 color;
            Bgr565 bg;
            Font font;
            string text;

            if (seconds > 60)
            {
                color = green;
                bg = black;
                font = littleFont;
                text = $"Logged In\n{seconds / 60}m Remaining";
            }
            else if (seconds > 0)
            {
                if (seconds % 2 == 0)
                {
                    color = red;
                    bg = black;
                }
                else
                {
                    color = black;
                    bg = red;
                }

                font = littleFont;
                text = $"Logging Out!\n{seconds}s Remaining";
            }
            else
            {
                color = blue;
                bg = black;
                font = bigFont;
                text = "Logged Out";
            }

            screen.Mutate(s => s
                .Fill(bg, new RectangleF(159, 88, 480 - 159, 110))
                .DrawText(new TextGraphicsOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 480 - 160,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                    text,
                    font,
                    color,
                    new PointF(160, 143)
                    ),
                draw);
        }

        static void DrawUser()
        {
            screen.Mutate(s => s
                .Fill(black, new RectangleF(5, 215, 470, 100))
                .DrawText(new TextGraphicsOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 480 - 12,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                    $"Hello, {user.Name}!\nRenewal due: {user.Expiration.ToString($"yyyy-MM-dd")}\nLogin to extend timer, '0#' to logout",
                    tinyFont,
                    white,
                    new PointF(6, 265)
                    )
                );
        }

        static void DrawPrompt(string contents)
        {
            screen.Mutate(s => s
                .Fill(black, new RectangleF(5, 215, 470, 100))
                .DrawText(new TextGraphicsOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 480 - 12,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                    contents,
                    littleFont,
                    white,
                    new PointF(6, 265)
                    )
                );
        }

        static void DrawEntry(string contents)
        {
            screen.Mutate(s => s
                .Fill(black, new RectangleF(5, 215, 470, 100))
                .DrawText(new TextGraphicsOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 480 - 12,
                    VerticalAlignment = VerticalAlignment.Top,
                },
                    contents,
                    hugeFont,
                    white,
                    new PointF(6, 248)
                    )
                );
        }

        static void DrawFatal(string contents)
        {
            screen.Mutate(s => s
                .Fill(black)
                .DrawPolygon(
                    red,
                    10,
                    new PointF(0, 0),
                    new PointF(480, 0),
                    new PointF(480, 319),
                    new PointF(0, 319)
                )
                .DrawText(new TextGraphicsOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 480 - 20,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                    $"! ERROR !\n{contents}",
                    bigFont,
                    red,
                    new PointF(10, 160)
                    )
                );
        }
    }
}