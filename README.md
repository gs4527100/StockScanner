# NseBhavcopy.Scanner

Fast daily stock scanners powered by **NSE Bhavcopy** (EOD OHLCV) — no live API calls during scans.

## Features

- Download NSE bhavcopy (legacy CSV + CM-UDiFF zip formats)
- Store daily bars in local **SQLite** database
- Filter by index: Nifty 50, Nifty 100, Nifty 500, Nifty IT, Nifty Bank, etc.
- Scanners (all run in **1–5 seconds** on cached data):
  - **3 Same-Color Volume** — green/red × increasing/decreasing in one scan
  - **5-Day Unusual Volume** — high volume vs baseline
  - **Weekly Volume Expansion** — current week volume &gt; previous week
  - **Next Day Bias** — backtest win rate: did the next trading day move in the scanner's expected direction?

## Run

```bash
cd src/NseBhavcopy.App
dotnet run
```

Open http://localhost:5003

## Workflow

1. **Download** — fetch bhavcopy for a date range (trading days only)
2. **Scan** — pick universe, scan date, and run a scanner

Data is stored in `Data/bhavcopy.db`.

## Note

Bhavcopy provides **daily EOD data only**. Intraday scanners (first candle, slot volume) require Fyers/API — use the main **Fyres** app for those.
