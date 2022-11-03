using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    public class StockAlertPool
    {
        public StockAlertPool()
        {
            try
            {
                var dealtfiles = Directory.EnumerateFiles($"{DateTime.Today:yyyyMMdd}", "ALT_*");
                dealtfiles.ToList().ForEach(file =>
                {

                    var stock = file.Split(new char[] { '\\' })[1].Replace(".xml", "");
                    var saved = DataSerializer.DeserializeToObject<List<AlertInfo>>(stock);
                    if (null != saved)
                    {
                        var code = stock.Replace("ALT_", "");
                        var dict = _alertPoolByStocks.GetOrAdd(code, new ConcurrentDictionary<uint, AlertInfo>());
                        saved.ForEach(item =>
                        {
                            dict.TryAdd(item.委托编号, item);
                        });
                    }
                });
            }
            catch
            {

            }
        }

        public void SaveAlerts()
        {
            _alertPoolByStocks.ToList().ForEach(stock => {
                DataSerializer.SerializeToFile($"ALT_{stock.Key}", stock.Value.Values.ToList());
            });
        }

        //public void UpdateAlertAmount(string code, Order order,
        //    ConcurrentDictionary<uint, Order> M1orders,
        //    ConcurrentDictionary<uint, Order> M3orders,
        //    ConcurrentDictionary<uint, Order> M5orders)
        //{
        //    if (_alertPoolByStocks.TryGetValue(code, out var alerts))
        //    {
        //        foreach (var altInfo in alerts.Values)
        //        {
        //            if (order.DealtTime.TimeOfDay.TotalMinutes - DateTime.ParseExact(altInfo.预警时间, "HH:mm:ss", null).TimeOfDay.TotalMinutes > 1)
        //                continue;

        //            lock (altInfo)
        //            {
        //                altInfo.成交金额 = order.AllAmount;

        //                if (_alertPoolWithOrders.TryGetValue(code, out var m13Orders) && m13Orders.TryGetValue(order.OrderId, out var mym13Orders))
        //                {
        //                    altInfo.M13净额 = mym13Orders.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);
        //                    altInfo.M35净额 = mym35Orders.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);
        //                    altInfo.M5净额 = mym5Orders.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);
        //                    altInfo.百万净额 = altInfo.M35净额 + altInfo.M5净额 + altInfo.M13净额;
        //                }
        //            }
        //        }
        //    }
        //}

        public List<uint> GetAssociatedOrders(string code, Order order)
        {
            if (_alertPoolByStocks.TryGetValue(code, out var alerts))
            {
                return alerts.Values.Select<AlertInfo,uint>(altInfo =>
                {
                    if (_alertPoolWithOrders.TryGetValue(code, out var m13Orders) && m13Orders.TryGetValue(order.OrderId, out var mym13Orders))
                    {
                        return altInfo.委托编号;
                    }
                    return 0;
                }).ToList();
            }
            return null;
        }

        public void UpdateAlert(string code, string name, string flag, Order order, 
            ConcurrentDictionary<uint, Order> M1orders,
            ConcurrentDictionary<uint, Order> M3orders,
            ConcurrentDictionary<uint, Order> M5orders, bool bUpdate=false)
        {

            return;// CUSTOMER

            var stkAlert = _alertPoolByStocks.GetOrAdd(code, _ => new ConcurrentDictionary<uint, AlertInfo>());
            var altInfo = stkAlert.GetOrAdd(order.OrderId, _ => new AlertInfo());
            //if (altInfo.委托数量 > 0)
            //    return;
            var mOrders = _alertPoolWithOrders.GetOrAdd(code, _ => new ConcurrentDictionary<uint, ConcurrentDictionary<uint, Order>>());
            //var m35Orders = _alertPoolWithOrdersM35.GetOrAdd(code, new ConcurrentDictionary<uint, ConcurrentDictionary<uint, Order>>());
            //var m5Orders = _alertPoolWithOrdersM5.GetOrAdd(code, new ConcurrentDictionary<uint, ConcurrentDictionary<uint, Order>>());
            var mymOrders = mOrders.GetOrAdd(order.OrderId, _ => new ConcurrentDictionary<uint, Order>());
            //var mym35Orders = m35Orders.GetOrAdd(order.OrderId, new ConcurrentDictionary<uint, Order>());
            //var mym5Orders = m5Orders.GetOrAdd(order.OrderId, new ConcurrentDictionary<uint, Order>());

            lock (altInfo)
            {
                if(!bUpdate)
                {
                    altInfo.委托数量 = order.OrderVol / 100;
                    altInfo.委托价格 = order.OrderPrice;
                    altInfo.委托编号 = order.OrderId;
                    altInfo.股票代码 = code;
                    altInfo.成交金额 = order.AllAmount;
                    altInfo.下单金额 = order.OrderVol * order.OrderPrice;
                    if (string.IsNullOrEmpty(altInfo.股票名称))
                    {
                        if (StockConfig.DictSERef.TryGetValue(code, out var see))
                        {
                            lock (see)
                            {
                                altInfo.股票名称 = see.Name;
                            }
                        }
                    }
                    altInfo.是否买单 = order.IsBuy ? "是" : "否";
                    altInfo.入池标记 = flag;
                    altInfo.预警时间 = order.DealtTime.ToString("HH:mm:ss") ?? DateTime.Now.ToString("HH:mm:ss");

                    altInfo.成交价格 = order.OrderDealts.Values.FirstOrDefault()?.DealtPrice ?? 0f;
                }
               
                var formerOrdersM1 = M1orders.Where(odr => odr.Value.DealtTime <= order.DealtTime).ToList();
                var m1 = formerOrdersM1.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);
                altInfo.M13买单数 = formerOrdersM1.Where(odr => odr.Value.IsBuy).Count();
                altInfo.M13卖单数 = formerOrdersM1.Where(odr => !odr.Value.IsBuy).Count();
                altInfo.M13单差 = altInfo.M13买单数 - altInfo.M13卖单数;
                altInfo.M13净额 = formerOrdersM1.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);

                var formerOrdersM35 = M3orders.Where(odr => odr.Value.DealtTime <= order.DealtTime).ToList();

                altInfo.M35买单数 = formerOrdersM35.Where(odr => odr.Value.IsBuy).Count();
                altInfo.M35卖单数 = formerOrdersM35.Where(odr => !odr.Value.IsBuy).Count();
                altInfo.M35单差 = altInfo.M35买单数 - altInfo.M35卖单数;
                altInfo.M35净额 = formerOrdersM35.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);

                var formerOrdersM5 = M5orders.Where(odr => odr.Value.DealtTime <= order.DealtTime).ToList();

                altInfo.M5买单数 = formerOrdersM5.Where(odr => odr.Value.IsBuy).Count();
                altInfo.M5卖单数 = formerOrdersM5.Where(odr => !odr.Value.IsBuy).Count();
                altInfo.M5单差 = altInfo.M5买单数 - altInfo.M5卖单数;
                altInfo.M5净额 = formerOrdersM5.Select(odr => odr.Value).Sum(val => val.IsBuy ? val.AllAmount : -val.AllAmount);
                if(!bUpdate)
                {
                    formerOrdersM1.ForEach(fodr => mymOrders.TryAdd(fodr.Key, fodr.Value));
                    formerOrdersM35.ForEach(fodr => mymOrders.TryAdd(fodr.Key, fodr.Value));
                    formerOrdersM5.ForEach(fodr => mymOrders.TryAdd(fodr.Key, fodr.Value));
                    mymOrders.TryRemove(order.OrderId, out _);
                }

                altInfo.百万净额 = altInfo.M35净额 + altInfo.M5净额 + m1;
            }
        }

        ConcurrentDictionary<string, ConcurrentDictionary<uint, AlertInfo>> _alertPoolByStocks = new ConcurrentDictionary<string, ConcurrentDictionary<uint, AlertInfo>>();
        ConcurrentDictionary<string, ConcurrentDictionary<uint, ConcurrentDictionary<uint, Order>>> _alertPoolWithOrders = new ConcurrentDictionary<string, ConcurrentDictionary<uint, ConcurrentDictionary<uint, Order>>>();
        public ConcurrentDictionary<string, ConcurrentDictionary<uint, AlertInfo>> AlertPoolByStocks => _alertPoolByStocks;
    }
}
