using PortfolioOptimizer.Data;

namespace PortfolioOptimizer.Services.Interfaces
{
    public interface IOptimizerService
    {
        StockMetrics CalculateStockMetrics(Stock stock);
        OptimalPortfolioResult CalculateOptimalPortfolio(List<Stock> stocks);

    }
}