using Microsoft.UI.Xaml;

namespace CoachPadWin;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            _ = new App();
        });
    }
}
