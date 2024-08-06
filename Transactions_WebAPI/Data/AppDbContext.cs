using Microsoft.EntityFrameworkCore;
using Transactions_WebAPI.Entities;

namespace Transactions_WebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Status)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.TransactionDate)
                .HasColumnType("timestamp without time zone");
        }

        public DbSet<Transaction> Transaction { get; set; }
    }
}
