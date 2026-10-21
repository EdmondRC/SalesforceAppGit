using SalesforceApp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Models
{
    public class ImportedRowMatched : ImportedRow
    {
        /// <summary>
        /// Each matched account paired with the specific MatchingRule that produced that match.
        /// A single row can match multiple accounts via different rules, so the rule is stored
        /// per account rather than once for the whole row.
        /// </summary>
        public List<MatchedAccountResult> MatchedAccountResults { get; set; } = new List<MatchedAccountResult>();

        public Enums.MatchStatus MatchStatus { get; set; } = Enums.MatchStatus.NotEvaluated;

        public ImportedRowMatched(ImportedRow row, List<MatchedAccountResult> matchedAccountResults)
            : base(row.RowNumber, row.RowData)
        {
            MatchedAccountResults = matchedAccountResults;
        }
        public ImportedRowMatched(int rowNumber, Dictionary<string, string> rowData)
            : base(rowNumber, rowData)

        { }
    }
}
