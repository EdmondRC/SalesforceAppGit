using SalesforceApp.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Mapping
{
    public class GetFieldMapping
    {
        public List<MappingRow> MappingRows { get; set; } = new List<MappingRow>();
        public GetFieldMapping(List<MappingRow> mappingRows)
        {
            MappingRows = mappingRows;
        }
    }
}
