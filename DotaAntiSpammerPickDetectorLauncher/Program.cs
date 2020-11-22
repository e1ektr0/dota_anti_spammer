using System;
using System.IO;
using System.Linq;
using System.Threading;
using DotaAntiSpammerNet.models;
using DotaAntiSpammerPickDetector;
using Newtonsoft.Json;

namespace DotaAntiSpammerPickDetectorLauncher
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Started");
            var pickDetector = new PickDetector();
            var pixel = JsonConvert.DeserializeObject<Pixels>(File.ReadAllText("p"));
     
            var json2 = JsonConvert.SerializeObject(pixel);
            File.WriteAllText("p", json2);
            

            Console.WriteLine("last" + pixel.Items.Last().Name);
            var last = new byte[0];
            var i = 0;
            while (true)
            {
                Thread.Sleep(5000);
                var makeScreenShot = PickDetector.MakeScreenShot();
                var heroPixels = pickDetector.GetHeroPixels(makeScreenShot);
                if (heroPixels.Count != 10) 
                    continue;
                
                if (heroPixels[5].Bytes.SequenceEqual(last))
                    continue;
                last = heroPixels[5].Bytes;
                Console.WriteLine("pick stage" + i);
                var name = Console.ReadLine();
                pixel.Items.Add(new HeroPixelDescription
                {
                    Name = name,
                    Bytes = last
                });
                var json = JsonConvert.SerializeObject(pixel);
                File.WriteAllText("p", json);
                var byteArray = makeScreenShot.ToByteArray();
                makeScreenShot.Save("test_l" + name + i + ".bmp");
                File.WriteAllBytes("test_l" + name + i, byteArray);
                i++;
            }
        }
    }
}