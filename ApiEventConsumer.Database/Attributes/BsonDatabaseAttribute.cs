namespace ApiEventConsumer.Database.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BsonDatabaseAttribute : Attribute
    {
        public string DatabaseName { get; }

        public BsonDatabaseAttribute(string databaseName)
        {
            DatabaseName = databaseName;
        }
    }
}
