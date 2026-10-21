using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML;
using ClosedXML.Excel;
using SalesforceApp.Services;

namespace SalesforceApp.Models
{
    public class AccountMatchingResult
    {
        private int TotalRecordsProcessed { get; set; }
        private int UINMatches { get; set; }
        private int Tier1Matches { get; set; }
        private int Tier2Matches { get; set; }
        private int MultipleMatches { get; set; }
        private int NoMatches { get; set; }
        private int NotEvaluated { get; set; }
        public string ResultsMessage { get; set; } = string.Empty;

        private XLWorkbook _workbook = new XLWorkbook();

        private List<ImportedRowMatched> _matchedRows = new List<ImportedRowMatched>();

        public AccountMatchingResult(List<ImportedRowMatched> matchedRows)
        {
            _matchedRows = matchedRows;
            foreach (var row in matchedRows)
            {
                TotalRecordsProcessed++;
                switch (row.MatchStatus)
                {
                    case Enums.MatchStatus.UINMatch:
                        UINMatches++;
                        break;
                    case Enums.MatchStatus.Tier1:
                        Tier1Matches++;
                        break;
                    case Enums.MatchStatus.Tier2:
                        Tier2Matches++;
                        break;
                    case Enums.MatchStatus.MultipleMatches:
                        MultipleMatches++;
                        break;
                    case Enums.MatchStatus.NoMatches:
                        NoMatches++;
                        break;
                    case Enums.MatchStatus.NotEvaluated:
                        NotEvaluated++;
                        break;
                }


            }

            ResultsMessage =
                $"Processed {TotalRecordsProcessed} records. " +
                $"UIN Matches: {UINMatches}, Tier 1 Matches: {Tier1Matches}, Tier 2 Matches: {Tier2Matches}, " +
                $"Multiple Matches: {MultipleMatches}, No Matches: {NoMatches}, Not Evaluated: {NotEvaluated}.";

            var worksheet = _workbook.Worksheets.Add("Match Results");
            var exportService = new ExcelExportService();
            exportService.WriteMatchResults(worksheet, _matchedRows);
        }

        public byte[] GetWorkbookBytes()
        {
            using var stream = new MemoryStream();
            _workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}