// Tests/FileUtilsTests.cs
using NUnit.Framework;
using PortfolioOptimizer.Data;
using PortfolioOptimizer.Services;
using System.Diagnostics;


namespace Tests
{
    public class FileUtilsTests
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void ShouldWork()
        {
            Assert.IsTrue(1 + 1 == 2);
            Assert.IsFalse(1 + 1 == 3);
        }


        [Test]
        public void ValidateDateRanges_WithLessThanTwoStocks_ReturnsTrue()
        {
            // Arrange
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", new[] { "2023-01-01", "2023-01-02" })
            };


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsTrue(result);
        }


        [Test]
        public void ValidateDateRanges_WithEmptyList_ReturnsTrue()
        {
            // Arrange
            var stocks = new List<Stock>();


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsTrue(result);
        }


        [Test]
        public void ValidateDateRanges_WithSameDatesAndCounts_ReturnsTrue()
        {
            // Arrange
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", new[] { "2023-01-01", "2023-01-02", "2023-01-03" }),
                CreateStock("GOOGL", new[] { "2023-01-01", "2023-01-02", "2023-01-03" }),
                CreateStock("MSFT", new[] { "2023-01-01", "2023-01-02", "2023-01-03" })
            };


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsTrue(result);
        }


        [Test]
        public void ValidateDateRanges_WithDifferentPriceCounts_ReturnsFalse()
        {
            // Arrange
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", new[] { "2023-01-01", "2023-01-02", "2023-01-03" }),
                CreateStock("GOOGL", new[] { "2023-01-01", "2023-01-02" }) // Missing one date
            };


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void ValidateDateRanges_WithSameCountButDifferentDates_ReturnsFalse()
        {
            // Arrange
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", new[] { "2023-01-01", "2023-01-02", "2023-01-03" }),
                CreateStock("GOOGL", new[] { "2023-01-01", "2023-01-02", "2023-01-04" }) // Different last date
            };


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void ValidateDateRanges_WithSameDatesInDifferentOrder_ReturnsTrue()
        {
            // Arrange
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", new[] { "2023-01-01", "2023-01-03", "2023-01-02" }), // Out of order
                CreateStock("GOOGL", new[] { "2023-01-02", "2023-01-01", "2023-01-03" }) // Different order
            };


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsTrue(result);
        }


        [Test]
        public void ValidateDateRanges_WithStockHavingNoPrices_ReturnsFalse()
        {
            // Arrange
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", new string[0]), // No prices
                CreateStock("GOOGL", new[] { "2023-01-01", "2023-01-02" })
            };


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void ValidateDateRanges_WithFirstStockHavingNoPrices_ReturnsFalse()
        {
            // Arrange
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", new string[0]), // No prices
                CreateStock("GOOGL", new[] { "2023-01-01", "2023-01-02" })
            };


            // Act
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;


            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void ValidateDateRanges_WithLargeDataSet_PerformsEfficiently()
        {
            // Arrange - Create stocks with 1000 price points each
            var dates = GenerateDateRange("2020-01-01", 1000);
            var stocks = new List<Stock>
            {
                CreateStock("AAPL", dates),
                CreateStock("GOOGL", dates),
                CreateStock("MSFT", dates),
                CreateStock("TSLA", dates),
                CreateStock("AMZN", dates)
            };


            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = FileUtils.ValidateDateRanges(stocks).IsValid;
            stopwatch.Stop();


            // Assert
            Assert.IsTrue(result);
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Validation should complete within 1 second for large datasets");
        }


        private Stock CreateStock(string name, string[] dateStrings)
        {
            var stock = new Stock
            {
                Name = name,
                Prices = new List<Price>()
            };


            foreach (var dateString in dateStrings)
            {
                stock.Prices.Add(new Price
                {
                    Date = DateTime.Parse(dateString),
                    Open = 100m,
                    Close = 105m,
                    Stock = stock
                });
            }


            return stock;
        }


        private string[] GenerateDateRange(string startDate, int count)
        {
            var start = DateTime.Parse(startDate);
            var dates = new string[count];

            for (int i = 0; i < count; i++)
            {
                dates[i] = start.AddDays(i).ToString("yyyy-MM-dd");
            }

            return dates;
        }
    }
}