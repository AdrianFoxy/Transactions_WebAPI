using Transactions_WebAPI.Entities;

namespace Transactions_WebAPI.Interfaces
{
    public interface IFileProcessingService
    {
        List<Transaction> ProcessCsvFile(Stream fileStream);
        Task SaveExcelFile(IEnumerable<Transaction> transactions, FileInfo file);
    }
}
