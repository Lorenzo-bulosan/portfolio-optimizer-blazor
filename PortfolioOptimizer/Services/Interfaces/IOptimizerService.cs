using PortfolioOptimizer.Data;

namespace PortfolioOptimizer.Services.Interfaces
{
    public interface IOptimizerService
    {
        StockMetrics CalculateStockMetrics(Stock stock);
        OptimalPortfolioResult CalculateOptimalPortfolio(List<Stock> stocks);
        OptimalPortfolioResult CalculateOptimalPortfolio(List<Stock> stocks, Dictionary<string, WeightConstraint> weightConstraints);
        List<ChartDataPoint> CalculatePortfolioHistoricalReturns(List<Stock> stocks, Dictionary<string, decimal> allocations);
        Dictionary<string, List<ChartDataPoint>> CalculateStockHistoricalReturns(List<Stock> stocks);
    }
}