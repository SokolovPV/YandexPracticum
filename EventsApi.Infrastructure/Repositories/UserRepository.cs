using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.Infrastructure.Repositories
{
	internal class UserRepository(AppDbContext appDbContext) : IUserRepository
	{
		public async Task AddAsync(User user, CancellationToken ct)
		{
			await appDbContext.Users.AddAsync(user, ct);
			await appDbContext.SaveChangesAsync(ct);
		}

		public async Task<bool> ExistsAsync(string login, CancellationToken ct)
		{
			return await appDbContext.Users.AnyAsync(u => u.Login == login, ct);
		}
	}
}
