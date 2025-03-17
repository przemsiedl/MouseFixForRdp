using System.Text.Json;

public class AppConfig
{
    public bool ServerMode { get; set; }
    public string ServerAddress { get; set; }
    public int Port { get; set; }
    
    public int SyncCursorInterval { get; set; }
    public string SyncCursorHotkey { get; set; }
    
    public bool ShowCursorOverlay { get; set; }
    public string CursorOverlayHotkey { get; set; }
    public int CursorOverlayInterval { get; set; }

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new Exception("Configuration file does not exists" + path);
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json);
    }
}

