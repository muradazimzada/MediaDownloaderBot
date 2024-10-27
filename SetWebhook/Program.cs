using System;
using System.Threading.Tasks;
using Telegram.Bot;

class Program
{
    static async Task Main(string[] args)
    {
        string token = "7265537695:AAFHEnYpRMcQoyjhOwSzFJKNtWUH60-vGto";
        string ngrokUrl = "https://9429-45-15-43-232.ngrok-free.app/api/bot";

        var botClient = new TelegramBotClient(token);

        await botClient.SetWebhookAsync(ngrokUrl);

        Console.WriteLine("Webhook set successfully");
    }
}
