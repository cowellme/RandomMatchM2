
using Microsoft.EntityFrameworkCore;
using RandomMatch.Server.Data;
using RandomMatch.Server.Models;
using RandomMatch.Server.Repositories;
using RandomMatch.Server.Services;
using Telegram.Bot;

namespace RandomMatch.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
                ));

            // Регистрируем репозитории и сервисы
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUserService, UserService>();

            var botToken = builder.Configuration["TelegramBot:Token"];
            if (string.IsNullOrWhiteSpace(botToken))
                throw new InvalidOperationException("TelegramBot:Token is missing in configuration.");

            builder.Services.AddSingleton<ITelegramBotClient>(sp =>
                new TelegramBotClient(botToken));

            // === Hosted Service для получения обновлений ===
            builder.Services.AddHostedService<TelegramBotUpdateHandler>();

            var app = builder.Build();

            //using var scope = app.Services.CreateScope();
            //var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //context.Database.EnsureDeleted();  // Удаляет ВСЮ базу
            //context.Database.EnsureCreated();  // Создаёт заново по моделям

            //CreateWomenBots(context);
            
            app.UseDefaultFiles();
            app.MapStaticAssets();



            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }

        private static void CreateWomenBots(AppDbContext context)
        {
            var strWoomen = File.ReadAllLines(@"C:\Users\Ksenia\Desktop\res.txt");
            var userRange = new List<TUser>();
            foreach (var line in strWoomen) 
            {
                var rnd = new Random();
                var vars = line.Split(';');
                var user = new TUser
                {
                    FirstName = vars[0],
                    Age = rnd.Next(16, 25),
                    AboutMe = vars[3],
                    City = vars[2],
                    PhotoId = "AgACAgIAAxkBAAIFPGmdYQoa5CJWsxslWdES78v8QxdQAALtFGsbY7LxSCz5b-MJQXMzAQADAgADcwADOgQ",
                    Gender = rnd.Next(0, 1) > 0 ? GenderUser.Man : GenderUser.Woman,
                };
                userRange.Add(user);
            }
            context.Users.AddRange(userRange);
            context.SaveChanges();
        }
    }
}
