namespace NseBhavcopy.App.Configuration;

public class BhavcopyOptions
{
    public const string SectionName = "Bhavcopy";

    public string DatabasePath { get; set; } = "Data/bhavcopy.db";

    public int RequestDelayMs { get; set; } = 500;
}
