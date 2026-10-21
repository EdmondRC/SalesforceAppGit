using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text;

namespace SalesforceApp.Services
{
    /// <summary>
    /// Reads headers and data rows from a CSV file, mirroring the API of
    /// <see cref="ExcelImportService"/> so both file types can be processed
    /// identically by the rest of the matching pipeline.
    /// </summary>
    public class CsvImportService
    {
        public List<string> GetHeaders(Stream stream, int headerRowNumber)
        {
            stream.Position = 0;
            var rows = ReadAllRows(stream);

            if (rows.Count < headerRowNumber)
            {
                return new List<string>();
            }

            return rows[headerRowNumber - 1]
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .ToList();
        }

        public List<Dictionary<string, string>> GetDataRows(Stream stream, int headerRowNumber)
        {
            if (stream == null || stream.Length == 0)
            {
                throw new ArgumentException("The provided stream is null or empty.");
            }

            try
            {
                stream.Position = 0;
                var rows = ReadAllRows(stream);

                if (rows.Count <= headerRowNumber)
                {
                    throw new InvalidOperationException("The CSV file does not contain any data rows after the header row.");
                }

                var headers = rows[headerRowNumber - 1];
                var dataRows = new List<Dictionary<string, string>>();

                for (int rowIndex = headerRowNumber; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var rowData = new Dictionary<string, string>();

                    for (int i = 0; i < headers.Count; i++)
                    {
                        rowData[headers[i]] = i < row.Count ? row[i] : string.Empty;
                    }

                    dataRows.Add(rowData);
                }

                return dataRows;
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
            {
                throw new InvalidOperationException("An error occurred while processing the CSV file.", ex);
            }
        }

        /// <summary>
        /// Parses the full CSV stream into rows of string fields, honoring RFC 4180-style
        /// quoting rules (quoted fields, embedded commas, embedded newlines, and escaped
        /// quotes represented as "").
        /// </summary>
        private static List<List<string>> ReadAllRows(Stream stream)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;
            bool fieldHasContent = false;

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            int current;
            int next;
            while ((current = reader.Read()) != -1)
            {
                char c = (char)current;

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        next = reader.Peek();
                        if (next == '"')
                        {
                            // Escaped quote within a quoted field.
                            currentField.Append('"');
                            reader.Read();
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        fieldHasContent = true;
                        break;

                    case ',':
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        fieldHasContent = true;
                        break;

                    case '\r':
                        // Ignore; line break is handled on '\n' (or standalone '\r' below).
                        next = reader.Peek();
                        if (next != '\n')
                        {
                            EndRow(rows, currentRow, currentField, ref fieldHasContent);
                        }
                        break;

                    case '\n':
                        EndRow(rows, currentRow, currentField, ref fieldHasContent);
                        break;

                    default:
                        currentField.Append(c);
                        fieldHasContent = true;
                        break;
                }
            }

            // Flush any trailing field/row that wasn't terminated by a newline.
            if (fieldHasContent || currentField.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentField.ToString());
                rows.Add(currentRow);
            }

            // Drop fully blank trailing lines (e.g. trailing newline at end of file).
            while (rows.Count > 0 && rows[^1].Count == 1 && string.IsNullOrWhiteSpace(rows[^1][0]))
            {
                rows.RemoveAt(rows.Count - 1);
            }

            return rows;
        }

        private static void EndRow(List<List<string>> rows, List<string> currentRow, StringBuilder currentField, ref bool fieldHasContent)
        {
            currentRow.Add(currentField.ToString());
            rows.Add(new List<string>(currentRow));
            currentRow.Clear();
            currentField.Clear();
            fieldHasContent = false;
        }
    }
}