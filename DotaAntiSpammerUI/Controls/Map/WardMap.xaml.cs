using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DotaAntiSpammerCommon.Models;
using DotaAntiSpammerNet.models;

namespace DotaAntiSpammerNet.Controls.Map
{
    public class Ward
    {
        public double X { get; set; }
        public double Y { get; set; }
        public bool Obs { get; set; }
        public int Time { get; set; }
        public bool Mine { get; set; }
    }

    class CanvasWithBitmap : Canvas
    {
        private readonly List<Ward> _wards = new List<Ward>();
        private BitmapSource _obsImage;
        private BitmapSource _sentryImage;
        private BitmapSource _mineImage;

        protected override void OnRender(DrawingContext dc)
        {
            foreach (var ward in _wards)
            {
                var size = 30;
                var rectangle = new Rect(ward.X - size / 2d, ward.Y - size, size, size);
                var radiusX = 8;
                var solidColorBrush = Brushes.Black.Clone();
                if (ward.Time < 0)
                {
                    solidColorBrush = Brushes.Chartreuse.Clone();
                }
                else if (ward.Time < 20)
                {
                    solidColorBrush = Brushes.Goldenrod.Clone();
                }
                else if (ward.Time < 60)
                {
                    solidColorBrush = Brushes.Brown.Clone();
                }

                solidColorBrush.Opacity = .66d;
                //if (!ward.Mine)
                dc.DrawEllipse(solidColorBrush, new Pen(), new Point((int) ward.X, (int) ward.Y), radiusX, radiusX);
                var bitmapSource = ward.Obs ? _obsImage : _sentryImage;
                if (ward.Mine)
                {
                    bitmapSource = _mineImage;
                    rectangle = new Rect(ward.X - size / 1.7f, ward.Y - size / 1.3f, size, size);
                }

                dc.DrawImage(bitmapSource, rectangle);
            }
        }

        private static BitmapSource CreateTransparency(BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32)
            {
                return source;
            }

            var bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;
            var stride = bytesPerPixel * source.PixelWidth;
            var buffer = new byte[stride * source.PixelHeight];

            source.CopyPixels(buffer, stride, 0);

            for (var y = 0; y < source.PixelHeight; y++)
            {
                for (var x = 0; x < source.PixelWidth; x++)
                {
                    var i = stride * y + bytesPerPixel * x;

                    if (buffer[i + 3] != 0)
                        buffer[i + 3] = 190; // set transparent 
                }
            }

            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, buffer, stride);
        }

        public void Add(double canvasX, double canvasY, bool resultObs, int resultTime, bool resultMine)
        {
            var ward = new Ward {X = canvasX, Y = canvasY, Obs = resultObs, Time = resultTime, Mine = resultMine};
            _wards.Add(ward);
        }

        public void Ini(Image obs, Image sentry, Image mine)
        {
            var imageSource = (BitmapFrame) obs.Source;
            _obsImage = CreateTransparency(imageSource);

            var sentrySource = (BitmapFrame) sentry.Source;
            _sentryImage = CreateTransparency(sentrySource);

            var mineSource = (BitmapFrame) mine.Source;
            _mineImage = CreateTransparency(mineSource);
        }
    }

    public partial class WardMap
    {
        public WardMap()
        {
            InitializeComponent();
        }

        private const int DefaultNetXOffset = 64;
        private const int DefaultNetYOffset = 63;
        private const int CellSize = 1 << 7;
        private const int FullSize = 1 << 14;

        private void Add(double x, double y, double vx, double vy, bool resultObs, int resultTime, bool resultMine)
        {
            var rx = (x - DefaultNetXOffset) * CellSize + vx;
            var ry = (y - DefaultNetYOffset) * CellSize + vy;
            var size = Width - 5;
            var canvasX = size * rx / FullSize;
            var canvasY = size * (FullSize - ry) / FullSize;
            Cnv.Add(canvasX, canvasY, resultObs, resultTime, resultMine);
        }

        public void Ini(int i, List<WardPlaced> matchPlayerWardResults, int heroId)
        {
            var instanceHero = HeroConfigAll.Instance.Heroes.First(n => n.Id == heroId);
            Image.Source = new BitmapImage(new Uri($"../../icons/{instanceHero.Name}.png", UriKind.Relative));
            Obs.Opacity = 0.25;
            Sentry.Opacity = 0.25;
            Cnv.Ini(Obs, Sentry, Mine);
            Border.BorderBrush = new SolidColorBrush(PlayerColors.Colors[i]);
            Visibility = Visibility.Visible;

            var playerWardResults = matchPlayerWardResults.Where(n => !n.Mine).ToList();
            var canvasCoords = matchPlayerWardResults.Where(n => n.Mine).Select(n =>
            {
                var rx = (n.X - DefaultNetXOffset) * CellSize + n.VecX;
                var ry = (n.Y - DefaultNetYOffset) * CellSize + n.VecY;
                var size = Width - 5;
                var canvasX = size * rx / FullSize;
                var canvasY = size * (FullSize - ry) / FullSize;
                return new {canvasX, canvasY, n};
            }).ToList();


            foreach (var a in canvasCoords)
            {
                var count = canvasCoords.Count(r =>
                {
                    if (r.n.MatchId != a.n.MatchId)
                        return false;
                    var abs = Math.Abs(Math.Abs(a.canvasX - r.canvasX) + Math.Abs(r.canvasY - a.canvasY));
                    return abs < 5;
                });
                if (count >= 4)
                    playerWardResults.Add(a.n);
            }

            foreach (var result in playerWardResults.OrderByDescending(n => n.Time))
            {
                Add(result.X, result.Y, result.VecX, result.VecY, result.Obs, result.Time, result.Mine);
            }
        }
    }
}