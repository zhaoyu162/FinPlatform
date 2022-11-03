using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class AlertInfo
    {
        public string 股票代码 { get; set; }
        public string 股票名称 { get; set; }
        public string 入池标记 { get; set; }
        public string 预警时间 { get; set; }
        public uint 委托编号 { get; set; }
        public double 下单金额 { get; set; }
        public double 成交金额 { get; set; }
        public float 委托价格 { get; set; }
        public uint 委托数量 { get; set; }
        public float 成交价格 { get; set; }
        public string 是否买单 { get; set; }

        [ColumnName("百万以上\n大单总净额")]
        public double 百万净额 { get; set; }

        [ColumnName("100-300万\n净额")]
        public double M13净额 { get; set; }
        [ColumnName("100-300万\n买 单 数")]
        public int M13买单数 { get; set; }
        [ColumnName("100-300万\n卖 单 数")]
        public int M13卖单数 { get; set; }
        [ColumnName("100-300万\n单差")]
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
    }
}
