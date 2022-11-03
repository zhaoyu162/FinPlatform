using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Collections.Concurrent;
using PropertyChanged;
using NetMQ;
using NetMQ.Sockets;
using System.Threading.Tasks;
using Nito.AsyncEx;
using FinWPF.quote_client;

namespace FinWPF.Quote
{
    [AddINotifyPropertyChangedInterface]
    public class DZHClient
    {
        public DZHClient()
        {
            _dtScrach = _dtScrach.AddHours(8);
            //DictSERef = new ConcurrentDictionary<string, StockEntity>();
            _socket_publish.Bind("inproc://teslamotor");
            _socket_publish.Options.SendHighWatermark = 100000;
        }

        async void _RecvRoute(object obj)
        {
            var server = ConfigurationManager.AppSettings["ZMQ"];// ">tcp://yoyotek.cn:9631"; // Settings.Default.Properties["ZMQServer"]?.ToString();
            using (var skt = new PullSocket())
            {
                //skt.SubscribeToAnyTopic();
                //skt.
                //skt.SetOption(ZSocketOption.IDENTITY, "yoyotek.cn");
                //skt.SetOption(ZSocketOption.RCVHWM, 0);
                //skt.Subscribe("BDPK");
                // WDPK, 五档盘口
                // SDPK, 十档盘口
                // ZBCJ，逐笔成交
                // ZBWT，逐笔委托
                // ZBDD，逐笔大单
                // BDPK，百档盘口
                // QQPK，期权盘口
                skt.Options.ReceiveHighWatermark = int.MaxValue;
                //if(server == null)
                //{
                //    Settings.Default.Properties.Add(new SettingsProperty("ZMQServer"));
                //    Settings.Default.Properties.Add( = server = "tcp://yoyotek.cn:19908";
                //}
                skt.Connect(server);
                while (true)
                {
                    try
                    {
                        var list = new List<byte[]>();

                        List<byte[]> frames = null;
                        while (skt.TryReceiveMultipartBytes(ref frames, 10) && list.Count <= 500)
                        {
                            list.AddRange(frames);
                        }

                        await Task.WhenAll(list.Where(frm => frm.Length > 5).Select(frm => _ResolveDatagram(frm)));

                        //NLog.LogManager.GetCurrentClassLogger().Info("received data");
                        //Task.Run(() => { });
                        //foreach (var frms in list.Where(frm => frm.Length > 5))
                        //{
                        //    await _ResolveDatagram(frms);
                        //}

                        if (list.Count < 500)
                            Thread.Sleep(10);
                    }
                    catch (ThreadAbortException ex)
                    {
                        //NLog.LogManager.GetCurrentClassLogger().Info($"ThreadAbortException of resoving data:{ex.Message}");
                    }
                    catch (System.Exception ex)
                    {
                        //NLog.LogManager.GetCurrentClassLogger().Info($"Exception of resoving data:{ex.Message}");
                    }
                }
            }
        }
        public void Start(int recv_thcount)
        {
            //_thread = new Thread(() =>
            //{
            //Parallel.For(0, Environment.ProcessorCount * 2, (n) => {
            //});
            if(null == _dealtOrderMgr)
                _dealtOrderMgr = new DealtOrderMgr();

            for (int n = 0; n < recv_thcount/*Math.Max(4,Environment.ProcessorCount / 10 + 1)*/; n++)
            {
                var thc = new Thread(new ThreadStart(() =>
                {
                    _RecvRoute(null);
                }))
                { IsBackground = true };
                thc.Start();
                //Task.Run(() => _RecvRoute(null));
            }
            //});

            //_thread.IsBackground = true;
            //_thread.Start();
        }

        public void Stop()
        {
            _thread.Abort();
            _thread.Join();
        }

