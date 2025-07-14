using PortfolioOptimizer.Data;

public class StockDetails
{
    public Stock Stock { get; set; } = new();

    public decimal Weight { get; set; } = new();

    public WeightConstraint WeightConstraint { get; set; } = new();
}