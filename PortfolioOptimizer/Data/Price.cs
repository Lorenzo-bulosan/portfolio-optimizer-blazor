using System.ComponentModel.DataAnnotations;

namespace PortfolioOptimizer.Data
{
    public class Price
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal? Open { get; set; }
        public decimal Close { get; set; }

        // Foreign key property
        public int StockId { get; set; }

        // Navigation property: Each price belongs to one stock
        public Stock Stock { get; set; } = null!;
    }
}
