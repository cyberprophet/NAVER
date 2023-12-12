using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using ShareInvest.Entities;
using ShareInvest.Observers;

namespace ShareInvest;

class Theme
{
    internal static IWebElement? GetNextPage(ChromeDriver driver, int nextPage)
    {
        foreach (var element in driver.FindElement(By.XPath("//*[@id=\"contentarea_left\"]/table[2]/tbody/tr")).FindElements(By.TagName("td")))
        {
            var link = element.FindElement(By.TagName("a"));

            if (int.TryParse(link.GetAttribute("href").Split('=')[^1], out int page) && page > nextPage)
            {
                return link;
            }
            continue;
        }
        return null;
    }
    internal void TerminateTheProcess()
    {
        Send?.Invoke(this, new ThemeEventArgs(nameof(TerminateTheProcess)));
    }
    internal IEnumerable<(string url, string themeCode)> GetThemes(ChromeDriver driver)
    {
        foreach (var tr in driver.FindElement(By.XPath("//*[@id=\"contentarea_left\"]/table[1]")).FindElements(By.TagName("tr")))
        {
            var model = new StockTheme
            {

            };
            foreach (var td in tr.FindElements(By.TagName("td")))
            {
                var className = td.GetAttribute("class").Replace("number ", string.Empty).Replace("ls ", string.Empty);

                if ("col_type".Equals(className[..^1]) is false)
                {
                    continue;
                }
                switch (className[^1])
                {
                    case '1':
                        var url = td.FindElement(By.TagName("a")).GetAttribute("href");
                        var themeCode = url.Split('=')[^1];

                        model.ThemeName = td.Text.Trim();
                        model.ThemeCode = themeCode;

                        yield return (url, themeCode);

                        continue;

                    case '2':
                        model.RateCompareToPreviousDay = td.Text.Split('%')[0];
                        continue;

                    case '3':
                        model.AverageRateLastThreeDays = td.Text.Split('%')[0];
                        continue;

                    case '4':

                        if (string.IsNullOrEmpty(model.RisingStockCount))
                        {
                            model.RisingStockCount = td.Text;

                            continue;
                        }
                        if (string.IsNullOrEmpty(model.FlatStockCount))
                        {
                            model.FlatStockCount = td.Text;

                            continue;
                        }
                        model.FallingStockCount = td.Text;
                        continue;

                    case '5':

                        if (td.FindElement(By.TagName("a")).GetAttribute("href").Split('=')[^1] is string firstCode && firstCode.Length == 6)
                        {
                            model.FirstLeadingStockCode = firstCode;
                        }
                        continue;

                    case '6':

                        if (td.FindElement(By.TagName("a")).GetAttribute("href").Split('=')[^1] is string secondCode && secondCode.Length == 6)
                        {
                            model.SecondLeadingStockCode = secondCode;
                        }
                        break;

                    default:

                        continue;
                }
                Send?.Invoke(this, new ThemeEventArgs(model));
            }
        }
    }
    internal void GetStocks(ChromeDriver driver, string code, string url)
    {
        driver.Navigate().GoToUrl(url);

        foreach (var tr in driver.FindElement(By.XPath("//*[@id=\"contentarea\"]/div[4]/table/tbody")).FindElements(By.TagName("tr")))
        {
            var detail = new StockThemeDetail
            {
                ThemeCode = code
            };
            foreach (var td in tr.FindElements(By.TagName("td")))
            {
                var className = td.GetAttribute("class");

                if ("number".Equals(className) || "blank".Equals(className.Split('_')[0]) || "division".Equals(className.Split('_')[0]))
                {
                    break;
                }
                if ("name".Equals(className))
                {
                    detail.StockCode = td.FindElement(By.TagName("a")).GetAttribute("href").Split('=')[^1];
                    detail.MarketClassification = "*".Equals(td.FindElement(By.TagName("span")).Text) ? "10" : "0";

                    continue;
                }
                detail.Description = td.FindElement(By.TagName("div")).FindElement(By.TagName("div")).FindElement(By.TagName("p")).GetAttribute("textContent");
            }
            if (string.IsNullOrEmpty(detail.StockCode))
            {
                continue;
            }
            Send?.Invoke(this, new ThemeEventArgs(detail));
        }
    }
    public event EventHandler<ThemeEventArgs>? Send;
}