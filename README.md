# Crypto Terminal

Live dashboard: https://cryptodashboard-sigma.vercel.app/

## The Project

Crypto Terminal is a personal full-stack project built to work around the limitations of CoinGecko's free tier API, which only provides up to 90 days of historical data per request.

By running a scheduled data pipeline on a persistent server, the project continuously collects and stores daily closing price and volume data, building up a historical dataset that grows over time. This data is served through a custom REST API and visualized through the dashboard.

The project was built entirely from scratch, covering backend development, database design, cloud deployment, and frontend visualization.

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 REST API written in C# |
| Database | PostgreSQL with Entity Framework Core |
| Hosting | AWS EC2 t3.micro running Ubuntu 24.04 |
| Data | CoinGecko API for daily price ingestion |
| Frontend | Vanilla HTML, CSS, and JavaScript |

## How It Works

On startup, the server automatically backfills any missing price data for all tracked assets. Every day at midnight UTC, it fetches the latest closing price and trading volume for each coin and stores it in the PostgreSQL database.

The REST API exposes endpoints for querying full history, the latest data point, and custom date ranges. All data served by the API powers the dashboard in real time.

## Tracked Assets

BTC, ETH, SOL, XRP, DOGE, ADA, USDC

## Data Attribution

All cryptocurrency market data is sourced from [CoinGecko](https://www.coingecko.com) via their free public API. This project is non-commercial and is built purely for educational and portfolio purposes.
