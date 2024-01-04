using ShareInvest.Entities;
using ShareInvest.Entities.TradingView;
using ShareInvest.Observers;
using ShareInvest.Properties;
using ShareInvest.RealType;
using ShareInvest.Utilities.TradingView;

using Skender.Stock.Indicators;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace ShareInvest;

class Simulation
{
    internal void TerminateTheProcess()
    {
        Send?.Invoke(this, new ThemeEventArgs(nameof(TerminateTheProcess)));
    }
    internal Futures ReactTheScenario(string code, string date, byte[] bytes, IEnumerable<Quote> futuresData, string? strategics = null)
    {
        string scenario = Encoding.UTF8.GetString(bytes).Replace("\"", string.Empty), dateTime = string.Empty;

        bool position = false, square = false;

        int label = 0;

        ConcurrentStack<Quote> quoteStack = new(futuresData);

        var simulation = new Futures(code, date);

        foreach (var str in scenario.Split("\\r\\n"))
        {
            var strArr = str.Split("\\t");

            switch (strArr.Length)
            {
                case 60:
                    Quote = new PriorityQuote
                    {
                        TopPriorityAskPrice = strArr[1],
                        TopPriorityBidPrice = strArr[2],
                        CurrentPrice = strArr[52]
                    };
                    continue;

                case 43 when quoteStack.TryPop(out var latestQuote):
                    _ = DateTime.TryParseExact(string.Concat(date, ' ', strArr[0]), Resources.DATETIME, null, DateTimeStyles.None, out var time);

                    decimal volume = Math.Abs(Convert.ToDecimal(strArr[6])), closePrice = Math.Abs(Convert.ToDecimal(strArr[1]));

                    bool isSameTime = latestQuote != null && latestQuote.Date.Minute == time.Minute && latestQuote.Date.Day == time.Day,
                         moreThanBefore = isSameTime ? (quoteStack.TryPeek(out var beforeQuote) && beforeQuote.Volume < latestQuote?.Volume) : (latestQuote?.Volume < volume);

                    Quote quote;

                    if (isSameTime)
                    {
                        quote = new Quote
                        {
                            Date = latestQuote != null ? latestQuote.Date : time,
                            Close = closePrice,
                            Open = latestQuote != null ? latestQuote.Open : closePrice,
                            High = latestQuote?.High < closePrice ? closePrice : (latestQuote != null ? latestQuote.High : closePrice),
                            Low = latestQuote?.Low > closePrice ? closePrice : (latestQuote != null ? latestQuote.Low : closePrice),
                            Volume = (latestQuote != null ? latestQuote.Volume : 0) + volume
                        };
                    }
                    else
                    {
                        quote = new Quote
                        {
                            Date = time,
                            Open = closePrice,
                            High = closePrice,
                            Low = closePrice,
                            Close = closePrice,
                            Volume = volume
                        };
                        if (latestQuote != null)
                        {
                            quoteStack.Push(latestQuote);
                        }
                        if (label > 0)
                        {
                            Send?.Invoke(this, new ScenarioArgs(new Marker
                            {
                                Strategics = strategics ?? string.Empty,
                                Code = code,
                                Label = label,
                                DateTime = dateTime,
                                LongPosition = position,
                                Square = square
                            }));
                            label = 0;
                        }
                    }
                    quoteStack.Push(quote);

                    Quote = new PriorityQuote
                    {
                        TopPriorityAskPrice = strArr[4],
                        TopPriorityBidPrice = strArr[5],
                        CurrentPrice = strArr[1]
                    };
                    var indicator = new Indicators
                    {
                        AtrStop = quoteStack.GetAtrStop().Condense().RemoveWarmupPeriods(),
                        Macd = quoteStack.GetMacd().Condense().RemoveWarmupPeriods(),
                        Slope = quoteStack.GetSlope(14).Condense().RemoveWarmupPeriods(),
                        SuperTrend = quoteStack.GetSuperTrend().Condense().RemoveWarmupPeriods()
                    };
                    if (Chart == null)
                    {
                        continue;
                    }
                    var i = Chart.UpdateFuturesIndicator(code, indicator);

                    var judge = new Strategics
                    {
                        DateTime = time,
                        JustBefore = JustBefore,
                        AtrStop = i.atrStop.Order ? 1 : -1,
                        SuperTrend = i.superTrend.Order ? 1 : -1,
                        Histogram = indicator.Macd!.Select(e => e.Histogram ?? double.NaN).TakeLast(5),
                        Slope = indicator.Slope!.Select(e => e.Slope ?? double.NaN).TakeLast(5)
                    };
                    if (judge.DecideOnPosition)
                    {
                        var tradingPosition = judge.Position > 0;

                        var transactionPrice = Math.Abs(Convert.ToDouble(tradingPosition ? Quote?.TopPriorityAskPrice : Quote?.TopPriorityBidPrice));

                        simulation.Calculate(tradingPosition ? 1 : -1, transactionPrice == 0 ? Math.Abs(Convert.ToDouble(Quote?.CurrentPrice)) : transactionPrice);

                        label += 1;
                        position = tradingPosition;
                        dateTime = time.ToString(Resources.DATEFORMAT);
                        square = false;

                        JustBefore = time;
                    }
                    simulation.Balance.CurrentPrice = Convert.ToDouble(quote.Close);
                    simulation.Balance.DateTime = time;
                    continue;

                case 3:
                    Quote = new PriorityQuote
                    {
                        TopPriorityAskPrice = strArr[1],
                        TopPriorityBidPrice = strArr[2],
                        CurrentPrice = strArr[0]
                    };
                    continue;
            }
        }
        label = 0;

        while (simulation.Balance.HoldingQuantity != 0)
        {
            var tradingPosition = simulation.Balance.HoldingQuantity < 0;

            var transactionPrice = Math.Abs(Convert.ToDouble(tradingPosition ? Quote?.TopPriorityAskPrice : Quote?.TopPriorityBidPrice));

            simulation.Calculate(tradingPosition ? 1 : -1, transactionPrice == 0 ? Math.Abs(Convert.ToDouble(Quote?.CurrentPrice)) : transactionPrice);

            label += 1;
            position = tradingPosition;
            square = true;
            dateTime = simulation.Balance.DateTime.ToString(Resources.DATEFORMAT);
        }
        Send?.Invoke(this, new ScenarioArgs(new Marker
        {
            Strategics = strategics ?? string.Empty,
            Code = code,
            Label = label,
            DateTime = dateTime,
            LongPosition = position,
            Square = square
        }));
        return simulation;
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
    PriorityQuote? Quote
    {
        get; set;
    }
    Chart? Chart
    {
        get; set;
    }
    DateTime JustBefore
    {
        get; set;
    }
    public event EventHandler<MsgEventArgs>? Send;
}