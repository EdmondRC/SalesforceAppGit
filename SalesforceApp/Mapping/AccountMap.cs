using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SalesforceApp.Enums;

namespace SalesforceApp.Mapping
{
    public class MappingRow
    {
        public SalesforceField SalesforceField { get; set; }
        public string? SelectedHeader { get; set; }
    }
}
