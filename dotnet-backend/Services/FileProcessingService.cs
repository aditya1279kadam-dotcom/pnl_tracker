using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FinanceOS.Backend.Models;

namespace FinanceOS.Backend.Services
{
    public interface IFileProcessingService
    {
        IEnumerable<T> ParseCsv<T>(Stream stream);
        IEnumerable<T> ParseExcel<T>(Stream stream);
        List<Dictionary<string, object>> ParseDynamicFile(Stream stream, string extension);
    }

    public class FileProcessingService : IFileProcessingService
    {
        public IEnumerable<T> ParseCsv<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower().Replace(" ", ""),
                HeaderValidated = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);
            return csv.GetRecords<T>().ToList();
        }

        public IEnumerable<T> ParseExcel<T>(Stream stream)
        {
            // Simple generic Excel parser would be complex, 
            // but for specific models we can use internal mapping or attributes.
            // For POC, we'll implement a dynamic dictionary parser and then map to models.
            throw new NotImplementedException("Use ParseDynamicFile for now");
        }

        public List<Dictionary<string, object>> ParseDynamicFile(Stream stream, string extension)
        {
            if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower().Replace(" ", ""),
                    HeaderValidated = null,
                    MissingFieldFound = null
                });
                return csv.GetRecords<dynamic>()
                    .Select(d => (IDictionary<string, object>)d)
                    .Select(d => d.ToDictionary(k => k.Key.ToLower().Replace(" ", ""), k => k.Value))
                    .ToList();
            }
            else
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RangeUsed().RowsUsed().ToList();
                var headerRow = rows.First();
                var headers = headerRow.Cells().Select(c => c.Value.ToString().ToLower().Replace(" ", "")).ToList();

                var result = new List<Dictionary<string, object>>();
                foreach (var row in rows.Skip(1))
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < headers.Count; i++)
                    {
                        dict[headers[i]] = row.Cell(i + 1).Value.ToString();
                    }
                    result.Add(dict);
                }
                return result;
            }
        }
    }
}
