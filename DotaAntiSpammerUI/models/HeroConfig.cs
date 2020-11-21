namespace DotaAntiSpammerNet.models
{
    public class HeroConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string localized_name { get; set; }

        public override string ToString()
        {
            return localized_name;
        }
    }
}