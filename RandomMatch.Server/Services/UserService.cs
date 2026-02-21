using Microsoft.EntityFrameworkCore;
using RandomMatch.Server.Models;
using RandomMatch.Server.Repositories;
using Telegram.Bot.Types;

namespace RandomMatch.Server.Services
{
    public interface IUserService
    {
        Task<TUser?> GetOrCreateUserAsync(long telegramId, string? username, string? firstName, string? lastName);
        Task<TUser?> GetUserByIdAsync(long id);
        Task<IEnumerable<TUser>> GetActiveUsersAsync();
        Task UpdateUser(TUser user);
        Task<IEnumerable<TUser>> GetAllAsync();
    }
    public class UserService : IUserService
    {
        private readonly IRepository<TUser> _userRepository;

        public UserService(IRepository<TUser> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<TUser?> GetUserByIdAsync(long id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TUser>> GetActiveUsersAsync()
        {
            var all = await _userRepository.GetAllAsync();
            return all.Where(u => u.IsActive);
        }

        public async Task UpdateUser(TUser user)
        {
            await Task.Run(() => _userRepository.Update(user));
            await _userRepository.SaveChangesAsync();
        }

        public async Task<TUser?> GetOrCreateUserAsync(long chatId, string? username, string? firstName, string? lastName)
        {
            var existing = await _userRepository.GetByIdAsync(chatId);
            if (existing != null)
            {
                bool changed = false;
                if (existing.Username != username) { existing.Username = username; changed = true; }
                if (existing.FirstName != firstName) { existing.FirstName = firstName; changed = true; }
                if (existing.LastName != lastName) { existing.LastName = lastName; changed = true; }

                if (changed)
                {
                    _userRepository.Update(existing);
                    await _userRepository.SaveChangesAsync();
                }
                return existing;
            }

            var newUser = new TUser
            {
                ChatId = chatId,
                Username = username,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            try
            {
                await _userRepository.AddAsync(newUser);
                await _userRepository.SaveChangesAsync();
                return newUser;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
            {
                // Конкурентная вставка — просто вернём существующего
                return await _userRepository.GetByIdAsync(chatId);
            }
        }

        public async Task<IEnumerable<TUser>> GetAllAsync()
        {
            var result = await _userRepository.GetAllAsync();
            return [.. result];
        }
    }
}
