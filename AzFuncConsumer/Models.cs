using System;
using System.Collections.Generic;
using System.Text;

namespace SBRRTest
{
    public class GetLoanOptionsRequestPayload
    {
        public int Id { get; set; }

        public int CreditScore { get; set; }
    }

    public class GetLoanOptionsResponsePayload
    {
        public string Provider { get; set; }

        public int LoanAmount { get; set; }
    }
}
