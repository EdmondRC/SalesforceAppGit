using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Models
{
    public class ImportedRow
    {
        public int RowNumber { get; set; }
        public Dictionary<string, string> RowData { get; set; }
        public ImportedRow(int rowNumber, Dictionary<string, string> rowData)
        {
            RowNumber = rowNumber;
            RowData = rowData;
        }
    }
}
