using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using DotaAntiSpammerNet.models;
using DotaAntiSpammerPickDetector;
using Newtonsoft.Json;
using NUnit.Framework;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DotaAntiSpammerPickDetectorTest
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void ScreenShot()
        {
            var pickDetector = new PickDetector();
            var makeScreenShot = PickDetector.MakeScreenShot();
            var heroPixels = pickDetector.GetHeroPixels(makeScreenShot);

            Assert.True(true);
        }

        Pixels _pixels = new Pixels();

        [Test]
        public void FromFile()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
            var x1 = Debug("test_lantimage14")[0];
            var x2 = Debug("debug")[0];
//            var abadon = Process("abadon",
//                "abaddon,earthshaker,lion,viper,dragon_knight,sven,vengefulspirit,sniper,warlock,necrolyte"); //abadon/shaker/lion/viper/dk/sven/venga/sniper/warlock/necr
//            var test1Desc = "abaddon,lion,viper,lich,juggernaut,earthshaker,drow_ranger,zuus,warlock,necrolyte";
//            var test1 = Process("test1", test1Desc); //abadon/shaker/lion/viper/dk/sven/venga/sniper/warlock/necr
            var test2 = Process("test_lantimage14", null); //abadon/shaker/lion/viper/dk/sven/venga/sniper/warlock/necr
            var d = Process("debug", null); //abadon/shaker/lion/viper/dk/sven/venga/sniper/warlock/necr
            //test_lantimage14

            var pickDetector = new PickDetector();
            Console.WriteLine(test2.First().H);
            Console.WriteLine(d.First().H);
//            pickDetector.DetectPicks(abadon);
//            pickDetector.DetectPicks(test1);
            pickDetector.DetectPicks(test2);
            Console.WriteLine();
            pickDetector.DetectPicks(d);
            
        }

        public List<HeroPixelWithPosition> Debug(string name)
        {
            var fileName = @"c:\temp\" + name;
            var readAllBytes = File.ReadAllBytes(fileName);
            var pickDetector = new PickDetector();
            var loadedPixels = pickDetector.GetHeroPixels(readAllBytes, 0);
            return loadedPixels;
        }
        private List<HeroPixelWithPosition> Process(string name, string heroes)
        {
            var fileName = @"c:\temp\" + name;
            var readAllBytes = File.ReadAllBytes(fileName);
            var pickDetector = new PickDetector();
            var loadedPixels = pickDetector.GetHeroPixels(readAllBytes, 0);

            var fromFile = Image.FromFile(@"c:\temp\" + name + ".bmp");
            var fromImage = Graphics.FromImage(fromFile);
            foreach (var pixel in loadedPixels)
            {
                var pixelFixedOffset = pixel.StartPosition;
                {
                    fromImage.DrawRectangle(new Pen(Color.White, 1), pixelFixedOffset, 0,
                        pixel.EndPosition - pixelFixedOffset, pixel.H);
                }
            }

            fromImage.Save();
            fromFile.Save(@"c:\temp\" + name + ".x.bmp", ImageFormat.Bmp);

            if (heroes != null)
            {
                var strings = heroes.Split(',');
                for (var i = 0; i < loadedPixels.Count; i++)
                {
                    var key = strings[i];
                    loadedPixels[i].Name = key;
                }

                _pixels.Items.AddRange(loadedPixels);
            }

            return loadedPixels;
        }

        private byte[] GetAbaddonTest()
        {
            var fromFile = (Bitmap) Image.FromFile(@"icons/abaddon.png");
            var clone = new Bitmap(fromFile.Width, fromFile.Height,
                PixelFormat.Format32bppPArgb);

            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(fromFile, new Rectangle(0, 0, clone.Width, clone.Height));
            }

            return new ScreenInstance(clone).ToByteArray().Take(clone.Width * 4).ToArray();
        }
    }
}