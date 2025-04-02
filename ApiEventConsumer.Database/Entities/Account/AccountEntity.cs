using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities
{
    [BsonCollection("users")]
    public class AccountEntity : MiniGameBaseEntity
    {
        [BsonElement("local")]
        public LocalAccount Local { get; set; }

        public string UserIdStr { get; set; }

        public string OperatorId { get; set; }

        public string PlayerId { get; set; }
    }

    public class LocalAccount
    {
        [BsonElement("ban_pass")]
        public int Ban_pass { get; set; }

        [BsonElement("ban_login")]
        public bool Ban_login { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("regDate")]
        public DateTime regDate { get; set; }

        [BsonElement("memberId")]
        public string MemberId { get; set; }

        [BsonElement("memberName")]
        public string MemberName { get; set; }

        [BsonElement("language")]
        public string Language { get; set; }
    }
}