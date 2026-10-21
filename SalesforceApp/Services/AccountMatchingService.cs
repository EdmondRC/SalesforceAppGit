using SalesforceApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SalesforceApp.Mapping;
using DocumentFormat.OpenXml.Bibliography;
using SalesforceApp.Logic;

namespace SalesforceApp.Services
{
    public class AccountMatchingService
    {
        public AccountMatchingResult MatchAccounts(
            AccountMatchingRequest request, string connectionString)
        {

            AccountMatchingEngine engine = new AccountMatchingEngine(request,connectionString);
            AccountMatchingResult result = engine.GetMatchingRetults();

            return result;
        }
    }
}