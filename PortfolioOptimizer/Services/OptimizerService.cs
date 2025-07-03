using PortfolioOptimizer.Data;
using PortfolioOptimizer.Services.Interfaces;
using Accord.Math.Optimization;
using Accord.Math;

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

        public OptimalPortfolioResult CalculateOptimalPortfolio(List<Stock> stocks)
        {
            _logger.LogTrace("Calculating optimal portfolio for {StockCount} stocks", stocks.Count);

            if (stocks.Count < 2)
                throw new InvalidOperationException("At least 2 stocks are required for portfolio optimization.");

            // Calculate returns matrix
            var returnsMatrix = CalculateReturnsMatrix(stocks);
            var expectedReturns = CalculateExpectedReturns(returnsMatrix);
            var covarianceMatrix = CalculateCovarianceMatrix(returnsMatrix);

            // Find optimal weights using mean-variance optimization
            var optimalWeights = FindOptimalWeights(expectedReturns, covarianceMatrix);

            // Calculate portfolio metrics
            var portfolioReturn = CalculatePortfolioReturn(expectedReturns, optimalWeights);
            var portfolioVolatility = CalculatePortfolioVolatility(covarianceMatrix, optimalWeights);
            var sharpeRatio = portfolioVolatility > 0 ? portfolioReturn / portfolioVolatility : 0;

            var allocations = new Dictionary<string, decimal>();
            for (int i = 0; i < stocks.Count; i++)
            {
                allocations[stocks[i].Name] = (decimal)optimalWeights[i];
            }

            var portfolioMetrics = new PortfolioMetrics
            {
                AnnualReturn = (decimal)portfolioReturn,
                AnnualVolatility = (decimal)portfolioVolatility,
                AnnualSharpeRatio = (decimal)sharpeRatio
            };

            _logger.LogTrace("Successfully calculated optimal portfolio");
            return new OptimalPortfolioResult
            {
                Allocations = allocations,
                Metrics = portfolioMetrics
            };
        }

        private double[,] CalculateReturnsMatrix(List<Stock> stocks)
        {
            // Find common date range
            var allDates = stocks.SelectMany(s => s.Prices.Select(p => p.Date)).Distinct().OrderBy(d => d).ToList();
            var commonDates = allDates.Where(date => stocks.All(stock => stock.Prices.Any(p => p.Date == date))).ToList();

            if (commonDates.Count < 2)
                throw new InvalidOperationException("Insufficient overlapping price data between stocks.");

            var returnsMatrix = new double[commonDates.Count - 1, stocks.Count];

            for (int stockIndex = 0; stockIndex < stocks.Count; stockIndex++)
            {
                var stockPrices = stocks[stockIndex].Prices.Where(p => commonDates.Contains(p.Date))
                                                          .OrderBy(p => p.Date).ToList();

                for (int dateIndex = 1; dateIndex < commonDates.Count; dateIndex++)
                {
                    var prevPrice = stockPrices[dateIndex - 1].Close;
                    var currentPrice = stockPrices[dateIndex].Close;

                    if (prevPrice == 0) continue;

                    var dailyReturn = (double)((currentPrice - prevPrice) / prevPrice);
                    returnsMatrix[dateIndex - 1, stockIndex] = dailyReturn;
                }
            }

            return returnsMatrix;
        }

        private double[] CalculateExpectedReturns(double[,] returnsMatrix)
        {
            int numPeriods = returnsMatrix.GetLength(0);
            int numStocks = returnsMatrix.GetLength(1);
            var expectedReturns = new double[numStocks];

            for (int stock = 0; stock < numStocks; stock++)
            {
                double sum = 0;
                for (int period = 0; period < numPeriods; period++)
                {
                    sum += returnsMatrix[period, stock];
                }
                expectedReturns[stock] = (sum / numPeriods) * 252; // Annualized
            }

            return expectedReturns;
        }

        private double[,] CalculateCovarianceMatrix(double[,] returnsMatrix)
        {
            int numPeriods = returnsMatrix.GetLength(0);
            int numStocks = returnsMatrix.GetLength(1);
            var covMatrix = new double[numStocks, numStocks];

            // Calculate means
            var means = new double[numStocks];
            for (int stock = 0; stock < numStocks; stock++)
            {
                double sum = 0;
                for (int period = 0; period < numPeriods; period++)
                {
                    sum += returnsMatrix[period, stock];
                }
                means[stock] = sum / numPeriods;
            }

            // Calculate covariance matrix
            for (int i = 0; i < numStocks; i++)
            {
                for (int j = 0; j < numStocks; j++)
                {
                    double sum = 0;
                    for (int period = 0; period < numPeriods; period++)
                    {
                        sum += (returnsMatrix[period, i] - means[i]) * (returnsMatrix[period, j] - means[j]);
                    }
                    covMatrix[i, j] = (sum / (numPeriods - 1)) * 252; // Annualized
                }
            }

            return covMatrix;
        }

        private double[] FindOptimalWeights(double[] expectedReturns, double[,] covarianceMatrix, double riskFreeRate = 0.0)
        {
            int n = expectedReturns.Length;
            double[] excessReturns = expectedReturns.Subtract(riskFreeRate);

            // Objective: maximize Sharpe ratio = (w·excessReturns) / sqrt(wᵀ Σ w)
            // Since Nelder-Mead minimizes, minimize negative Sharpe ratio + penalties for constraints

            Func<double[], double> objective = weights =>
            {
                // Penalize weights outside [0,1]
                double penalty = 0.0;
                foreach (var w in weights)
                {
                    if (w < 0) penalty += 1000 * (-w);
                    if (w > 1) penalty += 1000 * (w - 1);
                }

                // Penalize sum(weights) != 1
                double sumWeights = weights.Sum();
                penalty += 1000 * Math.Abs(sumWeights - 1.0);

                double portReturn = weights.Dot(excessReturns);
                double variance = weights.Dot(covarianceMatrix).Dot(weights);
                double stdDev = Math.Sqrt(variance);

                if (stdDev == 0) return 1e10 + penalty;

                double sharpe = portReturn / stdDev;

                // Minimize negative Sharpe + penalty
                return -sharpe + penalty;
            };

            // Initial guess: equal weights
            double[] initialGuess = Vector.Create(n, 1.0 / n);

            // Setup Nelder-Mead solver
            var nm = new NelderMead(numberOfVariables: n, function: objective);

            bool success = nm.Minimize(initialGuess);

            if (!success)
                throw new Exception("Optimization failed.");

            // Return optimized weights (may slightly violate constraints)
            return nm.Solution;
        }

        private double CalculatePortfolioReturn(double[] expectedReturns, double[] weights)
        {
            double portfolioReturn = 0;
            for (int i = 0; i < expectedReturns.Length; i++)
            {
                portfolioReturn += expectedReturns[i] * weights[i];
            }
            return portfolioReturn;
        }

        private double CalculatePortfolioVolatility(double[,] covarianceMatrix, double[] weights)
        {
            int numStocks = weights.Length;
            double variance = 0;

            for (int i = 0; i < numStocks; i++)
            {
                for (int j = 0; j < numStocks; j++)
                {
                    variance += weights[i] * weights[j] * covarianceMatrix[i, j];
                }
            }

            return Math.Sqrt(variance);
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
