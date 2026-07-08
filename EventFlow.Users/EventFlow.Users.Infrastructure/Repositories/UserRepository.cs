using EventFlow.Users.Application.Interfaces;
using EventFlow.Users.Domain.Entities;
using EventFlow.Users.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Users.Infrastructure.Repositories
{
	public class UserRepository(AppDbContext appDbContext) : IUserRepository
	{
		public async Task AddAsync(User user, CancellationToken ct)
		{
			await appDbContext.Users.AddAsync(user, ct);
			await appDbContext.SaveChangesAsync(ct);
		}

        public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct)
        {
            return await appDbContext.Users.FirstOrDefaultAsync(u => u.Login == login, ct);
        }

        public async Task<bool> ExistsAsync(string login, CancellationToken ct)
		{
			return await appDbContext.Users.AnyAsync(u => u.Login == login, ct);
		}

        public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct)
        {
            return await appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        }
    }
}
