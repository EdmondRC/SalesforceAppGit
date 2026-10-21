using ClosedXML.Excel;
using SalesforceApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SalesforceApp.Services
{
    public class ExcelExportService
    {
        public void WriteMatchResults(IXLWorksheet worksheet, List<ImportedRowMatched> matchedRows)
        {
            var dataFieldNames = matchedRows
                .SelectMany(r => r.RowData.Keys)
                .Distinct()
                .ToList();

            var accountProperties = typeof(AccountRecord)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToList();

            int col = 1;
            foreach (var fieldName in dataFieldNames)
            {
                worksheet.Cell(1, col).Value = fieldName;
                col++;
            }

            int matchStatusCol = col++;
            int matchingRuleCol = col++;
            worksheet.Cell(1, matchStatusCol).Value = "Match Status";
            worksheet.Cell(1, matchingRuleCol).Value = "Matching Rule";

            int firstAccountCol = col;
            foreach (var property in accountProperties)
            {
                worksheet.Cell(1, col).Value = $"Account.{property.Name}";
                col++;
            }

            int outputRow = 2;
            foreach (var row in matchedRows)
            {
                if (row.MatchedAccountResults.Count == 0)
                {
                    WriteDataRow(worksheet, outputRow, dataFieldNames, row, matchStatusCol, matchingRuleCol, null);
                    outputRow++;
                    continue;
                }

                foreach (var matchedAccountResult in row.MatchedAccountResults)
                {
                    WriteDataRow(worksheet, outputRow, dataFieldNames, row, matchStatusCol, matchingRuleCol, matchedAccountResult.Rule);
                    WriteAccountRow(worksheet, outputRow, firstAccountCol, accountProperties, matchedAccountResult.Account);
                    outputRow++;
                }
            }

            worksheet.Columns().AdjustToContents();
        }

        private void WriteDataRow(
            IXLWorksheet worksheet,
            int outputRow,
            List<string> dataFieldNames,
            ImportedRowMatched row,
            int matchStatusCol,
            int matchingRuleCol,
            Enums.MatchingRule? matchingRule)
        {
            int col = 1;
            foreach (var fieldName in dataFieldNames)
            {
                worksheet.Cell(outputRow, col).Value =
                    row.RowData.TryGetValue(fieldName, out var value) ? value : string.Empty;
                col++;
            }

            worksheet.Cell(outputRow, matchStatusCol).Value = row.MatchStatus.ToString();
            worksheet.Cell(outputRow, matchingRuleCol).Value = matchingRule?.ToString() ?? string.Empty;
        }

        private void WriteAccountRow(
            IXLWorksheet worksheet,
            int outputRow,
            int firstAccountCol,
            List<PropertyInfo> accountProperties,
            AccountRecord account)
        {
            int col = firstAccountCol;
            foreach (var property in accountProperties)
            {
                var value = property.GetValue(account);
                worksheet.Cell(outputRow, col).Value = value?.ToString() ?? string.Empty;
                col++;
            }
        }

        private string SaveUploadFile(IFormFile uploadFile)
        {
            var extension = Path.GetExtension(uploadFile.FileName);

            var fileName = $"{Guid.NewGuid()}{extension}";

            var uploadFolder = Path.Combine(Path.GetTempPath(), "SalesforceAccountMatching");
            Directory.CreateDirectory(uploadFolder);

            var tempPath = Path.Combine(
                uploadFolder,
                fileName);

            using var stream = new FileStream(tempPath, FileMode.Create);

            uploadFile.CopyTo(stream);

            return tempPath;
        }
    }
}