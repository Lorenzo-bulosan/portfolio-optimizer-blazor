using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PortfolioOptimizer.Data
{
    public class PortfolioMetrics
    {
        [Key]
        public int Id { get; set; }
        public DateTime StartDate { get; set; } = new();
        public DateTime EndDate { get; set; } = new();
        public decimal AnnualReturn { get; set; }
        public decimal AnnualVolatility { get; set; }
        public decimal AnnualSharpeRatio { get; set; }

        // Navigation property: Each portfolio metrics belongs to one portfolio
        public Portfolio Portfolio { get; set; } = new();
    }
}
