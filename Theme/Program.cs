using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using ShareInvest;

using (var service = ChromeDriverService.CreateDefaultService())
{
    var options = new ChromeOptions
    {

    };
#if DEBUG

#else
    options.AddArgument("--headless");
#endif
    options.AddArgument("--window-size=1920,1080");

    using (var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(0x40)))
    {
        int page = 1;

        driver.Navigate().GoToUrl($"https://finance.naver.com/sise/theme.naver");

        static IWebElement? getNextPage(ChromeDriver driver, int nextPage)
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
        static void getThemeModel(ChromeDriver driver)
        {
            foreach (var tr in driver.FindElement(By.XPath("//*[@id=\"contentarea_left\"]/table[1]")).FindElements(By.TagName("tr")))
            {
                var model = new ThemeModel();

                foreach (var td in tr.FindElements(By.TagName("td")))
                {
                    var className = td.GetAttribute("class").Replace("number ", string.Empty).Replace("ls ", string.Empty);

                    if (!"col_type".Equals(className[..^1]))
                    {
                        continue;
                    }
                    switch (className[^1])
                    {
                        case '1':
                            model.ThemeName = td.Text.Trim();
                            model.ThemeCode = td.FindElement(By.TagName("a")).GetAttribute("href").Split('=')[^1];
                            continue;

                        case '2':
                            model.RateCompareToPreviousDay = td.Text.Split('%')[0];
                            continue;

                        case '3':
                            model.AverageRateLast3Days = td.Text.Split('%')[0];
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
                    Console.WriteLine(new
                    {
                        model.ThemeCode,
                        model.ThemeName
                    });
                }
            }
        }
        while (getNextPage(driver, page++) is IWebElement nextPage)
        {
            getThemeModel(driver);

            nextPage.Click();

            Console.WriteLine(page);
        }
        getThemeModel(driver);

        driver.Quit();
    }
}