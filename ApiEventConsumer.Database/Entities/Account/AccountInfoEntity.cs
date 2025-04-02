using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities
{
    [BsonCollection("userinfos")]
    public class AccountInfoEntity : MiniGameBaseEntity
    {
        [BsonElement("id")]
        public string UserId { get; set; }

        public string Avatar { get; set; }

        public DateTime JoinedOn { get; set; }
     
        public double Red { get; set; }

        public double RedWin { get; set; }

        public double RedLost { get; set; }

        public double RedPlay { get; set; }      

        [BsonElement("type")]
        public bool IsBot { get; set; }

        public bool OtpFirst { get; set; }

        public int AvatarId { get; set; }

        public string Currency { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("UID")]
        public int UID { get; set; }

        public Security Security { get; set; }

        public bool Disabled { get; set; }

        public bool InRoom { get; set; }

        public string InGameRoom { get; set; }

        public byte Spins { get; set; }

        public bool Banned { get; set; }

        public int PatternId { get; set; }
    }

    public class Security
    {
        public int Login { get; set; }
    }
}