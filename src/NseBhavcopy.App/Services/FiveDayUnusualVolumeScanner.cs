using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public class FiveDayUnusualVolumeScanner
{
    private const int HistoryBufferDays = 45;

    private readonly BhavcopyStore _store;
    private readonly StockUniverseCatalog _catalog;

    public FiveDayUnusualVolumeScanner(BhavcopyStore store, StockUniverseCatalog catalog)
    {
        _store = store;
        _catalog = catalog;
    }

    public FiveDayUnusualVolumeScanReport Scan(
        FiveDayUnusualVolumeScanCriteria criteria,
        StockUniverse universe = StockUniverse.Nifty50)
    {
        var symbols = _catalog.GetSymbols(universe);
        var scanDate = criteria.ScanDate.Date;
        var fromDate = scanDate.AddDays(-HistoryBufferDays);
        var matches = new List<FiveDayUnusualVolumeResult>();
        var scanned = 0;

        foreach (var symbol in symbols)
        {
            scanned++;

            var dailyBars = DailyBarHelper.GetDailyBarsUpTo(
                _store.GetBarsForSymbol(symbol, fromDate, scanDate),
                scanDate);

            var windowBars = dailyBars.TakeLast(criteria.WindowDays).ToList();
            if (windowBars.Count < criteria.WindowDays)
            {
                continue;
            }

            var windowStart = windowBars[0].TradeDate;
            var averageVolume = DailyBarHelper.CalculateAverageDailyVolumeBefore(
                dailyBars,
                windowStart,
                criteria.BaselineLookbackDays);

            var sampleDays = DailyBarHelper.CountBaselineTradingDays(
                dailyBars,
                windowStart,
                criteria.BaselineLookbackDays);

            if (sampleDays == 0)
            {
                continue;
            }

            var unusualDays = new List<UnusualDayVolume>();
            foreach (var day in windowBars)
            {
                if (!criteria.IsUnusual(day.Volume, averageVolume, out var ratio))
                {
                    continue;
                }

                unusualDays.Add(UnusualDayVolume.FromBar(day, ratio));
            }

            if (unusualDays.Count == 0)
            {
                continue;
            }

            matches.Add(new FiveDayUnusualVolumeResult
            {
                Symbol = symbol,
                ScanDate = scanDate,
                AverageVolume = averageVolume,
                BaselineSampleDays = sampleDays,
                UnusualDays = unusualDays
            });
        }

        return new FiveDayUnusualVolumeScanReport
        {
            ScanDate = scanDate,
            WindowDays = criteria.WindowDays,
            BaselineLookbackDays = criteria.BaselineLookbackDays,
            MinVolumeMultiplier = criteria.MinVolumeMultiplier,
            MaxVolumeMultiplier = criteria.MaxVolumeMultiplier,
            TotalSymbols = symbols.Count,
            ScannedCount = scanned,
            Matches = matches
                .OrderByDescending(r => r.PeakVolumeRatio)
                .ThenBy(r => r.Symbol, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Message = $"Scanned {scanned}/{symbols.Count} symbols from local bhavcopy. " +
                        $"Flags stocks with unusual volume in the last {criteria.WindowDays} trading day(s) " +
                        $"vs prior {criteria.BaselineLookbackDays}-day average."
        };
    }
}
