using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office.Y2022.FeaturePropertyBag;
using DocumentFormat.OpenXml.Spreadsheet;
using SalesforceApp.Enums;
using SalesforceApp.Interfaces;
using SalesforceApp.Mapping;
using SalesforceApp.Models;
using SalesforceApp.Repositories;
using SalesforceApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Logic
{
    public class AccountMatchingEngine
    {
        private AccountMatchingRequest Request { get; set; }
        private List<AccountRecord> Accounts { get; set; }
        private ClosedXML.Excel.IXLRow _headerRow { get; set; }
        public AccountMatchingEngine(AccountMatchingRequest request, string connectionString)
        {
            Request = request;
            AccountRepository accountRepository = new AccountRepository(connectionString);
            Accounts = accountRepository.GetAccounts();
        }

        public AccountMatchingResult GetMatchingRetults()
        {
            var accounts = Accounts;
            List<ImportedRow> importedRows = new List<ImportedRow>();
            var request = Request;
            if (Request.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                //not implemented yet
                throw new ArgumentException("CSV file format is not supported yet.");
            }
            else if (Request.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) && Request.MappingRows.Count > 0)
            {

                importedRows = ImportedRowBuilder.ImportedRowListXL(request);
            }
            else
            {
                throw new ArgumentException("An error occured, either the wrong file type was imported or the list had no rows.");
            }

            RemappingLogic.RemappedImportedRows(importedRows, request.MappingRows);
            // Normalize the cached accounts 
            NormalizeAccountList.NormalizeAccounts(accounts);
            // Procedurally match the imported rows to the cached accounts using the matching logic
            AccountMatchingLogic accountMatchingLogic = new AccountMatchingLogic(accounts, importedRows);
            accountMatchingLogic.MatchingProcedure();

            var result = new AccountMatchingResult(accountMatchingLogic.GetMatchedRows());

            return result;

        }

    }
}