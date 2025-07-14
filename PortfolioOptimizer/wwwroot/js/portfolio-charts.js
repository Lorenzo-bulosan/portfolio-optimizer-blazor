let pieChart = null;
let lineChart = null;

window.renderPieChart = function (canvasId, data) {
    const ctx = document.getElementById(canvasId).getContext('2d');

    if (pieChart) {
        pieChart.destroy();
    }

    pieChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: data.map(d => d.label),
            datasets: [{
                data: data.map(d => d.value),
                backgroundColor: [
                    '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0',
                    '#9966FF', '#FF9F40', '#FF6384', '#C9CBCF'
                ]
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return context.label + ': ' + context.parsed.toFixed(2) + '%';
                        }
                    }
                }
            }
        }
    });
};

window.renderLineChart = function (canvasId, data,) {
    const ctx = document.getElementById(canvasId).getContext('2d');

    if (lineChart) {
        lineChart.destroy();
    }

    const datasets = [];

    // Add portfolio data
    datasets.push({
        label: 'Original Portfolio',
        data: data.portfolio,
        borderColor: '#000000',
        backgroundColor: 'rgba(0, 0, 0, 0.1)',
        borderWidth: 3,
        fill: false
    });

    // Add individual stock data
    const colors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'];
    let colorIndex = 0;

    for (const [stockName, stockData] of Object.entries(data.stocks)) {
        datasets.push({
            label: stockName,
            data: stockData,
            borderColor: colors[colorIndex % colors.length],
            backgroundColor: colors[colorIndex % colors.length] + '20',
            borderWidth: 2,
            fill: false
        });
        colorIndex++;
    }

    lineChart = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: datasets
        },
        options: {
            responsive: true,
            scales: {
                x: {
                    type: 'time',
                    time: {
                        parser: 'yyyy-MM-dd',
                        displayFormats: {
                            day: 'MMM dd',
                            month: 'MMM yyyy'
                        }
                    },
                    title: {
                        display: true,
                        text: 'Date'
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: 'Cumulative Return (%)'
                    },
                    ticks: {
                        callback: function (value) {
                            return value.toFixed(1) + '%';
                        }
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return context.dataset.label + ': ' + context.parsed.y.toFixed(2) + '%';
                        }
                    }
                }
            }
        }
    });
};
