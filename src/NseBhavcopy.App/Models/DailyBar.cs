namespace NseBhavcopy.App.Models;

public class DailyBar
{
    public string Symbol { get; init; } = string.Empty;

    public string Series { get; init; } = "EQ";

    public DateTime TradeDate { get; init; }

    public decimal Open { get; init; }

    public decimal High { get; init; }

    public decimal Low { get; init; }

    public decimal Close { get; init; }

    public long Volume { get; init; }

    public bool IsBearish => Close < Open;

    public bool IsBullish => Close > Open;
}
