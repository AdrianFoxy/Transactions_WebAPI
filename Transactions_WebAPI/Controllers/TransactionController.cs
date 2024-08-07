using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TimeZoneConverter;
using Transactions_WebAPI.Data;
using Transactions_WebAPI.Entities;
using Transactions_WebAPI.Interfaces;

namespace Transactions_WebAPI.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly DapperContext _dapperContext;
        private readonly IFileProcessingService _fileProcessingService;

        public TransactionController(IFileProcessingService fileProcessingService, DapperContext dapperContext)
        {
            _fileProcessingService = fileProcessingService;
            _dapperContext = dapperContext;
        }

        /// <summary>
        /// Import data from a CSV file to the database.
        /// </summary>
        /// <remarks>
        /// NOTE: The CSV file has been updated and a status column has been added, so use the correct dataset. Resources/import/dataset
        ///
        /// Also if there is a record with a similar transaction_id in the database - the transaction status is updated to the status of new transaction.
        /// </remarks>
        /// <param name="file">The CSV file containing transactions.</param>
        /// <returns>This endpoint returns a list of imported transactions.</returns>
        [HttpPost("import")]
        public async Task<ActionResult<List<Transaction>>> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("The uploaded file is not a CSV.");

            var transactions = _fileProcessingService.ProcessCsvFile(file.OpenReadStream());
            using var connection = _dapperContext.CreateConnection();

            foreach (var transaction in transactions)
            {
                var query = @"
                    INSERT INTO ""Transaction"" 
                    (
                        ""TransactionId"", ""Name"", ""Email"", ""Amount"", 
                        ""TransactionDate"", ""ClientLocation"", ""TimeZone"", ""Status""
                    )
                    VALUES 
                    (
                        @TransactionId, @Name, @Email, @Amount, 
                        @TransactionDate, @ClientLocation, @TimeZone, @Status
                    )
                    ON CONFLICT (""TransactionId"") DO UPDATE SET
                        ""Status"" = EXCLUDED.""Status"";
                ";

                await connection.ExecuteAsync(query, transaction);
            }

            return Ok(transactions);
        }

        /// <summary>
        /// Export all transactions to an Excel file.
        /// </summary>
        /// <remarks>
        /// The generated Excel file is saved in the Resources/export directory.
        /// </remarks>
        /// <returns>Returns an Ok result if the export is successful.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportToExcel()
        {
            using var connection = _dapperContext.CreateConnection();
            var result = await connection.QueryAsync<Transaction>("SELECT * FROM \"Transaction\"");

            if (result == null || !result.Any())
                return NotFound("No transactions found.");

            var filePath = CreateExportFilePath($"Transactions_Report_{DateTime.Now:yyyyMMddHHmmss}");
            await _fileProcessingService.SaveExcelFile(result, new FileInfo(filePath));

            return Ok();
        }

        /// <summary>
        /// Retrieves transactions within a specified date range, converted to the user's time zone.
        /// </summary>
        /// <remarks>
        /// NOTE: The user's time zone is IANA time zone format (e.g. "Europe/Kiev").
        ///
        /// Example data for request: startDate: 2023-12-31 00:00:00, endDate: 2024-01-01 23:59:59, userTimeZone: "Europe/Kiev"
        /// </remarks>
        /// <returns>Returns a list of transactions that fall within the specified date range, converted to the user's time zone.</returns>
        [HttpGet("date-range-user-time-zone")]
        public async Task<ActionResult<List<Transaction>>> GetTransactionsInDateRangeUserTimeZone
            ([Required] DateTime startDate, [Required] DateTime endDate, string userTimeZone, bool excelExport = false)
        {
            using var connection = _dapperContext.CreateConnection();
            var transactions = await connection.QueryAsync<Transaction>("SELECT * FROM \"Transaction\"");

            TimeZoneInfo userTimeZoneInfo = TZConvert.GetTimeZoneInfo(userTimeZone);

            var filteredTransactions = transactions.Where(t =>
            {
                // IANA timezones prefixed with Etc/GMT are reversed
                var timeZone = t.TimeZone;
                if (timeZone.StartsWith("Etc/GMT"))
                    timeZone = timeZone.Replace("+", "TEMP").Replace("-", "+").Replace("TEMP", "-");

                TimeZoneInfo clientTimeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
                DateTime clientLocalTime = TimeZoneInfo.ConvertTime(t.TransactionDate, clientTimeZoneInfo, userTimeZoneInfo);
                return clientLocalTime >= startDate && clientLocalTime <= endDate;
            })
            .OrderBy(t => t.TransactionDate)
            .ToList();

            if (excelExport)
            {
                var filePath = CreateExportFilePath($"Transactions_Report_{DateTime.Now:yyyyMMddHHmmss}");
                await _fileProcessingService.SaveExcelFile(filteredTransactions, new FileInfo(filePath));
            }

            return Ok(filteredTransactions);
        }

        /// <summary>
        /// Retrieves transactions within a specified date range, by transaction's local time.
        /// </summary>
        /// <remarks>
        /// Example data for request: startDate: 2023-12-31 00:00:00, endDate: 2024-01-01 23:59:59
        /// </remarks>
        /// <returns>Returns a list of transactions that fall within the specified date range, by transaction's local time.</returns>
        [HttpGet("date-range")]
        public async Task<ActionResult<List<Transaction>>> GetTransactionsInDateRange
            ([Required]DateTime startDate, [Required]DateTime endDate, bool excelExport = false)
        {
            using var connection = _dapperContext.CreateConnection();
            var query = @"
                SELECT * FROM ""Transaction""
                WHERE ""TransactionDate"" >= @StartDate AND ""TransactionDate"" <= @EndDate
                ORDER BY ""TransactionDate"";
            ";

            var transactions = await connection.QueryAsync<Transaction>(query, new { StartDate = startDate, EndDate = endDate });

            if (excelExport)
            {
                var filePath = CreateExportFilePath($"Transactions_Report_{DateTime.Now:yyyyMMddHHmmss}");
                await _fileProcessingService.SaveExcelFile(transactions, new FileInfo(filePath));
            }

            return Ok(transactions.ToList());
        }

        /// <summary>
        /// Retrieves transactions within a January 2024.
        /// </summary>
        /// <returns>Returns a list of transactions January 2024.</returns>
        [HttpGet("january-2024")]
        public async Task<ActionResult<List<Transaction>>> GetTransactionsForJanuary2024(bool excelExport = false)
        {
            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 1, 31).AddDays(1).AddTicks(-1); // End of January 31

            using var connection = _dapperContext.CreateConnection();
            var query = @"
                SELECT * FROM ""Transaction""
                WHERE ""TransactionDate"" >= @StartDate AND ""TransactionDate"" <= @EndDate
                ORDER BY ""TransactionDate"";
            ";

            var transactions = await connection.QueryAsync<Transaction>(query, new { StartDate = startDate, EndDate = endDate });

            if (excelExport)
            {
                var filePath = CreateExportFilePath($"Transactions_Report_{DateTime.Now:yyyyMMddHHmmss}");
                await _fileProcessingService.SaveExcelFile(transactions, new FileInfo(filePath));
            }
            return Ok(transactions.ToList());
        }

        /// <summary>
        /// For tests. Deletes all transactions from the database.
        /// </summary>
        /// <returns>Returns an Ok result with a message indicating that all transactions have been deleted.</returns>
        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllTransactions()
        {
            using var connection = _dapperContext.CreateConnection();
            var query = "DELETE FROM \"Transaction\"";
            await connection.ExecuteAsync(query);
            return Ok("All transactions have been deleted.");
        }

        private string CreateExportFilePath(string fileName)
        {
            var resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "export");
            Directory.CreateDirectory(resourcesPath);
            return Path.Combine(resourcesPath, $"{fileName}.xlsx");
        }
    }
}