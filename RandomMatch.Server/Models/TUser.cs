using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace RandomMatch.Server.Models
{
    public enum GenderUser
    {
        Man,
        Woman
    }
    public enum StateUser
    {
        New0,
        New1,
        New2,
        New3,
        New4,
        New5,
        New6,
        New7,
        New8,
        Search,
        Stop,
        New01,
        SearchMessage,
        Bot
    }
    public class TUser
    {
        public long ChatId { get; set; }
        public int Age { get; set; }
        public int SearchAge { get; set; }
        public string? City { get; set; }
        public string? AboutMe { get; set; }
        public string? PhotoId { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsBanned { get; set; } = false;
        public StateUser State { get; set; } = StateUser.New0;
        public GenderUser Gender { get; set; }
        public GenderUser SearchGender { get; set; }
        public string? Viewed { get; set; }

        internal void Like(ITelegramBotClient bot, TUser user, string? message = null)
        {
            if (State == StateUser.Bot)
            {
                return;
            }
            else if (!string.IsNullOrEmpty(message))
            {
                //var inlineButton = new KeyboardButton("Написать"); 
                var sendMessage = $"Твоя анкета понравилась: @{user.Username}\n\n" +
                        $"{user.FirstName}, {user.Age}, {user.City}\n{user.AboutMe}\n\n" +
                        $"{message}";
                if (!string.IsNullOrEmpty(user.PhotoId))
                {
                    bot.SendPhoto(ChatId, InputFile.FromFileId(user.PhotoId), caption: sendMessage);
                    return;
                }
                bot.SendMessage(ChatId, sendMessage);
            }
            else
            {

            }
        }
    }
}
