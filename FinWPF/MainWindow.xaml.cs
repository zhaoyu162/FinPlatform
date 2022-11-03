using FinWPF.Quote;
using FinWPF.quote_client;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using Timer = System.Timers.Timer;

namespace FinWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)?.Length > 1)
            {
                MessageBox.Show("已经开了本程序了！，禁止多开！");
                Close();
                return;
            }

            if (DateTime.Now.Date.Month > 8)
                return;

            InitializeComponent();
        }


        public List<string> _getListCodes(string original)
        {
            var listCodes = original?.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(code => {
                code = code.Replace(" ", "");
                if (code.Length == 8)
                    return code.ToUpper();
                if (code.Length == 6)
                    return code[0] < '5' ? "SZ" + code : "SH" + code;
                return "";
            }).Where(code => code.Length > 0).ToList();
            return listCodes;
        }
        public string SHReqResult { get; set; }
        public string SZReqResult { get; set; }
        private async Task _requestZbHis()
        {
            if (L2RequestListFinal.Length >= 8)
            {
                try
                {
                    var listCodes = _getListCodes(L2RequestListFinal);
                    if (!listCodes.Any())
                        return;

                    var server = App.ReqL2Uri;
                    var wbr = WebRequest.Create($"{server}/requestzbhis?list={listCodes.Distinct().Aggregate("", (a, b) => a + "," + b)}");
                    if (!wbr.RequestUri.AbsoluteUri.EndsWith("="))
                    {
                        var resp = await wbr.GetResponseAsync();
                        using (var stm = resp.GetResponseStream())
                        using (var rdr = new StreamReader(stm, Encoding.GetEncoding(936)))
                        {
                            var respText = await rdr.ReadToEndAsync();
                        }
                    }
                }
                catch (Exception EX)
                {

                }
            }
        }

        private async void _subsL2Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if( null != sender && 
                (DateTime.Now.TimeOfDay.TotalSeconds <= 60 * 9 * 60 + 26 * 60 || DateTime.Now.TimeOfDay.TotalSeconds >= 60 * 15 * 60 + 60
                || DateTime.Now.TimeOfDay.TotalMinutes > 60 * 11 + 31 && DateTime.Now.TimeOfDay.TotalMinutes < 60 * 12 + 57))
            {
                return;
            }

            if (DateTime.Now.TimeOfDay.TotalSeconds > 60 * 9 * 60 + 26 * 60 && DateTime.Now.TimeOfDay.TotalSeconds < 60 * 9 * 60 + 29 * 60 ||
                DateTime.Now.TimeOfDay.TotalSeconds > 60 * 13 * 60 && DateTime.Now.TimeOfDay.TotalSeconds < 60 * 13 * 60 + 60 ||
                DateTime.Now.TimeOfDay.TotalSeconds > 60 * 9 * 60 + 30 * 60 && DateTime.Now.TimeOfDay.TotalSeconds < 60 * 9 * 60 + 31 * 60)
            {
                await _requestZbHis();
            }
            else
            {
                if (!RequestL2Disabled && L2RequestListFinal.Length >= 8)
                {
                    try
                    {
                        var listCodes = _getListCodes(L2RequestListFinal);
                        if (!listCodes.Any())
                            return;

                        var server = App.ReqL2Uri;
                        var wbr = WebRequest.Create($"{server}/requestshqx?list={listCodes.Where(code => code.StartsWith("SH")).Distinct().Aggregate("", (a, b) => a + "," + b)}");
                        if (!wbr.RequestUri.AbsoluteUri.EndsWith("="))
                        {
                            var resp = await wbr.GetResponseAsync();
                            using (var stm = resp.GetResponseStream())
                            using (var rdr = new StreamReader(stm, Encoding.GetEncoding(936)))
                            {
                                var respText = await rdr.ReadToEndAsync();
                                SHReqResult = respText;
                            }
                        }

                        wbr = WebRequest.Create($"{server}/requestqx?list={listCodes.Where(code => code.StartsWith("SZ")).Distinct().Aggregate("", (a, b) => a + "," + b)}");
                        if (!wbr.RequestUri.AbsoluteUri.EndsWith("="))
                        {
                            var resp = await wbr.GetResponseAsync();
                            using (var stm = resp.GetResponseStream())
                            using (var rdr = new StreamReader(stm, Encoding.GetEncoding(936)))
                            {
                                var respText = await rdr.ReadToEndAsync();
                                SZReqResult = respText;
                            }
                        }
                    }
                    catch (Exception EX)
                    {
                        SHReqResult = EX.Message;
                    }
                }
            }

            await Task.Delay(10000);
            if(DzhClient.DealtOrdersManager != null)
            {
                DzhClient.DealtOrdersManager?.SumBigOrdersWithDealt();
                await Dispatcher.InvokeAsync(() => {
                    _refreshComputeFieldList();
                });
            }
        }

        void _refreshComputeFieldList()
        {
            if (_dataGridStocks.SelectedItem is ComputeMgr cpmgr)
            {
                if (DzhClient.DealtOrdersManager.ComputeMgrByCode.TryGetValue(cpmgr.股票代码.ToString().ToLower(), out var cmgr))
                {
                    CurrentComputeFielsList = cmgr.FieldsByName.Values.OrderByDescending(VAL => VAL.时间点).ToList();
                    CurrentBOComputeFielsList = cmgr.FieldsOfBOByName.Values.OrderByDescending(VAL => VAL.时间点).ToList();
                    if (DzhClient.DealtOrdersManager.StockAlertPool.AlertPoolByStocks.TryGetValue(cpmgr.股票代码.ToString(), out var altInfo))
                        CurrentAlertList = altInfo.Values.OrderByDescending(val => val.预警时间).ToList(); ;
                }
                else
                {
                    CurrentComputeFielsList = new List<ComputeFields>();
                    CurrentBOComputeFielsList = new List<BigOrderComputeFields>();
                    CurrentAlertList = new List<AlertInfo>();
                }
            }

            //var ordered = DzhClient.DealtOrdersManager.ComputeMgrByCode.Values.OrderByDescending(val => val.OrderEntityValue).Select(val => val.StockCode).ToList();
            //for(int n = 0, j = 0; n < ordered.Count; n++)
            //{
            //    var soo = L2RequestSymbolList.Find(so => so.Symbol == ordered[n].ToUpper());
            //    if(null!= soo)
            //        soo.Order = j++;
            //}
        }

        public bool RequestL2Disabled { get; set; } = true;
        public MarketDataSnapshot RealtimeSnapshort { get; set; }
        public List<AlertInfo> CurrentAlertPool { get; set; }

        Timer _subsL2Timer = new Timer(50000);
        Timer _realtimeBoardRefreshTimer = new Timer(1000);

        DZHClient _dzhClient = new DZHClient();
        public FinWPF.Quote.DZHClient DzhClient => _dzhClient;
        DBSaver _dbSaver = new DBSaver();

        DequeueThread<StockEntity> _tickThread;
        public FinWPF.DequeueThread<FinWPF.Quote.StockEntity> TickThread => _tickThread;

        DequeueThread<Symbol> _smblThread;
        public FinWPF.DequeueThread<FinWPF.Symbol> SmblThread => _smblThread;
        DequeueThread<DealtByOrder> _dealtThread;
        public FinWPF.DequeueThread<FinWPF.DealtByOrder> DealtThread => _dealtThread;
        const uint _singleSaveCount = 500;
        public string L2RequestList { get; set; } = "";
        public string L2RequestListFinal { get; set; } = "";
        public List<SymbolInOrder> L2RequestSymbolList { get; set; }
        public List<ComputeFields> CurrentComputeFielsList { get; set; }
        public List<BigOrderComputeFields> CurrentBOComputeFielsList { get; set; }
        public List<ComputeMgr> CurrentComputeMgrList{ get; set; }
        public List<AlertInfo> CurrentAlertList{ get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TickThread.Disabled = false; 
        }

        private void BtnStopSaving_Click(object sender, RoutedEventArgs e)
        {
            TickThread.Disabled = true;
        }

        private void BtnSaveReqList_Click(object sender, RoutedEventArgs e)
        {
            XDocument xd = new XDocument();
            var elem = new XElement("ReqList");
            var list = _getListCodes(L2RequestList);
            list.ForEach(stk => {
                elem.Add(new XElement("Stock", stk));
            });
            xd.Add(elem);
            xd.Save("l2reqlist.xml");
            L2RequestListFinal = L2RequestList;
            L2RequestSymbolList = list.Select(s=>new SymbolInOrder() { Order = 0, Symbol = s}).ToList();

            BtnStartL2_Click(null, null);
        }

        private async void BtnStartL2_Click(object sender, RoutedEventArgs e)
        {
            RequestL2Disabled = false;
            L2RequestSymbolList.ForEach(symbol => DzhClient.DealtOrdersManager.ComputeMgrByCode.GetOrAdd(symbol.Symbol.ToLower(), new Lazy<ComputeMgr>(()=>new ComputeMgr(symbol.Symbol.ToLower())).Value));
            DzhClient.DealtOrdersManager.ComputeMgrByCode.Keys.ToList().ForEach(k => {
                if (L2RequestSymbolList.Find(si => si.Symbol.ToLower() == k) == null)
                    DzhClient.DealtOrdersManager.ComputeMgrByCode.TryRemove(k, out var val);
            });
            CurrentComputeMgrList = DzhClient.DealtOrdersManager.ComputeMgrByCode.Values.ToList();
            //DzhClient.DealtOrdersManager.LoadDealts();
            if (null != sender)
                _subsL2Timer_Elapsed(null, null);

            if (DateTime.Now.TimeOfDay.TotalMinutes > 9 * 60 + 25 && DateTime.Now.TimeOfDay.TotalHours < 15)
            {
                // 盘中新增股票
                var listZbhis = new List<ComputeMgr>();
                var listZbSpecial = new List<ComputeMgr>();
                foreach (var cpmgr in CurrentComputeMgrList.Where(ccm => ccm.逐笔 == 0))
                {
                    if (cpmgr.总笔 >= 0 && cpmgr.总笔 < 100000)
                        listZbhis.Add(cpmgr);
                    else
                        listZbSpecial.Add(cpmgr);
                }

                await _compensateZbHis(listZbhis, listZbSpecial);
            }

        }

        private void BtnStopL2_Click(object sender, RoutedEventArgs e)
        {
            RequestL2Disabled = true;
        }

        private void BtnStopSavingDealts_Click(object sender, RoutedEventArgs e)
        {
            _dealtThread.Disabled = true;
        }

        private void BtnStartSaveDealts_Click(object sender, RoutedEventArgs e)
        {
            _dealtThread.Disabled = false;
            _subsL2Timer_Elapsed(null, null);
        }

        private async void btnCalcMainPower_Click(object sender, RoutedEventArgs e)
        {
            bdrBusyNote.Visibility = Visibility.Visible;
            await Task.Run(() => DzhClient.DealtOrdersManager.SumBigOrdersWithDealt());
            bdrBusyNote.Visibility = Visibility.Hidden;
            _refreshComputeFieldList();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _refreshComputeFieldList();
        }

        private void DataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {

        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridTextColumn column = e.Column as DataGridTextColumn;
            var binding = (column.Binding as Binding);
            binding.Converter = new BigDoubleConvertor();
            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            if(column.DisplayIndex == 0)
                column.HeaderStyle = Resources["StkDataGridColumnHeaderStyleLeft"] as Style;
            else if (column.DisplayIndex == (sender as DataGrid).Columns.Count - 1)
                column.HeaderStyle = Resources["StkDataGridColumnHeaderStyleRight"] as Style;
            else
                column.HeaderStyle = Resources["StkDataGridColumnHeaderStyleCenter"] as Style;
            //if (e.Column.Header.ToString() == "委托编号" || e.Column.Header.ToString() == "总笔")
            //{
            //    e.Column.Visibility = Visibility.Collapsed;
            //    return;
            //}

            var desc = e.PropertyDescriptor as PropertyDescriptor;
            var att = desc.Attributes[typeof(ColumnNameAttribute)] as ColumnNameAttribute;
            if (att != null)
            {
                e.Column.Header = att.Name;
            }
            else
            {
                if (e.Column.Header.ToString() is string header && header.Length > 4)
                {
                    var ns = header.Insert(2, "\n");
                    for (int i = 0; i < (header.Length - 3); i++)
                        ns = ns.Insert(0, " ");
                    e.Column.Header = ns;
                }

                
            }
            //column.ElementStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Right));
        }

        private async void btnRequestL2his_Click(object sender, RoutedEventArgs e)
        {
            await _requestZbHis();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.ReqL2Uri = ConfigurationManager.AppSettings["L2REQ"];
            DzhClient.NeedSaveData = int.Parse(ConfigurationManager.AppSettings["SAVE_THREAD_COUNT"]) > 0;

            _subsL2Timer.Elapsed += _subsL2Timer_Elapsed;
            if (DzhClient.NeedSaveData)
            {
                _dbSaver.LoadSymbols().ForEach(symb => {
                    StockConfig.Save_list_stocks.AddOrUpdate(symb.StockSymbol, symb, (key, v) => v);
                });

                _dbSaver.LoadTicks().ForEach(tick =>
                {
                    StockConfig.Save_list_ticks.AddOrUpdate(tick.SymbolCode, tick, (key, v) => {
                        if (v.TickTime < tick.TickTime)
                            return tick;
                        return v;
                    });
                });

            }

            _subsL2Timer.Start();
            bdrBusyNote.Visibility = Visibility.Visible;
            var tsk = Task.Run(() => _dzhClient.Start(int.Parse(ConfigurationManager.AppSettings["RECV_THREAD_COUNT"])));
            await tsk;
            bdrBusyNote.Visibility = Visibility.Hidden;

            _tickThread = new DequeueThread<StockEntity>(DzhClient.ListOfUpdates, _dbSaver.SaveTicks) { ThreadName = "TICK" };
            _dealtThread = new DequeueThread<DealtByOrder>(DzhClient.ListOfDealtsUpdates, _dbSaver.SaveDealts) { ThreadName = "DEALT" };

            for (var n = 0; n < int.Parse(ConfigurationManager.AppSettings["SAVE_THREAD_COUNT"]); n++)
            {
                _tickThread.Run();
                _dealtThread.Run();
            }

            _smblThread = new DequeueThread<Symbol>(DzhClient.ListOfUdpatedSymbol, _dbSaver.SaveSymbols) { ThreadName = "SYMBOL" };
            _smblThread.Run();
            if (File.Exists("l2reqlist.xml"))
            {
                XDocument xdc = XDocument.Load("l2reqlist.xml");
                if (xdc.Descendants("Stock").Any())
                {
                    L2RequestList = L2RequestListFinal = xdc.Descendants("Stock").Select(stk => stk.Value).Aggregate((s1, s2) => s1 + "\r\n" + s2);
                    L2RequestSymbolList = xdc.Descendants("Stock").Select(stk => new SymbolInOrder() { Symbol = stk.Value, Order = 0 }).ToList();
                    BtnStartL2_Click(null, null);
                }
            }

            //var dispTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.ApplicationIdle, (sender, e) => {
            //    RealtimeSnapshort = new MarketDataSnapshot()
            //    {
            //        CurrTickSymbol = DzhClient.MktSnapshot.CurrTickSymbol,
            //        CurrZbSymbol = DzhClient.MktSnapshot.CurrZbSymbol,
            //        TotalTickCount = DzhClient.MktSnapshot.TotalTickCount,
            //        TotalZbCount = DzhClient.MktSnapshot.TotalZbCount
            //    };
            //}, Dispatcher);
            //dispTimer.Start();

            _realtimeBoardRefreshTimer.Elapsed += async (s, ee) => {
                var newList = DzhClient?.DealtOrdersManager?.StockAlertPool?.AlertPoolByStocks?.Values.SelectMany(aif => aif.Values.ToList()).OrderByDescending(aif => aif.预警时间).ToList();
                if (CurrentAlertPool == null || newList?.Count > CurrentAlertPool?.Count)
                {
                    CurrentAlertPool = newList;
                }

                await Dispatcher.InvokeAsync(() => {
                    for (int i = 0; i < datagridAlertPool.Items.Count; i++)
                    {
                        DataGridRow row = datagridAlertPool.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                        if (row == null || !row.IsHitTestVisible)
                            break;
                        if (row.Item is AlertInfo aif)
                        {
                            if (DateTime.Now.TimeOfDay.TotalMinutes - DateTimeOffset.ParseExact(aif.预警时间, "HH:mm:ss", null).TimeOfDay.TotalMinutes < 3)
                                row.Background = Brushes.LightCoral;
                            else
                                row.Background = Brushes.WhiteSmoke;
                        }
                    }
                });
            };

            _realtimeBoardRefreshTimer.Start();

            btnFetchAllL1_Click(null, null);
        }

        private async Task _compensateZbHis(List<ComputeMgr> listZbhis, List<ComputeMgr> listZbSpecial)
        {
            try
            {
                if (listZbhis.Any(cpmgr1 => cpmgr1.总笔 > cpmgr1.逐笔))
                {
                    var server = App.ReqL2Uri;
                    var wbr = WebRequest.Create($"{server}/requestzbhis?list={listZbhis.Select(l => l.股票代码.ToUpper()).Distinct().Aggregate("", (a, b) => a + "," + b)}");
                    if (!wbr.RequestUri.AbsoluteUri.EndsWith("="))
                    {
                        var resp = await wbr.GetResponseAsync();
                        using (var stm = resp.GetResponseStream())
                        using (var rdr = new StreamReader(stm, Encoding.GetEncoding(936)))
                        {
                            var respText = await rdr.ReadToEndAsync();
                        }
                    }
                    //await Task.Delay(30000);
                }
            }
            catch (Exception EX)
            {

            }

            foreach (var cpmgr in listZbSpecial)
            {
                if (cpmgr.总笔 > 100000)
                {
                    //http://localhost:10010/requestzbhis?list=SH600905&from=0&count=9012
                    //while (true)
                    //{
                        if (cpmgr.总笔 == cpmgr.逐笔)
                            continue;

                        int from = 0;
                        int count = 36048;

                        while (from <= cpmgr.总笔)
                        {
                            try
                            {
                                if (count > cpmgr.总笔 - from)
                                    count = 9012;// (int)cpmgr.总笔 - from;

                                var server = App.ReqL2Uri;
                                var wbr = WebRequest.Create($"{server}/requestzbhis?list={cpmgr.股票代码.ToUpper()}&from={from}&count={count}");
                                if (!wbr.RequestUri.AbsoluteUri.EndsWith("="))
                                {
                                    var resp = await wbr.GetResponseAsync();
                                    using (var stm = resp.GetResponseStream())
                                    using (var rdr = new StreamReader(stm, Encoding.GetEncoding(936)))
                                    {
                                        var respText = await rdr.ReadToEndAsync();
                                        from += count;
                                    }
                                }
                            }
                            catch (Exception EX)
                            {
                                SHReqResult = EX.Message;
                                break;
                            }
                        }

                        //await Task.Delay(3000);
                    //}
                }
            }
        }
        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var listZbhis = new List<ComputeMgr>();
            var listZbSpecial = new List<ComputeMgr>();
            foreach ( var item in _dataGridStocks.SelectedItems)
            {
                if (item is ComputeMgr cpmgr)
                {
                    if (cpmgr.总笔 >= 0 && cpmgr.总笔 < 100000)
                        listZbhis.Add(cpmgr);
                    else
                        listZbSpecial.Add(cpmgr);
                }
            }

            await _compensateZbHis(listZbhis, listZbSpecial);
        }

        private async void btnFetchAllL1_Click(object sender, RoutedEventArgs e)
        {
            bdrBusyNote.Visibility = Visibility.Visible;
            try
            {
                await Task.Run(async () => {
                    var server = App.ReqL2Uri;
                    var wbr = WebRequest.Create($"{server}/getall");
                    var resp = await wbr.GetResponseAsync();
                    using (var stm = resp.GetResponseStream())
                    using (var rdr = new StreamReader(stm, Encoding.GetEncoding(936)))
                    {
                        var respText = await rdr.ReadToEndAsync();
                        SHReqResult = respText;
                    }

                    await Task.Delay(1000);
                    wbr = WebRequest.Create($"{server}/getfin");
                    resp = await wbr.GetResponseAsync();
                    using (var stm = resp.GetResponseStream())
                    using (var rdr = new StreamReader(stm, Encoding.GetEncoding(936)))
                    {
                        var respText = await rdr.ReadToEndAsync();
                        SHReqResult += "---" + respText;
                    }
                });
            }
            catch(Exception EX)
            {
                SHReqResult = EX.Message;
            }

            bdrBusyNote.Visibility = Visibility.Hidden;
        }

        private async void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            var listCM = new List<ComputeMgr>();
            foreach (var item in _dataGridStocks.SelectedItems)
            {
                if (item is ComputeMgr cpmgr)
                {
                    listCM.Add(cpmgr);
                }
            }

            await Task.WhenAll(listCM.Select(cpmgr => Task.Run(() => DzhClient.DealtOrdersManager.LoadDealts(cpmgr.股票代码))));
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class SymbolInOrder
    {
        public int Order { get; set; }
        public string Symbol { get; set; }
    }

    public class BooleanReverseConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)(value);
        }
    }

    public class BigDoubleConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is double)
            {
                double dv = (double)value;
                if (Math.Abs(dv) > 100000000)
                {
                    return $"{(dv / 100000000):0.00}亿";
                }
                else if(Math.Abs(dv) > 10000)
                {
                    return $"{(dv / 10000):0.00}万";
                }
                return $"{dv:0.00}";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)(value);
        }
    }
}
