// Program.cs
using System;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Wczytanie konfiguracji z pliku config.json
        AppConfig config = AppConfig.Load("config.json");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(config));
    }
}
