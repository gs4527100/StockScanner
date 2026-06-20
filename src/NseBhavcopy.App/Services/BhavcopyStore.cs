using Microsoft.Data.Sqlite;
using NseBhavcopy.App.Configuration;
using NseBhavcopy.App.Models;
using Microsoft.Extensions.Options;

namespace NseBhavcopy.App.Services;

public class BhavcopyStore
{
    private readonly string _connectionString;
    private readonly object _initLock = new();
    private bool _initialized;

    public BhavcopyStore(IOptions<BhavcopyOptions> options, IWebHostEnvironment environment)
    {
        var dbPath = options.Value.DatabasePath;
        if (!Path.IsPathRooted(dbPath))
        {
            dbPath = Path.Combine(environment.ContentRootPath, dbPath);
        }

        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ConnectionString;
    }

    public void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS DailyBars (
                    Symbol TEXT NOT NULL,
                    Series TEXT NOT NULL,
                    TradeDate TEXT NOT NULL,
                    Open REAL NOT NULL,
                    High REAL NOT NULL,
                    Low REAL NOT NULL,
                    Close REAL NOT NULL,
                    Volume INTEGER NOT NULL,
                    PRIMARY KEY (Symbol, Series, TradeDate)
                );
                CREATE INDEX IF NOT EXISTS IX_DailyBars_TradeDate ON DailyBars(TradeDate);
                CREATE INDEX IF NOT EXISTS IX_DailyBars_SymbolDate ON DailyBars(Symbol, TradeDate);
                """;
            command.ExecuteNonQuery();
            _initialized = true;
        }
    }

    public bool HasDate(DateTime date)
    {
        EnsureInitialized();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM DailyBars WHERE TradeDate = $date LIMIT 1";
        command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
        return command.ExecuteScalar() is not null;
    }

    public void UpsertBars(IEnumerable<DailyBar> bars)
    {
        EnsureInitialized();
        using var connection = Open();
        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO DailyBars (Symbol, Series, TradeDate, Open, High, Low, Close, Volume)
            VALUES ($symbol, $series, $tradeDate, $open, $high, $low, $close, $volume)
            ON CONFLICT(Symbol, Series, TradeDate) DO UPDATE SET
                Open = excluded.Open,
                High = excluded.High,
                Low = excluded.Low,
                Close = excluded.Close,
                Volume = excluded.Volume;
            """;

        var symbolParam = command.Parameters.Add("$symbol", SqliteType.Text);
        var seriesParam = command.Parameters.Add("$series", SqliteType.Text);
        var dateParam = command.Parameters.Add("$tradeDate", SqliteType.Text);
        var openParam = command.Parameters.Add("$open", SqliteType.Real);
        var highParam = command.Parameters.Add("$high", SqliteType.Real);
        var lowParam = command.Parameters.Add("$low", SqliteType.Real);
        var closeParam = command.Parameters.Add("$close", SqliteType.Real);
        var volumeParam = command.Parameters.Add("$volume", SqliteType.Integer);

        foreach (var bar in bars)
        {
            symbolParam.Value = bar.Symbol;
            seriesParam.Value = bar.Series;
            dateParam.Value = bar.TradeDate.ToString("yyyy-MM-dd");
            openParam.Value = (double)bar.Open;
            highParam.Value = (double)bar.High;
            lowParam.Value = (double)bar.Low;
            closeParam.Value = (double)bar.Close;
            volumeParam.Value = bar.Volume;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public IReadOnlyList<DailyBar> GetBarsForSymbol(string symbol, DateTime? fromDate = null, DateTime? toDate = null)
    {
        EnsureInitialized();
        using var connection = Open();
        using var command = connection.CreateCommand();

        var sql = """
            SELECT Symbol, Series, TradeDate, Open, High, Low, Close, Volume
            FROM DailyBars
            WHERE Symbol = $symbol AND Series = 'EQ'
            """;

        command.Parameters.AddWithValue("$symbol", symbol.ToUpperInvariant());

        if (fromDate.HasValue)
        {
            sql += " AND TradeDate >= $fromDate";
            command.Parameters.AddWithValue("$fromDate", fromDate.Value.ToString("yyyy-MM-dd"));
        }

        if (toDate.HasValue)
        {
            sql += " AND TradeDate <= $toDate";
            command.Parameters.AddWithValue("$toDate", toDate.Value.ToString("yyyy-MM-dd"));
        }

        sql += " ORDER BY TradeDate";
        command.CommandText = sql;

        var bars = new List<DailyBar>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            bars.Add(ReadBar(reader));
        }

        return bars;
    }

    public IReadOnlyDictionary<string, List<DailyBar>> GetBarsGroupedBySymbol(
        IEnumerable<string> symbols,
        DateTime fromDate,
        DateTime toDate)
    {
        EnsureInitialized();
        var symbolSet = symbols.Select(s => s.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var grouped = symbolSet.ToDictionary(s => s, _ => new List<DailyBar>(), StringComparer.OrdinalIgnoreCase);

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Symbol, Series, TradeDate, Open, High, Low, Close, Volume
            FROM DailyBars
            WHERE Series = 'EQ'
              AND TradeDate >= $fromDate
              AND TradeDate <= $toDate
            ORDER BY Symbol, TradeDate;
            """;
        command.Parameters.AddWithValue("$fromDate", fromDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$toDate", toDate.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var bar = ReadBar(reader);
            if (grouped.TryGetValue(bar.Symbol, out var list))
            {
                list.Add(bar);
            }
        }

        return grouped;
    }

    public IReadOnlyList<string> GetDistinctEqSymbols()
    {
        EnsureInitialized();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT Symbol FROM DailyBars
            WHERE Series = 'EQ'
            ORDER BY Symbol COLLATE NOCASE;
            """;

        var symbols = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            symbols.Add(reader.GetString(0));
        }

        return symbols;
    }

    public BhavcopyStoreStats GetStats()
    {
        EnsureInitialized();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COUNT(*),
                COUNT(DISTINCT Symbol),
                MIN(TradeDate),
                MAX(TradeDate)
            FROM DailyBars
            WHERE Series = 'EQ';
            """;

        using var reader = command.ExecuteReader();
        reader.Read();

        return new BhavcopyStoreStats
        {
            TotalBars = reader.GetInt32(0),
            SymbolCount = reader.GetInt32(1),
            MinDate = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2)),
            MaxDate = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3))
        };
    }

    private static DailyBar ReadBar(SqliteDataReader reader) =>
        new()
        {
            Symbol = reader.GetString(0),
            Series = reader.GetString(1),
            TradeDate = DateTime.Parse(reader.GetString(2)),
            Open = (decimal)reader.GetDouble(3),
            High = (decimal)reader.GetDouble(4),
            Low = (decimal)reader.GetDouble(5),
            Close = (decimal)reader.GetDouble(6),
            Volume = reader.GetInt64(7)
        };

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
