using Newtonsoft.Json;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using ShareInvest.Entities;
using ShareInvest.Entities.AnTalk;
using ShareInvest.Entities.TradingView;
using ShareInvest.Observers;
using ShareInvest.Properties;
using ShareInvest.Securities;
using ShareInvest.Utilities.TradingView;

using Skender.Stock.Indicators;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Media;
using System.Net;

namespace ShareInvest;

partial class InquiryByStockTheme : Form
{
    internal InquiryByStockTheme(Theme theme, Simulation simulation, Icon[] icons)
    {
        this.icons = icons;
        this.theme = theme;
        this.simulation = simulation;

        InitializeComponent();

        theme.Send += (_, e) =>
        {
            switch (e.Convey)
            {
                case StockTheme t:
                    themes.Enqueue(t);

                    notifyIcon.Text = t.ThemeName;
                    return;

                case StockThemeDetail td:

                    if (!string.IsNullOrEmpty(ThemeCode) && !ThemeCode.Equals(td.ThemeCode) && Transmission != null && themes.TryDequeue(out StockTheme? theme))
                    {
                        theme.ThemeDetail = details.Where(p => theme.ThemeCode!.Equals(p.ThemeCode));

                        _ = BeginInvoke(async () =>
                        {
                            var response = await Transmission.ExecutePostAsync(theme);

                            if (HttpStatusCode.OK == response.StatusCode && int.TryParse(response.Content, out int operation))
                            {
                                Cache.MarketOperation = (MarketOperation)operation;

                                notifyIcon.Text = $"[{Cache.MarketOperation}] {themes.Count:D4}.{theme.ThemeName}";
                            }
                        });
                    }
                    ThemeCode = td.ThemeCode;

                    details.Add(td);
                    return;

                case string:
                    _ = BeginInvoke(async () =>
                    {
                        TimeSpan delay = TimeSpan.FromMilliseconds(0x400 * 3);

                        while (Transmission != null && themes.TryDequeue(out StockTheme? theme))
                        {
                            theme.ThemeDetail = details.Where(p => theme.ThemeCode!.Equals(p.ThemeCode));

                            var response = await Transmission.ExecutePostAsync(theme);

                            if (HttpStatusCode.OK == response.StatusCode && int.TryParse(response.Content, out int operation))
                            {
                                Cache.MarketOperation = (MarketOperation)operation;

                                notifyIcon.Text = $"[{Cache.MarketOperation}] {themes.Count:D4}.{theme.ThemeName}";
                            }
                        }
                        if (MarketOperation.장종료_시간외종료 != Cache.MarketOperation)
                        {
                            await Task.Delay(delay);

                            Dispose();
                        }
                    });
                    _ = Task.Run(async () => await ReactTheScenarioAsync());
                    return;
            }
        };
        simulation.Send += (_, arg) =>
        {
            switch (arg)
            {
                case ScenarioArgs e when Transmission != null:
#if DEBUG
                    Debug.WriteLine(JsonConvert.SerializeObject(e.ArrowMarker, Formatting.Indented));
#else
                    _ = BeginInvoke(async () => await Transmission.ExecutePostAsync(e.ArrowMarker));
#endif
                    break;

                case ThemeEventArgs t when t.Convey is string msg:
                    _ = BeginInvoke(Dispose);
                    break;
            }
        };
        timer.Start();
    }
    async Task ReactTheScenarioAsync()
    {
        if (Transmission == null)
        {
            return;
        }
        while (MarketOperation.장종료_시간외종료 != await Transmission.GetMarketOperationAsync())
        {
            await Task.Delay(0x400 * 0x40 * 0x40);
        }
        var futures = await Transmission.ExecuteGetAsync<AntFutures>(nameof(AntFutures));
        var indicators = await Transmission.ExecuteGetAsync<Indicators>(nameof(Scenario));

        int index = 0;

        if (futures == null || indicators == null)
        {
            return;
        }
        foreach (var indicator in indicators.OrderBy(ks => Guid.NewGuid()))
        {
            index++;

            foreach (var kf in futures.OrderBy(ks => Guid.NewGuid()))
            {
                var futuresData = await Transmission.ExecuteGetAsync<Quote>(string.Concat(nameof(AntFutures), '/', nameof(MinuteChart)), new
                {
                    code = kf.Code,
                    first = true,
                    date = kf.DateArr[0]
                });
                if (futuresData == null)
                {
                    continue;
                }
                simulation.InitializedScenario(kf.Code, futuresData);

                foreach (var date in kf.DateArr)
                {
                    var bytes = await Transmission.ExecuteStreamAsync(new
                    {
                        date,
                        code = kf.Code
                    });
                    if (bytes == null)
                    {
                        continue;
                    }
                    _ = BeginInvoke(() =>
                    {
                        notifyIcon.Text = $"[{(kf.Code.Length == 0x8 ? kf.Code : kf.Name)}] {date}\n{indicator.Strategics} ({index}/{indicators.Count()})";
                    });
                    var result = simulation.ReactTheScenario(date, bytes, futuresData,
                                                             strategics: indicator.Strategics,
                                                             histogram: indicator.HistogramTakeCount,
                                                             slope: indicator.SlopeTakeCount,
                                                             seedMoney: indicator.SeedMoney);
                    if (result == null)
                    {
                        break;
                    }
#if DEBUG
                    Debug.WriteLine(JsonConvert.SerializeObject(result.Balance, Formatting.Indented));
#else
                    _ = await Transmission.ExecutePostAsync(new Entities.TradingView.Scenario
                    {
                        Code = kf.Code,
                        Date = result.Balance.Date,
                        CumulativeRevenue = result.Balance.CumulativeRevenue,
                        Strategics = result.Balance.Strategics
                    });
#endif
                    var appendfuturesData = await Transmission.ExecuteGetAsync<Quote>(string.Concat(nameof(AntFutures), '/', nameof(MinuteChart)), new
                    {
                        code = kf.Code,
                        date = result.Balance.Date
                    });
                    if (appendfuturesData == null)
                    {
                        break;
                    }
                    futuresData = futuresData.Union(appendfuturesData).OrderBy(ks => ks.Date).TakeLast(0x400).ToArray();
                }
                _ = BeginInvoke(() => notifyIcon.Text = kf.Code.Length == 0x8 ? kf.Code : kf.Name);
            }
#if DEBUG
            Debug.WriteLine(indicator.Strategics);
#endif
        }
        simulation.TerminateTheProcess();
    }
    void TimerTick(object _, EventArgs e)
    {
        if (FormBorderStyle.Sizable == FormBorderStyle && FormWindowState.Minimized != WindowState)
        {
            _ = Task.Run(async () =>
            {
                while (string.IsNullOrEmpty(webView.AccessToken))
                {
                    await Task.Delay(0x400 * 5);
                }
                using (var sp = new SoundPlayer(Resources.DING))
                {
                    Transmission = new Transmission(webView.Url, webView.AccessToken);

                    sp.PlaySync();
                }
                if (Environment.ProcessorCount < 0x10)
                {
                    using (var service = ChromeDriverService.CreateDefaultService())
                    {
                        service.HideCommandPromptWindow = true;

                        int page = 1;

                        var options = new ChromeOptions
                        {

                        };
                        options.AddArguments("--headless", "--window-size=1920,1080", Resources.USERAGENT);

                        using (var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(0x40)))
                        {
                            var queue = new Queue<(string url, string themeCode)>();

                            driver.Navigate().GoToUrl($"https://finance.naver.com/sise/theme.naver");

                            while (Theme.GetNextPage(driver, page++) is IWebElement nextPage)
                            {
                                foreach (var tuple in theme.GetThemes(driver))
                                {
                                    queue.Enqueue(tuple);
                                }
                                nextPage.Click();
                            }
                            foreach (var tuple in theme.GetThemes(driver))
                            {
                                queue.Enqueue(tuple);
                            }
                            while (queue.TryDequeue(out (string url, string themeCode) e))
                            {
                                theme.GetStocks(driver, e.themeCode, e.url);
                            }
                            driver.Close();
                        }
                    }
                    theme.TerminateTheProcess();
                    return;
                }
                await ReactTheScenarioAsync();
            });
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Minimized;
            return;
        }
        notifyIcon.Icon = icons[DateTime.Now.Second % 3];
    }
    void SecuritiesResize(object _, EventArgs e)
    {
        SuspendLayout();

        Visible = false;
        ShowIcon = false;
        notifyIcon.Visible = true;

        ResumeLayout();
    }
    void JustBeforeFormClosing(object _, FormClosingEventArgs e)
    {
        if (Visible)
        {
            Hide();
        }
        if (CloseReason.UserClosing == e.CloseReason && DialogResult.Cancel == IsCancelled)
        {
            e.Cancel = true;

            return;
        }
        Dispose();
    }
    void StripItemClicked(object _, ToolStripItemClickedEventArgs e)
    {
        if (reference.Name!.Equals(e.ClickedItem?.Name))
        {
            _ = BeginInvoke(() => Process.Start(new ProcessStartInfo("http://share.enterprises")
            {
                UseShellExecute = true
            }));
            return;
        }
        Close();
    }
    string? ThemeCode
    {
        get; set;
    }
    Transmission? Transmission
    {
        get; set;
    }
    DialogResult IsCancelled
    {
        get => MessageBox.Show(Resources.WARNING.Replace('|', '\n'), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
    }
    readonly Simulation simulation;
    readonly Theme theme;
    readonly Icon[] icons;
    readonly CoreWebView webView = new();
    readonly ConcurrentQueue<StockTheme> themes = new();
    readonly ConcurrentBag<StockThemeDetail> details = [];
}