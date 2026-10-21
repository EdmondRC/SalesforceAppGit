using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Enums
{
    public enum  MatchStatus
    {
            UINMatch = 6,
            Tier1 = 5,
            Tier2 = 4,
            MultipleMatches = 3,
            NoMatches = 2,
            NotEvaluated = 1
    }
}
