# PortfolioOptimizer

The goal of this project is to create a portfolio optimizer that can help investors maximize their returns while minimizing risk.
The optimizer will use historical stock data to calculate the optimal allocation of assets in a portfolio.

## Current Features

* Calculates key metrics for stocks from user file uploads
* Calculates the optimal allocation of each stock that achieves maximum Sharpe Ratio for the portfolio
* Accepts Single CSV file uploads with multiple stocks or multiple CSV files with single stock data as long as it has the same date range

## How to Use

* Build and Run the blazor app in Visual Studio
* Navigate to the Portfolio Tab
* Upload 1 or more CSV files with at least 2 different stock data between all the files and has the same date range (You can use your own or a test file from the folder you can find in the root called 'TestData')
	- Can accept either 1 single CSV but with multiple stock data
	- Or multiple CSV files with just one stock data each
	- Or a combination of both i.e one CSV with multiple stocks and one or more CSV with a single stock
* The app will process the data and display key metrics for each stock in the portfolio
* Click the button at the bottom of the page to get the optimal weights for each stock inside the portfolio
