using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SalesforceApp.Interfaces;
using SalesforceApp.Models;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Reflection.Metadata;

namespace SalesforceApp.Repositories
{
   
    public class AccountRepository : IAccountRepository
    {
        private readonly string _connectionString;
        private string _whereClause = ""; // Initialize with an empty string
        private int _top = 0; // 0 means no limit on the number of records returned

        public AccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// This method allows you to set a custom WHERE clause for the SQL query. The provided WHERE clause will be appended to the base query when retrieving accounts from Salesforce.
        /// The User should have knowledge of the database schema and the fields available in the Account object to construct a valid WHERE clause.
        /// Example usage: SetWhereClause("Type = 'Insured' AND State = 'NC'") will filter the accounts to only those that are of type 'Insured' and located in the state of North Carolina. 
        /// </summary>
        /// <param name="whereClause"></param>
        public void SetWhereClause(string whereClause)
        {
            _whereClause = whereClause;
        }
        /// <summary>
        /// Enter an integer value to limit the number of records returned. 
        /// </summary>
        /// <param name="top"></param>
        public void SetTop(int top)
        {
            _top = top;
        }

        public List<AccountRecord> GetAccounts()
        {
            // Implement the logic to retrieve accounts from Salesforce using the connection string
            // For demonstration purposes, we'll return a dummy list of accounts
            var accounts = new List<AccountRecord>();
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            string topClause = " ";
            if (_top > 0)
            {
                topClause = $"TOP {_top}";
            }
            string whereClause = " ";
            if (!string.IsNullOrEmpty(_whereClause))
            {
                whereClause = "AND " + _whereClause; // Add AND before the where clause
            }
            const string _query =
                @$"SELECT @Top
                Id, Name, Type, Phone, BillingStreet, BillingCity, BillingState, BillingPostalCode, BillingCountry,
                    ShippingStreet, ShippingCity, ShippingState, ShippingPostalCode, ShippingCountry,
                    CS_X_Date__c, Website, EIN__c, InsureXdate_compID__c, Ministry_Partner_ID__c,
                    BMIC_Mktg_UIN__c, CS_EPIC_PO_Box__c, AESF__Lookup_Code__c, LastModifiedDate, CreatedDate
                FROM sfAccountMatchCache 
                WHERE 1=1 @Where";

            var queryWithParameters = _query.Replace("@Top", topClause).Replace("@Where", whereClause);

            using var command = new SqlCommand(queryWithParameters, connection);

            

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var account = new AccountRecord
                {
                    Id = reader["Id"].ToString(),
                    Name = reader["Name"].ToString(),
                    Type = reader["Type"].ToString(),
                    Phone = reader["Phone"].ToString(),
                    BillingStreet = reader["BillingStreet"].ToString(),
                    BillingCity = reader["BillingCity"].ToString(),
                    BillingState = reader["BillingState"].ToString(),
                    BillingPostalCode = reader["BillingPostalCode"].ToString(),
                    BillingCountry = reader["BillingCountry"].ToString(),
                    ShippingStreet = reader["ShippingStreet"].ToString(),
                    ShippingCity = reader["ShippingCity"].ToString(),
                    ShippingState = reader["ShippingState"].ToString(),
                    ShippingPostalCode = reader["ShippingPostalCode"].ToString(),
                    ShippingCountry = reader["ShippingCountry"].ToString(),
                    CS_X_Date__c = reader.IsDBNull(reader.GetOrdinal("CS_X_Date__c")) ? null : reader.GetDateTime(reader.GetOrdinal("CS_X_Date__c")),
                    Website = reader["Website"].ToString(),
                    EIN__c = reader["EIN__c"].ToString(),
                    InsureXdate_compID__c = reader["InsureXdate_compID__c"].ToString(),
                    Ministry_Partner_ID__c = reader["Ministry_Partner_ID__c"].ToString(),
                    BMIC_Mktg_UIN__c = reader["BMIC_Mktg_UIN__c"].ToString(),
                    CS_EPIC_PO_Box__c = reader["CS_EPIC_PO_Box__c"].ToString(),
                    AESF__Lookup_Code__c = reader["AESF__Lookup_Code__c"].ToString(),
                    LastModifiedDate = reader.IsDBNull(reader.GetOrdinal("LastModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastModifiedDate")),
                    CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedDate"))

                };
                accounts.Add(account);
            }
            return accounts;
        }
    }
}
