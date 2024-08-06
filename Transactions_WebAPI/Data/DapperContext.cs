using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Data;

namespace Transactions_WebAPI.Data
{
    public class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AppDbContext") ?? throw new ArgumentNullException(nameof(configuration), "Connection string cannot be null.");
        }

        public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
    }
}