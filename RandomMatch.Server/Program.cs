
using Microsoft.EntityFrameworkCore;
using RandomMatch.Server.Data;
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

            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureDeleted();  // Удаляет ВСЮ базу
            context.Database.EnsureCreated();  // Создаёт заново по моделям


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
    }
}
