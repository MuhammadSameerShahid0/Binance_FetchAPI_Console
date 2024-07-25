using Console_Binance.Models;
using Microsoft.EntityFrameworkCore;

namespace Console_Binance.DataContext
{
    public class CryptoDataContext : DbContext
    {
        public DbSet<TickerLists> CryptoTickerList { get; set; }
        public DbSet<CryptoBars> CryptoBars { get; set; }
        public CryptoDataContext(DbContextOptions<CryptoDataContext> options) : base(options)
        { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TickerLists>()
                .Property(a => a.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
