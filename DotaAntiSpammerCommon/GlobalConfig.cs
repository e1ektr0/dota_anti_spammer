using System;

namespace DotaAntiSpammerCommon
{
    public static class GlobalConfig
    {
        public static string ApiUrl { get; } = "http://" + GetIp() + ":5003";

        private static string GetIp()
        {
            var ip = Environment.GetEnvironmentVariable("dota_server_ip");
            return ip ?? "194.87.103.72";
        }

        public static string StatsUrl { get; } = "/stats";
        public static string WardsUrl { get; } = "/stats/wards";
    }
}