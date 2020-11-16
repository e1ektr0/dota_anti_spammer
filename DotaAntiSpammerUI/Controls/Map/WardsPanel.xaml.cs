using System.Collections.Generic;
using System.Linq;
using System.Windows;

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

        public void Ini(DotaAntiSpammerCommon.Models.Match match)
        {
            var x = 0;
            foreach (var wardMap in _wardMaps)
            {
                wardMap.Visibility = Visibility.Collapsed;
            }
            
            for (var i = 0; i < match.Players.Count; i++)
            {
                var matchPlayer = match.Players[i];
                if(matchPlayer?.WardResults == null || !matchPlayer.WardResults.Any())
                    continue;

                _wardMaps[x].Ini(i, matchPlayer.WardResults);
                x++;
            }
        }
    }
}