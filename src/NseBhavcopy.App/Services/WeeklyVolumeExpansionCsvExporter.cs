using System.Globalization;
using System.Text;
using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public static class WeeklyVolumeExpansionCsvExporter
{
    public static byte[] Export(WeeklyVolumeExpansionScanReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Headers));

        foreach (var row in report.Matches)
        {
            builder.AppendLine(string.Join(',',
                Csv(row.Symbol),
                Csv(row.CompanyName),
                row.PreviousWeekVolume.ToString(CultureInfo.InvariantCulture),
                Csv(row.PreviousWeekColorLabel),
                row.CurrentWeekVolume.ToString(CultureInfo.InvariantCulture),
                Csv(row.CurrentWeekColorLabel),
                row.VolumeExpansionPercent.ToString("0.##", CultureInfo.InvariantCulture),
                row.BodyPercent.ToString("0.##", CultureInfo.InvariantCulture),
                row.UpperWickPercent.ToString("0.##", CultureInfo.InvariantCulture),
                row.LowerWickPercent.ToString("0.##", CultureInfo.InvariantCulture),
                row.ScanDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static string BuildFileName(WeeklyVolumeExpansionScanReport report) =>
        $"weekly-volume-expansion-{report.ScanDate:yyyy-MM-dd}.csv";

    private static readonly string[] Headers =
    [
        "Symbol",
        "Company Name",
        "Previous Week Volume",
        "Previous Week Color",
        "Current Week Volume",
        "Current Week Color",
        "Volume Expansion %",
        "Body %",
        "Upper Wick %",
        "Lower Wick %",
        "Scan Date"
    ];

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
