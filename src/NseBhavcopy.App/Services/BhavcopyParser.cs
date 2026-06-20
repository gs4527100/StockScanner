using System.Globalization;
using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public class BhavcopyParser
{
    public IReadOnlyList<DailyBar> Parse(string csv, string format)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<DailyBar>();
        }

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return Array.Empty<DailyBar>();
        }

        var header = SplitCsvLine(lines[0]);
        var isUdff = format.StartsWith("udiff", StringComparison.OrdinalIgnoreCase)
            || header.Any(h => h.Equals("TckrSymb", StringComparison.OrdinalIgnoreCase));

        return isUdff ? ParseUdff(lines, header) : ParseLegacy(lines, header);
    }

    private static IReadOnlyList<DailyBar> ParseLegacy(string[] lines, string[] header)
    {
        var symbolIdx = IndexOf(header, "SYMBOL");
        var seriesIdx = IndexOf(header, "SERIES");
        var openIdx = IndexOf(header, "OPEN_PRICE", "OPEN");
        var highIdx = IndexOf(header, "HIGH_PRICE", "HIGH");
        var lowIdx = IndexOf(header, "LOW_PRICE", "LOW");
        var closeIdx = IndexOf(header, "CLOSE_PRICE", "CLOSE");
        var volumeIdx = IndexOf(header, "TTL_TRD_QNTY", "TOTTRDQTY");
        var dateIdx = IndexOf(header, "TIMESTAMP", "DATE1", "DATE");

        var bars = new List<DailyBar>();

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = SplitCsvLine(lines[i]);
            if (parts.Length < 5)
            {
                continue;
            }

            var symbol = Get(parts, symbolIdx);
            var series = Get(parts, seriesIdx);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            if (!TryParseDecimal(Get(parts, openIdx), out var open)
                || !TryParseDecimal(Get(parts, highIdx), out var high)
                || !TryParseDecimal(Get(parts, lowIdx), out var low)
                || !TryParseDecimal(Get(parts, closeIdx), out var close)
                || !TryParseLong(Get(parts, volumeIdx), out var volume))
            {
                continue;
            }

            var tradeDate = ParseDate(Get(parts, dateIdx));
            if (tradeDate is null)
            {
                continue;
            }

            bars.Add(new DailyBar
            {
                Symbol = symbol.Trim().ToUpperInvariant(),
                Series = string.IsNullOrWhiteSpace(series) ? "EQ" : series.Trim().ToUpperInvariant(),
                TradeDate = tradeDate.Value,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            });
        }

        return bars;
    }

    private static IReadOnlyList<DailyBar> ParseUdff(string[] lines, string[] header)
    {
        var symbolIdx = IndexOf(header, "TckrSymb", "SYMBOL");
        var seriesIdx = IndexOf(header, "SctySrs", "SERIES");
        var openIdx = IndexOf(header, "OpnPric", "OPEN_PRICE");
        var highIdx = IndexOf(header, "HghPric", "HIGH_PRICE");
        var lowIdx = IndexOf(header, "LwPric", "LOW_PRICE");
        var closeIdx = IndexOf(header, "ClsPric", "CLOSE_PRICE");
        var volumeIdx = IndexOf(header, "TtlTradgVol", "TTL_TRD_QNTY");
        var dateIdx = IndexOf(header, "TradDt", "BizDt", "TIMESTAMP");

        var bars = new List<DailyBar>();

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = SplitCsvLine(lines[i]);
            if (parts.Length < 5)
            {
                continue;
            }

            var symbol = Get(parts, symbolIdx);
            var series = Get(parts, seriesIdx);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            if (!TryParseDecimal(Get(parts, openIdx), out var open)
                || !TryParseDecimal(Get(parts, highIdx), out var high)
                || !TryParseDecimal(Get(parts, lowIdx), out var low)
                || !TryParseDecimal(Get(parts, closeIdx), out var close)
                || !TryParseLong(Get(parts, volumeIdx), out var volume))
            {
                continue;
            }

            var tradeDate = ParseDate(Get(parts, dateIdx));
            if (tradeDate is null)
            {
                continue;
            }

            bars.Add(new DailyBar
            {
                Symbol = symbol.Trim().ToUpperInvariant(),
                Series = string.IsNullOrWhiteSpace(series) ? "EQ" : series.Trim().ToUpperInvariant(),
                TradeDate = tradeDate.Value,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            });
        }

        return bars;
    }

    private static string[] SplitCsvLine(string line) =>
        line.Split(',', StringSplitOptions.TrimEntries);

    private static int IndexOf(string[] header, params string[] names)
    {
        for (var i = 0; i < header.Length; i++)
        {
            foreach (var name in names)
            {
                if (header[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string Get(string[] parts, int index) =>
        index >= 0 && index < parts.Length ? parts[index] : string.Empty;

    private static bool TryParseDecimal(string value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

    private static bool TryParseLong(string value, out long result) =>
        long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[]
        {
            "yyyy-MM-dd",
            "dd-MMM-yyyy",
            "dd-MM-yyyy",
            "yyyyMMdd",
            "dd MMM yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.Date;
            }
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;
    }
}
