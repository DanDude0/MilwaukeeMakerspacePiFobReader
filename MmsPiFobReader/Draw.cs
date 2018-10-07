using System;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace MmsPiFobReader
{
	static class Draw
	{
		private static Screen screen = new Screen();
		private static FontFamily arial = new FontCollection().Install("LiberationSans-Regular.ttf");
		private static Font hugeFont = new Font(arial, 120f, FontStyle.Regular);
		private static Font bigFont = new Font(arial, 56f, FontStyle.Regular);
		private static Font littleFont = new Font(arial, 45f, FontStyle.Regular);
		private static Font tinyFont = new Font(arial, 31f, FontStyle.Regular);
		private static Font entFont = new Font(arial, 24f, FontStyle.Regular);
		private static Bgr565 black = new Bgr565(0, 0, 0);
		private static Bgr565 white = new Bgr565(1, 1, 1);
		private static Bgr565 red = new Bgr565(1, 0.1f, 0.1f);
		private static Bgr565 green = new Bgr565(0.1f, 1, 0.1f);
		private static Bgr565 blue = new Bgr565(0.3f, 0.3f, 1);
		private static Bgr565 grey = new Bgr565(0.5f, 0.5f, 0.5f);
		private static Image<Bgr565> logo200 = Image.Load<Bgr565>("mms200x226.png");
		private static Image<Bgr565> logo150 = Image.Load<Bgr565>("mms150x170.png");

		public static void Loading(string message)
		{
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
					HorizontalAlignment = HorizontalAlignment.Center,
					WrapTextWidth = 480 - 20,
					VerticalAlignment = VerticalAlignment.Center,
				},
					message,
					bigFont,
					white,
					new PointF(10, 160)
					)
				);
		}

		public static void Heading(string name)
		{
			screen.Mutate(s => s
				.Fill(black)
				.DrawImage(logo150, 1, new Point(0, 0))
				.DrawText(new TextGraphicsOptions {
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

		public static void Status(int seconds, bool draw = true)
		{
			Bgr565 color;
			Bgr565 bg;
			Font font;
			string text;

			if (seconds > 60) {
				color = green;
				bg = black;
				font = littleFont;
				text = $"Logged In\n{seconds / 60}m Remaining";
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
				.Fill(bg, new RectangleF(159, 88, 480 - 159, 110))
				.DrawText(new TextGraphicsOptions {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrapTextWidth = 480 - 160,
					VerticalAlignment = VerticalAlignment.Center,
				},
					text,
					font,
					color,
					new PointF(160, 143)
					),
					draw
				);
		}

		public static void User(AuthenticationResult user)
		{
			screen.Mutate(s => s
				.Fill(black, new RectangleF(5, 215, 470, 100))
				.DrawText(new TextGraphicsOptions {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrapTextWidth = 480 - 12,
					VerticalAlignment = VerticalAlignment.Center,
				},
					$"Hello, {user.Name}!\nRenewal due: {user.Expiration.ToString($"yyyy-MM-dd")}\nLogin to extend, '0       ' to Logout",
					tinyFont,
					white,
					new PointF(6, 263)
					)
				.DrawText(new TextGraphicsOptions {
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
				},
					"ENT",
					entFont,
					white,
					new PointF(297, 298)
					)
				);
		}

		public static void Prompt(string contents)
		{
			screen.Mutate(s => s
				.Fill(black, new RectangleF(5, 215, 470, 100))
				.DrawText(new TextGraphicsOptions {
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

		public static void Entry(string contents)
		{
			screen.Mutate(s => s
				.Fill(black, new RectangleF(5, 215, 470, 100))
				.DrawText(new TextGraphicsOptions {
					HorizontalAlignment = HorizontalAlignment.Center,
					WrapTextWidth = 480 - 12,
					VerticalAlignment = VerticalAlignment.Top,
				},
					contents,
					hugeFont,
					white,
					new PointF(6, 240)
					)
				);
		}

		public static void Fatal(string contents)
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
				.DrawText(new TextGraphicsOptions {
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
