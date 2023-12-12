using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using ShareInvest;

using (var service = ChromeDriverService.CreateDefaultService())
{
    var queue = new Queue<ThemeModel>();

    var options = new ChromeOptions
    {

    };
    options.AddArguments("--headless", "--window-size=1920,1080", "user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

    int page = 1, length = 0;

    using (var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(0x40)))
    {
        driver.Navigate().GoToUrl($"https://finance.naver.com/sise/theme.naver");

        IWebElement? getNextPage(ChromeDriver driver, int nextPage)
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
        void getThemeModel(ChromeDriver driver)
        {
            foreach (var tr in driver.FindElement(By.XPath("//*[@id=\"contentarea_left\"]/table[1]")).FindElements(By.TagName("tr")))
            {
                var model = new ThemeModel
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
                            var href = td.FindElement(By.TagName("a")).GetAttribute("href");

                            model.ThemeName = td.Text.Trim();
                            model.ThemeCode = href.Split('=')[^1];
                            model.DetailGroupUrl = href;
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
                        model.ThemeName,
                        model.DetailGroupUrl
                    });
                    queue.Enqueue(model);
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

        while (queue.TryDequeue(out ThemeModel? model))
        {
            driver.Navigate().GoToUrl(model.DetailGroupUrl);

            foreach (var tr in driver.FindElement(By.XPath("//*[@id=\"contentarea\"]/div[4]/table/tbody")).FindElements(By.TagName("tr")))
            {
                var detail = new ThemeDetail
                {

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
                        var link = td.FindElement(By.TagName("a"));

                        detail.Name = link.Text;
                        detail.Code = link.GetAttribute("href").Split('=')[^1];
                        detail.Classification = "*".Equals(td.FindElement(By.TagName("span")).Text) ? "10" : "0";

                        continue;
                    }
                    detail.Description = td.FindElement(By.TagName("div")).FindElement(By.TagName("div")).FindElement(By.TagName("p")).GetAttribute("textContent");
                }
                if (string.IsNullOrEmpty(detail.Name))
                {
                    continue;
                }
                Console.WriteLine(new
                {
                    detail.Code,
                    detail.Name,
                    detail.Classification,
                    detail.Description,
                    detail.Description?.Length
                });
                if (length < detail.Description?.Length)
                {
                    length = detail.Description.Length;
                }
            }
        }
        driver.Close();
    }
    Console.WriteLine(length);
}