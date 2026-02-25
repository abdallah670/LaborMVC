using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.Response
{
    public class StripePaymentIntentResult
    {
        public string PaymentIntentId { get; init; } = default!;
        public string ClientSecret { get; init; } = default!;
    }
}
