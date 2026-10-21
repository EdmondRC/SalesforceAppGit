using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Models
{
    public class IndexAccountRecords
    {
        public static Dictionary<string, List<AccountRecord>> BuildIndex(List<AccountRecord> accounts, string fieldName)
        {
            return accounts
                .Where(a =>
                    !string.IsNullOrWhiteSpace(
                        a.GetType().GetProperty(fieldName)?.GetValue(a)?.ToString()))
                .GroupBy(a =>
                    a.GetType().GetProperty(fieldName)?.GetValue(a)?.ToString()!)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());
        }
    }
}
