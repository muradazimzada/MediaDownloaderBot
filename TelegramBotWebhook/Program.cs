using Newtonsoft.Json.Serialization;
using Telegram.Bot;

namespace TelegramBotWebhook
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                };
            });

            // Add background task queue and hosted service
            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            builder.Services.AddHostedService<QueuedHostedService>();

            // Configure Telegram Bot Client
            builder.Services.AddSingleton<ITelegramBotClient>(provider =>
                new TelegramBotClient(builder.Configuration["TelegramBotToken"]));

            // Add logging middleware
            //builder.Services.add<RequestResponseLoggingMiddleware>();

            // Add Swagger for API documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseMiddleware<RequestResponseLoggingMiddleware>(); // Register the middleware here
            app.MapControllers();
            app.Run();
        }
    }
}
