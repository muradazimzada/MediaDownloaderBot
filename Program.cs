using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Npgsql;

class Program
{
    private static ITelegramBotClient botClient;
    private static YoutubeClient youtubeClient = new YoutubeClient();
    private static string connectionString = "Host=localhost;Username=your_user;Password=your_password;Database=media_downloader";

    static async Task Main()
    {
        botClient = new TelegramBotClient("7265537695:AAFHEnYpRMcQoyjhOwSzFJKNtWUH60-vGto");

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions
        );

        Console.ReadLine();
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;
        var link = message.Text;

        try
        {
            var filePath = await DownloadVideo(link);
            SaveToDatabase(link, filePath);
            await botClient.SendTextMessageAsync(chatId, "Download complete. Sending file...");
            await botClient.SendDocumentAsync(chatId, new InputOnlineFile(new FileStream(filePath, FileMode.Open), Path.GetFileName(filePath)));
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(chatId, $"Failed to download video: {ex.Message}");
        }
    }

    static async Task<string> DownloadVideo(string url)
    {
        var video = await youtubeClient.Videos.GetAsync(url);
        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

        var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
        var filePath = $"D:/downloads/{video.Title}.{streamInfo.Container}";

        await youtubeClient.Videos.Streams.DownloadAsync(streamInfo, filePath);
        return filePath;
    }

    static void SaveToDatabase(string link, string filePath)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = new NpgsqlCommand("INSERT INTO videos (link, file_path) VALUES (@link, @filePath)", connection);
        command.Parameters.AddWithValue("link", link);
        command.Parameters.AddWithValue("filePath", filePath);
        command.ExecuteNonQuery();
    }
}
