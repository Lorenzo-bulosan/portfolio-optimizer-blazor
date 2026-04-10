
# PortfolioOptimizer
The goal of this project is to create a portfolio optimizer that can help investors maximize their returns while minimizing risk.
The optimizer will use historical stock data to calculate the optimal allocation of assets in a portfolio.

### Hosted in Azure
https://portfolio-optimizer-fwczgpddf7e3ajaz.northeurope-01.azurewebsites.net/

## Current Features

* Calculates key metrics for stocks from user file uploads
* Calculates the historical return of users custom portfolio
* Calculates the optimal allocation of each stock that achieves maximum Sharpe Ratio for the portfolio
* Accepts single or multiple CSV stock data as long as there are at least 2 different stocks between all the files
* Allows users to include or exclude stocks from the portfolio calculation
* Allows user to set minimum and maximum weights for each stock for when calculating the optimal weights
* Displays a pie chart of the optimal weights for each stock in the portfolio
* Displays a graph comparing the historical return of the optimal portfolio with the individual stocks in the portfolio

## How to Use

* Build and Run the blazor app in Visual Studio
* Navigate to the Portfolio Tab
* Upload 1 or more CSV files with at least 2 different stock data between all the files (You can use your own or a test file like "ContainsMultipleStocks.csv" from the folder you can find in the root called 'TestData')
* The app will calculate some metrics from these stocks like the Sharpe ratio of each stock etc
* The app also allows the user to include or exclude these stocks via checkboxes for the calculation of an optimal portfolio
* Click the build portfolio button and a graph with historical returns should appear comparing the custom portfolio from the user and the individual stocks returs
* Click the button at the bottom of the page to calculate and get the optimal weights for each stock inside the portfolio
* The app should display an error if the user trying to calculate optimal weights from stocks data that does not have the same date range
	- When this happens users can uncheck the checkbox for the stock that has a different date range given in the warning
	- User is then abnle to Click the calculate button again
* The app should show the optimal portfolio historical returns in the line graph along with the original portfolio and invidividual stock returns
* The app should also display a pie chart with the optimal weights of each stock in the portfolio
* The app also allows the user to set minimum and maximum weights for each stock via input fields and recalculate the optimal portfolio following the constraints
