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
        {
            List<TUser> _bots = CreateBots();
            _allUsers = _bots; //await userService.GetAllAsync();
            _startUp = false;
        }

        try
        {
            var from = update.Message.From;
            var message = update.Message.Text;
            var user = await userService.GetOrCreateUserAsync(from.Id, from.Username, from.FirstName, from.LastName);

            if (user != null)
            {
                await Dialog.TextMessage(_botClient, user, message, update.Message.Photo);
                
                if(user.State == StateUser.SearchMessage)
                {
                    user.State = StateUser.Search;
                    var persone = LastPersone(user);
                    if (persone != null)
                    {
                        persone.Like(_botClient, user, message);
                        await _botClient.SendMessage(user.ChatId, "Сообщение отправленно!");
                    }
                }

                if (user.State == StateUser.Search)
                {
                    var pool = _allUsers
                        .Where(x => x.Gender == user.SearchGender &&
                        x.Age == user.SearchAge).ToList();


                    switch (message)
                    {
                        case "❤️":
                            
                            var persone = LastPersone(user);
                            if (persone != null)
                            {
                                persone.Like(_botClient, user);
                                await _botClient.SendMessage(user.ChatId, $"Лайк отправлен: {persone.FirstName}, ждем ответа");
                                goto default;
                            }
                            
                            break;
                        case "💌 / 📹":
                            user.State = StateUser.SearchMessage;
                            persone = LastPersone(user);
                            if (persone != null)
                            {
                                await _botClient.SendMessage(user.ChatId, $"Напиши сообщение для: {persone.FirstName}");
                            }
                            break;
                        case "💤":
                            user.State = StateUser.Stop;
                            await _botClient.SendMessage(user.ChatId, $"1. Продолжить просмотр анкет\n" +
                                $"2. Моя анкета\n" +
                                $"3. Выключить моя анкету");
                            break;
                        default:
                            TUser? person;
                            if (!string.IsNullOrEmpty(user.Viewed))
                            {
                                person = pool.FirstOrDefault(x => !user.Viewed.Contains($"{x.ChatId};"));
                            }
                            else
                            {
                                person = pool.First();
                            }

                            await Dialog.ViewProfile(_botClient, user, person);
                            break;
                    }

                }
                await userService.UpdateUser(user);
            }
    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения от Telegram");
        }
    }

    private TUser? LastPersone(TUser user)
    {
        var chatIds = user.Viewed?.Split(";");
        var chatId = chatIds?.Last(x => !string.IsNullOrEmpty(x));
        if (long.TryParse(chatId, out var result))
        {
            var persone = _allUsers.FirstOrDefault(x => x.ChatId == result);
            return persone;
        }
        return null;
    }

    private List<TUser> CreateBots()
    {
        var result = new List<TUser>();
        for(int i = 0; i < 500; i++)
        {
            var rnd = new Random();
            result.Add(new TUser
            {
                FirstName = $"test_{i}",
                LastName = $"test_{i}",
                Age = rnd.Next(16, 25),
                AboutMe = $"{i}",
                Gender = rnd.Next(0,1) > 0 ? GenderUser.Man : GenderUser.Woman,
                ChatId = i
            });
        }
        return result;
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