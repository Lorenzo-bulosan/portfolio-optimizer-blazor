using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PortfolioOptimizer.Data
{
    public class Portfolio
    {
        [Key]
        public int Id { get; set; }
        public List<DateTime> StartDate { get; set; } = new();
        public List<DateTime> EndDate { get; set; } = new();

        // Navigation property: Each portfolio can have multiple stocks
        public List<Stock> Stocks { get; set; } = new();
    }
}
