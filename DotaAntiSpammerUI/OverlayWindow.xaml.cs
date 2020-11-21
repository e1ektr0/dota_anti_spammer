using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using DotaAntiSpammerCommon.Models;
using DotaAntiSpammerNet.Common;
using DotaAntiSpammerNet.models;
using DotaAntiSpammerPickDetector;

namespace DotaAntiSpammerNet
{
    public sealed partial class OverlayWindow
    {
        private bool _notShowedYet;
        private Match _currentMatch;

        public OverlayWindow()
        {
            InitializeComponent();
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            if (Width < screenWidth)
            {
                var windowWidth = Width;
                Left = screenWidth / 2 - windowWidth / 2;
            }
            else
            {
                Left = 0;
                var delta = Width - screenWidth;
                var midColumnWidth = Match.MidColumn.Width.Value - delta * 2;
                Match.MidColumn.Width = new GridLength(midColumnWidth, GridUnitType.Pixel);
                Width = Width - delta;
            }

            _notShowedYet = true;
            CreateTray();
        }

        private void CreateTray()
        {
            var trayIcon = new NotifyIcon {Text = @"Dota Anti Spammer"};
            var trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", (sender, args) => { Close(); });

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            trayIcon.Icon = new Icon("dota_anti_spam.ico");
            trayIcon.DoubleClick += (sender, args) => { ShowHideInvoke(); };
        }


        /// <summary>
        ///     Raises the <see cref="E:System.Windows.Window.SourceInitialized" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.MakeWindowTransparent();
        }

        public void Ini(Match match)
        {
            _currentMatch = match;
            Match.Ini(match);
        }

        public void ShowHideInvoke()
        {
            Dispatcher.Invoke(() =>
            {
                if (_notShowedYet)
                {
                    Show();
                    _notShowedYet = false;
                    return;
                }

                if (Visibility == Visibility.Hidden || Visibility == Visibility.Collapsed)
                    Show();
                else
                    Hide();
            });
        }

        public void ShowInvoke()
        {
            Dispatcher?.Invoke(Show);
            _notShowedYet = false;
        }

        public void Map()
        {
            if (WardsPanel.Visibility == Visibility.Collapsed)
            {
//                Match.Visibility = Visibility.Collapsed;
                WardsPanel.Visibility = Visibility.Visible;
            }
            else
            {
//                Match.Visibility = Visibility.Visible;
                WardsPanel.Visibility = Visibility.Collapsed;
            }
        }

        public void WardIni(List<PlayerWards> pixels)
        {
            Dispatcher.Invoke(() =>
            {
                if (pixels.Any(n => n.Wards.Any()))
                    WardsPanel.Ini(_currentMatch, pixels);
                WardsPanel.Visibility = Visibility.Visible;
            });
        }
    }
}