using ShareInvest.Properties;

namespace ShareInvest;

static class Program
{
    [STAThread]
    static void Main(string[] arg)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InquiryByStockTheme(arg[0],
        [
            Resources.GREEN,
            Resources.WHITE,
            Resources.DARK
        ]));
        GC.Collect();
    }
}