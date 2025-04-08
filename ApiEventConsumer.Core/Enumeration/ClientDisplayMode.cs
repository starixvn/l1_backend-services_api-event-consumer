namespace ApiEventConsumer.Core.Enumeration
{
    public class ClientDisplayMode : Enumeration
    {
        public const string PortraitValue = "2";
        public const string LandscapeValue = "1";

        public static readonly Role Portrait = new Role(PortraitValue, "Portrait");
        public static readonly Role Landscape = new Role(LandscapeValue, "Landscape");

        public ClientDisplayMode() : base("default", "Default Display Mode")
        {
        }

        public ClientDisplayMode(string value, string displayName)
            : base(value, displayName)
        {
        }
    }
}
