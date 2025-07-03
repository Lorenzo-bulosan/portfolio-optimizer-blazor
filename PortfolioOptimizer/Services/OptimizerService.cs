using PortfolioOptimizer.Data;
using PortfolioOptimizer.Services.Interfaces;
using System.Buffers;

namespace PortfolioOptimizer.Services
{
    public class OptimizerService : IOptimizerService
    {
        private readonly ILogger<OptimizerService> _logger;
        public OptimizerService(ILogger<OptimizerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Calculate annualized return and Sharpe Ratio
        public StockMetrics CalculateStockMetrics(Stock stock)
        {
            _logger.LogTrace("Calculating metrics for stock: {StockName}", stock.Name);

            // Sort by date if possible
            var prices = stock!.Prices.OrderBy(p => p.Date).ToList();
            if (prices.Count < 2)
            {
                throw new InvalidOperationException("Not enough price data to calculate metrics.");
            }

            // Calculate daily returns
            var returns = new List<double>();
            for (int i = 1; i < prices.Count; i++)
            {
                if (prices[i - 1].Close == 0) continue;
                var r = (prices[i].Close - prices[i - 1].Close) / prices[i - 1].Close;
                returns.Add((double)r);
            }

            // Assume 252 trading days/year
            var avgReturn = returns.Average();
            var annualizedVolatility = StdDev(returns) * Math.Sqrt(252); // Annualized volatility
            var annualizedReturn = Math.Pow(1 + (double)avgReturn, 252) - 1; // Annualized return
            var sharpeRatio = annualizedVolatility > 0 ? annualizedReturn / annualizedVolatility : 0;

            // Update portfolio metrics
            var stockMetrics = new StockMetrics
            {
                AnnualReturn = (decimal)annualizedReturn,
                AnnualVolatility = (decimal)annualizedVolatility,
                AnnualSharpeRatio = (decimal)sharpeRatio
            };

            _logger.LogTrace("Successfully calculated metrics for stock: {StockName}", stock.Name);
            return stockMetrics;
        }

        private double StdDev(List<double> returns)
        {
            if (returns.Count < 2) return 0;
            var avg = returns.Average();
            var sumSq = returns.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumSq / (returns.Count - 1));
        }
    }
}
