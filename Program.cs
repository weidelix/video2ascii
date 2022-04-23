using System.Threading.Tasks;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using FFMediaToolkit.Graphics;
using FFMediaToolkit.Decoding;
using FFMediaToolkit;

static class App {
	static string FilePath = "D:\\Downloads\\cocomelon.mp4"; 
	static Size Size = new Size(Console.BufferWidth, Console.BufferHeight);
	static int BufferFillSize = 60;
	static readonly string brightness =  " .:-=+*#%@";
	// static readonly string brightness =  " .:░▒▓█";
	
	public static async Task Main() {
		// Array.Reverse(brightness);
		FFmpegLoader.FFmpegPath = "D:\\Apps\\ffmpeg\\bin";
		MediaFile file = MediaFile.Open(FilePath, new MediaOptions { TargetVideoSize = Size });

		DateTime time1 = DateTime.Now;
		DateTime time2 = DateTime.Now;
		var frames = new Queue<byte[]>();
		bool isEnd = false;
		bool stop = false;
		bool isBuffering = false;
		
		using (var stdout = Console.OpenStandardOutput(Size.Width * Size.Height))
		{
			while(!stop)
			{
				if (frames.Count > BufferFillSize / 2 || isEnd) {
					time2 = DateTime.Now;
					var deltaTime = (float)((time2.Ticks - time1.Ticks) / 10000000.0);			
					var framerate = (file.Video.Info.RealFrameRate.num / file.Video.Info.RealFrameRate.den);
					
					if (frames.TryDequeue(out var frame)) {
						if (frame != null) {
							await Task.Delay(TimeSpan.FromMilliseconds(1000d / framerate))
												.ContinueWith(_ => {
																				Console.SetCursorPosition(0, 0);
																				stdout.Write(frame, 0, frame.Count<byte>());
																		});
						}
					}

					if (frames.Count == 0 && isEnd) {
						stop = true;
					}

					Console.SetCursorPosition(0, 0);
					Console.WriteLine("Frame buffer count: " + frames.Count);
					Console.WriteLine("Delta time: " + deltaTime);
					Console.WriteLine("Framerate: " + framerate); 
				}
				time1 = time2;

				if (frames.Count < BufferFillSize && !isBuffering) {
					isBuffering = true;
					
					_ = Task.Run(() => {
						foreach (var i in Enumerable.Range(0, BufferFillSize)) {
							var builder = new StringBuilder(Size.Width * Size.Height - 1);

							isEnd = !file.Video.TryGetNextFrame(out var imageData);
							var frame = imageData.ToBitmap();

							foreach (int y in Enumerable.Range(0, Size.Height - 1)) {					
								foreach (int x in Enumerable.Range(0, Size.Width)) {
									Color color = frame.GetPixel(x, y);

									// Accuracy
									// float luminance = 0.2126f * color.R + 0.587f * color.G + 0.0722f * color.B;
									
									// Approximate
									float luminance = (color.R + color.R + color.R + color.B + color.G + color.G + color.G + color.G) >> 3;

									int index = (int)(((float)(brightness.Length - 1) / 255f) * luminance);
									builder.Append(brightness[index]);  
								}
							}
							frames.Enqueue(Encoding.ASCII.GetBytes(builder.ToString()));
						}
						isBuffering = false;
					});
				}
			}
		}

		file.Dispose();
	}

	static unsafe Bitmap ToBitmap(this ImageData bitmap)
	{
		fixed(byte* p = bitmap.Data)
		{
			var image = new Bitmap(bitmap.ImageSize.Width, 
														 bitmap.ImageSize.Height - 1, 
														 bitmap.Stride, 
														 System.Drawing.Imaging.PixelFormat.Format24bppRgb, 
														 new IntPtr(p));
			return image;
		}
	}
}