namespace PortfolioOptimizer.Data
{
    public class OptimalPortfolioResult
    {
        public Dictionary<string, decimal> Allocations { get; set; } = new();
        public PortfolioMetrics Metrics { get; set; } = new();
    }
}
