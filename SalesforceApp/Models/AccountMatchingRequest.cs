using DocumentFormat.OpenXml.Wordprocessing;
using SalesforceApp.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Models
{
    public class AccountMatchingRequest
    {
        public Stream FileStream { get; }
        public string FileName { get; set; } 
        public int HeaderRowNumber { get; }
        public List<MappingRow> MappingRows { get; }

        public AccountMatchingRequest(
            int headerRowNumber,
            string fileName,
            Stream fileStream,
            List<MappingRow> mappingRows)
        {
            HeaderRowNumber = headerRowNumber;
            FileName = fileName;
            FileStream = fileStream;
            MappingRows = mappingRows;
        }
    }
}

