namespace ProxyServer;

public static class Logger
{
    public static void Log(string message, ConsoleColor color = ConsoleColor.White, bool newLine = true)
    {
        Console.ForegroundColor = color;
        if (newLine) Console.WriteLine(message);
        else Console.Write(message);
        Console.ResetColor();
    }
}