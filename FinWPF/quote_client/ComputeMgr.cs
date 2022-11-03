using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    [AddINotifyPropertyChangedInterface]
    public class ComputeMgr
    {
        public ComputeMgr(string code)
        {
            股票代码 = code;
            var saved = DataSerializer.DeserializeToObject<List<ComputeFields>>("CPM_" + 股票代码);
            if (saved != null)
            {
                saved.ForEach(item => { _fieldsByName.TryAdd(item.时间点, item);
                    if(item.时间点.Contains("东财"))
                        _sortedComputeFieldsEm[item.时间点] = item;
                    else
                        _sortedComputeFieldsComm[item.时间点] = item;
                });
            }

            CalcRealtime();

            //var saved1 = DataSerializer.DeserializeToObject<List<BigOrderComputeFields>>("CPMBO_" + 股票代码);
            //if (saved1 != null)
            //{
            //    saved1.ForEach(item => _fieldsOfBigOrderByName.TryAdd(item.时间点, item));
            //}
        }

        ConcurrentDictionary<string, ComputeFields> _fieldsByName = new ConcurrentDictionary<string, ComputeFields>();
        ConcurrentDictionary<string, BigOrderComputeFields> _fieldsOfBigOrderByName = new ConcurrentDictionary<string, BigOrderComputeFields>();
        internal ConcurrentDictionary<string, ComputeFields> FieldsByName => _fieldsByName;

        internal ConcurrentDictionary<string, BigOrderComputeFields> FieldsOfBOByName => _fieldsOfBigOrderByName;
        public string 股票代码 { get; set; }
        public string 股票名称 { get; set; }

        //public string 普通挡位 { get; set; }
        public string 东财挡位 { get; set; }

        public double 涨幅 { get; set; }
        public double 开盘涨幅 { get; set; }
        public double 主力净额 { get; set; }
        public double 东财主力净额 { get; set; }
        public double 主力差额 { get; set; }

        //[ColumnName("拉升意愿")]
        //public double 主力额比 { get; set; }
        [ColumnName("  东财\n拉升意愿")]
        public double 东财主力额比 { get; set; }
        //public double 大单比 { get; set; }
        public double 东财大单比 { get; set; }

        public int 逐笔 { get; set; }
        public uint 总笔 { get; set; }
        public string 结论 { get; set; }
        public double 流通市值 { get; set; }
        public double 流通股本 { get; set; }
        public double 百万净额 { get; set; }
        public int 百万卖单数 { get; set; }
        public int 百万买单数 { get; set; }
        public double 百万单差 { get; set; }

        [ColumnName("100-300万\n净  额")]
        public double M13净额 { get; set; }
        [ColumnName("100-300万\n买 单 数")]
        public int M13买单数 { get; set; }
        [ColumnName("100-300万\n卖 单 数")]
        public int M13卖单数 { get; set; }
        [ColumnName("100-300万\n单  差")]
        public double M13单差 { get; set; }

        [ColumnName("300-500万\n净额")]
        public double M35净额 { get; set; }
        [ColumnName("300-500万\n买 单 数")]
        public int M35买单数 { get; set; }
        [ColumnName("300-500万\n卖 单 数")]
        public int M35卖单数 { get; set; }
        [ColumnName("300-500万\n单差")]
        public double M35单差 { get; set; }

        [ColumnName("500万\n净额")]
        public double M5净额 { get; set; }
        [ColumnName("500万以上\n买 单 数")]
        public int M5买单数 { get; set; }
        [ColumnName("500万以上\n卖 单 数")]
        public int M5卖单数 { get; set; }
        [ColumnName("500万\n单差")]
        public double M5单差 { get; set; }

        public void CalcRealtime()
        {
            if (StockConfig.DictSERef.TryGetValue(股票代码, out var see))
            {
                lock (see)
                {
                    股票名称 = see.Name;
                    涨幅 = (see.Latest - see.PreClose) / see.PreClose * 100;
                    开盘涨幅 = (see.Open - see.PreClose) / see.PreClose * 100;
                    流通市值 = see.FreeStockCounts * see.Latest;
                    流通股本 = see.FreeStockCounts;
                    总笔 = see.TotalDealtCount;
                }
            }
        }

        SortedList<string, ComputeFields> _sortedComputeFieldsComm = new SortedList<string, ComputeFields>();
        SortedList<string, ComputeFields> _sortedComputeFieldsEm = new SortedList<string, ComputeFields>();

        public async void CalcByTimePoint(string timeStr, ConcurrentDictionary<uint, Order> orders, int zbNumber, 
            Func<Order, double> amountExpress,
            Func<Order, double> amountPTExpress, 
            ConcurrentDictionary<uint, Order> M1orders, 
            ConcurrentDictionary<uint, Order> M3orders, 
            ConcurrentDictionary<uint, Order> M5orders)
        {
            var totalAmount = 0.0;
            var price = 0.0;
            var openPrice = 0.0;
            var lastClose = 0.0;

            if (StockConfig.DictSERef.TryGetValue(股票代码, out var see))
            {
                lock (see)
                {
                    股票名称 = see.Name;
                    总笔 = see.TotalDealtCount;
                    流通市值 = see.FreeStockCounts * see.Latest;
                    price = see.Latest;
                    openPrice = see.Open;
                    totalAmount = 流通市值 > 0 ? 流通市值 : see.TotalAmount;
                    lastClose = see.PreClose;
                }
            }

            var fields = _fieldsByName.GetOrAdd(timeStr, _=>new ComputeFields() { 股票代码 = 股票代码, 股票名称 = 股票名称, 时间点 = timeStr });

            if (fields.逐笔数 > 0)
                return;

            //var fieldsOfBo = _fieldsOfBigOrderByName.GetOrAdd(timeStr, (k) => new BigOrderComputeFields() { 股票代码 = 股票代码, 股票名称 = 股票名称, 时间点 = timeStr });

            // calc field below
            //var bigOrders = orders.Values.Where(odr => odr.OrderPrice * odr.OrderVol >= dblThreshold).GroupBy(odr => odr.IsBuy);
            var wpbOrders = Task.Run(() => orders.Values.Where(odr => odr.OrderAmount >= _weakPowerBuy.Item1 && odr.OrderAmount < _weakPowerBuy.Item2).ToList());
            var mpbOrders = Task.Run(() => orders.Values.Where(odr => odr.OrderAmount >= _mainPowerBuy.Item1 && odr.OrderAmount < _mainPowerBuy.Item2).ToList());
            var mcbOrders = Task.Run(() => orders.Values.Where(odr => odr.OrderAmount >= _mainCompBuy.Item1 && odr.OrderAmount < _mainCompBuy.Item2).ToList());
            await Task.WhenAll(wpbOrders, mpbOrders, mcbOrders);
            //var mpbOrders = orders.Values.Where(odr => {
            //    var dealtAmout = odr.OrderDealts.Sum(odl => odl.Value.DealtVol * odl.Value.DealtPrice);
            //    return dealtAmout >= _mainPowerBuy.Item1 && dealtAmout < _mainPowerBuy.Item2;
            //}).GroupBy(odr => odr.IsBuy);
            //var mcbOrders = orders.Values.Where(odr => odr.OrderPrice * odr.OrderVol >= _mainCompBuy.Item1 && odr.OrderPrice * odr.OrderVol < _mainCompBuy.Item2).GroupBy(odr => odr.IsBuy);
            //var mcbOrders = orders.Values.Where(odr => {
            //    var dealtAmout = odr.OrderDealts.Sum(odl => odl.Value.DealtVol * odl.Value.DealtPrice);
            //    return dealtAmout >= _mainCompBuy.Item1 && dealtAmout < _mainCompBuy.Item2;
            //}).GroupBy(odr => odr.IsBuy);
            var wpbAmount = wpbOrders.Result.Sum(amountExpress);
            var mpbAmount = mpbOrders.Result.Sum(amountExpress);
            var mcbAmount = mcbOrders.Result.Sum(amountExpress);
            //var mcbBuyAmount = mcbOrders.Where(bgk => bgk.Key).FirstOrDefault()?.Select(odr => odr.OrderDealts.Values.Where(dealt => dealt.IsPositive).Sum(dealt => dealt.DealtPrice * dealt.DealtVol * 1.0)).Sum();
            //var wpbSellAmount = wpbOrders.Result.Where(bgk => !bgk.Key).FirstOrDefault()?.Select(amountExpress).Sum() ?? 0;
            //var mpbSellAmount = mpbOrders.Result.Where(bgk => !bgk.Key).FirstOrDefault()?.Select(amountExpress).Sum() ?? 0;
            //var mpbSellAmount = mpbOrders.Where(bgk => !bgk.Key).FirstOrDefault()?.Select(odr => odr.OrderDealts.Values.Where(dealt => dealt.IsPositive).Sum(dealt => dealt.DealtPrice * dealt.DealtVol * 1.0)).Sum();
            //var mcbSellAmount = mcbOrders.Result.Where(bgk => !bgk.Key).FirstOrDefault()?.Select(amountExpress).Sum() ?? 0;
            //var mcbSellAmount = mcbOrders.Where(bgk => !bgk.Key).FirstOrDefault()?.Select(odr => odr.OrderDealts.Values.Where(dealt => dealt.IsPositive).Sum(dealt => dealt.DealtPrice * dealt.DealtVol * 1.0)).Sum();
            fields.主力净额 = mpbAmount + mcbAmount;
            fields.百万净额 = mcbAmount;
            fields.主力额比 = fields.主力净额 / totalAmount * see.Latest * 100;

            //fieldsOfBo.成交额 = se.TotalAmount;

            fields.百万卖单数 = mcbOrders.Result.Where(odr => !odr.IsBuy).Count();
            fields.百万买单数 = mcbOrders.Result.Count - fields.百万卖单数;
            fields.百万单差 = fields.百万买单数 - fields.百万卖单数;

            fields.大单比 = fields.百万净额 / fields.主力净额 * 100;
            fields.逐笔数 = zbNumber;
            if(timeStr.IndexOf("东财") >= 0)
            {
                东财主力净额 = fields.主力净额;
                东财主力额比 = fields.主力额比;
                东财大单比 = fields.大单比;
                _sortedComputeFieldsEm[timeStr] = fields;

                if(_sortedComputeFieldsEm.Count > 1)
                {
                    var firstOne = _sortedComputeFieldsEm.Values[_sortedComputeFieldsEm.Count - 1];
                    var prevOne = _sortedComputeFieldsEm.Values[_sortedComputeFieldsEm.Count - 2];
                    if(!_sortedComputeFieldsEm.ContainsKey("竞价·东财"))
                    {
                        firstOne = _sortedComputeFieldsEm.Values[0];
                    }
                    东财挡位 = ((int)((fields.主力净额 - firstOne.主力净额) / 1000000)).ToString();
                    if (int.Parse((prevOne.挡位?? "0").Replace("*","")) < 0 && int.Parse((东财挡位?? "0").Replace("*","")) > 0)
                        东财挡位 += "*";
                    fields.挡位 = 东财挡位;
                }

                主力净额 = mpbOrders.Result.Sum(amountPTExpress) + mcbOrders.Result.Sum(amountPTExpress);
            }
            else
            {
                主力净额 = fields.主力净额;
                //主力额比 = fields.主力额比;
                //大单比 = fields.大单比;
                _sortedComputeFieldsComm[timeStr] = fields;

                //if (_sortedComputeFieldsComm.Count > 1)
                //{
                //    var firstOne = _sortedComputeFieldsComm.Values[_sortedComputeFieldsComm.Count - 1];
                //    var prevOne = _sortedComputeFieldsComm.Values[_sortedComputeFieldsComm.Count - 2];
                //    if (!_sortedComputeFieldsComm.ContainsKey("竞价·普通"))
                //    {
                //        firstOne = _sortedComputeFieldsComm.Values[0];
                //    }
                //    普通挡位 = ((int)((fields.主力净额 - firstOne.主力净额) / 1000000)).ToString();
                //    if (int.Parse((prevOne.挡位??"0").Replace("*", "")) < 0 && int.Parse((普通挡位?? "0").Replace("*", "")) > 0)
                //        普通挡位 += "*";
                //    fields.挡位 = 普通挡位;
                //}
            }

            //if (东财主力净额 > 0 && 主力净额 > 0)
            //    结论 = 东财主力净额 > 主力净额 ? "好" : "不好";
            //else if (主力净额 * 东财主力净额 < 0)
            //    结论 = 东财主力净额 > 0 ? "好" : "不好";
            //else
            //    结论 = 东财主力净额 > 主力净额 ? "略好" : "不好";

            if (东财主力净额 - 主力净额 > 50000000 && 东财主力净额 > 0)
            {
                结论 = "好";
                if (price / lastClose < 1.03)
                    结论 = "极好";
            }
            else
            {
                结论 = "不好";
            }
            主力差额 = 东财主力净额 - 主力净额;

            百万净额 = fields.百万净额;
            百万单差 = fields.百万单差;
            百万买单数 = fields.百万买单数;
            百万卖单数 = fields.百万卖单数;

            M13买单数 = M1orders.Where(odr => odr.Value.IsBuy).Count();
            M13卖单数 = M1orders.Count - M13买单数;
            M13单差 = M13买单数 - M13卖单数;
            M13净额 = M1orders.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);

            M35买单数 = M3orders.Where(odr=>odr.Value.IsBuy).Count();
            M35卖单数 = M3orders.Count() - M35买单数;
            M35单差 = M35买单数 - M35卖单数;
            M35净额 = M3orders.Select(odr=>odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);

            M5买单数 = M5orders.Where(odr => odr.Value.IsBuy).Count();
            M5卖单数 = M5orders.Count() - M5买单数;
            M5单差 = M5买单数 - M5卖单数;
            M5净额 = M5orders.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);

            fields.M13买单数 = M13买单数;
            fields.M13卖单数 = M13卖单数;
            fields.M13单差 = M13单差;
            fields.M13净额 = M13净额;
            fields.M35买单数 = M35买单数;
            fields.M35卖单数 = M35卖单数;
            fields.M35单差 = M35单差;
            fields.M35净额 = M35净额;

            fields.M5买单数 = M5买单数;
            fields.M5卖单数 = M5卖单数;
            fields.M5单差 = M5单差;
            fields.M5净额 = M5净额;

            //fieldsOfBo.M35买单数 = M35买单数;
            //fieldsOfBo.M35卖单数 = M35卖单数;
            //fieldsOfBo.M35单差 = M35单差;
            //fieldsOfBo.M35额差 = M35额差;
            //fieldsOfBo.M35额比 = M35额比;
            //fieldsOfBo.M5买单数 = M5买单数;
            //fieldsOfBo.M5卖单数 = M5卖单数;
            //fieldsOfBo.M5单差 = M5单差;
            //fieldsOfBo.M5额差 = M5额差;
            //fieldsOfBo.M5额比 = M5额比;
            //fieldsOfBo.M3百万额比 = M35百万额比 = M35额差 / fields.百万净额 * 100;
            //fieldsOfBo.M5百万额比 = M5百万额比 = M5额差 / fields.百万净额 * 100;

            DataSerializer.SerializeToFile("CPM_" + 股票代码, _fieldsByName.Values.ToList());
            //DataSerializer.SerializeToFile("CPMBO_" + 股票代码, _fieldsOfBigOrderByName.Values.ToList());
        }

        readonly (double, double) _weakPowerBuy = (0, 200000);
        readonly (double, double) _mainPowerBuy = (200000, 1000000);
        readonly (double, double) _mainCompBuy = (1000000, 10000000000);
    }
}
