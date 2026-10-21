using SalesforceApp.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SalesforceApp.Models;

namespace SalesforceApp.Logic
{
    public static class RemappingLogic
    {
        public static void RemappedImportedRows(List<ImportedRow> importedRows, List<MappingRow> mappingRows)
        {

            if (importedRows == null || mappingRows == null)
            {
                throw new ArgumentNullException("Imported rows and mapping rows cannot be null.");
            }

            // Build a lookup of original header -> Salesforce field name, for headers that were mapped.
            var headerToSalesforceField = mappingRows
                .Where(m => !string.IsNullOrEmpty(m.SelectedHeader))
                .ToDictionary(m => m.SelectedHeader!, m => m.SalesforceField.ToString());

            foreach (var importedRow in importedRows)
            {
                var newRowData = new Dictionary<string, string>();
                foreach (var kvp in importedRow.RowData)
                {
                    // If this header was mapped to a Salesforce field, rename the key to the
                    // Salesforce field name. Otherwise, keep the original header and value as-is.
                    string key = headerToSalesforceField.TryGetValue(kvp.Key, out var salesforceField)
                        ? salesforceField
                        : kvp.Key;

                    newRowData[key] = kvp.Value;
                }
                importedRow.RowData = newRowData;
            }
        }
    }
}