        async Task  _ResolveDatagram(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                //while (br.BaseStream.CanRead)
                {
                    switch (br.ReadInt32())
                    {
                        case 0:
                            await _ResolveL1Quote(br);
                            break;
                        //case 1:
                        //    _ResolveL2Quote(br);
                        //    break;
                        case 4:// 逐笔数据
                            await _ResolveZhubiData(br);
                            break;

                        //case 5: // 逐笔委托数据
                        //    _ResolveZhubiOrderData(br);
                        //    break;
                        //case 6: // 高五档盘口数据
                        //    _ResolveHigh5Data(br);
                        //    break;
                        //case 7: // 百档盘口
                        //    _ResolveBdPankou(br);
                        //    break;
                        case 8:
                            _ResolveBigorder(br);
                            break;
                        //case 10:
                        //    _ResolveQQData(br);
                        //    break;
                        case 12:
                            _ResolveFinData(br);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        void _ResolveQQData(BinaryReader br)
        {
            int count = br.ReadInt32(); // qq数量

            for (var n = 0; n < count; n++)
            {
                br.ReadInt16();
                UInt32 unixTime = br.ReadUInt32();
                var code = Encoding.ASCII.GetString(br.ReadBytes(12)).ToLower().Split(new char[] { '\0' })[0];
                var name = Encoding.GetEncoding(936).GetString(br.ReadBytes(32)).ToLower().Split(new char[] { '\0' })[0];

                br.ReadBytes(16);
                float fn = br.ReadSingle();
                float v = br.ReadSingle();
                br.ReadSingle();
                float bp1 = br.ReadSingle();
                br.ReadBytes(8);
                float bv1 = br.ReadSingle();
                br.ReadBytes(8);
                float sp1 = br.ReadSingle();
                br.ReadBytes(8);
                float sv1 = br.ReadSingle();

                br.ReadBytes(17 * 4);

                var log = "接收到QQ数据：" + name + "，time:" + _dtScrach.AddSeconds(unixTime).ToString("MM-dd HH:mm:ss") + ",最新价格：" + fn.ToString() + "总手：" + v.ToString() + " 买一：" + bp1.ToString() + "," + bv1.ToString() + " 卖一：" + sp1.ToString() + "," + sv1.ToString();
                writelog(DateTime.Now.ToString("yyyyMMdd") + "\\期权\\" + name + ".txt", log);
                if(code.ToLower() == "ho10000834")
                    Console.WriteLine(log);
            }
        }
        void _ResolveBigorder(BinaryReader br)
        {
            int count = br.ReadInt32(); // 大单数量
            var code = Encoding.ASCII.GetString(br.ReadBytes(16)).ToLower().Split(new char[] { '\0' })[0];
            var name = Encoding.GetEncoding(936).GetString(br.ReadBytes(32)).ToLower().Split(new char[] { '\0' })[0];

            for (var n = 0; n < count; n++)
            {
                var packid = br.ReadUInt32();
                var orderid1_2 = br.ReadUInt32();// 0Xfffffff;
                var orderid2_2 = br.ReadUInt32();// & 0Xfffffff;
                var orderid1 = orderid1_2 & 0xfffffff;
                var orderid2 = orderid2_2 & 0xfffffff;
                var orderid1_1 = orderid1_2 >> 28;
                var orderid2_1 = orderid2_2 >> 28;

                var buyPrice = br.ReadUInt32();// 买价
                var sellPrice = br.ReadUInt32();// 卖价
                var buyvol = br.ReadUInt32(); // 买单量
                var sellvol = br.ReadUInt32(); // 卖单量

                _dealtOrderMgr.AddOrUpdateByOrder(code, packid, buyvol, buyPrice / 100f, orderid1, sellvol, sellPrice / 100f, orderid2);

                //var log = "接收到大单数据：" + code + "," + name + "," + buyPrice.ToString() + "," + sellPrice.ToString() +"," + buyvol.ToString() + "," + sellvol.ToString() + ",序号:" + packid.ToString() + ",买单号：" + orderid1_1.ToString() + "|" + orderid1.ToString() + ",卖单号：" + orderid2_1.ToString() + "|" + orderid2.ToString();
                //writelog(DateTime.Now.ToString("yyyyMMdd") + "\\逐笔大单\\" + code + ".txt", log);
                //Console.WriteLine(log);
            }

        }

        void _ResolveFinData(BinaryReader br)
        {
            var count = br.ReadInt32();
            var code = Encoding.ASCII.GetString(br.ReadBytes(16)).ToLower().Split(new char[] { '\0' })[0];
            StockEntity se = StockConfig.DictSERef.GetOrAdd(code.ToLower(), _ => new StockEntity());

            br.ReadBytes(40 * 4);
            se.TotalStockCounts = Math.Round(br.ReadSingle(), 1) * 10000.0;
            se.FreeStockCounts = Math.Round(br.ReadSingle(), 1) * 10000.0;
            br.ReadBytes(14 * 4);

            DealtOrdersManager?.CalcRealtime(code);
        }

        async Task _ResolveL1Quote(BinaryReader br)
        {
            await Task.Run(() => {
                int count = br.ReadInt32();
                int size = br.ReadInt32();

                for (var n = 0; n < count; n++)
                {
                    var code = Encoding.ASCII.GetString(br.ReadBytes(16)).ToLower();
                    if (code.Length == 0)
                    {
                        br.ReadBytes(170 - 16);
                        continue;
                    }

                    code = code.Split(new char[] { '\0' })[0];
                    if (!code.StartsWith("sh6") && !code.StartsWith("sz0") && !code.StartsWith("sz3"))
                    {
                        br.ReadBytes(170 - 16);
                        continue;
                    }
                    var name = Encoding.GetEncoding(936).GetString(br.ReadBytes(16)).Split(new char[] { '\0' })[0];
                    var factor = code.StartsWith("sh510") || code.StartsWith("sz160") ? 1000.0f : 100.0f;
                    var pc = br.ReadInt32() / factor;
                    var lh = br.ReadInt32() / factor;
                    var ll = br.ReadInt32() / factor;
                    br.ReadBytes(2); // 忽略一个字段SHORT
                    var currTime = DateTimeOffset.FromUnixTimeSeconds(br.ReadUInt32()).ToLocalTime().DateTime;

                    StockEntity se = StockConfig.DictSERef.GetOrAdd(code, _ => new StockEntity());
                    if(se.Code == null)
                    {
                        se.UniqueCode = code;
                        se.Code = code.Substring(2);
                        se.Name = name;
                        se.ShardValue = code.Substring(code.Length - 1);
                        se.TotalVolume = 0;
                    }
                    else
                    {
                        if(se.QuoteTime >= currTime)
                        {
                            br.ReadBytes(170 - 16 - 16 - 18);
                            continue;
                        }
                    }

                    se.PreClose = pc;
                    se.LimitHigh = lh;
                    se.LimitLow = ll;
                    var qtimePrev = se.QuoteTime;
                    se.QuoteTime = currTime;

                    RealtimeTick rtt;
                    if (StockConfig.Save_list_ticks.TryGetValue(code, out rtt) && rtt.TickTime.Date == se.QuoteTime.Date)
                    {
                        se.TotalVolume = (uint)rtt.DealtVol;
                    }

                    se.Open = br.ReadInt32() / factor;
                    se.Highest = br.ReadInt32() / factor;
                    se.Lowest = br.ReadInt32() / factor;
                    se.Latest = br.ReadInt32() / factor;
                    var VOL = br.ReadUInt32();
                    
                    Symbol symb;
                    if(StockConfig.Save_list_stocks.TryGetValue(se.UniqueCode, out symb))
                    {
                        se.SymbId = symb.ID;
                    }
                    //if (VOL > VOL_old)
                    //{
                    //    se.DataInfoList.Add(new NewDataInfo()
                    //    {
                    //        Delta_num = (int)(VOL - VOL_old),
                    //        Close_num = (int)VOL,
                    //        Last_price = se.Latest,
                    //        Time = se.QuoteTime.ToString("hh:mm:ss")
                    //    });

                    //    if (se.DataInfoList.Count > 30)
                    //        se.DataInfoList.RemoveAt(0);
                    //}

                    se.TotalAmount = (UInt64)br.ReadDouble();
                    se.TotalDealtCount = br.ReadUInt32();

                    if (VOL > se.TotalVolume)
                        se.CurrVolume = VOL - se.TotalVolume;
                    se.TotalVolume = VOL;

                    for (int k = 0; k < 10; k++)
                    {
                        if (se.BidPairs[k] == null)
                            se.BidPairs[k] = new StockEntity.PCPair();
                        if (se.AskPairs[k] == null)
                            se.AskPairs[k] = new StockEntity.PCPair();
                    }

                    for (int k = 0; k < 5; k++) {se.BidPairs[k].Price = br.ReadInt32() / factor;}
                    for (int k = 0; k < 5; k++) { se.BidPairs[k].Count = br.ReadInt32() * 100; }
                    for (int k = 0; k < 5; k++) { se.AskPairs[k].Price = br.ReadInt32() / factor; }
                    for (int k = 0; k < 5; k++) { se.AskPairs[k].Count = br.ReadInt32() * 100; }

                    var rn1 = br.ReadInt32();
                    var rn2 = br.ReadInt32();

                    if (qtimePrev != se.QuoteTime
                        //(se.UniqueCode.StartsWith("sh00") || se.UniqueCode.StartsWith("sh60") || se.UniqueCode.StartsWith("sz39")
                        //|| se.UniqueCode.StartsWith("sz00") || se.UniqueCode.StartsWith("sz30"))
                        )
                    {
                        if(NeedSaveData)
                        {
                            if (!StockConfig.Save_list_stocks.ContainsKey(se.UniqueCode))
                            {
                                var newSymbol = new Symbol() { StockSymbol = se.UniqueCode, Enabled = true, StockName = se.Name };
                                StockConfig.Save_list_stocks.AddOrUpdate(se.UniqueCode, newSymbol, (k, v) => v);
                                ListOfUdpatedSymbol.Enqueue(newSymbol);
                            }

                            ListOfUpdates.Enqueue(se);
                        }
                       
                        if (se.UniqueCode == "sh600000")
                        {
                            NLog.LogManager.GetCurrentClassLogger().Info($"{se.UniqueCode}@{se.QuoteTime} received");
                        }
                    }

                    //using (_tickAsyncMonitor.Enter())
                    {
                        MktSnapshot.TotalTickCount++;
                        MktSnapshot.CurrTickSymbol = se.UniqueCode;
                    }
                    DealtOrdersManager?.CalcRealtime(se.UniqueCode);
                }
            });
        }
        async Task _send_notify_frame(string code)
        {
            using (await _sendAsyncMonitor.EnterAsync())
            {
                _socket_publish.SendMoreFrame("SHARD_"+code.Substring(code.Length - 1)).SendFrame(code);
                //Task.Factory.StartNew(() => _socket_publish.SendMoreFrame(code.Substring(code.Length - 1)).SendFrame(code));
               // ; // send shard key firstly
            }
            //await Task.Run(() =>
            //    {
            //        lock (_socket_publish)
            //        {
            //        }
            //    });
        }
  
        HashSet<string> codeHash = new HashSet<string>();
        HashSet<string> codeHashOfOrders = new HashSet<string>();
        HashSet<string> codeHashOfHigh5 = new HashSet<string>();
        HashSet<string> codeHashOfBdpk = new HashSet<string>();
        AsyncMonitor _sendAsyncMonitor = new AsyncMonitor();
        AsyncMonitor _tickAsyncMonitor = new AsyncMonitor();
        AsyncMonitor _zbAsyncMonitor = new AsyncMonitor();
        public ConcurrentQueue<StockEntity> ListOfUpdates { get; } = new ConcurrentQueue<StockEntity>();
        public ConcurrentQueue<DealtByOrder> ListOfDealtsUpdates { get; } = new ConcurrentQueue<DealtByOrder>();
        public ConcurrentQueue<Symbol> ListOfUdpatedSymbol { get; } = new ConcurrentQueue<Symbol>();
        public MarketDataSnapshot MktSnapshot { get; set; } = new MarketDataSnapshot();

        void writelog(string savepath, string logstring = "")
        {
            //try
            //{
            //    Directory.CreateDirectory(Path.GetDirectoryName(savepath));
            //    StreamWriter log = new StreamWriter(savepath, true);
            //    log.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + logstring);
            //    log.Flush();
            //    log.Close();
            //}
            //catch (System.Exception ex)
            //{

            //}
        }

        async Task _ResolveZhubiData(BinaryReader br)
        {
            await Task.Run(() =>
            {
                int count = br.ReadInt32();
                int nLatest = br.ReadInt32();
                var nPackIndex = br.ReadUInt32(); // 包序列，从0开始，请按照这个序号进行排序重新组合全部的包
                var code = Encoding.ASCII.GetString(br.ReadBytes(16)).ToLower().Split(new char[] { '\0' })[0];
                var name = Encoding.GetEncoding(936).GetString(br.ReadBytes(32)).ToLower().Split(new char[] { '\0' })[0];
             
                var bytes = br.ReadBytes(count * 12);
                using (var mbr = new MemoryStream(bytes))
                using (var reader = new BinaryReader(mbr))
                {
                    var factor = code.StartsWith("sh510") || code.StartsWith("sz160") ? 1000.0f : 100.0f;

                    for (var n = 0; n < count; n++)
                    {
                        var dealtId = nPackIndex + n;
                        var time = reader.ReadUInt32();
                        var price = reader.ReadUInt32();
                        var vol = reader.ReadInt32();
                        var localTime = DateTimeOffset.FromUnixTimeSeconds(time).ToLocalTime();

                        if(NeedSaveData)
                        {
                            var key = $"{code}-{time}-{dealtId}";
                            var dealt = new DealtByOrder()
                            {
                                DealtId = dealtId,
                                DealtDate = localTime.Date,
                                DealtTime = localTime.TimeOfDay,
                                DealtPrice = price / factor,
                                DealtVol = Math.Abs(vol),
                                IsBuy = vol > 0,
                                SymbolCode = code,
                                SymbolId = StockConfig.Save_list_stocks[code].ID
                            };

                            StockConfig.Save_list_dealts.AddOrUpdate(key, (k) =>
                            {
                                ListOfDealtsUpdates.Enqueue(dealt);
                                return dealt;
                            }, (k, v) => v);

                            StockConfig.Save_list_dealts.TryRemove(key, out dealt);
                        }

                        _dealtOrderMgr.AddOrUpdateByDealt(code, (uint)dealtId, vol, price / 100f, time);
                    }
                }

                //using (_zbAsyncMonitor.EnterAsync())
                {
                    MktSnapshot.CurrZbSymbol = code;
                    MktSnapshot.TotalZbCount += count;
                }
            });
        }


        void _ResolveZhubiOrderData(BinaryReader br)
        {
            int size = br.ReadInt32();
            var code = Encoding.ASCII.GetString(br.ReadBytes(16)).ToLower().Split(new char[] { '\0' })[0];
            var name = Encoding.GetEncoding(936).GetString(br.ReadBytes(32)).ToLower().Split(new char[] { '\0' })[0];

            br.ReadBytes(4); // 略过4个不用的字节，具体请参照c++的格式声明

            var details = "";
            var countOfInts = br.ReadInt32(); // 这个很关键，表示的是所有价格的委托队列总体占用的int个数，所以要根据这个判定是否读取到了最后。
            for (var n = 0; n < countOfInts; n++)
            {
                var price = br.ReadInt16();
                var bos = br.ReadInt16();
                var nVols = br.ReadInt32(); // 委托量个数，后面是一个连续的int数组
                //var nouse = br.ReadBytes(12);
                details = "价格:" + price.ToString() + ", 总笔数:" + nVols.ToString() + " 逐笔量：";
                for (var j = 0; j < nVols; j++)// 读取连续的数组元素
                {
                    var vol = br.ReadInt32(); // 读取某一个量
                    if (vol > 0)
                        details += vol.ToString() + ",";
                }

                writelog(DateTime.Now.ToString("yyyyMMdd") + "\\逐笔委托\\" + code + ".txt", string.Format("stock {0}, {1}", name, details));
                n += 2 + nVols; // 依据以上的计算，这里N前进了nVols+2个int的长度
            }
          
            //var order_numbers = br.ReadBytes(4 * 100);

            //Console.WriteLine("接收到逐笔委托：" + name + " 已收到" + codeHashOfOrders.Count.ToString() + "个股票逐笔委托数据！");
        }

        void _ResolveHigh5Data(BinaryReader br) // 高五档盘口数据解析
        {
            int size = br.ReadInt32();
            var code = Encoding.ASCII.GetString(br.ReadBytes(16)).ToLower().Split(new char[] { '\0' })[0];
            var name = Encoding.GetEncoding(936).GetString(br.ReadBytes(32)).ToLower().Split(new char[] { '\0' })[0];
            br.ReadBytes(29);
            var price = br.ReadSingle();
            br.ReadBytes(size - 48 - 4 - 33);
            codeHashOfHigh5.Add(code);
            var log = ("接收到高5档盘口数据：" + name + " 买六价：" + price.ToString());
            writelog(DateTime.Now.ToString("yyyyMMdd") + "\\高五档盘口\\" + code + ".txt", log);
        }

        public DealtOrderMgr DealtOrdersManager => _dealtOrderMgr;

        DateTime    _dtScrach     =     new DateTime(1970, 1, 1);
        DealtOrderMgr _dealtOrderMgr;
        public bool NeedSaveData { get; set; } = true;

        ~DZHClient()
        {
            _socket_publish.Dispose();
        }
        NetMQ.Sockets.PushSocket _socket_publish = new PushSocket();
        Thread _thread       =     null;
    }
}
