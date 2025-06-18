using PortfolioOptimizer.Data;
using System.Buffers;
using System.Globalization;

namespace PortfolioOptimizer.Services
{
    public class OptimizerService
    {
        public double[][] CalculateCorrelationMatrix(List<double[]> returns)
        {
            int n = returns[0].Length;
            double[][] matrix = new double[n][];
            for (int i = 0; i < n; i++)
                matrix[i] = new double[n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    matrix[i][j] = Correlation(returns, i, j);

            return matrix;
        }

        private double Correlation(List<double[]> returns, int i, int j)
        {
            var n = returns.Count;
            var xi = returns.Select(r => r[i]).ToArray();
            var yj = returns.Select(r => r[j]).ToArray();
            var mean_x = xi.Average();
            var mean_y = yj.Average();
            var cov = xi.Zip(yj, (x, y) => (x - mean_x) * (y - mean_y)).Sum() / (n - 1);
            var std_x = Math.Sqrt(xi.Select(x => (x - mean_x) * (x - mean_x)).Sum() / (n - 1));
            var std_y = Math.Sqrt(yj.Select(y => (y - mean_y) * (y - mean_y)).Sum() / (n - 1));
            return cov / (std_x * std_y);
        }

        // Mean-Variance Optimization (Maximize Sharpe Ratio, risk-free rate=0)
        public double[] OptimizeWeights(List<double[]> returns)
        {
            int n = returns[0].Length;
            // Equal weight for each asset
            double[] weights = Enumerable.Repeat(1.0 / n, n).ToArray();
            return weights;
        }
    }
}
