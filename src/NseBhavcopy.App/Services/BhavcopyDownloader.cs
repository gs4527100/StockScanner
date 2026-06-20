using System.IO.Compression;
using System.Net;
using System.Text;
using NseBhavcopy.App.Configuration;
using NseBhavcopy.App.Models;
using Microsoft.Extensions.Options;

namespace NseBhavcopy.App.Services;

public class BhavcopyDownloader
{
    private static readonly DateTime UdffStartDate = new(2024, 7, 8);

    private static readonly string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly BhavcopyParser _parser;
    private readonly BhavcopyStore _store;
    private readonly BhavcopyOptions _options;
    private readonly ILogger<BhavcopyDownloader> _logger;

    public BhavcopyDownloader(
        BhavcopyParser parser,
        BhavcopyStore store,
        IOptions<BhavcopyOptions> options,
        ILogger<BhavcopyDownloader> logger)
    {
        _parser = parser;
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BhavcopyDownloadResult> DownloadDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var tradeDate = date.Date;

        if (_store.HasDate(tradeDate))
        {
            return new BhavcopyDownloadResult
            {
                Date = tradeDate,
                Success = true,
                RowCount = 0,
                Format = "cached"
            };
        }

        if (tradeDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new BhavcopyDownloadResult
            {
                Date = tradeDate,
                Success = false,
                Error = "Weekend — no bhavcopy."
            };
        }

        try
        {
            var (bytes, format) = tradeDate >= UdffStartDate
                ? await TryDownloadUdffAsync(tradeDate, cancellationToken)
                : await TryDownloadLegacyAsync(tradeDate, cancellationToken);

            if (bytes is null)
            {
                var fallback = tradeDate >= UdffStartDate
                    ? await TryDownloadLegacyAsync(tradeDate, cancellationToken)
                    : await TryDownloadUdffAsync(tradeDate, cancellationToken);

                if (fallback.bytes is null)
                {
                    return new BhavcopyDownloadResult
                    {
                        Date = tradeDate,
                        Success = false,
                        Error = "Bhavcopy not found on NSE archives."
                    };
                }

                bytes = fallback.bytes;
                format = fallback.format;
            }

            var csv = format == "udiff-zip"
                ? ExtractCsvFromZip(bytes)
                : Encoding.UTF8.GetString(bytes);

            var rows = _parser.Parse(csv, format);
            _store.UpsertBars(rows);

            return new BhavcopyDownloadResult
            {
                Date = tradeDate,
                Success = true,
                RowCount = rows.Count,
                Format = format
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download bhavcopy for {Date}", tradeDate);
            return new BhavcopyDownloadResult
            {
                Date = tradeDate,
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<BhavcopyBulkDownloadReport> DownloadRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        if (from > to)
        {
            (from, to) = (to, from);
        }

        var results = new List<BhavcopyDownloadResult>();
        var current = from;

        while (current <= to)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                results.Add(new BhavcopyDownloadResult
                {
                    Date = current,
                    Success = false,
                    Error = "Skipped (weekend)"
                });
                current = current.AddDays(1);
                continue;
            }

            if (_store.HasDate(current))
            {
                results.Add(new BhavcopyDownloadResult
                {
                    Date = current,
                    Success = true,
                    Format = "cached"
                });
                current = current.AddDays(1);
                continue;
            }

            var result = await DownloadDateAsync(current, cancellationToken);
            results.Add(result);

            if (_options.RequestDelayMs > 0)
            {
                await Task.Delay(_options.RequestDelayMs, cancellationToken);
            }

            current = current.AddDays(1);
        }

        return new BhavcopyBulkDownloadReport
        {
            FromDate = from,
            ToDate = to,
            Attempted = results.Count(r => r.Format != "cached" && r.Error != "Skipped (weekend)"),
            Succeeded = results.Count(r => r.Success && r.Format != "cached"),
            Skipped = results.Count(r => r.Format == "cached" || r.Error == "Skipped (weekend)"),
            Failed = results.Count(r => !r.Success && r.Error != "Skipped (weekend)"),
            Results = results
        };
    }

    private static async Task<(byte[]? bytes, string format)> TryDownloadUdffAsync(
        DateTime date,
        CancellationToken cancellationToken)
    {
        var url =
            $"https://nsearchives.nseindia.com/content/cm/BhavCopy_NSE_CM_0_0_0_{date:yyyyMMdd}_F_0000.csv.zip";

        var bytes = await FetchAsync(url, cancellationToken);
        return bytes is null ? (null, "udiff-zip") : (bytes, "udiff-zip");
    }

    private static async Task<(byte[]? bytes, string format)> TryDownloadLegacyAsync(
        DateTime date,
        CancellationToken cancellationToken)
    {
        var url =
            $"https://nsearchives.nseindia.com/products/content/sec_bhavdata_full_{date:ddMMyyyy}.csv";

        var bytes = await FetchAsync(url, cancellationToken);
        return bytes is null ? (null, "legacy") : (bytes, "legacy");
    }

    private static async Task<byte[]?> FetchAsync(string url, CancellationToken cancellationToken)
    {
        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true
        };

        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.nseindia.com/");

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static string ExtractCsvFromZip(byte[] zipBytes)
    {
        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("No CSV found in bhavcopy zip.");

        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
