using System.Text.Json;

namespace ProxyServer;

public class ProxySettingsLoader
{
    private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public ProxySettings Load()
    {
        try 
        {
            if (!File.Exists(_path))
            {
                var defaultSettings = new ProxySettings(); 
                Save(defaultSettings);
                
                return defaultSettings;
            }

            var jsonString = File.ReadAllText(_path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            return JsonSerializer.Deserialize<ProxySettings>(jsonString, options) ?? new ProxySettings();
        }
        catch (Exception ex)
        {
            Logger.Log($"[SETTINGS] LOAD SETTINGS ERROR {_path}: {ex}", ConsoleColor.Red);
            return new ProxySettings(); 
        }
    }
    
    public void Save(ProxySettings settings)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonString = JsonSerializer.Serialize(settings, options);
        
        File.WriteAllText(_path, jsonString);
    }
}