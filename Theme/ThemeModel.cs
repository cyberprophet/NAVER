namespace ShareInvest;

class ThemeModel
{
    internal string? ThemeName
    {
        get; set;
    }
    internal string? ThemeCode
    {
        get; set;
    }
    internal string? RateCompareToPreviousDay
    {
        get; set;
    }
    internal string? AverageRateLast3Days
    {
        get; set;
    }
    internal string? RisingStockCount
    {
        get; set;
    }
    internal string? FlatStockCount
    {
        get; set;
    }
    internal string? FallingStockCount
    {
        get; set;
    }
    internal string? FirstLeadingStockCode
    {
        get; set;
    }
    internal string? SecondLeadingStockCode
    {
        get; set;
    }
}