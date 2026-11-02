using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PinBrowser;

public class MainForm : Form
{
    const double MinOpacity = 0.2; // thấp hơn nữa thì gần như không thấy gì để thao tác lại

    readonly Settings _settings = Settings.Load();
    readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    readonly ToolStrip _bar = new() { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(4, 2, 4, 2) };
    readonly ToolStripButton _back = new("◀") { ToolTipText = "Quay lại", Enabled = false };
    readonly ToolStripButton _forward = new("▶") { ToolTipText = "Tiến tới", Enabled = false };
    readonly ToolStripButton _reload = new("⟳") { ToolTipText = "Tải lại (F5)" };
    readonly ToolStripTextBox _address = new() { AutoSize = false, Width = 360, ToolTipText = "Nhập URL hoặc từ khóa rồi Enter (Ctrl+L)" };
    readonly ToolStripButton _pin = new("📌 Ghim") { CheckOnClick = true, ToolTipText = "Luôn nằm trên mọi cửa sổ khác (Ctrl+T)" };
    readonly ToolStripLabel _opacityLabel = new("Độ trong suốt:");
    readonly TrackBar _opacityTrack = new()
    {
        Minimum = (int)(MinOpacity * 100), Maximum = 100, TickStyle = TickStyle.None,
        AutoSize = false, Width = 130, Height = 24,
    };
    readonly ToolStripLabel _opacityValue = new("100%") { AutoSize = false, Width = 40 };

    public MainForm()
    {
        Text = "PinBrowser";
        MinimumSize = new Size(360, 200);
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;

        var track = new ToolStripControlHost(_opacityTrack) { AutoSize = false, Width = 130, Height = 24 };
        _bar.Items.AddRange(new ToolStripItem[]
        {
            _back, _forward, _reload, _address,
            new ToolStripSeparator(), _pin,
            new ToolStripSeparator(), _opacityLabel, track, _opacityValue,
        });
        _bar.Dock = DockStyle.Top;
        _bar.Resize += (_, _) => FitAddressBar(); // thanh địa chỉ co giãn theo chiều rộng cửa sổ

        Controls.Add(_web);
        Controls.Add(_bar);

        ApplySettings();
        WireEvents();

        Load += async (_, _) => await InitBrowserAsync();
        FormClosing += (_, _) => SaveSettings();
    }

    void ApplySettings()
    {
        Size = new Size(Math.Max(_settings.Width, MinimumSize.Width), Math.Max(_settings.Height, MinimumSize.Height));

        // Vị trí đã lưu có thể nằm ngoài màn hình (đổi màn/độ phân giải) → đặt giữa màn hình chính
        var saved = new Rectangle(_settings.X, _settings.Y, Width, Height);
        bool onScreen = _settings.X >= 0 && Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(saved));
        if (onScreen)
        {
            Location = saved.Location;
        }
        else
        {
            var area = Screen.PrimaryScreen!.WorkingArea;
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        }

        var opacity = Math.Clamp(_settings.Opacity, MinOpacity, 1.0);
        _opacityTrack.Value = (int)Math.Round(opacity * 100);
        SetOpacity(opacity);

        _pin.Checked = _settings.Pinned;
        TopMost = _settings.Pinned;
        UpdatePinText();
    }

    void WireEvents()
    {
        _back.Click += (_, _) => { if (_web.CanGoBack) _web.GoBack(); };
        _forward.Click += (_, _) => { if (_web.CanGoForward) _web.GoForward(); };
        _reload.Click += (_, _) => _web.Reload();

        _address.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Navigate(_address.Text);
        };

        _pin.CheckedChanged += (_, _) =>
        {
            TopMost = _pin.Checked;
            UpdatePinText();
        };

        _opacityTrack.ValueChanged += (_, _) => SetOpacity(_opacityTrack.Value / 100.0);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F5) { _web.Reload(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.L) { _address.Focus(); _address.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.T) { _pin.Checked = !_pin.Checked; e.Handled = true; }
        };

        Shown += (_, _) => FitAddressBar();
    }

    async Task InitBrowserAsync()
    {
        try
        {
            // Profile riêng trong AppData để không ghi vào thư mục chạy
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PinBrowser", "WebView2");
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: dataDir);
            await _web.EnsureCoreWebView2Async(env);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Không khởi tạo được WebView2. Cần cài Microsoft Edge WebView2 Runtime:\n" +
                "https://developer.microsoft.com/microsoft-edge/webview2/\n\n" + ex.Message,
                "PinBrowser", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var core = _web.CoreWebView2;
        core.NewWindowRequested += (_, e) => { e.Handled = true; core.Navigate(e.Uri); }; // target=_blank → mở trong cùng cửa sổ
        core.SourceChanged += (_, _) =>
        {
            if (!_address.Focused) _address.Text = core.Source;
        };
        core.HistoryChanged += (_, _) =>
        {
            _back.Enabled = _web.CanGoBack;
            _forward.Enabled = _web.CanGoForward;
        };
        core.DocumentTitleChanged += (_, _) =>
            Text = string.IsNullOrWhiteSpace(core.DocumentTitle) ? "PinBrowser" : $"{core.DocumentTitle} - PinBrowser";

        Navigate(_settings.Url);
    }

    void Navigate(string input)
    {
        if (_web.CoreWebView2 is null) return;
        var text = input.Trim();
        if (text.Length == 0) return;

        string url;
        if (Uri.TryCreate(text, UriKind.Absolute, out var abs) &&
            (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps || abs.Scheme == Uri.UriSchemeFile || abs.Scheme == "about"))
            url = text;
        else if (!text.Contains(' ') && (text.Contains('.') || text.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)))
            url = "https://" + text;
        else
            url = "https://www.google.com/search?q=" + Uri.EscapeDataString(text);

        _web.CoreWebView2.Navigate(url);
        _web.Focus();
    }

    void SetOpacity(double value)
    {
        Opacity = value;
        _opacityValue.Text = $"{(int)Math.Round(value * 100)}%";
    }

    void UpdatePinText() => _pin.Text = _pin.Checked ? "📌 Đã ghim" : "📌 Ghim";

    void FitAddressBar()
    {
        int others = _bar.Items.Cast<ToolStripItem>().Where(i => i != _address).Sum(i => i.Width + i.Margin.Horizontal);
        _address.Width = Math.Max(120, _bar.Width - others - _bar.Padding.Horizontal - 16);
    }

    void SaveSettings()
    {
        var b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _settings.X = b.X; _settings.Y = b.Y; _settings.Width = b.Width; _settings.Height = b.Height;
        _settings.Opacity = Opacity;
        _settings.Pinned = TopMost;
        var src = _web.CoreWebView2?.Source;
        if (!string.IsNullOrEmpty(src) && src != "about:blank") _settings.Url = src;
        _settings.Save();
    }
}
