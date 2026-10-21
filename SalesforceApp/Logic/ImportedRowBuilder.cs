using DocumentFormat.OpenXml.Office2016.Excel;
using SalesforceApp.Models;
using SalesforceApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Logic
{
    public static class ImportedRowBuilder
    {
        public static List<ImportedRow> ImportedRowListXL(AccountMatchingRequest request)
        {
            List<ImportedRow> importedRows = new List<ImportedRow>();
            Stream stream = request.FileStream;
            int headerRowIndex = request.HeaderRowNumber;
            int rowNumber = 1;
            // Take the steam and create the Imported Row objects
            ExcelImportService excelImportService = new ExcelImportService();
            var rowData = excelImportService.GetDataRows(stream, headerRowIndex);
            foreach (var row in rowData)
            {
                importedRows.Add(new ImportedRow(rowNumber++, row));
            }
            return importedRows;
        }

        public static List<ImportedRow> ImportedRowListCsv(AccountMatchingRequest request)
        {
            List<ImportedRow> importedRows = new List<ImportedRow>();
            Stream stream = request.FileStream;
            int headerRowIndex = request.HeaderRowNumber;
            int rowNumber = 1;
            // Take the stream and create the Imported Row objects
            CsvImportService csvImportService = new CsvImportService();
            var rowData = csvImportService.GetDataRows(stream, headerRowIndex);
            foreach (var row in rowData)
            {
                importedRows.Add(new ImportedRow(rowNumber++, row));
            }
            return importedRows;
        }
    }
}