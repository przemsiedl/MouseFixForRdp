using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

public class MainForm : Form
{
    private const int SyncCursorHotkeyId = 1;
    private const int ShowCursorHotkeyId = 2;
    private const int WM_HOTKEY = 0x0312;

    public MainForm(AppConfig config)
    {
        this.FormBorderStyle = FormBorderStyle.FixedSingle;

        this.config = config;
        cursorOverlayEnabled = config.ShowCursorOverlay;

        this.Text = "CurSync";
        this.Size = new Size(350, 100);
        this.MaximizeBox = false;

        if (config.ServerMode)
        {
            if (!Enum.TryParse(config.SyncCursorHotkey, out Keys syncCursorHotkey))
            {
                syncCursorHotkey = Keys.F9;
            }
            RegisterHotKey(this.Handle, SyncCursorHotkeyId, 0, (uint)syncCursorHotkey);


            if (!Enum.TryParse(config.CursorOverlayHotkey, out Keys overlayHotkey))
            {
                syncCursorHotkey = Keys.F9;
            }
            RegisterHotKey(this.Handle, ShowCursorHotkeyId, 0, (uint)overlayHotkey);
        }

        overlayForm = new OverlayCursorForm();
        overlayForm.Visible = config.ShowCursorOverlay;

        overlayTimer = new System.Windows.Forms.Timer();
        overlayTimer.Interval = config.CursorOverlayInterval;
        overlayTimer.Tick += OverlayTimer_Tick;
        overlayTimer.Start();

        if (config.ServerMode)
        {
            networkThread = new Thread(new ThreadStart(RunServer));
            networkThread.IsBackground = true;
            networkThread.Start();
        }
        else if (!config.ServerMode)
        {
            networkThread = new Thread(new ThreadStart(RunClient));
            networkThread.IsBackground = true;
            networkThread.Start();
        }
    }

    private AppConfig config;

    private bool syncCursorEnabled = false;
    private bool cursorOverlayEnabled = false;

    private OverlayCursorForm overlayForm;
    private System.Windows.Forms.Timer overlayTimer;

    private Thread networkThread;

    List<TcpClient> Clients = new List<TcpClient>();

    TcpListener tcpListener;

    private void OverlayTimer_Tick(object sender, EventArgs e)
    {
        Point pos = Cursor.Position;
        overlayForm.Location = new Point(pos.X + 2, pos.Y + 2);
    }

    private void RunServer()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, config.Port);
            tcpListener.Start();
            Thread syncThread = new(new ThreadStart(ServerClientSync));
            syncThread.IsBackground = true;
            syncThread.Start();

            while (true)
            {
                var client = tcpListener.AcceptTcpClient();
                Clients.Add(client);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Server error: " + ex.Message);
        }
    }

    private void ServerClientSync()
    {
        List<TcpClient> clientsToRemove = new();
        while (true)
        {
            Point pos = Cursor.Position;
            string message = $"{pos.X},{pos.Y},{(syncCursorEnabled ? 1 : 0)},{(cursorOverlayEnabled ? 1 : 0)}\n";
            byte[] data = Encoding.ASCII.GetBytes(message);

            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    try
                    {
                        client.GetStream().Write(data, 0, data.Length);
                    }
                    catch (Exception e)
                    {
                        clientsToRemove.Add(client);
                    }
                }

                foreach (var client in clientsToRemove)
                {
                    Clients.Remove(client);
                }
            }

            clientsToRemove.Clear();
            Thread.Sleep(config.SyncCursorInterval);
        }
    }

    private void RunClient()
    {
        try
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect(config.ServerAddress, config.Port);
            using (StreamReader reader = new(tcpClient.GetStream()))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line != null)
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length == 4)
                        {
                            if (int.TryParse(parts[0], out int x) &&
                                int.TryParse(parts[1], out int y) &&
                                int.TryParse(parts[2], out int sync) &&
                                int.TryParse(parts[3], out int showCur))
                            {
                                if (cursorOverlayEnabled != showCur > 0 || syncCursorEnabled != sync > 0)
                                {
                                    syncCursorEnabled = sync > 0;
                                    cursorOverlayEnabled = showCur > 0;

                                    this.Invoke((MethodInvoker)(() =>
                                    {
                                        overlayForm.Visible = cursorOverlayEnabled;
                                        this.Text = $"CurSync: {(syncCursorEnabled ? "YES" : "NO")}, ShowCursor: {(cursorOverlayEnabled ? "YES" : "NO")}";
                                    }));
                                }

                                if (syncCursorEnabled)
                                {
                                    Cursor.Position = new Point(x, y);
                                }
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Błąd klienta: " + ex.Message);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            if (m.WParam.ToInt32() == SyncCursorHotkeyId)
            {
                syncCursorEnabled = !syncCursorEnabled;
            }
            if (m.WParam.ToInt32() == ShowCursorHotkeyId)
            {
                cursorOverlayEnabled = !cursorOverlayEnabled;
            }

            this.Invoke((MethodInvoker)(() =>
            {
                this.Text = $"CurSync: {(syncCursorEnabled ? "YES" : "NO")}, ShowCursor: {(cursorOverlayEnabled ? "YES" : "NO")}";
            }));
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        UnregisterHotKey(this.Handle, SyncCursorHotkeyId);
        UnregisterHotKey(this.Handle, ShowCursorHotkeyId);

        foreach (var client in Clients)
        {
            try { client.GetStream().Close(); } catch { }
            try { client.GetStream().Close(); } catch { }
        }
        try { tcpListener?.Stop(); } catch { }
    }
}
