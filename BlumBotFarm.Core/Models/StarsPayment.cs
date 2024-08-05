using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BlumBotFarm.Core.Models
{
    public class StarsPayment
    {
        public int      Id                { get; set; }
        public int      UserId            { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal  AmountUsd         { get; set; }

        public int      AmountStars       { get; set; }
        public DateTime CreatedDateTime   { get; set; }
        public bool     IsCompleted       { get; set; }
        public DateTime CompletedDateTime { get; set; }
    }
}
