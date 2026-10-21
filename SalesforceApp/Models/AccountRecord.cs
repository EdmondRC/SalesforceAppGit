using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Models
{
    public class AccountRecord
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Phone { get; set; }
        public string? BillingStreet { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingState { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }
        public string? ShippingStreet { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }
        public string? EIN__c { get; set; }
        public DateTime? CS_X_Date__c { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Website { get; set; }
        public string? InsureXdate_compID__c { get; set; }
        public string? Ministry_Partner_ID__c { get; set; }
        public string? BMIC_Mktg_UIN__c { get; set; }
        public string? CS_EPIC_PO_Box__c { get; set; }
        public string? AESF__Lookup_Code__c { get; set; }
    }
}
