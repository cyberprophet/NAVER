using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using ShareInvest.Entities;
using ShareInvest.Properties;

namespace ShareInvest;

partial class InquiryByStockTheme : Form
{
    internal InquiryByStockTheme(string accessToken, Icon[] icons)
    {
        this.accessToken = accessToken;
        this.icons = icons;

        InitializeComponent();

        theme = new Theme();

        theme.Send += (_, e) =>
        {
            switch (e.Convey)
            {
                case StockTheme t:
                    notifyIcon.Text = t.ThemeName;
                    break;

                case StockThemeDetail td:
                    notifyIcon.Text = td.Description?.Length > 127 ? td.Description[..127] : td.Description;
                    break;

                case string:
                    _ = BeginInvoke(Dispose);
                    return;
            }

        };
        timer.Start();
    }
    void TimerTick(object _, EventArgs e)
    {
        if (FormBorderStyle.Sizable == FormBorderStyle && FormWindowState.Minimized != WindowState)
        {
            _ = Task.Run(() =>
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
    DialogResult IsCancelled
    {
        get => MessageBox.Show(Resources.WARNING.Replace('|', '\n'), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
    }
    readonly Theme theme;
    readonly Icon[] icons;
    readonly string accessToken;
}