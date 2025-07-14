// PortfolioOptimizer/Services/FileUtils.cs
using PortfolioOptimizer.Data;
using System.Globalization;

namespace PortfolioOptimizer.Services
{
    public static class FileUtils
    {

        public static async Task<List<Stock>> ParseCsvFromStreamAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var buffer = new char[81920]; // 80KB char buffer
            var contentBuilder = new System.Text.StringBuilder();

            int charsRead;
            while ((charsRead = await reader.ReadBlockAsync(buffer, 0, buffer.Length)) > 0)
            {
                contentBuilder.Append(buffer, 0, charsRead);
            }

            var content = contentBuilder.ToString();
            return ParseCsv(content);
        }

        public static List<Stock> ParseCsv(string content)
        {
            using var reader = new StringReader(content);

            // Headers
            string? headerLine = reader.ReadLine();
            if (headerLine == null)
                throw new InvalidDataException("CSV file is empty.");

            // Find column indexes
            var headers = headerLine.Split(',');
            int dateIdx = Array.FindIndex(headers, h => string.Equals(h, "date", StringComparison.OrdinalIgnoreCase));
            int openIdx = Array.FindIndex(headers, h => string.Equals(h, "open", StringComparison.OrdinalIgnoreCase));
            int closeIdx = Array.FindIndex(headers, h => string.Equals(h, "close", StringComparison.OrdinalIgnoreCase));
            int nameIdx = Array.FindIndex(headers, h => string.Equals(h, "name", StringComparison.OrdinalIgnoreCase));

            // Required columns
            if (dateIdx == -1 || closeIdx == -1 || nameIdx == -1)
                throw new InvalidDataException("CSV file missing required columns (date, open, close, name).");

            var stocksDict = new Dictionary<string, Stock>();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] fields = line.Split(',');

                // Skip lines with missing data
                if (fields.Length < headers.Length ||
                    string.IsNullOrWhiteSpace(fields[dateIdx]) ||
                    string.IsNullOrWhiteSpace(fields[nameIdx]) ||
                    string.IsNullOrWhiteSpace(fields[closeIdx]))
                    continue;

                if (!DateTime.TryParse(fields[dateIdx], out DateTime date))
                    continue;
                if (!decimal.TryParse(fields[openIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal open))
                    continue;
                if (!decimal.TryParse(fields[closeIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal close))
                    continue;

                string stockName = fields[nameIdx];

                // Get or create stock
                if (!stocksDict.ContainsKey(stockName))
                {
                    stocksDict[stockName] = new Stock
                    {
                        Name = stockName,
                        Prices = new List<Price>()
                    };
                }

                var stock = stocksDict[stockName];

                // Create price object
                var price = new Price
                {
                    Date = date,
                    Open = open,
                    Close = close,
                    Stock = stock
                };

                stock.Prices.Add(price);
            }

            return stocksDict.Values.ToList();
        }

        // Keep the old method for backward compatibility, but make it use the new method
        public static Stock ParseCsvSingleStock(string content)
        {
            var stocks = ParseCsv(content);
            return stocks.FirstOrDefault() ?? new Stock();
        }

        public static (bool IsValid, List<string> FailingStocks) ValidateDateRanges(List<Stock> stocks)
        {
            if (stocks.Count < 2) return (true, new List<string>());

            // Early validation: check if all stocks have prices
            if (stocks.Any(s => !s.Prices.Any()))
            {
                return (false, stocks.Where(s => !s.Prices.Any()).Select(s => s.Name).ToList());
            }

            // Quick check: if price counts differ
            var expectedCount = stocks[0].Prices.Count;
            var failingCounts = stocks.Where(s => s.Prices.Count != expectedCount).Select(s => s.Name).ToList();
            if (failingCounts.Any()) return (false, failingCounts);

            // For performance with large datasets, use HashSet for O(1) lookups
            // Get sorted dates from first stock as reference
            var referenceDates = stocks[0].Prices
                .Select(p => p.Date)
                .OrderBy(d => d)
                .ToHashSet();

            // Validate each subsequent stock against the reference
            var failingDates = new List<string>();
            for (int i = 1; i < stocks.Count; i++)
            {
                var stockDates = stocks[i].Prices
                    .Select(p => p.Date)
                    .ToHashSet();

                // Quick count check first
                if (stockDates.Count != referenceDates.Count)
                {
                    failingDates.Add(stocks[i].Name);
                    continue;
                }

                // Check if all dates match using SetEquals (more efficient than SequenceEqual for large sets)
                if (!referenceDates.SetEquals(stockDates))
                {
                    failingDates.Add(stocks[i].Name);
                }
            }

            return (failingDates.Count == 0, failingDates);
        }
    }
}