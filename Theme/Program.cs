using OpenQA.Selenium.Chrome;

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

    var driver = new ChromeDriver(service, options);

    driver.Navigate().GoToUrl("https://finance.naver.com/sise/theme.naver");

    driver.Quit();
}