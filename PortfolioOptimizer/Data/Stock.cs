using System.ComponentModel.DataAnnotations;

namespace PortfolioOptimizer.Data
{
    public class Stock
    {
        [Key]
        public int Id { get; set; }        
        public string Name { get; set; } = string.Empty;

        // Navigation property: Each stock can have multiple prices
        public List<Price> Prices { get; set; } = new();
    }
}
