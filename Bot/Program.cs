using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bot;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var client = new TelegramBotClient(Environment.GetEnvironmentVariable("BOT_TOKEN")!);

client.StartReceiving(HandleMessage,
    (_, ex, _) =>
    {
        logger.Error(ex, "There was an error");
        return Task.CompletedTask;
    });

logger.Information("Bot started");
await Task.Delay(-1);

return;

async Task HandleMessage(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    if (update.Message is null)
    {
        return;
    }
    
    if (update.Message is not
        {
            Document: { MimeType: MediaTypeNames.Application.Json } file, 
            Caption: { } password
        })
    {
        const string errorMessage = "Прикрепите к сообщению файл настроек и напишите текстом его мастер-пароль";
        await bot.SendMessage(update.Message.Chat.Id, errorMessage, cancellationToken: ct);
        return;
    }

    var jsonFile = await bot.GetFile(file.FileId, ct);
    var jsonStream = new MemoryStream();
    await bot.DownloadFile(jsonFile, jsonStream, ct);
    jsonStream.Seek(0, SeekOrigin.Begin);

    var prefs = (await JsonSerializer.DeserializeAsync<JsonObject>(jsonStream, cancellationToken: ct))!;
    
    Preferences.Decode(prefs.AsObject(), password);
    Preferences.Fix(prefs["content"]!.AsObject());
    Preferences.Encode(prefs.AsObject(), password);

    prefs = HashUtil.NormalizeJsonStructure(prefs);
    var fixedJson = HashUtil.CalculateAndSetFileHash(prefs.ToString());
    var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_fixed.json";
    
    var resultStream = new MemoryStream(Encoding.UTF8.GetBytes(fixedJson));
    await bot.SendDocument(update.Message.Chat.Id, new InputFileStream(resultStream, fileName), cancellationToken: ct);
}