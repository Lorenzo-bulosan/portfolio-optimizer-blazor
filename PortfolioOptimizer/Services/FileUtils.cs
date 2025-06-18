using PortfolioOptimizer.Data;
using System.Globalization;

namespace PortfolioOptimizer.Services
{
    public static class FileUtils
    {
        public static Stock ParseCsvSingleStock(string content)
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

            var stock = new Stock
            {
                Prices = new List<Price>()
            };

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

                // Populate Price object
                var price = new Price
                {
                    Date = date,
                    Open = open,
                    Close = close,
                    Stock = stock // optional for initial population
                };

                // Populate Stock object
                if (string.IsNullOrWhiteSpace(stock.Name))
                {
                    stock.Name = fields[nameIdx];
                }
                stock.Prices.Add(price);
            }

            return stock;
        }
    }
}
