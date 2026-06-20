using NseBhavcopy.App.Models;

namespace NseBhavcopy.App.Services;

public class StockUniverseCatalog
{
    private static readonly IReadOnlyDictionary<StockUniverse, (string CsvFile, string Name, string Group)> Definitions =
        new Dictionary<StockUniverse, (string, string, string)>
        {
            [StockUniverse.Nifty50] = ("ind_nifty50list.csv", "Nifty 50", "Broad Market"),
            [StockUniverse.NiftyNext50] = ("ind_niftynext50list.csv", "Nifty Next 50", "Broad Market"),
            [StockUniverse.Nifty100] = ("ind_nifty100list.csv", "Nifty 100", "Broad Market"),
            [StockUniverse.Nifty500] = ("ind_nifty500list.csv", "Nifty 500", "Broad Market"),
            [StockUniverse.NiftyIt] = ("ind_niftyitlist.csv", "Nifty IT", "Sector"),
            [StockUniverse.NiftyBank] = ("ind_niftybanklist.csv", "Nifty Bank", "Sector"),
            [StockUniverse.AllEquity] = ("", "All NSE Equity", "Full Market")
        };

    private readonly BhavcopyStore _store;
    private readonly IReadOnlyDictionary<StockUniverse, StockCategory> _categories;
    private readonly IReadOnlyList<StockCategory> _categoryList;

    public StockUniverseCatalog(IWebHostEnvironment environment, BhavcopyStore store)
    {
        _store = store;
        var indicesPath = Path.Combine(environment.ContentRootPath, "Resources", "Indices");
        var categories = new Dictionary<StockUniverse, StockCategory>();

        foreach (var (universe, definition) in Definitions)
        {
            if (universe == StockUniverse.AllEquity)
            {
                categories[universe] = new StockCategory(
                    universe,
                    StockUniverses.GetId(universe),
                    definition.Name,
                    definition.Group,
                    Array.Empty<string>());
                continue;
            }

            var symbols = LoadIndexSymbols(Path.Combine(indicesPath, definition.CsvFile));
            categories[universe] = new StockCategory(
                universe,
                StockUniverses.GetId(universe),
                definition.Name,
                definition.Group,
                symbols);
        }

        _categories = categories;
        _categoryList = categories.Values
            .OrderBy(c => c.CategoryGroup, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<StockCategory> GetAll() => _categoryList;

    public IReadOnlyList<string> GetSymbols(StockUniverse universe) =>
        universe == StockUniverse.AllEquity
            ? _store.GetDistinctEqSymbols()
            : _categories[universe].Symbols;

    public int GetSymbolCount(StockUniverse universe) => GetSymbols(universe).Count;

    public string GetLabel(StockUniverse universe) => _categories[universe].Name;

    private static IReadOnlyList<string> LoadIndexSymbols(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            return Array.Empty<string>();
        }

        var symbols = new List<string>();

        using var reader = new StreamReader(csvPath);
        var header = reader.ReadLine();
        if (header is null)
        {
            return Array.Empty<string>();
        }

        var columns = header.Split(',');
        var symbolIndex = Array.FindIndex(columns, c => c.Trim().Equals("Symbol", StringComparison.OrdinalIgnoreCase));
        var seriesIndex = Array.FindIndex(columns, c => c.Trim().Equals("Series", StringComparison.OrdinalIgnoreCase));

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            var symbol = symbolIndex >= 0 && symbolIndex < parts.Length
                ? parts[symbolIndex].Trim()
                : parts.Length > 2 ? parts[2].Trim() : parts[0].Trim();
            var series = seriesIndex >= 0 && seriesIndex < parts.Length
                ? parts[seriesIndex].Trim()
                : parts.Length > 3 ? parts[3].Trim() : "EQ";

            if (!string.Equals(series, "EQ", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            symbols.Add(symbol.ToUpperInvariant());
        }

        return symbols
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
