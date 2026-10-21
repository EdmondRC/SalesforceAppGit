using SalesforceApp.Models;
using SalesforceApp.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Logic
{
    public static class NormalizeAccountList
    {
        public static void NormalizeAccounts(List<AccountRecord> accounts) 
        {
           foreach (var account in accounts)
           {
                account.Name = StringMatchingHelpers.NormalizeString(account.Name);
                account.Phone = StringMatchingHelpers.NormalizePhone(account.Phone);
                account.Website = StringMatchingHelpers.NormalizeWebsite(account.Website);
                account.BillingStreet = AddressNormalizer.NormalizeStreetAddress(account.BillingStreet);
                account.BillingCity = StringMatchingHelpers.NormalizeString(account.BillingCity);
                account.BillingState = StateNormalizer.NormalizeState(account.BillingState);
                account.BillingPostalCode = StringMatchingHelpers.NormalizeString(account.BillingPostalCode);
                account.BillingCountry = StringMatchingHelpers.NormalizeString(account.BillingCountry);
                account.ShippingStreet = AddressNormalizer.NormalizeStreetAddress(account.ShippingStreet);
                account.ShippingCity = StringMatchingHelpers.NormalizeString(account.ShippingCity);
                account.ShippingState = StateNormalizer.NormalizeState(account.ShippingState);
                account.ShippingPostalCode = StringMatchingHelpers.NormalizeString(account.ShippingPostalCode);
                account.EIN__c = StringMatchingHelpers.NormalizeString(account.EIN__c);
            }
        }
    }
}
