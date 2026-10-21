using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace SalesforceApp.Services
{
    public class ExcelImportService
    {

        public List<string> GetHeaders(Stream stream, int headerRowNumber)
        {
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheets.First();
                var headerRow = worksheet.Row(headerRowNumber);
                var headers = new List<string>();
                foreach (var cell in headerRow.Cells())
                {
                    if (!string.IsNullOrWhiteSpace(cell.GetString()))
                    {
                        headers.Add(cell.GetString());
                    }
                }
                return headers;
            }
        }

        public List<Dictionary<string, string>> GetDataRows(Stream stream, int headerRowNumber)

        {
            if (stream == null || stream.Length == 0)
            {
                throw new ArgumentException("The provided stream is null or empty.");
            }
            try
            {
                stream.Position = 0; // Reset the stream position to the beginning
                var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var dataRows = new List<Dictionary<string, string>>();
                var headers = worksheet.Row(headerRowNumber)
                       .CellsUsed()
                       .Select(c => c.GetString())
                       .ToList();
                if (worksheet.Rows().Count() <= headerRowNumber)
                {
                    throw new InvalidOperationException("The worksheet does not contain any data rows after the header row.");
                }
                int rowCount = worksheet.LastRowUsed().RowNumber();

                for (int rowIndex = headerRowNumber + 1; rowIndex <= rowCount; rowIndex++)
                {
                    var row = worksheet.Row(rowIndex);
                    var rowData = new Dictionary<string, string>();
                    for (int i = 0; i < headers.Count; i++)
                    {
                        rowData[headers[i]] = row.Cell(i + 1).GetString();
                    }
                    dataRows.Add(rowData);
                }
                return dataRows;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while processing the Excel file.", ex);
            }
        }

    
    }
     
}