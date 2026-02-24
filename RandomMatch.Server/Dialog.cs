using RandomMatch.Server.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace RandomMatch.Server.Services;

internal class Dialog
{
    private static ReplyKeyboardMarkup mainKeyboard = new ReplyKeyboardMarkup(new KeyboardButton("❤️"), new KeyboardButton("💌 / 📹"), new KeyboardButton("👎"), new KeyboardButton("⬅️"), new KeyboardButton("💤")) { ResizeKeyboard = true};
    public static async Task TextMessage(ITelegramBotClient bot, TUser user, string message, PhotoSize[]? photo = null)
    {
        var chatId = user.ChatId;
        switch (user.State)
        {
            #region New
            case StateUser.New0:
                switch (message)
                {
                    case "/start":
                        user.State = StateUser.New01;
                        await bot.SendMessage(chatId, "Укажи какой возраст ищем:");
                        break;

                    default:
                        await bot.SendMessage(chatId, "Не распознанная команда");
                        break;
                }
                break;
            case StateUser.New01:
                if (int.TryParse(message, out var searchAge) && searchAge >= 16)
                {
                    user.State = StateUser.New1;
                    user.SearchAge = searchAge;
                    await bot.SendMessage(chatId, "Укажи свой возраст:");
                }
                else
                {
                    await bot.SendMessage(chatId, "Используй целые числа!");
                }
                break;
            case StateUser.New1:
                if (int.TryParse(message, out var age) && age >= 16)
                {
                    user.State = StateUser.New2;
                    user.Age = age;
                    await bot.SendMessage(chatId, "Укажи свой пол:", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Муж."), new KeyboardButton("Жен.")) { ResizeKeyboard = true});
                }
                else
                {
                    await bot.SendMessage(chatId, "Используй целые числа!");
                }
                break;
            case StateUser.New2:
                switch (message.ToLower())
                {
                    case "муж.":
                        user.State = StateUser.New3;
                        user.Gender = GenderUser.Man;
                        await bot.SendMessage(chatId, "Укажи кого ищем:", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Муж."), new KeyboardButton("Жен.")) { ResizeKeyboard = true });
                        break;
                    case "жен.":
                        user.State = StateUser.New3;
                        user.Gender = GenderUser.Woman;
                        await bot.SendMessage(chatId, "Укажи кого ищем:", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Муж."), new KeyboardButton("Жен.")) { ResizeKeyboard = true });
                        break;
                    default:
                        await bot.SendMessage(chatId, "Укажи с помощью кнопок", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Муж."), new KeyboardButton("Жен.")) { ResizeKeyboard = true });
                        break;
                }
                break;
            case StateUser.New3:
                ReplyKeyboardMarkup? keyboardCity = null;
                if (!string.IsNullOrEmpty(user.City))
                {
                    keyboardCity = new ReplyKeyboardMarkup(new KeyboardButton(user.City) ) { ResizeKeyboard = true};
                }
                switch (message.ToLower())
                {
                    case "муж.":
                        user.State = StateUser.New4;
                        user.SearchGender = GenderUser.Man;
                        await bot.SendMessage(chatId, "Напиши свой город:", replyMarkup: keyboardCity == null ? ReplyMarkup.RemoveKeyboard : keyboardCity);
                        break;
                    case "жен.":
                        user.State = StateUser.New4;
                        user.SearchGender = GenderUser.Woman;
                        await bot.SendMessage(chatId, "Напиши свой город:", replyMarkup: keyboardCity == null ? ReplyMarkup.RemoveKeyboard : keyboardCity);
                        break;
                    default:
                        await bot.SendMessage(chatId, "Укажи с помощью кнопок", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Муж."), new KeyboardButton("Жен.")) { ResizeKeyboard = true });
                        break;
                }
                break;
            case StateUser.New4:
                ReplyKeyboardMarkup? keyboardName = null;
                if (!string.IsNullOrEmpty(user.FirstName))
                {
                    keyboardName = new ReplyKeyboardMarkup(new KeyboardButton(user.FirstName)) { ResizeKeyboard = true };
                }
                user.State = StateUser.New5;
                user.City = message;
                await bot.SendMessage(chatId, "Как к тебе обращаться?", replyMarkup: keyboardName == null ? ReplyMarkup.RemoveKeyboard : keyboardName);
                break;
            case StateUser.New5:
                ReplyKeyboardMarkup? keyboardAboutMe = null;
                if (!string.IsNullOrEmpty(user.AboutMe))
                {
                    keyboardAboutMe = new ReplyKeyboardMarkup(new KeyboardButton("Оставить текущее")) { ResizeKeyboard = true };
                }
                user.State = StateUser.New6; // скип фото
                user.FirstName = message;
                await bot.SendMessage(chatId, "Напиши о себе:", replyMarkup: keyboardAboutMe == null ? ReplyMarkup.RemoveKeyboard : keyboardAboutMe);
                break;
            case StateUser.New6:
                ReplyKeyboardMarkup? keyboardPhoto = null;
                if (!string.IsNullOrEmpty(user.PhotoId))
                {
                    keyboardPhoto = new ReplyKeyboardMarkup(new KeyboardButton("Оставить текущее")) { ResizeKeyboard = true };
                }
                user.State = StateUser.New7;
                user.AboutMe = message == "Оставить текущее" ? user.AboutMe : message;
                await bot.SendMessage(chatId, "Пришли своё фото:", replyMarkup: keyboardPhoto == null ? ReplyMarkup.RemoveKeyboard : keyboardPhoto);
                break;
            case StateUser.New7:
                if (photo != null)
                {
                    user.State = StateUser.New8;
                    user.PhotoId = photo[0].FileId;
                    await SendProfile(bot, user).ConfigureAwait(false);
                    await bot.SendMessage(chatId, "Сохранить анкету?", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Да"), new KeyboardButton("Нет")) { ResizeKeyboard = true });
                }
                else if (message.ToLower() == "оставить текущее")
                {
                    user.State = StateUser.New8;
                    await SendProfile(bot, user).ConfigureAwait(false);
                    await bot.SendMessage(chatId, "Сохранить анкету?", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Да"), new KeyboardButton("Нет")) { ResizeKeyboard = true });
                }
                break;
            case StateUser.New8:
                switch (message.ToLower())
                {
                    case "да":
                        user.State = StateUser.Search;
                        await bot.SendMessage(chatId, "Анкета успешно сохранена!", replyMarkup: mainKeyboard);
                        break;
                    case "нет":
                        user.State = StateUser.Stop;
                        await bot.SendMessage(chatId, "Анкета отключена!", replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Заполнить анкету")) { ResizeKeyboard = true });
                        break;
                    default: 
                        break;
                }
                break;

            #endregion

            case StateUser.Stop:
                switch (message.ToLower())
                {
                    case "1":
                        user.State = StateUser.Search;
                        break;
                    
                    case "2":
                        user.State = StateUser.MyProfile;
                        await SendProfile(bot, user);
                        break;
                    case "3":
                        user.State = StateUser.MyProfile;
                        
                        break;
                    case "4":
                        user.State = StateUser.New5;
                        break;
                }    
                break;

            case StateUser.MyProfile:
                switch (message)
                {
                    case "2":
                        user.State = StateUser.New01;
                        await bot.SendMessage(chatId, "Укажи какой возраст ищем:");
                        break;
                    case "3":
                        user.State = StateUser.New7;
                        keyboardPhoto = null;
                        if (!string.IsNullOrEmpty(user.PhotoId))
                        {
                            keyboardPhoto = new ReplyKeyboardMarkup(new KeyboardButton("Оставить текущее")) { ResizeKeyboard = true };
                        }
                        await bot.SendMessage(chatId, "Пришли своё фото:", replyMarkup: keyboardPhoto == null ? ReplyMarkup.RemoveKeyboard : keyboardPhoto);
                        break;
                }
                break;

            case StateUser.Search:
                switch (message)
                {
                    
                }
                break;
            
        }
    }

    public static async Task SendProfile(ITelegramBotClient bot, TUser user)
    {
        //if (user.State != StateUser.Search) return;
        var message = $"Так выглядит твоя анкета:\n{user.FirstName}, {user.Age}, {user.City}\n\n{user.AboutMe}";
        if (!string.IsNullOrEmpty(user.PhotoId))
            await bot.SendPhoto(user.ChatId, InputFile.FromFileId(user.PhotoId), message).ConfigureAwait(true);
        else
            await bot.SendMessage(user.ChatId, message).ConfigureAwait(true);

        if (user.State != StateUser.New8)
        {
            await bot.SendMessage(user.ChatId, "1. Смотреть анкеты\n" +
            "2. Заполнить анкету заново\n" +
            "3. Изменить фото\n" +
            "4. Изменть текст анкеты\n");
        }
    }

    internal static async Task ViewProfile(ITelegramBotClient bot, TUser user, TUser? person)
    {
        //if (user.State != StateUser.Search) return;
        if (person == null)
        {
            await bot.SendMessage(user.ChatId, "Подходящих анкет не найдено!", replyMarkup: mainKeyboard);
            return; 
        }
        var message = $"{person.FirstName}, {person.Age}, {user.City}\n\n{person.AboutMe}";
        if (!string.IsNullOrEmpty(user.PhotoId))
            await bot.SendPhoto(user.ChatId, InputFile.FromFileId(person.PhotoId), message, replyMarkup: mainKeyboard);
        else
            await bot.SendMessage(user.ChatId, message, replyMarkup: mainKeyboard);
        user.Viewed += $"{person.ChatId};";
    }
}