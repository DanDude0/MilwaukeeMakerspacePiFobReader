using System;
using Microsoft.VisualBasic;
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
		private static FontFamily arial = new FontCollection().Add("LiberationSans-Regular.ttf");
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
		private static Color purple = Color.FromRgb(196, 0, 255);
		private static Color grey = Color.FromRgb(128, 128, 128);
		private static Image logo200 = Image.Load("mms200x226.png");
		private static Image logo150 = Image.Load("mms150x170.png");
		private static DrawingOptions noAntiAlias = new DrawingOptions {
			GraphicsOptions = new GraphicsOptions {
				Antialias = false
			}
		};

		public static void Loading(string message)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black
				)
				.DrawPolygon(
					noAntiAlias,
					grey,
					10,
					new PointF(0, 0),
					new PointF(479, 0),
					new PointF(479, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextOptions(bigFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 20,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(480 / 2, 152),
				},
					message,
					white
				)
			);
		}

		public static void Heading(string name, string warning)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black
				)
				.DrawImage(logo150, new Point(0, 0), 1)
				.DrawText(new TextOptions(littleFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 160,
					VerticalAlignment = VerticalAlignment.Top,
					Origin = new PointF((480 + 160) / 2, -8),
				},
					name,
					red
				)
				.DrawPolygon(
					noAntiAlias,
					grey,
					2,
					new PointF(0, 210),
					new PointF(479, 210),
					new PointF(479, 319),
					new PointF(0, 319)
				)
			); ;

			if (warning.Length > 0)
				screen.Mutate(s => s
					.DrawText(new TextOptions(tinyFont) {
						HorizontalAlignment = HorizontalAlignment.Center,
						WrappingLength = 150,
						VerticalAlignment = VerticalAlignment.Top,
						Origin = new PointF(150 / 2, 50),
					},
						warning,
						white
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
				.Fill(
					noAntiAlias,
					bg,
					new RectangleF(159, 96, 480 - 159, 110)
				)
				.DrawText(new TextOptions(font) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 160,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF((480 + 160) / 2, 144),
				},
					text,
					color
				),
				draw
			);
		}

		public static void User(AuthenticationResult user)
		{
			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black,
					new RectangleF(2, 212, 476, 106)
				)
				.DrawText(new TextOptions(tinyFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 12,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(480 / 2, 259),
				},
					$"Hello, {user.Name}!\nRenewal due: {user.Expiration.ToString($"yyyy-MM-dd")}\nLogin to extend, '0       ' to Logout",
					white
				)
				.DrawText(new TextOptions(entFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(297, 295),
				},
					"ENT",
					white
				)
			);
		}

		public static void Prompt(string contents)
		{
			Log.Message("Draw.Prompt: " + contents);

			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black,
					new RectangleF(2, 212, 476, 106)
				)
				.DrawText(new TextOptions(littleFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 12,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(480 / 2, 256),
				},
					contents,
					white
				)
			);
		}

		public static void Entry(string contents)
		{
			Log.Message("Draw.Entry: " + contents);

			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black,
					new RectangleF(2, 212, 476, 106)
				)
				.DrawText(new TextOptions(hugeFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 12,
					VerticalAlignment = VerticalAlignment.Top,
					Origin = new PointF(480 / 2, 215),
				},
					contents,
					white
				)
			);
		}

		public static void Fatal(string contents)
		{
			Log.Message("Draw.Fatal: " + contents);

			if (MenuOverride)
				return;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black
				)
				.DrawPolygon(
					noAntiAlias,
					red,
					10,
					new PointF(0, 0),
					new PointF(479, 0),
					new PointF(479, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextOptions(bigFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 20,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(480 / 2, 152),
				},
					$"! ERROR !\n{contents}",
					red
				)
			);
		}

		public static void Service(string message)
		{
			Log.Message("Draw.Service: " + message);

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black
				)
				.DrawPolygon(
					noAntiAlias,
					purple,
					10,
					new PointF(0, 0),
					new PointF(479, 0),
					new PointF(479, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextOptions(microFont) {
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = 480 - 20,
					VerticalAlignment = VerticalAlignment.Top,
					Origin = new PointF(10, 7),
				},
					message,
					white
				)
			);
		}

		public static void Cabinet(string message)
		{
			Log.Message("Draw.Cabinet: " + message);

			Font font = bigFont;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black
				)
				.DrawPolygon(
					noAntiAlias,
					blue,
					10,
					new PointF(0, 0),
					new PointF(479, 0),
					new PointF(479, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextOptions(font) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 20,
					VerticalAlignment = VerticalAlignment.Top,
					Origin = new PointF(480 / 2, 7),
				},
					message,
					white
				)
				.DrawPolygon(
					noAntiAlias,
					grey,
					2,
					new PointF(20, 160),
					new PointF(459, 160),
					new PointF(459, 299),
					new PointF(20, 299)
				)
			);
		}

		public static void CabinetPrompt(string contents)
		{
			Log.Message("Draw.CabinetPrompt: " + contents);

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black,
					new RectangleF(21, 162, 436, 136)
				)
				.DrawText(new TextOptions(littleFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 42,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(480 / 2, 223),
				},
					contents,
					white
				)
			);
		}

		public static void CabinetEntry(string contents)
		{
			Log.Message("Draw.CabinetEntry: " + contents);

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black,
					new RectangleF(22, 162, 436, 136)
				)
				.DrawText(new TextOptions(hugeFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 42,
					VerticalAlignment = VerticalAlignment.Top,
					Origin = new PointF(480 / 2, 162),
				},
					contents,
					white
				)
			);
		}

		public static void FullScreenPrompt(string message)
		{
			Log.Message("Draw.FullScreenPrompt: " + message);

			var lineCount = 1;

			foreach (var c in message)
				if (c == '\n')
					lineCount += 1;

			Font font = bigFont;

			screen.Mutate(s => s
				.Fill(
					noAntiAlias,
					black
				)
				.DrawPolygon(
					noAntiAlias,
					blue,
					10,
					new PointF(0, 0),
					new PointF(479, 0),
					new PointF(479, 319),
					new PointF(0, 319)
				)
				.DrawText(new TextOptions(smallerFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 20,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(480 / 2, 120),
				},
					message,
					white
				)
				.DrawText(new TextOptions(tinyFont) {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = 480 - 12,
					VerticalAlignment = VerticalAlignment.Center,
					Origin = new PointF(480 / 2, 280),
				},
					$"ESC to Cancel  ENT to Continue",
					white
				)
			);
		}
	}
}
