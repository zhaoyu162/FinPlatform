using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF
{
    [Table("kline")]
    internal class QuestKLineModel
    {
        public DateTimeOffset TimeStamp { get; set; }
        public DateTimeOffset ds { get; set; }
        public string kind { get; set; }
        public string code { get; set; }
        public double open { get; set; }
        public double close { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double volume { get; set; }
        public double amount { get; set; }
    }

    internal class QuestRealtimeTickModel
    {
    }
}
