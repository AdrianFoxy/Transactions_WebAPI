using CsvHelper.Configuration.Attributes;

namespace Transactions_WebAPI.Entities
{
    public class TransactionCsv
    {
        [Name("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [Name("name")]
        public string Name { get; set; } = string.Empty;

        [Name("email")]
        public string Email { get; set; } = string.Empty;

        [Name("amount")]
        [TypeConverter(typeof(CurrencyConverter))]
        public decimal Amount { get; set; }

        [Name("transaction_date")]
        public DateTime TransactionDate { get; set; }

        [Name("client_location")]
        public string ClientLocation { get; set; } = string.Empty;

        [Name("status")]
        public string Status { get; set; } = string.Empty;
    }
}
