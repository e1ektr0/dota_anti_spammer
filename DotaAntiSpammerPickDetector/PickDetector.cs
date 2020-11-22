using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DotaAntiSpammerPickDetector
{
    public class HeroPixelDescription
    {
        public string Name { get; set; }
        public byte[] Bytes { get; set; }
        public int Id { get; set; }
    }

    public class HeroPixelWithPosition : HeroPixelDescription
    {
        public int StartPosition { get; }
        public int EndPosition { get; set; }
        public int H { get; set; }

        public HeroPixelWithPosition(int startPosition)
        {
            StartPosition = startPosition;
        }
    }

    public class ScreenInstance
    {
        public ScreenInstance(Bitmap captureBitmap)
        {
            if (captureBitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                var clone = new Bitmap(captureBitmap.Width, captureBitmap.Height,
                    PixelFormat.Format32bppPArgb);

                using (Graphics gr = Graphics.FromImage(clone))
                {
                    gr.DrawImage(captureBitmap, new Rectangle(0, 0, clone.Width, clone.Height));
                }

                captureBitmap = clone;
            }

            CaptureBitmap = captureBitmap;
        }

        public Bitmap CaptureBitmap { get; }

        public byte[] ToByteArray()
        {
            var height = 20;
            var rectangle = new Rectangle(0, 0, CaptureBitmap.Width, height);
            var bmpData = CaptureBitmap.LockBits(rectangle,
                ImageLockMode.ReadOnly,
                CaptureBitmap.PixelFormat);
            var buffer = bmpData.Scan0;
            var result = new byte[CaptureBitmap.Width * CaptureBitmap.Height * 4];
            unsafe
            {
                var ptr = (byte*) buffer.ToPointer();
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = ptr[i];
                }
            }

            CaptureBitmap.UnlockBits(bmpData);

            return result;
        }

        public void Save(string fileName)
        {
            CaptureBitmap.Save(fileName);
        }
    }

    public class PickDetector
    {
        private Pixels _pixels;

        public PickDetector()
        {
            var readAllText = File.ReadAllText("p");
            _pixels = JsonConvert.DeserializeObject<Pixels>(readAllText);
        }

        public static ScreenInstance MakeScreenShot()
        {
            var captureRectangle = Screen.AllScreens[0].Bounds;
            var captureBitmap = new Bitmap(captureRectangle.Width, 30, PixelFormat.Format32bppArgb);
            var captureGraphics = Graphics.FromImage(captureBitmap);
            captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top,
                0, 0, captureRectangle.Size);

            return new ScreenInstance(captureBitmap);
        }

        public List<HeroPixelWithPosition> GetHeroPixels(ScreenInstance makeScreenShot)
        {
            var captureBitmap = makeScreenShot.CaptureBitmap;

            var width = captureBitmap.Width;
            var height = 20;
            var rectangle = new Rectangle(0, 0, width, height);
            var bmpData = captureBitmap.LockBits(rectangle,
                ImageLockMode.ReadOnly,
                captureBitmap.PixelFormat);
            var buffer = bmpData.Scan0;

            try
            {
                unsafe
                {
                    var ptr = (byte*) buffer.ToPointer();
                    var heroPixels = GetHeroPixelsUnsafe(width, ptr, height);

                    captureBitmap.UnlockBits(bmpData);
                    return heroPixels;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new List<HeroPixelWithPosition>();
            }
        }

        public List<HeroPixelWithPosition> GetHeroPixels(byte[] rawBitmap, int h = 30)
        {
            unsafe
            {
                fixed (byte* p = rawBitmap)
                {
                    int rawBitmapLength;
                    if (h != 0)
                    {
                        rawBitmapLength = rawBitmap.Length / h / 4;
                    }
                    else
                    {
                        rawBitmapLength = 3000;
                        h = rawBitmap.Length / rawBitmapLength / 4;
                    }

                    return GetHeroPixelsUnsafe(rawBitmapLength, p, h);
                }
            }
        }

        private static unsafe List<HeroPixelWithPosition> GetHeroPixelsUnsafe(int width, byte* ptr, int height)
        {
            var pixels = new List<HeroPixelWithPosition>();

            var wrongPixelFlag = false;
            var correctPixel = false;
            var offset = 0;
            for (var j = 0; j < width; j++)
            {
                var b = ptr[offset + 0];
                var g = ptr[offset + 1];
                var r = ptr[offset + 2];
                if (b > 50 || g > 50 || r > 50)
                {
                    //radiant
                    if (wrongPixelFlag)
                    {
                        var startPosition = offset / 4;
                        var pixel1 = new HeroPixelWithPosition(startPosition);
                        pixels.Add(pixel1);
                    }

                    correctPixel = true;
                    wrongPixelFlag = false;
                }
                else
                {
                    if (correctPixel)
                    {
                        if (pixels.Any())
                        {
                            var pixel = pixels.Last();
                            pixel.EndPosition = offset / 4;
                            var widthHero = pixel.EndPosition - pixel.StartPosition;
                            var middle = widthHero / 2;
                            var startSearch = (pixel.StartPosition + middle) * 4;
                            var bi = ptr[startSearch + 0];
                            var gi = ptr[startSearch + 1];
                            var ri = ptr[startSearch + 2];
                            for (var i = 1; i < height; i++)
                            {
                                var p = startSearch + i * width * 4;
                                var bi1 = ptr[p + 0];
                                var gi1 = ptr[p + 1];
                                var ri1 = ptr[p + 2];
                                var abs = Math.Abs(bi1 - bi);
                                var f = 9;
                                if (abs < f || Math.Abs(gi1 - gi) < f || Math.Abs(ri - ri1) < f)
                                {
                                    bi = bi1;
                                    gi = gi1;
                                    ri = ri1;
                                }
                                else
                                {
                                    pixel.H = i;
                                    break;
                                }
                            }

                            if (pixels.Count == 10)
                                break;
                        }
                    }

                    correctPixel = false;
                    wrongPixelFlag = true;
                }

                offset += 4;
            }

            var heroes = pixels.ToList();
            if (heroes.Count != 10)
                return new List<HeroPixelWithPosition>();
            var hSearch = heroes.Where(n => n.H != 0).Min(n => n.H);
            foreach (var pixel in pixels)
            {
                pixel.H = hSearch;
                var pixelStartPosition = pixel.StartPosition + 1;
                var pixelEndPosition = pixel.EndPosition - 1;
                var widthHero = pixelEndPosition - pixelStartPosition;
                var startSearch = pixelStartPosition * 4;
                pixel.Bytes = new byte[widthHero * 4];
                var offsetS = startSearch + (hSearch + 1) * width * 4;
                for (var i = 0; i < widthHero * 4; i++)
                    pixel.Bytes[i] = ptr[i + offsetS];
            }

            return pixels;
        }

        private class Distance
        {
            public long Min { get; }
            public HeroPixelDescription HeroPixelWithPosition { get; }

            public Distance(long min, HeroPixelDescription heroPixelWithPosition)
            {
                Min = min;
                HeroPixelWithPosition = heroPixelWithPosition;
            }
        }

        public void DetectPicks(List<HeroPixelWithPosition> heroPixels)
        {
            if (heroPixels.Count != 10)
                return;
            DetectPicks(heroPixels, _pixels);
        }

        private void DetectPicks(List<HeroPixelWithPosition> heroPixels, Pixels pixels)
        {
            if (heroPixels.Count != 10)
                return;
            foreach (var heroPixel in heroPixels)
            {
                var distances = new List<Distance>();
                var heroPixelDescriptions = pixels.Items;
                foreach (var pixel in heroPixelDescriptions.Where(n => n.Bytes.Length >= 700))
                {
                    var pixelBytes = pixel.Bytes.ToArray();
                    var pixelDistance = new List<Distance>();
                    for (var i = 0; i < heroPixel.Bytes.Length / 4; i++)
                    {
                        var point = (float) pixelBytes.Length / heroPixel.Bytes.Length;
                        var b = heroPixel.Bytes[i * 4];
                        var g = heroPixel.Bytes[i * 4 + 1];
                        var r = heroPixel.Bytes[i * 4 + 2];
                        var min = int.MaxValue;
                        var position = 0;
                        var endPosition = (int) (i * point) + 1;
                        if (endPosition > pixelBytes.Length)
                            endPosition = pixelBytes.Length;
                        for (var x = (int) (i * point); x < endPosition; x++)
                        {
                            var b1 = pixelBytes[x * 4 + 0];
                            var g1 = pixelBytes[x * 4 + 1];
                            var r1 = pixelBytes[x * 4 + 2];
                            if (b1 == byte.MaxValue && g1 == byte.MaxValue && r1 == byte.MaxValue)
                                continue;
                            var distanceToPixel = Math.Abs(b1 - b) + Math.Abs(r1 - r) + Math.Abs(g1 - g);
                            if (distanceToPixel >= min)
                                continue;

                            min = distanceToPixel;
                            position = x;
                        }

                        pixelBytes[position * 4 + 0] = byte.MaxValue;
                        pixelBytes[position * 4 + 1] = byte.MaxValue;
                        pixelBytes[position * 4 + 2] = byte.MaxValue;
                        pixelDistance.Add(new Distance(min, pixel));
                    }

                    distances.Add(new Distance(pixelDistance.Sum(n => n.Min), pixel));
                }

                var orderedEnumerable = distances.OrderBy(n => n.Min);
                var distance = orderedEnumerable.First();
                if(distance.Min<7000)
                    heroPixel.Name = distance.HeroPixelWithPosition.Name;
                Console.WriteLine(heroPixel.Name + distance.Min);
            }
        }
    }

    public class Pixels
    {    
        public List<HeroPixelDescription> Items { get; set; } = new List<HeroPixelDescription>();
    }
}