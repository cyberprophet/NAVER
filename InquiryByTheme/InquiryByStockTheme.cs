using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using ShareInvest.Entities;
using ShareInvest.Properties;
using ShareInvest.Securities;

using System.Collections.Concurrent;
using System.Media;
using System.Net;

namespace ShareInvest;

partial class InquiryByStockTheme : Form
{
    internal InquiryByStockTheme(Theme theme, Icon[] icons)
    {
        this.icons = icons;
        this.theme = theme;

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

                    TimeSpan delay = TimeSpan.FromMilliseconds(0x400 * 3);

                    if (MarketOperation.장종료_시간외종료 == Cache.MarketOperation)
                    {
                        var now = DateTime.Now;

                        DateTime targetTime = new(now.Year, now.Month, now.Day + 1, 8, Random.Shared.Next(55, 60), Random.Shared.Next(0, 60));

                        delay = targetTime - now;
                    }
                    _ = BeginInvoke(async () =>
                    {
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
                        await Task.Delay(delay);

                        Dispose();
                    });
                    return;
            }
        };
        timer.Start();
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
            _ = BeginInvoke(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://share.enterprises")
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
    readonly Theme theme;
    readonly Icon[] icons;
    readonly CoreWebView webView = new();
    readonly ConcurrentQueue<StockTheme> themes = new();
    readonly ConcurrentBag<StockThemeDetail> details = [];
}