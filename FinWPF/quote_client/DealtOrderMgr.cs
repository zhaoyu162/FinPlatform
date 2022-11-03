using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    public class DealtOrderMgr
    {
        public DealtOrderMgr()
        {
            _player.Load();
        }

        public void LoadDealts()
        {
            var dealtfiles = Directory.EnumerateFiles($"{DateTime.Today:yyyyMMdd}", "DOM_*");
            dealtfiles.ToList().AsParallel().ForAll(file => {
                var stock = file.Split(new char[] { '\\' })[1].Replace(".bin", "");
                var saved = DataSerializer.DeserializeToObject<List<DealtOrder>>(stock);
                if (null != saved)
                {
                    var code = stock.Replace("DOM_", "");
                    if (computeMgrByCode.ContainsKey(code))
                    {
                        var dict = _stocksWithDealtOrders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, DealtOrder>());
                        foreach (var item in saved.OrderBy(item => item.DealtId))
                        {
                            dict.TryAdd(item.DealtId, item);
                            if (item.BuyOrderVol > 0)
                                _updateOrders(code, item);
                        };
                    }
                }
            });
        }

        public void LoadDealts(string code)
        {
            try
            {
                var saved = DataSerializer.DeserializeToObject<List<DealtOrder>>($"DOM_{code}");
                if (null != saved)
                {
                    if (computeMgrByCode.ContainsKey(code))
                    {
                        var dict = _stocksWithDealtOrders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, DealtOrder>());
                        foreach (var item in saved.OrderBy(item => item.DealtId))
                        {
                            dict.TryAdd(item.DealtId, item);
                            if (item.BuyOrderVol > 0)
                                _updateOrders(code, item);
                        };
                    }
                }
            }
            catch
            {

            }
        }


        public void SaveDealts()
        {
            _stocksWithDealtOrders.ToList().ForEach(stock => {
                DataSerializer.SerializeToFile($"DOM_{stock.Key}", stock.Value.Values.ToList());
            });
        }

        ConcurrentDictionary<string, ConcurrentDictionary<uint, DealtOrder>> 
            _stocksWithDealtOrders = new ConcurrentDictionary<string, ConcurrentDictionary<uint, DealtOrder>>();
        ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>> _AllOrders = new ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>>();
        ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>> _M1Orders = new ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>>();
        ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>> _M3Orders = new ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>>();
        ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>> _M5Orders = new ConcurrentDictionary<string, ConcurrentDictionary<uint, Order>>();

        SoundPlayer _player = new SoundPlayer("ALERT-SOUND1.wav");

        public void AddOrUpdateByDealt(string code, uint dealtId, int vol, float price, uint dateTime)
        {
            if (!ComputeMgrByCode.TryGetValue(code, out _))
                return;

            var dealtOrders = _stocksWithDealtOrders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, DealtOrder>());
            var dealtOrder = dealtOrders.GetOrAdd(dealtId, _ => new DealtOrder());

            lock (dealtOrder)
            {
                dealtOrder.IsBuyPositive = vol > 0;
                dealtOrder.DealtVol = (uint)Math.Abs(vol);
                dealtOrder.DealtTime = DateTimeOffset.FromUnixTimeSeconds(dateTime).LocalDateTime;
                dealtOrder.DealtId = dealtId;
                dealtOrder.DealtPrice = price;
                if (dealtOrder.BuyOrderVol > 0)
                {
                    _updateOrders(code, dealtOrder);
                }
            }
        }

        public void AddOrUpdateByOrder(string code, uint dealtId, uint bVol, float bPrice, uint boId, uint sVol, float sPrice, uint soId)
        {
            if (!ComputeMgrByCode.TryGetValue(code, out _))
                return;

            var dealtOrders = _stocksWithDealtOrders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, DealtOrder>());
            var dealtOrder = dealtOrders.GetOrAdd(dealtId, _ => new DealtOrder());
            lock (dealtOrder)
            {
                dealtOrder.SellOrderId = soId;
                dealtOrder.BuyOrderId = boId;
                dealtOrder.SellOrderPrice = sPrice;
                dealtOrder.BuyOrderPrice = bPrice;
                dealtOrder.SellOrderVol = sVol;
                dealtOrder.BuyOrderVol = bVol;
                dealtOrder.DealtId = dealtId;

                if (dealtOrder.DealtVol > 0)
                {
                    _updateOrders(code, dealtOrder);
                }
            }
        }

        public void CalcRealtime(string code)
        {
            if(computeMgrByCode.TryGetValue(code, out var cmgr))
            {
                cmgr.CalcRealtime();
            }
        }

        void _updateOrders(string code, DealtOrder dealtOrder)
        {
            var orders = _AllOrders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, Order>());
            var order = orders.GetOrAdd(dealtOrder.BuyOrderId, _ => new Order());
            var orderBuy = order;
            var cmgr = computeMgrByCode.GetOrAdd(code, _ => new ComputeMgr(code));
           
            var isNewBuy = false;
            var isNewSell = false;

            if (_stocksWithDealtOrders.TryGetValue(code, out var odrs))
            {
                cmgr.逐笔 = odrs.Count;
                if (StockConfig.DictSERef.TryGetValue(code, out var see))
                {
                    lock (see)
                    {
                        cmgr.股票名称 = see.Name;
                        cmgr.总笔 = see.TotalDealtCount;
                    }
                }
            }

            //lock (order)
            {
                isNewBuy = order.OrderVol == 0;
                order.OrderId = dealtOrder.BuyOrderId;
                order.OrderPrice = dealtOrder.BuyOrderPrice;
                order.OrderVol = dealtOrder.BuyOrderVol;
                order.IsBuy = true;
                //if (dealtOrder.IsBuyPositive)
                {
                    var bDealt = order.OrderDealts.GetOrAdd(dealtOrder.DealtId, _=>new OrderDealt());
                    if (bDealt.DealtVol == 0)
                    {
                        //lock (dealtOrder)
                        {
                            bDealt.DealtPrice = dealtOrder.DealtPrice;
                            bDealt.DealtVol = dealtOrder.DealtVol;
                            bDealt.IsPositive = dealtOrder.IsBuyPositive;
                            bDealt.DealtTime = dealtOrder.DealtTime;

                            if (bDealt.IsPositive)
                                order.PositiveAmount += bDealt.DealtVol * bDealt.DealtPrice;
                            order.AllAmount += bDealt.DealtVol * bDealt.DealtPrice;

                            if (bDealt.DealtTime < order.DealtTime || order.DealtTime == DateTime.MinValue)
                                order.DealtTime = bDealt.DealtTime;
                        }

                        if (order.OrderPrice > 0)
                            order.OrderAmount = order.OrderPrice * order.OrderVol;
                        else
                            order.OrderAmount = order.OrderDealts.Values.First().DealtPrice * order.OrderVol;

                        order.MinDealtId = Math.Min(order.MinDealtId, dealtOrder.DealtId);
                        order.MaxDealtId = Math.Max(order.MaxDealtId, dealtOrder.DealtId);
                        //lock (bDealt)
                        //{
                        //    bDealt.DealtPrice = dealtOrder.DealtPrice;
                        //    bDealt.DealtVol = dealtOrder.DealtVol;
                        //    bDealt.IsPositive = dealtOrder.IsBuyPositive;
                        //    bDealt.DealtTime = dealtOrder.DealtTime;
                        //}

                        //order.PositiveAmount = order.OrderDealts.Values.Where(val => val.IsPositive).Sum(val => val.DealtPrice * val.DealtVol);
                        //order.AllAmount = order.OrderDealts.Values.Sum(val => val.DealtPrice * val.DealtVol);
                    }
                }
            }

            var orderSell = order = orders.GetOrAdd(dealtOrder.SellOrderId, _ => new Order());
            //lock (order)
            {
                isNewSell = order.OrderVol == 0;
                order.OrderId = dealtOrder.SellOrderId;
                order.OrderPrice = dealtOrder.SellOrderPrice;
                order.OrderVol = dealtOrder.SellOrderVol;
                order.IsBuy = false;
                //if (!dealtOrder.IsBuyPositive)
                {
                    var bDealt = order.OrderDealts.GetOrAdd(dealtOrder.DealtId, _ => new OrderDealt());
                    if (bDealt.DealtVol == 0)
                    {
                        //lock (dealtOrder)
                        {
                            bDealt.DealtPrice = dealtOrder.DealtPrice;
                            bDealt.DealtVol = dealtOrder.DealtVol;
                            bDealt.IsPositive = !dealtOrder.IsBuyPositive;
                            bDealt.DealtTime = dealtOrder.DealtTime;

                            if (bDealt.IsPositive)
                                order.PositiveAmount += bDealt.DealtVol * bDealt.DealtPrice;
                            order.AllAmount += bDealt.DealtVol * bDealt.DealtPrice;

                            if (bDealt.DealtTime < order.DealtTime || order.DealtTime == DateTime.MinValue)
                                order.DealtTime = bDealt.DealtTime; // set the first dealt time to order's dealt time
                        }

                        if (order.OrderPrice > 0)
                            order.OrderAmount = order.OrderPrice * order.OrderVol;
                        else
                            order.OrderAmount = order.OrderDealts.Values.First().DealtPrice * order.OrderVol;
                        //_stockAlertPool.UpdateAlertAmount(code, order);
                        order.MinDealtId = Math.Min(order.MinDealtId, dealtOrder.DealtId);
                        order.MaxDealtId = Math.Max(order.MaxDealtId, dealtOrder.DealtId);
                    }
                }
            }

            if (orderBuy != null)
            {
                order = orderBuy;
                cmgr.CalcRealtime();
                var amounts = order.OrderAmount;
                if (amounts > 1000000)
                {
                    var _m1 = _M1Orders.GetOrAdd(code,_=>new ConcurrentDictionary<uint, Order>());
                    var _m3 = _M3Orders.GetOrAdd(code,_=>new ConcurrentDictionary<uint, Order>());
                    var _m5 = _M5Orders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, Order>());

                    if (amounts > 5000000)
                    {
                        _m5.TryAdd(order.OrderId, order);
                        //if(order.AllAmount > 5000000)
                        _stockAlertPool.UpdateAlert(code, "", "五百万级别", order, _m1, _m3, _m5);

                        //if (isNewBuy && (amounts > 10000000 || order.OrderVol > 900000))
                        //    _player.Play();
                    }
                    else if (amounts > 3000000)
                    {
                        _m3.TryAdd(order.OrderId, order);
                        //if (order.AllAmount > 3000000)
                        _stockAlertPool.UpdateAlert(code, "", "三百万级别", order, _m1, _m3, _m5);
                    }
                    else if (amounts > 1000000)
                    {
                        _m1.TryAdd(order.OrderId, order);
                        //if (order.AllAmount > 1000000)
                        _stockAlertPool.UpdateAlert(code, "", "一百万级别", order, _m1, _m3, _m5);
                    }

                    //_stockAlertPool.GetAssociatedOrders(code, order).Where(id => id > 0).ToList().ForEach(odrId => {
                    //    if (orders.TryGetValue(odrId, out var tempOdr) && order.DealtTime.Subtract(tempOdr.DealtTime).TotalMinutes < 2)
                    //        _stockAlertPool.UpdateAlert(code, "", "", tempOdr, _m1, _m3, _m5, true);
                    //});
                }
            }
            //else
            //{
            //    //_stockAlertPool.UpdateAlertAmount(code, order);

            if (orderSell != null)
            {
                order = orderSell;
                cmgr.CalcRealtime();
                var amounts = order.OrderAmount;
                if (amounts > 1000000)
                {
                    var _m1 = _M1Orders.GetOrAdd(code, _=>new ConcurrentDictionary<uint, Order>());
                    var _m3 = _M3Orders.GetOrAdd(code, _=>new ConcurrentDictionary<uint, Order>());
                    var _m5 = _M5Orders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, Order>());

                    if (amounts > 5000000)
                    {
                        _m5.TryAdd(order.OrderId, order);
                        //if (order.AllAmount > 5000000)
                        _stockAlertPool.UpdateAlert(code, "", "五百万级别【卖单】", order, _m1, _m3, _m5);

                        //if (isNewSell && (amounts > 10000000 || order.OrderVol > 900000))
                        //    _player.Play();
                    }
                    else if (amounts > 3000000)
                    {
                        _m3.TryAdd(order.OrderId, order);
                        //if (order.AllAmount > 3000000)
                        _stockAlertPool.UpdateAlert(code, "", "三百万级别【卖单】", order, _m1, _m3, _m5);

                    }
                    else if (amounts > 1000000)
                    {
                        //_stockAlertPool.UpdateAlert(code, "", "一百万级别【卖单】", order);
                        _m1.TryAdd(order.OrderId, order);
                        //if (order.AllAmount > 1000000)
                        _stockAlertPool.UpdateAlert(code, "", "一百万级别【卖单】", order, _m1, _m3, _m5);
                    }

                    //_stockAlertPool.GetAssociatedOrders(code, order).Where(id => id > 0).ToList().ForEach(odrId => {
                    //    if(orders.TryGetValue(odrId, out var tempOdr) && order.DealtTime.Subtract( tempOdr.DealtTime).TotalMinutes < 2)
                    //        _stockAlertPool.UpdateAlert(code, "", "", tempOdr, _m1, _m3, _m5, true);
                    //});
                }
                //else
                //{
                //    //_stockAlertPool.UpdateAlertAmount(code, order);
                //}
                //var bDealt = order.OrderDealts.GetOrAdd(dealtOrder.DealtId, new OrderDealt());
                //lock (bDealt)
                //{
                //    bDealt.DealtPrice = dealtOrder.DealtPrice;
                //    bDealt.DealtVol = dealtOrder.DealtVol;
                //    bDealt.IsPositive = !dealtOrder.IsBuyPositive;
                //    bDealt.DealtTime = dealtOrder.DealtTime;
                //}

                //order.PositiveAmount = order.OrderDealts.Values.Where(val => val.IsPositive).Sum(val => val.DealtPrice * val.DealtVol);
                //order.AllAmount = order.OrderDealts.Values.Sum(val => val.DealtPrice * val.DealtVol);
            }
        }

        ConcurrentDictionary<string, ComputeMgr> computeMgrByCode = new ConcurrentDictionary<string, ComputeMgr>();
        public ConcurrentDictionary<string, ComputeMgr> ComputeMgrByCode => computeMgrByCode;

        StockAlertPool _stockAlertPool = new StockAlertPool();
        public StockAlertPool StockAlertPool => _stockAlertPool;

        public void SumBigOrdersWithDealt()
        {
            Parallel.ForEach(_AllOrders.Keys, (stock) =>
            {
                if (_AllOrders.TryGetValue(stock, out var orders))
                {
                    var cmgr = computeMgrByCode.GetOrAdd(stock, _=>new ComputeMgr(stock));
                    var timeStr = DateTime.Now.ToString("HH:mm");
                    if (DateTime.Now.TimeOfDay.TotalMinutes < 9 * 60 + 30)
                        timeStr = $"竞价统计";
                    var _m1 = _M1Orders.GetOrAdd(stock, _ => new ConcurrentDictionary<uint, Order>());
                    var _m3 = _M3Orders.GetOrAdd(stock, _ => new ConcurrentDictionary<uint, Order>());
                    var _m5 = _M5Orders.GetOrAdd(stock, _ => new ConcurrentDictionary<uint, Order>());
                    cmgr.CalcByTimePoint($"{timeStr}·东财", orders, _stocksWithDealtOrders[stock].Count, (odr) => odr.IsBuy?odr.AllAmount:-odr.AllAmount, (odr) => odr.IsBuy ? odr.PositiveAmount : -odr.PositiveAmount, _m1, _m3, _m5);
                    //cmgr.CalcByTimePoint($"{timeStr}·普通", orders, _stocksWithDealtOrders[stock].Count, (odr) => odr.IsBuy?odr.PositiveAmount:-odr.PositiveAmount, _m1, _m3, _m5);
                }
            });       
            SaveDealts();
            _stockAlertPool.SaveAlerts();
        }
    }
}
