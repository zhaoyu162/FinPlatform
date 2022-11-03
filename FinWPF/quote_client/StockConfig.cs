using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    static class StockConfig
    {
        static StockConfig()
        {
            try
            {
                //_load_index_groups();
            }
            catch(Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Fatal($"StockConfig LoadFile Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        static void _load_index_groups()
        {
            var files = Directory.EnumerateFiles("index", "*.xls");
            files.ToList().ForEach(file =>
            {
                var list = new HashSet<string>();
                File.ReadAllLines(file).ToList().ForEach(stockWithName =>
                {
                    //if (!indic_filter_list.ContainsKey(stock))
                    //    indic_filter_list.Add(stock, true);
                    var stock = stockWithName.Substring(0, 7);
                    if (!list.Contains(stock))
                        list.Add(stock);
                });

                IndexList.Add(Path.GetFileNameWithoutExtension(file).Substring(0,7), list);
                IndexNameList.Add(Path.GetFileNameWithoutExtension(file).Substring(0, 7), Path.GetFileNameWithoutExtension(file).Substring(8));
            });

            using (var displayed = File.OpenText("index\\displayed.txt"))
            {
                while(!displayed.EndOfStream)
                {
                    var index = displayed.ReadLine();
                    var fields = index.Split(new char[] { ',' });
                    if (fields.Length == 2 && !DisplayedIndicies.ContainsKey(fields[0]))
                    {
                        DisplayedIndicies.Add(fields[0], fields[1]);
                    }
                }
            }
        }

        public static Dictionary<string, HashSet<string>> IndexList = new Dictionary<string, HashSet<string>>();
        public static Dictionary<string, string> IndexNameList = new Dictionary<string, string>();

        public static ConcurrentDictionary<string, FinWPF.Quote.StockEntity> DictSERef
        {
            get;
            set;
        } = new ConcurrentDictionary<string, FinWPF.Quote.StockEntity>();
        public static Dictionary<string, string> DisplayedIndicies = new Dictionary<string, string>();
        public static ConcurrentDictionary<string, Symbol> AllIndicies = new ConcurrentDictionary<string, Symbol>();

        public static ConcurrentDictionary<string, Symbol> Save_list_stocks  { get; } = new ConcurrentDictionary<string, Symbol>();
        public static ConcurrentDictionary<string, RealtimeTick> Save_list_ticks  { get; } = new ConcurrentDictionary<string, RealtimeTick>();
        public static ConcurrentDictionary<string, DealtByOrder> Save_list_dealts  { get; } = new ConcurrentDictionary<string, DealtByOrder>();
    }

    [AddINotifyPropertyChangedInterface]
    public class MarketDataSnapshot
    {
        public string CurrTickSymbol { get; set; }
        public string CurrZbSymbol { get; set; }
        public long TotalTickCount { get; set; }
        public long TotalZbCount { get; set; }
    }
}
