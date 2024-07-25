using Console_Binance.DataContext;
using Console_Binance.Interfaces;
using Console_Binance.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Console_Binance
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.AddHttpClient();

            services.AddDbContext<CryptoDataContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            services.AddScoped<ICryptoData, CryptoDataRepository>();
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var repository = serviceProvider.GetService<ICryptoData>();
                try
                {
                    var symbols = await repository.TickerList24Hr();
                    foreach (var symbol in symbols)
                    {
                        Console.WriteLine($"Fetching data for {symbol.Symbol}");
                        await repository.DataCryptoBars(symbol.Symbol);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
