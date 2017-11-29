using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemoServer
{
    class Order
    {
        public int OrderNumber { get; set; }
        public string DishName { get; set; }
        public bool ReadyStatus { get; set; }
    }
}
