using CsvHelper;
using GeoTimeZone;
using OfficeOpenXml;
using System.Globalization;
using Transactions_WebAPI.Entities;
using Transactions_WebAPI.Interfaces;

namespace Transactions_WebAPI.Services
{
    public class FileProcessingService : IFileProcessingService
    {
        public List<Transaction> ProcessCsvFile(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<TransactionCsv>().ToList();
            var transactions = new List<Transaction>();

            foreach (var record in records)
            {
                var coordinates = record.ClientLocation.Split(',')
                    .Select(coord => coord.Trim())
                    .Select(coord => double.Parse(coord, CultureInfo.InvariantCulture))
                    .ToArray();

                var timeZoneId = TimeZoneLookup.GetTimeZone(coordinates[0], coordinates[1]).Result;

                var transaction = new Transaction
                {
                    TransactionId = record.TransactionId,
                    Name = record.Name,
                    Email = record.Email,
                    Amount = record.Amount,
                    TransactionDate = record.TransactionDate,
                    ClientLocation = record.ClientLocation,
                    TimeZone = timeZoneId,
                    Status = record.Status
                };

                transactions.Add(transaction);
            }

            return transactions;
        }

        public async Task SaveExcelFile(IEnumerable<Transaction> transactions, FileInfo file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(file);
            var worksheet = package.Workbook.Worksheets.Add("Transactions");
            worksheet.Cells.LoadFromCollection(transactions, true);

            // Formats the header
            worksheet.Cells.AutoFitColumns();
            worksheet.Row(1).Style.Font.Size = 12;
            worksheet.Row(1).Style.Font.Bold = true;

            // Find the column index for TransactionDate and format the TransactionDate column as date
            var transactionDateColumnIndex = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column]
                .First(cell => cell.Text == nameof(Transaction.TransactionDate)).Start.Column;

            worksheet.Column(transactionDateColumnIndex).Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Column(transactionDateColumnIndex).Width = Math.Max(worksheet.Column(transactionDateColumnIndex).Width, 20);

            await package.SaveAsync();
        }
    }
}