// PortfolioOptimizer/Services/FileUtils.cs
using PortfolioOptimizer.Data;
using System.Globalization;

namespace PortfolioOptimizer.Services
{
    public static class FileUtils
    {
        public static List<Stock> ParseCsv(string content)
        {
            using var reader = new StringReader(content);

            // Headers
            string? headerLine = reader.ReadLine();
            if (headerLine == null)
                throw new InvalidDataException("CSV file is empty.");

            // Find column indexes
            var headers = headerLine.Split(',');
            int dateIdx = Array.IndexOf(headers, "date");
            int openIdx = Array.IndexOf(headers, "open");
            int closeIdx = Array.IndexOf(headers, "close");
            int nameIdx = Array.IndexOf(headers, "Name");

            // Required columns
            if (dateIdx == -1 || closeIdx == -1 || nameIdx == -1)
                throw new InvalidDataException("CSV file missing required columns (date, open, close, Name).");

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

        public static bool ValidateDateRanges(List<Stock> stocks)
        {
            // Minimum 2 stocks required
            if (stocks.Count < 2) return true;

            // Check if all stocks have at least same number of prices
            var expectedCount = stocks.First().Prices.Count;
            var allSamePriceCount = stocks.All(s => s.Prices.Count == expectedCount);
            if (!allSamePriceCount) return false;

            // Check if all stocks have the same dates as you can't compare some stocks with prices on Monday and others missing that Date even if overall they have the same count of prices
            var firstStock = stocks.First();
            if (!firstStock.Prices.Any()) return false;

            var expectedDates = firstStock.Prices.Select(p => p.Date).OrderBy(d => d).ToList();

            foreach (var stock in stocks.Skip(1))
            {
                var stockDates = stock.Prices.Select(p => p.Date).OrderBy(d => d).ToList();

                if (!expectedDates.SequenceEqual(stockDates))
                {
                    return false;
                }
            }

            return true;
        }
    }
}