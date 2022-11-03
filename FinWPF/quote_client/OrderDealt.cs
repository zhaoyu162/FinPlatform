using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinWPF.quote_client
{
    [Serializable]

    public class OrderDealt
    {
        public uint DealtVol { get; set; }
        public float DealtPrice { get; set; }
        public bool IsPositive { get; set; }
        public DateTime DealtTime { get; set; }
    }
    [Serializable]

    public class Order
    {
        public uint OrderId { get; set; }
        public uint MinDealtId { get; set; } = uint.MaxValue;
        public uint MaxDealtId { get; set; }
        public uint OrderVol { get; set; }
        public float OrderPrice { get; set; }
        public DateTime DealtTime { get; set; }
        public bool IsBuy { get; set; }
        public double PositiveAmount { get; set; }
        public double AllAmount { get; set; }
        public double OrderAmount { get; set; }
        public ConcurrentDictionary<uint, OrderDealt> OrderDealts { get; set; } = new ConcurrentDictionary<uint, OrderDealt>();
    }
}
