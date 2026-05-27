using System.Text.Json;

namespace WebFunControl.Services;

/// <summary>
/// 设备配对持久化服务 — 将已连接过的设备保存到本地
/// </summary>
public class DeviceStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WebFunControl", "paired_devices.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>已配对设备列表</summary>
    public List<Models.PairedDevice> Devices { get; private set; } = [];

    /// <summary>加载已保存的设备</summary>
    public void Load()
    {
        try
        {
            if (File.Exists(StorePath))
            {
                var json = File.ReadAllText(StorePath);
                Devices = JsonSerializer.Deserialize<List<Models.PairedDevice>>(json, JsonOptions) ?? [];
            }
        }
        catch
        {
            Devices = [];
        }
    }

    /// <summary>持久化保存</summary>
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(Devices, JsonOptions);
            File.WriteAllText(StorePath, json);
        }
        catch { }
    }

    /// <summary>添加或更新设备</summary>
    public void AddOrUpdate(string id, string name)
    {
        var existing = Devices.FirstOrDefault(d => d.Id == id);
        if (existing != null)
        {
            existing.Name = name;
        }
        else
        {
            Devices.Add(new Models.PairedDevice { Id = id, Name = name });
        }
        Save();
    }

    /// <summary>移除设备</summary>
    public void Remove(string id)
    {
        Devices.RemoveAll(d => d.Id == id);
        Save();
    }
}
