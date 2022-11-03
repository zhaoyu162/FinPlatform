using FinWPF.Quote;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF
{
    public class DBSaver
    {
        public DBSaver()
        {
            _modelForSymbols.Configuration.AutoDetectChangesEnabled = false;
        }

        public async Task<int> SaveTicks(List<StockEntity> entities)
        {
            var model = new FinDBEntities();
            model.Configuration.AutoDetectChangesEnabled = false;
            model.Database.CommandTimeout = 60;
            HashSet<string> dict = new HashSet<string>();

            entities.ForEach(entity =>
            {
                var key = $"{entity.Code}-{entity.QuoteTime.ToString("yyyyMMddHHmmss")}";
                if (!dict.Contains(key))
                {
                    var rtt = new RealtimeTick()
                    {
                        BuyPrices = entity.BidPairs.Select(bp => bp.Price.ToString("0.000")).Aggregate((p1, p2) => p1 + "," + p2),
                        BuyVolms = entity.BidPairs.Select(bp => bp.Count.ToString()).Aggregate((p1, p2) => p1 + "," + p2),
                        SellPrices = entity.AskPairs.Select(bp => bp.Price.ToString("0.000")).Aggregate((p1, p2) => p1 + "," + p2),
                        SellVolms = entity.AskPairs.Select(bp => bp.Count.ToString()).Aggregate((p1, p2) => p1 + "," + p2),
                        ClosePrice = entity.PreClose,
                        CurrVolm = entity.CurrVolume,
                        DealtVol = entity.TotalVolume,
                        HighPrice = entity.Highest,
                        LowPrice = entity.Lowest,
                        LowerLimitPrice = entity.LimitLow,
                        UpperLimitPrice = entity.LimitHigh,
                        OpenPrice = entity.Open,
                        SymbolCode = entity.UniqueCode,
                        SymbolID = entity.SymbId,
                        SymbolName = entity.Name,
                        TickTime = entity.QuoteTime
                    };

                    model.Entry(rtt).State = System.Data.Entity.EntityState.Added;
                    dict.Add(key);
                }
            });
            return await model.SaveChangesAsync();
        }

        public List<Symbol> LoadSymbols()
        {
            return _modelForSymbols.Symbol.ToList();
        }

        public List<RealtimeTick> LoadTicks()
        {
            //var model = new FinDBEntities();
            //return model.RealtimeTick.OrderByDescending(rtt => rtt.ID).Take(10000).ToList();
            return new List<RealtimeTick>();
        }

        public async Task<int> SaveSymbols(List<Symbol> ListOfSymb)
        {
            using(await _asyncSaverSymbol.EnterAsync())
            {
                ListOfSymb.ForEach(symb => { if (symb.ID == 0) {
                        _modelForSymbols.Entry(symb).State = System.Data.Entity.EntityState.Added;
                    } });

                return await _modelForSymbols.SaveChangesAsync();
            }
        }

        public async Task<int> SaveDealts(List<DealtByOrder> dealts)
        {
            var model = new FinDBEntities();
            model.Configuration.AutoDetectChangesEnabled = false;
            HashSet<string> dict = new HashSet<string>();
            dealts.ForEach(entity =>
            {
                var key = $"{entity.SymbolCode}-{entity.DealtDate.ToString("yyyyMMdd")}-{entity.DealtId}";
                if (!dict.Contains(key))
                {
                    model.Entry(entity).State = System.Data.Entity.EntityState.Added;
                    dict.Add(key);
                }
            });
            return await model.SaveChangesAsync();
        }

        FinDBEntities _modelForSymbols = new FinDBEntities();
        AsyncMonitor _asyncSaverSymbol = new AsyncMonitor();
    }
}
