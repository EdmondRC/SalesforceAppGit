using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Enums
{
    public enum MatchingRule
    {
        UniqueId,
        NameFullAddress,
        NamePartialAddress,
        NamePhone,
        NameWebsite,
        FullAddressPhone,
        FullAddressEmail,
        PartialAddressPhone,
        PartialAddressWebsite,
        None
    }
}
