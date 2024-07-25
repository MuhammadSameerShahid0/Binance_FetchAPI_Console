using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console_Binance.Models
{
    public class TickerLists
    {
        [Key]
        public int Id { get; set; }
        public string Symbol { get; set; }
        public decimal Volume { get; set; }
    }
}
