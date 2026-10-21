using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SalesforceApp.Enums;

namespace SalesforceApp.Mapping
{
    /// <summary>
    /// A named, reusable set of column mappings (imported header -> Salesforce field)
    /// that can be saved and re-applied to future uploads.
    /// </summary>
    public class MappingConfiguration
    {
        public string Name { get; set; } = string.Empty;

        public int HeaderRowNumber { get; set; } = 1;

        /// <summary>
        /// Maps each Salesforce field to the header name the user selected for it.
        /// Fields with no mapping are omitted.
        /// </summary>
        public Dictionary<SalesforceField, string> FieldMappings { get; set; } = new();
    }
}