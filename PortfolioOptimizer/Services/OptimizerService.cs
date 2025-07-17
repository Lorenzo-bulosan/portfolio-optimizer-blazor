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
            // Create default constraints (0% to 100% for all stocks)
            var defaultConstraints = stocks.ToDictionary(
                s => s.Name,
                s => new WeightConstraint { MinWeight = 0.0m, MaxWeight = 100.0m }
            );
            return CalculateOptimalPortfolio(stocks, defaultConstraints);
        }

        public OptimalPortfolioResult CalculateOptimalPortfolio(List<Stock> stocks, Dictionary<string, WeightConstraint> weightConstraints)
        {
            _logger.LogTrace("Calculating optimal portfolio for {StockCount} stocks with weight constraints", stocks.Count);

            if (stocks.Count < 2)
                throw new InvalidOperationException("At least 2 stocks are required for portfolio optimization.");

            // Calculate returns matrix
            var returnsMatrix = CalculateReturnsMatrix(stocks);
            var expectedReturns = CalculateExpectedReturns(returnsMatrix);
            var covarianceMatrix = CalculateCovarianceMatrix(returnsMatrix);

            // Find optimal weights using mean-variance optimization with constraints
            var optimalWeights = FindOptimalWeights(expectedReturns, covarianceMatrix, stocks, weightConstraints);

            // Calculate portfolio metrics
            var portfolioReturn = CalculatePortfolioReturn(expectedReturns, optimalWeights);
            var portfolioVolatility = CalculatePortfolioVolatility(covarianceMatrix, optimalWeights);
            var sharpeRatio = portfolioVolatility > 0 ? portfolioReturn / portfolioVolatility : 0;

            var stockDetails = new Dictionary<string, StockDetails>();
            for (int i = 0; i < stocks.Count; i++)
            {
                stockDetails.Add(stocks[i].Name, new StockDetails
                {
                    Stock = stocks[i],
                    OptimalWeight = (decimal)optimalWeights[i]
                });
            }

            var portfolioMetrics = new PortfolioMetrics
            {
                AnnualReturn = (decimal)portfolioReturn,
                AnnualVolatility = (decimal)portfolioVolatility,
                AnnualSharpeRatio = (decimal)sharpeRatio
            };

            _logger.LogTrace("Successfully calculated optimal portfolio with constraints");
            return new OptimalPortfolioResult
            {
                Stocks = stockDetails,
                Metrics = portfolioMetrics
            };
        }

        public OptimalPortfolioResult CalculatePortfolioMetrics(List<StockDetails> stocks)
        {
            _logger.LogTrace("Calculating portfolio metrics for {StockCount} stocks with user weights", stocks.Count);

            if (stocks.Count < 2)
                throw new InvalidOperationException("At least 2 stocks are required for portfolio optimization.");

            // Calculate returns matrix
            var returnsMatrix = CalculateReturnsMatrix(stocks.Select(s => s.Stock).ToList());
            var expectedReturns = CalculateExpectedReturns(returnsMatrix);
            var covarianceMatrix = CalculateCovarianceMatrix(returnsMatrix);

            var weights = stocks.Select(s => (double)s.Weight).ToArray();

            // Calculate portfolio metrics
            var portfolioReturn = CalculatePortfolioReturn(expectedReturns, weights);
            var portfolioVolatility = CalculatePortfolioVolatility(covarianceMatrix, weights);
            var sharpeRatio = portfolioVolatility > 0 ? portfolioReturn / portfolioVolatility : 0;

            var stockDetails = new Dictionary<string, StockDetails>();
            for (int i = 0; i < stocks.Count; i++)
            {
                stockDetails.Add(stocks[i].Stock.Name, new StockDetails
                {
                    Stock = stocks[i].Stock,
                    OptimalWeight = (decimal)weights[i]
                });
            }

            var portfolioMetrics = new PortfolioMetrics
            {
                AnnualReturn = (decimal)portfolioReturn,
                AnnualVolatility = (decimal)portfolioVolatility,
                AnnualSharpeRatio = (decimal)sharpeRatio
            };

            _logger.LogTrace("Successfully calculated portfolio with user weights");
            return new OptimalPortfolioResult
            {
                Stocks = stockDetails,
                Metrics = portfolioMetrics
            };
        }

        public List<ChartDataPoint> CalculatePortfolioHistoricalReturns(
            List<Stock> stocks,
            Dictionary<string, decimal> allocations)
        {
            return CalculatePortfolioHistoricalReturns(stocks, allocations, null);
        }

        public List<ChartDataPoint> CalculatePortfolioHistoricalReturns(
            List<Stock> stocks,
            Dictionary<string, decimal> allocations,
            decimal? initialInvestment)
        {
            _logger.LogTrace("Calculating portfolio historical returns");

            // Find common date range
            var allDates = stocks.SelectMany(s => s.Prices.Select(p => p.Date)).Distinct().OrderBy(d => d).ToList();
            var commonDates = allDates.Where(date => stocks.All(stock => stock.Prices.Any(p => p.Date == date))).ToList();

            if (commonDates.Count < 2)
                throw new InvalidOperationException("Insufficient overlapping price data between stocks.");

            var portfolioReturns = new List<ChartDataPoint>();
            decimal cumulativeReturn = 1.0m;

            // Add initial point
            if (initialInvestment.HasValue)
            {
                portfolioReturns.Add(new ChartDataPoint
                {
                    Date = commonDates[0],
                    Value = initialInvestment.Value
                });
            }

            for (int i = 1; i < commonDates.Count; i++)
            {
                decimal portfolioDailyReturn = 0m;

                foreach (var stock in stocks)
                {
                    if (!allocations.ContainsKey(stock.Name)) continue;

                    var prevPrice = stock.Prices.First(p => p.Date == commonDates[i - 1]).Close;
                    var currentPrice = stock.Prices.First(p => p.Date == commonDates[i]).Close;

                    if (prevPrice == 0) continue;

                    var stockReturn = (currentPrice - prevPrice) / prevPrice;
                    portfolioDailyReturn += allocations[stock.Name] * stockReturn;
                }

                cumulativeReturn *= (1 + portfolioDailyReturn);
                
                var returnValue = initialInvestment.HasValue 
                    ? initialInvestment.Value * cumulativeReturn  // Dollar value
                    : cumulativeReturn - 1m;                     // Percentage return

                portfolioReturns.Add(new ChartDataPoint
                {
                    Date = commonDates[i],
                    Value = returnValue
                });
            }

            return portfolioReturns;
        }

        public Dictionary<string, List<ChartDataPoint>> CalculateStockHistoricalReturns(List<Stock> stocks)
        {
            return CalculateStockHistoricalReturns(stocks, null);
        }

        public Dictionary<string, List<ChartDataPoint>> CalculateStockHistoricalReturns(List<Stock> stocks, decimal? initialInvestment)
        {
            _logger.LogTrace("Calculating individual stock historical returns");

            var result = new Dictionary<string, List<ChartDataPoint>>();

            foreach (var stock in stocks)
            {
                var stockReturns = new List<ChartDataPoint>();
                var prices = stock.Prices.OrderBy(p => p.Date).ToList();

                if (prices.Count < 2) continue;

                decimal cumulativeReturn = 1.0m;
                
                // Add initial point
                var initialValue = initialInvestment ?? 0m;
                stockReturns.Add(new ChartDataPoint
                {
                    Date = prices[0].Date,
                    Value = initialValue
                });

                for (int i = 1; i < prices.Count; i++)
                {
                    if (prices[i - 1].Close == 0) continue;

                    var dailyReturn = (prices[i].Close - prices[i - 1].Close) / prices[i - 1].Close;
                    cumulativeReturn *= (1 + dailyReturn);

                    var returnValue = initialInvestment.HasValue 
                        ? initialInvestment.Value * cumulativeReturn  // Dollar value
                        : cumulativeReturn - 1m;                     // Percentage return

                    stockReturns.Add(new ChartDataPoint
                    {
                        Date = prices[i].Date,
                        Value = returnValue
                    });
                }

                result[stock.Name] = stockReturns;
            }

            return result;
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

        private double[] FindOptimalWeights(
            double[] expectedReturns,
            double[,] covarianceMatrix,
            List<Stock> stocks,
            Dictionary<string, WeightConstraint> weightConstraints,
            double riskFreeRate = 0.0)
        {
            int n = expectedReturns.Length;
            double[] excessReturns = expectedReturns.Subtract(riskFreeRate);

            // Extract min/max weight constraints
            double[] minWeights = new double[n];
            double[] maxWeights = new double[n];

            for (int i = 0; i < n; i++)
            {
                var name = stocks[i].Name;
                if (weightConstraints.TryGetValue(name, out var constraint))
                {
                    minWeights[i] = (double)constraint.MinWeight;
                    maxWeights[i] = (double)constraint.MaxWeight;
                }
                else
                {
                    minWeights[i] = 0.0;
                    maxWeights[i] = 1.0;
                }
            }

            // Objective: maximize Sharpe ratio -> minimize -Sharpe + constraint penalties
            Func<double[], double> objective = weights =>
            {
                double penalty = 0.0;

                double sumWeights = 0.0;
                for (int i = 0; i < n; i++)
                {
                    double w = weights[i];
                    sumWeights += w;

                    if (w < minWeights[i])
                        penalty += 10000 * Math.Pow(minWeights[i] - w, 2);
                    if (w > maxWeights[i])
                        penalty += 10000 * Math.Pow(w - maxWeights[i], 2);
                }

                // Enforce weights summing to 1
                penalty += 10000 * Math.Pow(sumWeights - 1.0, 2);

                double portfolioReturn = weights.Dot(excessReturns);
                double variance = weights.Dot(covarianceMatrix).Dot(weights);
                double stdDev = Math.Sqrt(Math.Max(variance, 1e-10)); // Guard against zero

                double sharpeRatio = portfolioReturn / stdDev;

                return -sharpeRatio + penalty;
            };

            // Smarter initial guess: normalized min-weight baseline + proportionally distributed slack
            double[] initialGuess = new double[n];
            double totalMin = minWeights.Sum();
            double slack = 1.0 - totalMin;

            if (slack < 0)
            {
                // Normalize minWeights if they exceed 1.0
                for (int i = 0; i < n; i++)
                    initialGuess[i] = minWeights[i] / totalMin;
            }
            else
            {
                double totalSlackRoom = 0.0;
                double[] room = new double[n];
                for (int i = 0; i < n; i++)
                {
                    room[i] = Math.Max(0, maxWeights[i] - minWeights[i]);
                    totalSlackRoom += room[i];
                }

                for (int i = 0; i < n; i++)
                {
                    double weight = minWeights[i];
                    if (totalSlackRoom > 0)
                        weight += slack * (room[i] / totalSlackRoom);
                    initialGuess[i] = weight;
                }
            }

            // Run optimization
            var optimizer = new NelderMead(n, objective);
            bool success = optimizer.Minimize(initialGuess);

            if (!success)
                throw new Exception("Optimization failed to converge.");

            // Normalize to exactly sum to 1
            var solution = optimizer.Solution;
            double total = solution.Sum();
            if (Math.Abs(total - 1.0) > 1e-6)
            {
                for (int i = 0; i < n; i++)
                    solution[i] /= total;
            }

            return solution;
        }


        // Overload for backward compatibility
        private double[] FindOptimalWeights(double[] expectedReturns, double[,] covarianceMatrix, double riskFreeRate = 0.0)
        {
            int n = expectedReturns.Length;
            var stocks = new List<Stock>();
            var constraints = new Dictionary<string, WeightConstraint>();

            // Create dummy stocks and default constraints for backward compatibility
            for (int i = 0; i < n; i++)
            {
                var dummyStock = new Stock { Name = $"Stock_{i}" };
                stocks.Add(dummyStock);
                constraints[dummyStock.Name] = new WeightConstraint { MinWeight = 0.0m, MaxWeight = 100.0m };
            }

            return FindOptimalWeights(expectedReturns, covarianceMatrix, stocks, constraints, riskFreeRate);
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
