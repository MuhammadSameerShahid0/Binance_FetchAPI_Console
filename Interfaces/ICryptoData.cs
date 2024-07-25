using Console_Binance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console_Binance.Interfaces
{
    public interface ICryptoData
    {
        Task<List<TickerLists>> TickerList24Hr();
        Task<List<CryptoBars>> DataCryptoBars(string ticker);
    }
}
