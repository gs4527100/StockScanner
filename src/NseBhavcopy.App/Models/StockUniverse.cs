namespace NseBhavcopy.App.Models;

public enum StockUniverse
{
    Nifty50,
    NiftyNext50,
    Nifty100,
    Nifty500,
    NiftyIt,
    NiftyBank,
    AllEquity
}

public sealed record StockUniverseOption(string Id, string Label, StockUniverse Value);

public static class StockUniverses
{
    public static StockUniverse Default => StockUniverse.Nifty50;

    public static IReadOnlyList<StockUniverseOption> All { get; } =
    [
        new("nifty50", "Nifty 50", StockUniverse.Nifty50),
        new("niftynext50", "Nifty Next 50", StockUniverse.NiftyNext50),
        new("nifty100", "Nifty 100", StockUniverse.Nifty100),
        new("nifty500", "Nifty 500", StockUniverse.Nifty500),
        new("niftyit", "Nifty IT", StockUniverse.NiftyIt),
        new("niftybank", "Nifty Bank", StockUniverse.NiftyBank),
        new("allequity", "All NSE Equity (from bhavcopy)", StockUniverse.AllEquity)
    ];

    public static string GetId(StockUniverse universe) =>
        All.First(o => o.Value == universe).Id;

    public static string GetLabel(StockUniverse universe) =>
        All.First(o => o.Value == universe).Label;

    public static StockUniverse Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Default;
        }

        return All.FirstOrDefault(o => string.Equals(o.Id, value, StringComparison.OrdinalIgnoreCase))?.Value
            ?? Default;
    }
}

public sealed record StockCategory(
    StockUniverse Universe,
    string Id,
    string Name,
    string CategoryGroup,
    IReadOnlyList<string> Symbols)
{
    public int SymbolCount => Symbols.Count;
}
