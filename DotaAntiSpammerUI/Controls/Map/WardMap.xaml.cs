using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    }

    class CanvasWithBitmap : Canvas
    {
        private readonly List<Ward> _wards = new List<Ward>();
        private Image _obs;
        private Image _sentry;

        protected override void OnRender(DrawingContext dc)
        {
            foreach (var ward in _wards)
            {
                var size = 30;
                var rectangle = new Rect(ward.X-size/2d, ward.Y - size, size, size);
                var radiusX = 8;
                var solidColorBrush = Brushes.Black;
                if (ward.Time < 0)
                {
                    solidColorBrush = Brushes.Chartreuse;
                }
                else if (ward.Time < 20)
                {
                    solidColorBrush = Brushes.Goldenrod;
                }
                else if(ward.Time<60)
                {
                    solidColorBrush = Brushes.Brown;
                }
                dc.DrawEllipse(solidColorBrush, new Pen(), new Point((int) ward.X, (int) ward.Y), radiusX, radiusX);
                var image =ward.Obs? _obs:_sentry;
                dc.DrawImage(image.Source, rectangle);
            }
        }


        public void Add(double canvasX, double canvasY, bool resultObs, int resultTime)
        {
            _wards.Add(new Ward {X = canvasX, Y = canvasY, Obs = resultObs, Time = resultTime});
        }

        public void Ini(Image obs, Image sentry)
        {
            _obs = obs;
            _sentry = sentry;
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

        private void Add(double x, double y, double vx, double vy, bool resultObs, int resultTime)
        {
            var rx = (x - DefaultNetXOffset) * CellSize + vx;
            var ry = (y - DefaultNetYOffset) * CellSize + vy;
            var size = Width - 5;
            var canvasX = size * rx / FullSize;
            var canvasY = size * (FullSize - ry) / FullSize;
            Cnv.Add(canvasX, canvasY, resultObs, resultTime);
        }

        public void Ini(int i, List<WardPlaced> matchPlayerWardResults)
        {
            Cnv.Ini(Obs, Sentry);
            Border.BorderBrush = new SolidColorBrush(PlayerColors.Colors[i]);
            Visibility = Visibility.Visible;

            foreach (var result in matchPlayerWardResults.OrderByDescending(n=>n.Time))
            {
                Add(result.X, result.Y, result.VecX, result.VecY, result.Obs, result.Time);
            }
        }
    }
}