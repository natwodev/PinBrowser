using System.Text.Json;

namespace PinBrowser;

/// <summary>Trạng thái lưu giữa các lần mở: URL, độ trong suốt, ghim, vị trí/kích thước.</summary>
class Settings
{
    public string Url { get; set; } = "https://www.google.com";
    public double Opacity { get; set; } = 1.0;
    public bool Pinned { get; set; } = true;
    public int X { get; set; } = -1;
    public int Y { get; set; } = -1;
    public int Width { get; set; } = 900;
    public int Height { get; set; } = 650;

    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PinBrowser", "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch { /* file hỏng thì dùng mặc định */ }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch { /* không chặn việc thoát app vì lỗi ghi file */ }
    }
}
