using System;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MmsPiFobReader
{
	static class Draw
	{
		public static bool MenuOverride = false;

		private static Screen screen = new Screen();
		private static FontFamily arial = new FontCollection().Install("LiberationSans-Regular.ttf");
		private static Font hugeFont = new Font(arial, 120, FontStyle.Regular);
		private static Font bigFont = new Font(arial, 56, FontStyle.Regular);
		private static Font littleFont = new Font(arial, 45, FontStyle.Regular);
		private static Font smallerFont = new Font(arial, 42, FontStyle.Regular);
		private static Font tinyFont = new Font(arial, 31, FontStyle.Regular);
		private static Font entFont = new Font(arial, 24, FontStyle.Regular);
		private static Font microFont = new Font(arial, 23, FontStyle.Regular);
		private static Color black = Color.FromRgb(0, 0, 0);
		private static Color white = Color.FromRgb(255, 255, 255);
		private static Color red = Color.FromRgb(255, 26, 26);
		private static Color green = Color.FromRgb(26, 255, 26);
		private static Color blue = Color.FromRgb(76, 76, 255);
		private static Color grey = Color.FromRgb(128, 128, 128);
		private static Image logo200 = Image.Load("mms200x226.png");
		private static Image logo150 = Image.Load("mms150x170.png");

		public static void Loading(string message)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(black)
				.DrawPolygon(
					grey,
					10,
					new PointF(0, 0),
					new PointF(480, 0),
					new PointF(480, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 20,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					message,
					bigFont,
					white,
					new PointF(10, 152)
					)
				);
		}

		public static void Heading(string name, string warning)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(black)
				.DrawImage(logo150, new Point(0, 0), 1)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 160,
						VerticalAlignment = VerticalAlignment.Top,
					}
				},
					name,
					littleFont,
					red,
					new PointF(160, -8)
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

			if (warning.Length > 0)
				screen.Mutate(s => s
					.DrawText(new TextGraphicsOptions {
						TextOptions = new TextOptions {
							HorizontalAlignment = HorizontalAlignment.Center,
							WrapTextWidth = 150,
							VerticalAlignment = VerticalAlignment.Top,
						}
					},
						warning,
						tinyFont,
						white,
						new PointF(0, 50)
					)
				);
		}

		public static void Status(int seconds, bool draw = true)
		{
			if (MenuOverride)
				return;

			Color color;
			Color bg;
			Font font;
			string text;

			if (seconds > 60) {
				var minutes = seconds / 60;

				if (minutes > 99)
					font = smallerFont;
				else
					font = littleFont;

				color = green;
				bg = black;
				text = $"Logged In\n{minutes}m Remaining";
			}
			else if (seconds > 0) {
				if (seconds % 2 == 0) {
					color = red;
					bg = black;
				}
				else {
					color = black;
					bg = red;
				}

				font = littleFont;
				text = $"Logging Out!\n{seconds}s Remaining";
			}
			else {
				color = blue;
				bg = black;
				font = bigFont;
				text = "Logged Out";
			}

			screen.Mutate(s => s
				.Fill(bg, new RectangleF(159, 96, 480 - 159, 110))
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 160,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					text,
					font,
					color,
					new PointF(160, 144)
					),
					draw
				);
		}

		public static void User(AuthenticationResult user)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(black, new RectangleF(5, 215, 470, 100))
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 12,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					$"Hello, {user.Name}!\nRenewal due: {user.Expiration.ToString($"yyyy-MM-dd")}\nLogin to extend, '0       ' to Logout",
					tinyFont,
					white,
					new PointF(6, 259)
					)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					"ENT",
					entFont,
					white,
					new PointF(297, 295)
					)
				);
		}

		public static void Prompt(string contents)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(black, new RectangleF(5, 215, 470, 100))
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 12,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					contents,
					littleFont,
					white,
					new PointF(6, 256)
					)
				);
		}

		public static void Entry(string contents)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(black, new RectangleF(5, 215, 470, 100))
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 12,
						VerticalAlignment = VerticalAlignment.Top,
					}
				},
					contents,
					hugeFont,
					white,
					new PointF(6, 215)
					)
				);
		}

		public static void Fatal(string contents)
		{
			if (MenuOverride)
				return;

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
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 20,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					$"! ERROR !\n{contents}",
					bigFont,
					red,
					new PointF(10, 152)
					)
				);
		}

		public static void Service(string message)
		{
			screen.Mutate(s => s
				.Fill(black)
				.DrawPolygon(
					blue,
					10,
					new PointF(0, 0),
					new PointF(480, 0),
					new PointF(480, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Left,
						WrapTextWidth = 480 - 20,
						VerticalAlignment = VerticalAlignment.Top,
					}
				},
					message,
					microFont,
					white,
					new PointF(10, 7)
					)
				);
		}

		public static void Cabinet(string message, string entry)
		{
			var lineCount = 1;

			foreach (var c in message)
				if (c == '\n')
					lineCount += 1;

			Font font = tinyFont;

			if (lineCount > 11)
				font = new Font(tinyFont, 21);
			else if (lineCount > 10)
				font = new Font(tinyFont, 23);
			else if (lineCount > 9)
				font = new Font(tinyFont, 26);
			else if (lineCount > 8)
				font = new Font(tinyFont, 28);

			screen.Mutate(s => s
				.Fill(black)
				.DrawPolygon(
					blue,
					10,
					new PointF(0, 0),
					new PointF(480, 0),
					new PointF(480, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Left,
						WrapTextWidth = 480 - 20,
						VerticalAlignment = VerticalAlignment.Top,
					}
				},
					message,
					font,
					white,
					new PointF(10, 7)
					)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Left,
						WrapTextWidth = 480 - 20,
						VerticalAlignment = VerticalAlignment.Top,
					}
				},
					entry,
					smallerFont,
					white,
					new PointF(10, 220)
					)
				);
		}

		public static void FullScreenPrompt(string message)
		{
			var lineCount = 1;

			foreach (var c in message)
				if (c == '\n')
					lineCount += 1;

			Font font = bigFont;

			screen.Mutate(s => s
				.Fill(black)
				.DrawPolygon(
					blue,
					10,
					new PointF(0, 0),
					new PointF(480, 0),
					new PointF(480, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 20,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					message,
					smallerFont,
					white,
					new PointF(10, 120)
				)
				.DrawText(new TextGraphicsOptions {
					TextOptions = new TextOptions {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrapTextWidth = 480 - 12,
						VerticalAlignment = VerticalAlignment.Center,
					}
				},
					$"ESC to Cancel  ENT to Continue",
					tinyFont,
					white,
					new PointF(6, 280)
				)
				);
		}
	}
}
