using ShareInvest.Properties;

namespace ShareInvest;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InquiryByStockTheme(new Theme(), [
            Resources.GREEN,
            Resources.WHITE,
            Resources.DARK
        ]));
        GC.Collect();
    }
}