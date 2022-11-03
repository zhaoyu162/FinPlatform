using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    [Serializable]

    public class DealtOrder
    {
        public DateTime DealtTime { get; set; }
        public uint DealtId { get; set; }
        public uint BuyOrderId { get; set; }
        public uint SellOrderId { get; set; }
        public uint BuyOrderVol { get; set; }
        public uint SellOrderVol { get; set; }
        public float BuyOrderPrice { get; set; }
        public float SellOrderPrice { get; set; }
        public uint DealtVol { get; set; }
        public float DealtPrice { get; set; }
        public bool IsBuyPositive { get; set; }
    }
}
