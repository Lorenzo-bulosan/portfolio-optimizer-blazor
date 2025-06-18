using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PortfolioOptimizer.Data
{
    public class PortfolioMetrics
    {
        [Key]
        public int Id { get; set; }
        public List<DateTime> StartDate { get; set; } = new();
        public List<DateTime> EndDate { get; set; } = new();
        public decimal TotalReturn { get; set; }
        public decimal Volatility { get; set; }

        // Navigation property: Each portfolio metrics belongs to one portfolio
        public Portfolio Portfolio { get; set; } = new();
    }
}
