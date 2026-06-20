namespace NseBhavcopy.App.Services;

public class EquityCompanyNameCatalog
{
    private readonly IReadOnlyDictionary<string, string> _namesBySymbol;

    public EquityCompanyNameCatalog(IWebHostEnvironment environment)
    {
        var indicesPath = Path.Combine(environment.ContentRootPath, "Resources", "Indices");
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(indicesPath))
        {
            foreach (var csvPath in Directory.EnumerateFiles(indicesPath, "*.csv"))
            {
                LoadFromCsv(csvPath, names);
            }
        }

        _namesBySymbol = names;
    }

    public string GetCompanyName(string symbol) =>
        _namesBySymbol.TryGetValue(symbol, out var name) ? name : string.Empty;

    private static void LoadFromCsv(string csvPath, Dictionary<string, string> names)
    {
        using var reader = new StreamReader(csvPath);
        var header = reader.ReadLine();
        if (header is null)
        {
            return;
        }

        var columns = header.Split(',');
        var nameIndex = Array.FindIndex(columns, c => c.Trim().Equals("Company Name", StringComparison.OrdinalIgnoreCase));
        var symbolIndex = Array.FindIndex(columns, c => c.Trim().Equals("Symbol", StringComparison.OrdinalIgnoreCase));

        if (symbolIndex < 0)
        {
            return;
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (symbolIndex >= parts.Length)
            {
                continue;
            }

            var symbol = parts[symbolIndex].Trim().ToUpperInvariant();
            var companyName = nameIndex >= 0 && nameIndex < parts.Length
                ? parts[nameIndex].Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(companyName))
            {
                names[symbol] = companyName;
            }
        }
    }
}
