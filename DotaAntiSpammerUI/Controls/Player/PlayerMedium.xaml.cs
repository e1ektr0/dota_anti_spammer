﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DotaAntiSpammerNet.Controls.Hero;
using DotaAntiSpammerNet.models;

namespace DotaAntiSpammerNet.Controls.Player
{
    public partial class PlayerMedium
    {
        private readonly List<HeroMedium> _heroes;

        public PlayerMedium()
        {
            InitializeComponent();
            _heroes = new List<HeroMedium>
            {
                Hero0,
                Hero1,
                Hero2
            };
        }

        public void Ini(int i, DotaAntiSpammerCommon.Models.Player player)
        {
            if (player == null)
            {
                Games.Text = "No info";
                WinRate.Text = "";
                foreach (var heroMedium in _heroes)
                {
                    heroMedium.Ini(null);
                }
                return;
            }
            Border.BorderBrush = new SolidColorBrush(PlayerColors.Colors[i]);
            for (var j = 0; j < _heroes.Count; j++)
            {

                if (player.Heroes.Count > j)
                {

                    var playerHero = player.Heroes[j];
                    _heroes[j].Ini(playerHero);                    
                }
                else
                {
                    _heroes[j].Ini(null);                    

                }
            }

            Games.Text = $"{player.TotalGames}";
            WinRate.Text = $"{player.WinRate:0.00}%";
        }
    }
}