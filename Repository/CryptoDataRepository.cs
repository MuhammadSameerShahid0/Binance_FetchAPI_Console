using Console_Binance.DataContext;
using Console_Binance.Interfaces;
using Console_Binance.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Console_Binance.Repository
{
    public class CryptoDataRepository : ICryptoData
    {
        private readonly HttpClient _httpClient;
        private readonly CryptoDataContext _context;

        public CryptoDataRepository(HttpClient httpClient, CryptoDataContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        public async Task<List<CryptoBars>> DataCryptoBars(string ticker)
        {
            var BarsCrypto = new List<CryptoBars>();
            var endTime = DateTimeOffset.UtcNow;
            var startTime = endTime.AddDays(-30);

            if (await CheckDataExistingBars(ticker, startTime, endTime))
            {
                Console.WriteLine($"Data for ticker '{ticker}' already exists in the database");
                return BarsCrypto;
            }

            while (startTime < endTime)
            {
                var nextEndTime = startTime.AddHours(8);
                if (nextEndTime > endTime) nextEndTime = endTime;

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.binance.com/api/v3/klines?symbol={ticker}&interval=1m&startTime={startTime.ToUnixTimeMilliseconds()}&endTime={nextEndTime.ToUnixTimeMilliseconds()}");
                try
                {
                    BarsCrypto = new List<CryptoBars>();
                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonstring = await response.Content.ReadAsStringAsync();
                        var jsonArray = JArray.Parse(jsonstring);
                        foreach (var item in jsonArray)
                        {
                            var utcTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].Value<long>()).UtcDateTime;
                            var estTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcTime, "Eastern Standard Time");

                            var cryptobar = new CryptoBars
                            {
                                Ticker = ticker,
                                Timestamp = estTime,
                                Open = item[1].Value<decimal>(),
                                High = item[2].Value<decimal>(),
                                Low = item[3].Value<decimal>(),
                                Close = item[4].Value<decimal>(),
                                Volume = item[5].Value<decimal>(),
                                Quote_Volume = item[10].Value<decimal>(),
                            };

                            BarsCrypto.Add(cryptobar);
                        }
                        await _context.CryptoBars.AddRangeAsync(BarsCrypto);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine($"No data found for ticker {ticker}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                startTime = nextEndTime;
            }
            return BarsCrypto;
        }

        public async Task<List<TickerLists>> TickerList24Hr()
        {
            var tickerslist = new List<TickerLists>();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.binance.com/api/v3/ticker/24hr");

            try
            {
                int existingTickerCount = await _context.CryptoTickerList.CountAsync();
                if (existingTickerCount < 100)
                {
                    tickerslist = new List<TickerLists>();
                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonstring = await response.Content.ReadAsStringAsync();
                        var jsonArray = JArray.Parse(jsonstring);
                        int count = existingTickerCount;
                        foreach (var item in jsonArray)
                        {
                            if (count == 100) { break; }
                            var symbol = item["symbol"].ToString();
                            var volume = Convert.ToDecimal(item["volume"].ToString());
                            if (symbol.Contains("USDT") && volume != 0)
                            {
                                var ticker = new TickerLists
                                {
                                    Symbol = symbol,
                                    Volume = volume
                                };
                                if (await CheckingExistingDataTicker(ticker.Symbol))
                                {
                                    Console.WriteLine($"Symbol {ticker.Symbol} already exists in the database.");
                                }
                                else
                                {
                                    tickerslist.Add(ticker);
                                    count++;
                                }
                            }
                        }
                        await _context.CryptoTickerList.AddRangeAsync(tickerslist);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine("No data found from API");
                    }
                }
                else
                {
                    Console.WriteLine("Database already contains 100 tickers.");
                }
                var allTickers = await _context.CryptoTickerList.ToListAsync();
                foreach (var ticker in allTickers)
                {
                    Console.WriteLine($"Fetching data for {ticker.Symbol}");
                    await DataCryptoBars(ticker.Symbol);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
            }
            return tickerslist;
        }

        private async Task<bool> CheckDataExistingBars(string ticker, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            return await _context.CryptoBars
                .AnyAsync(b => b.Ticker == ticker && b.Timestamp >= startTime.UtcDateTime && b.Timestamp <= endTime.UtcDateTime);
        }

        private async Task<bool> CheckingExistingDataTicker(string symbol)
        {
            return await _context.CryptoTickerList.AnyAsync(s => s.Symbol == symbol);
        }
    }
}
