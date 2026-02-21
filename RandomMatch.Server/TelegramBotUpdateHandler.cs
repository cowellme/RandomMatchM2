// Services/TelegramBotUpdateHandler.cs

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
// ⬅️ важно!
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using RandomMatch.Server.Models;

namespace RandomMatch.Server.Services;

public class TelegramBotUpdateHandler : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _serviceScopeFactory; // 👈 вместо IUserService
    private readonly ILogger<TelegramBotUpdateHandler> _logger;
    private bool _startUp = true;
    private IEnumerable<TUser> _allUsers = new List<TUser>();

    public TelegramBotUpdateHandler(
        ITelegramBotClient botClient,
        IServiceScopeFactory serviceScopeFactory, // внедряем фабрику
        ILogger<TelegramBotUpdateHandler> logger)
    {
        _botClient = botClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Запуск обработчика Telegram-бота...");

        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation("Бот @{Username} запущен.", me.Username);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        
        if (update.Message?.From == null) return;

        using var scope = _serviceScopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var dateTimeNow = DateTime.Now;
        if (_startUp || dateTimeNow == DateTime.FromBinary(11))
            _allUsers = await userService.GetAllAsync();

        try
        {
            var from = update.Message.From;
            var message = update.Message.Text;
            var user = await userService.GetOrCreateUserAsync(from.Id, from.Username, from.FirstName, from.LastName);

            if (user != null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    await Dialog.TextMessage(_botClient, user, message, update.Message.Photo);
                    await userService.UpdateUser(user);
                }

                if (user.State == StateUser.Search)
                {
                    var pool = _allUsers
                        .Where(x => x.Gender == user.SearchGender &&
                        x.Age == user.SearchAge).ToList();
                }
            }
    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения от Telegram");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Ошибка Telegram API:\n{apiEx.ErrorCode}\n{apiEx.Message}",
            _ => exception.ToString()
        };

        _logger.LogError("Ошибка при получении обновлений: {Error}", errorMessage);
        return Task.CompletedTask;
    }
}