using System.ComponentModel.DataAnnotations;

namespace Transactions_WebAPI.Entities
{
    public class Transaction
    {
        [Key]
        public string TransactionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ClientLocation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
    }
}
