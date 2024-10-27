using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Collections.Concurrent;

namespace TelegramBotWebhook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<BotController> _logger;
        private readonly string _mediaDirectory = "F:/DOWNLOADS/MEDIA"; // Ensure this path matches your Docker volume mapping
        private readonly YoutubeClient _youtubeClient;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly string _connectionString;

        // Static cache to store download statuses and results
        private static readonly ConcurrentDictionary<string, string> _downloadCache = new ConcurrentDictionary<string, string>();

        public BotController(ITelegramBotClient botClient, ILogger<BotController> logger, IConfiguration configuration, IBackgroundTaskQueue taskQueue)
        {
            _botClient = botClient;
            _logger = logger;
            _youtubeClient = new YoutubeClient();
            _taskQueue = taskQueue;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null)
            {
                _logger.LogError("Update is null");
                return BadRequest(new { message = "The update field is required." });
            }

            _logger.LogInformation($"Received update: {update.Id}");

            // Ignore messages sent by the bot itself
            if (update.Message?.From?.IsBot == true)
            {
                _logger.LogInformation("Message is from the bot itself, ignoring.");
                return Ok();
            }

            // Handle the update (e.g., message processing)
            if (update.Message != null)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;

                _logger.LogInformation($"Received message from {chatId}: {messageText}");

                // Process the message and handle file type request
                await ProcessMessage(chatId, messageText);
            }
            else if (update.CallbackQuery != null)
            {
                // Handle the callback query
                await ProcessCallbackQuery(update.CallbackQuery);
            }

            return Ok();
        }

        private async Task ProcessMessage(long chatId, string messageText)
        {
            if (messageText.StartsWith("/start"))
            {
                await _botClient.SendTextMessageAsync(chatId, "Welcome to the bot! Please send the YouTube link.");
            }
            else if (Uri.IsWellFormedUriString(messageText, UriKind.Absolute))
            {
                // Assume the message is a YouTube link and ask for the type of file
                await _botClient.SendTextMessageAsync(chatId, "Please specify the type of file:", replyMarkup: GetFileTypeInlineKeyboard());
                // Save the link temporarily (in a more complex bot, you might use a database or state management)
                TemporaryDataStore.SaveLink(chatId, messageText);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "Unrecognized command. Please send a valid YouTube link or specify the file type (audio or video).");
            }
        }

        private async Task ProcessCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var fileType = callbackQuery.Data;

            // Get the previously saved link and download accordingly
            var link = TemporaryDataStore.GetLink(chatId);
            if (link != null)
            {
                _taskQueue.QueueBackgroundWorkItem(async token =>
                {
                    await DownloadFile(chatId, link, fileType, token);
                });

                await _botClient.SendTextMessageAsync(chatId, "Your request is being processed. You will be notified once the file is ready.");
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "No link found. Please send the YouTube link first.");
            }

            // Answer the callback query to remove the loading indicator
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private InlineKeyboardMarkup GetFileTypeInlineKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] // first row
                {
                    InlineKeyboardButton.WithCallbackData("Audio", "audio"),
                    InlineKeyboardButton.WithCallbackData("Video", "video"),
                }
            });
        }

        private async Task DownloadFile(long chatId, string link, string fileType, CancellationToken token)
        {
            string filePath;
            // Notify the user that the download is starting
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            await _botClient.SendTextMessageAsync(chatId, "Starting download...");

            if (fileType == "audio")
            {
                var videoPath = await DownloadVideo(chatId, link, token); // First download the video
                filePath = await ExtractAudio(chatId, videoPath, token); // Then extract audio from the video
            }
            else
            {
                filePath = await DownloadVideo(chatId, link, token);
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                // Save to database
                await SaveToDatabase(link, filePath);

                // Notify the user that the download is complete and the file is being sent
                await _botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
                await _botClient.SendTextMessageAsync(chatId, "Download complete. Sending file...");
                await _botClient.SendDocumentAsync(chatId, new Telegram.Bot.Types.InputFiles.InputOnlineFile(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), Path.GetFileName(filePath)));
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "Failed to download the file.");
            }
        }

        private async Task<string> DownloadVideo(long chatId, string link, CancellationToken token)
        {
            // Check if the link is already in the cache
            if (_downloadCache.TryGetValue(link, out var cachedFilePath))
            {
                _logger.LogInformation($"Using cached file path for link: {link}");
                return cachedFilePath;
            }

            var video = await _youtubeClient.Videos.GetAsync(link);
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
            var videoPath = Path.Combine(_mediaDirectory, $"{video.Title}.mp4");

            // Notify the user that the download is in progress
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            await _botClient.SendTextMessageAsync(chatId, "Downloading video...");

            // Download the video directly to the file path
            await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, videoPath);

            _downloadCache.TryAdd(link, videoPath);
            return videoPath;
        }

        private async Task<string> ExtractAudio(long chatId, string videoPath, CancellationToken token)
        {
            var audioPath = Path.ChangeExtension(videoPath, ".mp3");
            var startInfo = new ProcessStartInfo
            {
                FileName = "F:\\ffmpeg-2024-08-07-git-94165d1b79-essentials_build\\ffmpeg-2024-08-07-git-94165d1b79-essentials_build\\bin\\ffmpeg.exe", // Full path to ffmpeg.exe
                Arguments = $"-i \"{videoPath}\" -q:a 0 -map a \"{audioPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                // Notify the user that the extraction is in progress
                await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                await _botClient.SendTextMessageAsync(chatId, "Extracting audio...");

                process.Start();
                await process.WaitForExitAsync();
            }

            if (System.IO.File.Exists(audioPath)) // Ensure correct namespace usage
            {
                return audioPath;
            }
            else
            {
                _logger.LogError($"Audio extraction failed. File not found: {audioPath}");
                return null;
            }
        }

        private async Task SaveToDatabase(string link, string filePath)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new NpgsqlCommand("INSERT INTO videos (link, file_path) VALUES (@link, @filePath)", connection);
                command.Parameters.AddWithValue("@link", link);
                command.Parameters.AddWithValue("@filePath", filePath);

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    // Temporary data store to hold the link
    public static class TemporaryDataStore
    {
        private static readonly Dictionary<long, string> Links = new Dictionary<long, string>();

        public static void SaveLink(long chatId, string link)
        {
            Links[chatId] = link;
        }

        public static string GetLink(long chatId)
        {
            Links.TryGetValue(chatId, out var link);
            return link;
        }
    }
}
