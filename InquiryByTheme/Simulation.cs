using ShareInvest.Utilities.TradingView;

using Skender.Stock.Indicators;

using System.Text;

namespace ShareInvest;

class Simulation
{
    internal void ReactTheScenario(byte[] bytes)
    {
        string scenario = Encoding.UTF8.GetString(bytes).Replace("\"", string.Empty);

        foreach (var str in scenario.Split("\\r\\n"))
        {
            var strArr = str.Split("\\t");

            switch (strArr.Length)
            {
                case 60:

                    continue;

                case 43:

                    continue;

                case 3:

                    continue;
            }
        }
        Send?.Invoke(this, new EventArgs());
    }
    internal void InitializedScenario(string code, IEnumerable<Quote> futuresData)
    {
        Chart = new MinuteChart(code);

        _ = futuresData.GetSuperTrend().Condense().RemoveWarmupPeriods().ToArray();
        _ = futuresData.GetAtrStop().Condense().RemoveWarmupPeriods().ToArray();
        var initMacd = futuresData.GetMacd().Condense().RemoveWarmupPeriods().ToArray();
        var initSlope = futuresData.GetSlope(14).Condense().RemoveWarmupPeriods();

        _ = Chart.InitializedMacdData(initMacd);
        _ = Chart.InitializedSlopeData(initSlope);

        _ = Chart.InitializedCandlestickData(futuresData);
        _ = Chart.InitializedVolumeData(futuresData);
    }
    Chart? Chart
    {
        get; set;
    }
    public event EventHandler? Send;
}