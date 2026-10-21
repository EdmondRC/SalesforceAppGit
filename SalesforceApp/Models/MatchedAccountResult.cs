using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SalesforceApp.Enums;

namespace SalesforceApp.Models
{
    /// <summary>
    /// Pairs a matched AccountRecord with the specific MatchingRule that produced the match,
    /// since a single row can match multiple accounts via different rules.
    /// </summary>
    public class MatchedAccountResult
    {
        public AccountRecord Account { get; set; } = null!;
        public MatchingRule Rule { get; set; }

        public MatchedAccountResult() { }

        public MatchedAccountResult(AccountRecord account, MatchingRule rule)
        {
            Account = account;
            Rule = rule;
        }
    }
}