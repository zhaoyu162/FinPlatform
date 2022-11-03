using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using PropertyChanged;

namespace FinWPF.Quote
{
    [AddINotifyPropertyChangedInterface]
    //最新值
    public class NewDataInfo
    {
        public int Close_num { get; set; }//此次总量
    
        public int Delta_num { get; set; }//此次增量
  
        public double Last_price { get; set; }

        public string Time { get; set; }

        public List<ZhubiData> ZhubiDataList { get; set; }
    }

    public class ZhubiData
    {
        public UInt32 Time { get;set;}   // 毫秒数，from 1970
        public float Price { get; set; }  // 价格
        public int Volumn { get; set; }     // 成交量
    }

    [AddINotifyPropertyChangedInterface]
    public class StockEntity
    {
        /// <summary>
        /// Price&Volume for ask&bid panels.
        /// </summary>
        [AddINotifyPropertyChangedInterface]
        public class PCPair
        {
            public float Price { get; set; }
            
            public int Count { get; set; }

            public int Index { get; set; }
            public string Alias { get; set; }
        }

        public StockEntity()
        {
            //AskPairs = new List<PCPair>();
            //BidPairs = new List<PCPair>();

            //for (int i = 0; i < 10; i++)
            //{
            //    AskPairs.Add(new PCPair { Price = 0f, Count = 0, Index = i, Alias = $"卖{10-i}" });
            //    BidPairs.Add(new PCPair { Price = 0f, Count = 0, Index = i, Alias = $"买{i + 1}" });
            //}

            //DataInfoList = new List<NewDataInfo>();
        }

        public StockEntity(string code) : this()
        {
        }

        public int DigitCount { 
            get 
            {
                if (!string.IsNullOrEmpty(UniqueCode) &&
                    (UniqueCode.StartsWith("h510") || UniqueCode.StartsWith("z160")))
                {
                    return 3;
                }

                return 2;
            } 
        }
        public string Code { get; set; }
        public long SymbId { get; set; }
        public string UniqueCode { get; set; }
     
        public string Name { get; set; }
     
        public float PreClose { get; set; }
        public float Open { get; set; }
        public float Highest { get; set; }
        public bool SymbolSaved { get; set; }

        public float Lowest { get; set; }
      
        public float Latest { get; set; }
      
        public float LimitHigh { get; set; }
      
        public float LimitLow { get; set; }
        
        public int LentOfBroker { get; set; }
    
        public int LengOfMe { get; set; }
        public string ShardValue { get; set; }
        public DateTime QuoteTime { get; set; }
        public UInt32 TotalDealtCount { get; set; }
        public UInt32 TotalVolume { get; set; }
        public UInt32 CurrVolume { get; set; }
        public UInt64 TotalAmount { get; set; }

        public double FreeStockCounts { get; set; }
        public double TotalStockCounts { get; set; }
        public PCPair[] AskPairs
        {
            get;
            set;
        } = new PCPair[10];
        public PCPair[] BidPairs
        {
            get;
            set;
        } = new PCPair[10];

        //public List<NewDataInfo> DataInfoList { get; set; }
    }
}
