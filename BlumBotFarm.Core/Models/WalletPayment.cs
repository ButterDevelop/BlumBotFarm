using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BlumBotFarm.Core.Models
{
    public class WalletPayment
    {
        public int       Id                     { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal   AmountUsd              { get; set; }

        public string    AutoConversionCurrency { get; set; } = "USDT";
        public string    Description            { get; set; } = "Top up your balance";
        public string    ReturnUrl              { get; set; } = "https://t.me/autoblumfarmbot";
        public string    FailReturnUrl          { get; set; } = "https://t.me/wallet";
        public string    CustomData             { get; set; } = string.Empty;
        public string    ExternalId             { get; set; } = string.Empty;
        public int       TimeoutSeconds         { get; set; } = 10800;
        public int       CustomerTelegramId     { get; set; }
        
        public long?     WalletOrderId          { get; set; }
        public string?   Status                 { get; set; } = string.Empty;
        public string?   OrderNumber            { get; set; } = string.Empty;
        public DateTime? CreatedDateTime        { get; set; }
        public DateTime? ExpirationDateTime     { get; set; }
        public DateTime? CompletedDateTime      { get; set; }
        public string?   PayLink                { get; set; } = string.Empty;
        public string?   DirectPayLink          { get; set; } = string.Empty;
    }
}
