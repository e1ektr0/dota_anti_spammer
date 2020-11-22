using System.Collections.Generic;
using System.Windows;
using DotaAntiSpammerCommon.Models;

namespace DotaAntiSpammerNet.Controls.Map
{
    public partial class WardsPanel
    {
        private readonly List<WardMap> _wardMaps;
        public WardsPanel()
        {
            InitializeComponent();
            _wardMaps = new List<WardMap>
            {
                Map1,
                Map2,
                Map3,
                Map4,
                Map5,
            };
        }

        public void Ini(DotaAntiSpammerCommon.Models.Match match, List<PlayerWards> pixels)
        {
            foreach (var wardMap in _wardMaps)
            {
                wardMap.Visibility = Visibility.Collapsed;
            }

            int i = 0;
            foreach (var playerWards in pixels)
            {
                
                var index = match.Players.FindIndex(n => n?.AccountId == playerWards.AccountId);
                _wardMaps[i].Ini(index, playerWards.Wards, playerWards.HeroId);
                i++;
            }
        }
    }
}