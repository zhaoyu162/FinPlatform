using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    [Serializable]
    public class ComputeFields
    {
        public string 时间点 { get; set; }
        public string 股票代码 { get; set; }
        public string 股票名称 { get; set; }
        public string 挡位 { get; set; }

        public int 逐笔数 { get; set; }

        public double 涨幅 { get; set; }
        public double 主力净额 { get; set; }
        [ColumnName("拉升意愿")]
        public double 主力额比 { get; set; }
        public double 大单比 { get; set; }

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
    }

    [Serializable]
    public class BigOrderComputeFields
    {
        public string 时间点 { get; set; }
        public string 股票代码 { get; set; }
        public string 股票名称 { get; set; }
        public double 成交额 { get; set; }
        //public double 百万净额 { get; set; }
        //public double 百万单差 { get; set; }
        //public double 百万额差 { get; set; }
        [ColumnName("300-500万\n买单数")]
        public int M35买单数 { get; set; }
        [ColumnName("300-500万\n卖单数")]
        public int M35卖单数 { get; set; }
        [ColumnName("300-500万\n单差")]
        public double M35单差 { get; set; }
        [ColumnName("300-500万\n额差")]
        public double M35额差 { get; set; }
        [ColumnName("300万占比\n成交总额")]
        public double M35额比 { get; set; }
        [ColumnName("300-500万占比\n百万总额")]
        public double M3百万额比 { get; set; }
        [ColumnName("500万以上\n买 单 数")]
        public int M5买单数 { get; set; }
        [ColumnName("500万以上\n卖 单 数")]
        public int M5卖单数 { get; set; }
        [ColumnName("500万\n单差")]
        public double M5单差 { get; set; }
        [ColumnName("500万\n额差")]
        public double M5额差 { get; set; }
        [ColumnName("500万占比\n成交总额")]
        public double M5额比 { get; set; }
        [ColumnName("500万占比\n百万总额")]
        public double M5百万额比 { get; set; }
    }
}
