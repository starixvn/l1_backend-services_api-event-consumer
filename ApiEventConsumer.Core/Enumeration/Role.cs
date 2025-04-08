namespace ApiEventConsumer.Core.Enumeration
{
    public class Role : Enumeration
    {
        public const string OperationValue = "operation";
        public const string AdminValue = "admin";

        public static readonly Role Operation = new Role(OperationValue, "Operation");
        public static readonly Role Admin = new Role(AdminValue, "Admin");


        public Role() : base("default", "Default Role")
        {
        }

        public Role(string value, string displayName)
            : base(value, displayName)
        {
        }
    }
}
