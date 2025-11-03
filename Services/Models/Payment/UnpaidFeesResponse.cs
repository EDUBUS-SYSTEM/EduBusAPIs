using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Payment
{
    public class UnpaidFeesResponse
    {
        public bool HasUnpaidFees { get; set; }
        public int Count { get; set; }
    }
}
