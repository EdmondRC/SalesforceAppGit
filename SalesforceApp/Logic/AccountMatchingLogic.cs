
using Microsoft.IdentityModel.Tokens;
using SalesforceApp.Enums;
using SalesforceApp.Mapping;
using SalesforceApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Logic
{
    public class AccountMatchingLogic
    {
        private List<ImportedRowMatched> _matchedRows = new List<ImportedRowMatched>();
        private List<ImportedRow> _importedRows = new List<ImportedRow>();
        private List<AccountRecord> _accounts = new List<AccountRecord>();

        private static readonly List<string> UniqueIdFields = new List<string>
        {
            "EIN__c",
            "Ministry_Partner_ID__c",
            "BMIC_Mktg_UIN__c",
            "AESF__Lookup_Code__c",
            "Id"
        };

        // Tier rank: higher value = higher precedence. Tier1 beats Tier2.
        private const int Tier1Rank = 2;
        private const int Tier2Rank = 1;

        private class MatchCandidate
        {
            public AccountRecord Account { get; set; } = null!;
            public MatchingRule Rule { get; set; }
            public int TierRank { get; set; }
        }

        /// <summary>
        /// Holds the normalized values of an imported row's fields, computed once per row so
        /// every matching rule compares against the same normalized representation that the
        /// cached accounts were normalized with (NormalizeAccountList.NormalizeAccounts).
        /// </summary>
        private class NormalizedRow
        {
            public string Name { get; set; } = string.Empty;
            public string BillingStreet { get; set; } = string.Empty;
            public string BillingCity { get; set; } = string.Empty;
            public string BillingState { get; set; } = string.Empty;
            public string BillingPostalCode { get; set; } = string.Empty;
            public string ShippingStreet { get; set; } = string.Empty;
            public string ShippingCity { get; set; } = string.Empty;
            public string ShippingState { get; set; } = string.Empty;
            public string ShippingPostalCode { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Website { get; set; } = string.Empty;
        }

        public AccountMatchingLogic(List<AccountRecord> accounts, List<ImportedRow> importedRows)
        {
            _accounts = accounts;
            _importedRows = importedRows;
        }
        public List<ImportedRowMatched> GetMatchedRows()
        {
            return _matchedRows;
        }
        public void MatchingProcedure()
        {
            foreach (var row in _importedRows)
            {
                ImportedRowMatched matchedRow = new ImportedRowMatched(row.RowNumber, row.RowData);
                _matchedRows.Add(matchedRow);
            }

            // Step 1: Unique identifier matching. A unique match takes precedence over everything else.
            foreach (string field in UniqueIdFields)
            {
                if (_importedRows[0].RowData.ContainsKey(field))
                {
                    var index = IndexAccountRecords.BuildIndex(_accounts, field);
                    foreach (var row in _matchedRows)
                    {
                        if (row.MatchStatus == MatchStatus.UINMatch)
                        {
                            continue; // Already matched on an earlier unique field
                        }

                        if (row.RowData.ContainsKey(field) && !string.IsNullOrEmpty(row.RowData[field]))
                        {
                            if (index.TryGetValue(row.RowData[field], out var accounts))
                            {
                                row.MatchedAccountResults.Clear();
                                row.MatchedAccountResults.AddRange(
                                    accounts.Select(a => new MatchedAccountResult(a, MatchingRule.UniqueId)));
                                row.MatchStatus = MatchStatus.UINMatch;
                            }
                        }
                    }
                }
            }

            // Build indexes once, up front, for every field used by the remaining rules.
            // Accounts were already normalized by NormalizeAccountList.NormalizeAccounts, so these
            // indexes are keyed on normalized values.
            Dictionary<string, List<AccountRecord>> accountNameIndex = IndexAccountRecords.BuildIndex(_accounts, "Name");
            Dictionary<string, List<AccountRecord>> accountPhoneIndex = IndexAccountRecords.BuildIndex(_accounts, "Phone");
            Dictionary<string, List<AccountRecord>> accountWebsiteIndex = IndexAccountRecords.BuildIndex(_accounts, "Website");

            // Address indexes: each street+city/postal code combination maps to the accounts that
            // have that combination on EITHER their Billing OR Shipping address, so a row's Billing
            // or Shipping fields can be looked up against either side of the account in O(1).
            var accountFullAddressIndex = BuildFullAddressIndex(_accounts);
            var accountStreetCityIndex = BuildStreetCityIndex(_accounts);
            var accountPostalCodeIndex = BuildPostalCodeIndex(_accounts);

            // Step 2: Evaluate remaining rows across all non-unique matching rules, keep only the
            // highest tier of match found, and resolve the final status.
            foreach (var row in _matchedRows)
            {
                if (row.MatchStatus == MatchStatus.UINMatch)
                {
                    continue;
                }

                if (!CanEvaluateRow(row.RowData))
                {
                    row.MatchStatus = MatchStatus.NotEvaluated;
                    continue;
                }

                // Normalize the row's fields once, using the exact same normalization routines
                // applied to the cached accounts, so comparisons are apples-to-apples.
                var normalizedRow = NormalizeRow(row.RowData);

                var candidates = new List<MatchCandidate>();

                // Name-based rules
                if (!string.IsNullOrEmpty(normalizedRow.Name) && accountNameIndex.TryGetValue(normalizedRow.Name, out List<AccountRecord> accountsByName))
                {
                    foreach (var account in accountsByName)
                    {
                        bool fullAddressMatches = FullAddressMatches(normalizedRow, account);
                        bool partialAddressMatches = !fullAddressMatches && PartialAddressMatches(normalizedRow, account);
                        bool phoneMatches = PhoneMatches(normalizedRow, account);
                        bool websiteMatches = WebsiteMatches(normalizedRow, account);

                        if (fullAddressMatches)
                        {
                            candidates.Add(new MatchCandidate { Account = account, Rule = MatchingRule.NameFullAddress, TierRank = Tier1Rank });
                        }
                        else if (partialAddressMatches)
                        {
                            candidates.Add(new MatchCandidate { Account = account, Rule = MatchingRule.NamePartialAddress, TierRank = Tier1Rank });
                        }
                        else if (phoneMatches)
                        {
                            candidates.Add(new MatchCandidate { Account = account, Rule = MatchingRule.NamePhone, TierRank = Tier1Rank });
                        }
                        else if (websiteMatches)
                        {
                            candidates.Add(new MatchCandidate { Account = account, Rule = MatchingRule.NameWebsite, TierRank = Tier1Rank });
                        }
                    }
                }

                // Non-name-based rules, driven off indexes instead of scanning every account.
                var addressCandidateAccounts = GetAddressCandidateAccounts(
                    normalizedRow, accountFullAddressIndex, accountStreetCityIndex, accountPostalCodeIndex);

                foreach (var account in addressCandidateAccounts)
                {
                    bool fullAddressMatches = FullAddressMatches(normalizedRow, account);
                    bool partialAddressMatches = !fullAddressMatches && PartialAddressMatches(normalizedRow, account);
                    bool phoneMatches = PhoneMatches(normalizedRow, account);
                    bool websiteMatches = WebsiteMatches(normalizedRow, account);

                    if (fullAddressMatches && phoneMatches)
                    {
                        candidates.Add(new MatchCandidate { Account = account, Rule = MatchingRule.FullAddressPhone, TierRank = Tier1Rank });
                    }
                    else if (partialAddressMatches && phoneMatches)
                    {
                        candidates.Add(new MatchCandidate { Account = account, Rule = MatchingRule.PartialAddressPhone, TierRank = Tier2Rank });
                    }
                    else if (partialAddressMatches && websiteMatches)
                    {
                        candidates.Add(new MatchCandidate { Account = account, Rule = MatchingRule.PartialAddressWebsite, TierRank = Tier2Rank });
                    }
                }

                if (candidates.Count == 0)
                {
                    row.MatchStatus = MatchStatus.NoMatches;
                    continue;
                }

                // Keep only the best rule found per account, then keep only the accounts at the
                // highest tier rank overall. Lower tier matches are discarded entirely.
                var bestPerAccount = candidates
                    .GroupBy(c => c.Account)
                    .Select(g => g.OrderByDescending(c => c.TierRank).First())
                    .ToList();

                int bestTierRank = bestPerAccount.Max(c => c.TierRank);
                var finalists = bestPerAccount.Where(c => c.TierRank == bestTierRank).ToList();

                row.MatchedAccountResults.Clear();
                row.MatchedAccountResults.AddRange(
                    finalists.Select(c => new MatchedAccountResult(c.Account, c.Rule)));

                if (finalists.Count > 1)
                {
                    row.MatchStatus = MatchStatus.MultipleMatches;
                }
                else
                {
                    row.MatchStatus = bestTierRank == Tier1Rank ? MatchStatus.Tier1 : MatchStatus.Tier2;
                }
            }
        }

        /// <summary>
        /// Determines whether a row has enough data to reasonably attempt any matching rule.
        /// A row is evaluable if it has a Name, a usable address (full or partial), a Phone,
        /// or a Website populated - any one of these is enough to drive at least one rule.
        /// </summary>
        private bool CanEvaluateRow(Dictionary<string, string> rowData)
        {
            bool hasName = rowData.ContainsKey("Name") && !string.IsNullOrWhiteSpace(rowData["Name"]);

            bool hasBillingAddress =
                (rowData.ContainsKey("BillingStreet") && !string.IsNullOrWhiteSpace(rowData["BillingStreet"])) ||
                (rowData.ContainsKey("BillingCity") && !string.IsNullOrWhiteSpace(rowData["BillingCity"])) ||
                (rowData.ContainsKey("BillingPostalCode") && !string.IsNullOrWhiteSpace(rowData["BillingPostalCode"]));

            bool hasShippingAddress =
                (rowData.ContainsKey("ShippingStreet") && !string.IsNullOrWhiteSpace(rowData["ShippingStreet"])) ||
                (rowData.ContainsKey("ShippingCity") && !string.IsNullOrWhiteSpace(rowData["ShippingCity"])) ||
                (rowData.ContainsKey("ShippingPostalCode") && !string.IsNullOrWhiteSpace(rowData["ShippingPostalCode"]));

            bool hasPhone = rowData.ContainsKey("Phone") && !string.IsNullOrWhiteSpace(rowData["Phone"]);
            bool hasWebsite = rowData.ContainsKey("Website") && !string.IsNullOrWhiteSpace(rowData["Website"]);

            return hasName || hasBillingAddress || hasShippingAddress || hasPhone || hasWebsite;
        }

        /// <summary>
        /// Normalizes a row's relevant fields using the exact same normalization helpers applied
        /// to the cached accounts (see NormalizeAccountList.NormalizeAccounts), so comparisons are
        /// always apples-to-apples. This does not mutate the original RowData, so the exported
        /// report continues to show the user's original input.
        /// </summary>
        private NormalizedRow NormalizeRow(Dictionary<string, string> rowData)
        {
            string Get(string key) => rowData.TryGetValue(key, out var value) ? value : string.Empty;

            return new NormalizedRow
            {
                Name = StringMatchingHelpers.NormalizeString(Get("Name")),
                BillingStreet = AddressNormalizer.NormalizeStreetAddress(Get("BillingStreet")),
                BillingCity = StringMatchingHelpers.NormalizeString(Get("BillingCity")),
                BillingState = StateNormalizer.NormalizeState(Get("BillingState")),
                BillingPostalCode = StringMatchingHelpers.NormalizeString(Get("BillingPostalCode")),
                ShippingStreet = AddressNormalizer.NormalizeStreetAddress(Get("ShippingStreet")),
                ShippingCity = StringMatchingHelpers.NormalizeString(Get("ShippingCity")),
                ShippingState = StateNormalizer.NormalizeState(Get("ShippingState")),
                ShippingPostalCode = StringMatchingHelpers.NormalizeString(Get("ShippingPostalCode")),
                Phone = StringMatchingHelpers.NormalizePhone(Get("Phone")),
                Website = StringMatchingHelpers.NormalizeWebsite(Get("Website"))
            };
        }

        /// <summary>
        /// Builds an index keyed on "Street|City|State|PostalCode" covering both an account's
        /// Billing and Shipping address, so a row's Billing or Shipping fields can be looked up
        /// against either side of the account in O(1).
        /// </summary>
        private Dictionary<string, List<AccountRecord>> BuildFullAddressIndex(List<AccountRecord> accounts)
        {
            var index = new Dictionary<string, List<AccountRecord>>();

            void AddEntry(string street, string city, string state, string postalCode, AccountRecord account)
            {
                if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(city) || string.IsNullOrEmpty(state) || string.IsNullOrEmpty(postalCode))
                {
                    return;
                }

                string key = $"{street}|{city}|{state}|{postalCode}";
                if (!index.TryGetValue(key, out var list))
                {
                    list = new List<AccountRecord>();
                    index[key] = list;
                }
                list.Add(account);
            }

            foreach (var account in accounts)
            {
                AddEntry(account.BillingStreet ?? string.Empty, account.BillingCity ?? string.Empty, account.BillingState ?? string.Empty, account.BillingPostalCode ?? string.Empty, account);
                AddEntry(account.ShippingStreet ?? string.Empty, account.ShippingCity ?? string.Empty, account.ShippingState ?? string.Empty, account.ShippingPostalCode ?? string.Empty, account);
            }

            return index;
        }

        /// <summary>
        /// Builds an index keyed on "Street|City" covering both an account's Billing and Shipping
        /// address, for partial address matching.
        /// </summary>
        private Dictionary<string, List<AccountRecord>> BuildStreetCityIndex(List<AccountRecord> accounts)
        {
            var index = new Dictionary<string, List<AccountRecord>>();

            void AddEntry(string street, string city, AccountRecord account)
            {
                if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(city))
                {
                    return;
                }

                string key = $"{street}|{city}";
                if (!index.TryGetValue(key, out var list))
                {
                    list = new List<AccountRecord>();
                    index[key] = list;
                }
                list.Add(account);
            }

            foreach (var account in accounts)
            {
                AddEntry(account.BillingStreet ?? string.Empty, account.BillingCity ?? string.Empty, account);
                AddEntry(account.ShippingStreet ?? string.Empty, account.ShippingCity ?? string.Empty, account);
            }

            return index;
        }

        /// <summary>
        /// Builds an index keyed on postal code covering both an account's Billing and Shipping
        /// postal code, for partial address matching.
        /// </summary>
        private Dictionary<string, List<AccountRecord>> BuildPostalCodeIndex(List<AccountRecord> accounts)
        {
            var index = new Dictionary<string, List<AccountRecord>>();

            void AddEntry(string postalCode, AccountRecord account)
            {
                if (string.IsNullOrEmpty(postalCode))
                {
                    return;
                }

                if (!index.TryGetValue(postalCode, out var list))
                {
                    list = new List<AccountRecord>();
                    index[postalCode] = list;
                }
                list.Add(account);
            }

            foreach (var account in accounts)
            {
                AddEntry(account.BillingPostalCode ?? string.Empty, account);
                AddEntry(account.ShippingPostalCode ?? string.Empty, account);
            }

            return index;
        }

        /// <summary>
        /// Uses the address indexes to find the small set of accounts that could possibly match
        /// the row's Billing or Shipping address (full or partial), instead of scanning every
        /// cached account for every row.
        /// </summary>
        private List<AccountRecord> GetAddressCandidateAccounts(
            NormalizedRow row,
            Dictionary<string, List<AccountRecord>> fullAddressIndex,
            Dictionary<string, List<AccountRecord>> streetCityIndex,
            Dictionary<string, List<AccountRecord>> postalCodeIndex)
        {
            var results = new HashSet<AccountRecord>();

            void TryAddFull(string street, string city, string state, string postalCode)
            {
                if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(city) || string.IsNullOrEmpty(state) || string.IsNullOrEmpty(postalCode))
                {
                    return;
                }

                string key = $"{street}|{city}|{state}|{postalCode}";
                if (fullAddressIndex.TryGetValue(key, out var accounts))
                {
                    results.UnionWith(accounts);
                }
            }

            void TryAddStreetCity(string street, string city)
            {
                if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(city))
                {
                    return;
                }

                string key = $"{street}|{city}";
                if (streetCityIndex.TryGetValue(key, out var accounts))
                {
                    results.UnionWith(accounts);
                }
            }

            void TryAddPostalCode(string postalCode)
            {
                if (string.IsNullOrEmpty(postalCode))
                {
                    return;
                }

                if (postalCodeIndex.TryGetValue(postalCode, out var accounts))
                {
                    results.UnionWith(accounts);
                }
            }

            TryAddFull(row.BillingStreet, row.BillingCity, row.BillingState, row.BillingPostalCode);
            TryAddFull(row.ShippingStreet, row.ShippingCity, row.ShippingState, row.ShippingPostalCode);

            TryAddStreetCity(row.BillingStreet, row.BillingCity);
            TryAddStreetCity(row.ShippingStreet, row.ShippingCity);

            TryAddPostalCode(row.BillingPostalCode);
            TryAddPostalCode(row.ShippingPostalCode);

            return results.ToList();
        }

        /// <summary>
        /// Checks the row's Billing and Shipping address fields against both the account's Billing
        /// and account's Shipping address fields (all four combinations), so either address on the
        /// imported row can match either address on the account. Both sides are already normalized.
        /// </summary>
        private bool FullAddressMatches(NormalizedRow row, AccountRecord account)
        {
            string accountBillingStreet = account.BillingStreet ?? string.Empty;
            string accountBillingCity = account.BillingCity ?? string.Empty;
            string accountBillingState = account.BillingState ?? string.Empty;
            string accountBillingPostalCode = account.BillingPostalCode ?? string.Empty;
            string accountShippingStreet = account.ShippingStreet ?? string.Empty;
            string accountShippingCity = account.ShippingCity ?? string.Empty;
            string accountShippingState = account.ShippingState ?? string.Empty;
            string accountShippingPostalCode = account.ShippingPostalCode ?? string.Empty;

            bool rowBillingMatchesAccountBilling =
                !string.IsNullOrEmpty(row.BillingStreet) && !string.IsNullOrEmpty(row.BillingCity) && !string.IsNullOrEmpty(row.BillingState) && !string.IsNullOrEmpty(row.BillingPostalCode) &&
                row.BillingStreet == accountBillingStreet &&
                row.BillingCity == accountBillingCity &&
                row.BillingState == accountBillingState &&
                row.BillingPostalCode == accountBillingPostalCode;

            bool rowBillingMatchesAccountShipping =
                !string.IsNullOrEmpty(row.BillingStreet) && !string.IsNullOrEmpty(row.BillingCity) && !string.IsNullOrEmpty(row.BillingState) && !string.IsNullOrEmpty(row.BillingPostalCode) &&
                row.BillingStreet == accountShippingStreet &&
                row.BillingCity == accountShippingCity &&
                row.BillingState == accountShippingState &&
                row.BillingPostalCode == accountShippingPostalCode;

            bool rowShippingMatchesAccountBilling =
                !string.IsNullOrEmpty(row.ShippingStreet) && !string.IsNullOrEmpty(row.ShippingCity) && !string.IsNullOrEmpty(row.ShippingState) && !string.IsNullOrEmpty(row.ShippingPostalCode) &&
                row.ShippingStreet == accountBillingStreet &&
                row.ShippingCity == accountBillingCity &&
                row.ShippingState == accountBillingState &&
                row.ShippingPostalCode == accountBillingPostalCode;

            bool rowShippingMatchesAccountShipping =
                !string.IsNullOrEmpty(row.ShippingStreet) && !string.IsNullOrEmpty(row.ShippingCity) && !string.IsNullOrEmpty(row.ShippingState) && !string.IsNullOrEmpty(row.ShippingPostalCode) &&
                row.ShippingStreet == accountShippingStreet &&
                row.ShippingCity == accountShippingCity &&
                row.ShippingState == accountShippingState &&
                row.ShippingPostalCode == accountShippingPostalCode;

            return rowBillingMatchesAccountBilling
                || rowBillingMatchesAccountShipping
                || rowShippingMatchesAccountBilling
                || rowShippingMatchesAccountShipping;
        }

        /// <summary>
        /// Checks the row's Billing and Shipping street/city or postal code against both the
        /// account's Billing and account's Shipping address fields (all four combinations). Both
        /// sides are already normalized.
        /// </summary>
        private bool PartialAddressMatches(NormalizedRow row, AccountRecord account)
        {
            string accountBillingStreet = account.BillingStreet ?? string.Empty;
            string accountBillingCity = account.BillingCity ?? string.Empty;
            string accountBillingPostalCode = account.BillingPostalCode ?? string.Empty;
            string accountShippingStreet = account.ShippingStreet ?? string.Empty;
            string accountShippingCity = account.ShippingCity ?? string.Empty;
            string accountShippingPostalCode = account.ShippingPostalCode ?? string.Empty;

            bool rowBillingStreetCityMatchesAccountBilling =
                !string.IsNullOrEmpty(row.BillingStreet) && !string.IsNullOrEmpty(row.BillingCity) &&
                row.BillingStreet == accountBillingStreet && row.BillingCity == accountBillingCity;

            bool rowBillingStreetCityMatchesAccountShipping =
                !string.IsNullOrEmpty(row.BillingStreet) && !string.IsNullOrEmpty(row.BillingCity) &&
                row.BillingStreet == accountShippingStreet && row.BillingCity == accountShippingCity;

            bool rowShippingStreetCityMatchesAccountBilling =
                !string.IsNullOrEmpty(row.ShippingStreet) && !string.IsNullOrEmpty(row.ShippingCity) &&
                row.ShippingStreet == accountBillingStreet && row.ShippingCity == accountBillingCity;

            bool rowShippingStreetCityMatchesAccountShipping =
                !string.IsNullOrEmpty(row.ShippingStreet) && !string.IsNullOrEmpty(row.ShippingCity) &&
                row.ShippingStreet == accountShippingStreet && row.ShippingCity == accountShippingCity;

            bool rowBillingPostalCodeMatchesAccount =
                !string.IsNullOrEmpty(row.BillingPostalCode) &&
                (row.BillingPostalCode == accountBillingPostalCode || row.BillingPostalCode == accountShippingPostalCode);

            bool rowShippingPostalCodeMatchesAccount =
                !string.IsNullOrEmpty(row.ShippingPostalCode) &&
                (row.ShippingPostalCode == accountShippingPostalCode || row.ShippingPostalCode == accountBillingPostalCode);

            return rowBillingStreetCityMatchesAccountBilling
                || rowBillingStreetCityMatchesAccountShipping
                || rowShippingStreetCityMatchesAccountBilling
                || rowShippingStreetCityMatchesAccountShipping
                || rowBillingPostalCodeMatchesAccount
                || rowShippingPostalCodeMatchesAccount;
        }

        private bool PhoneMatches(NormalizedRow row, AccountRecord account)
        {
            string accountPhone = account.Phone ?? string.Empty;
            return !string.IsNullOrEmpty(accountPhone) && !string.IsNullOrEmpty(row.Phone) && row.Phone == accountPhone;
        }

        private bool WebsiteMatches(NormalizedRow row, AccountRecord account)
        {
            string accountWebsite = account.Website ?? string.Empty;
            return !string.IsNullOrEmpty(accountWebsite) && !string.IsNullOrEmpty(row.Website) && row.Website == accountWebsite;
        }
    }
}