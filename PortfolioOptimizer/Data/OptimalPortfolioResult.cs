namespace PortfolioOptimizer.Data
{
    public class OptimalPortfolioResult
    {
        public Dictionary<string, StockDetails> Stocks { get; set; } = new();
        public PortfolioMetrics Metrics { get; set; } = new();
    }
}
