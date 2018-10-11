using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceBusTest
{
    class GetLoanOptionsRequestPayload
    {
        public int CreditScore { get; set; }
    }

    class GetLoanOptionsResponsePayload
    {
        public string Provider { get; set; }

        public int LoanAmount { get; set; }
    }
}
