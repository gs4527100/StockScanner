namespace NseBhavcopy.App.Configuration;

public class NextDayBiasOptions
{
    public const string SectionName = "NextDayBias";

    public int DefaultLookbackCalendarDays { get; set; } = 30;

    public int MaxCalendarDays { get; set; } = 120;
}
