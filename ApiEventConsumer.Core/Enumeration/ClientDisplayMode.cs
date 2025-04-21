namespace ApiEventConsumer.Core.Enumeration
{
    public class ClientDisplayMode : Enumeration
    {
        public const string PortraitValue = "2";
        public const string LandscapeValue = "1";

        public static readonly ClientDisplayMode Portrait = new(PortraitValue, "Portrait");
        public static readonly ClientDisplayMode Landscape = new (LandscapeValue, "Landscape");

        public ClientDisplayMode() : base("default", "Default Display Mode")
        {
        }

        public ClientDisplayMode(string value, string displayName)
            : base(value, displayName)
        {
        }
    }
}
