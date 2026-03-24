namespace ProxyServer;

public class ProxyStatistics
{
    public int Port;
    public long Sent;
    public long Received;
    
    public DateTime StartTime;
    
    public TimeSpan Uptime => DateTime.UtcNow - StartTime;
    
    public string GetFormattedSize(long bytes)
    {
        string[] suffixes = { "BYTE", "KB", "MB", "GB", "TB" };
        double formattedSize = bytes;
        int counter = 0;

        while (formattedSize >= 1024 && counter < suffixes.Length - 1)
        {
            counter++;
            formattedSize /= 1024;
        }
        
        return $"{(counter == 0 ? formattedSize : Math.Round(formattedSize, 2))} {suffixes[counter]}";
    }
}