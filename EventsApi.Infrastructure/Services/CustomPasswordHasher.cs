using EventsApi.Application.Interfaces;

namespace EventsApi.Infrastructure.Services
{
	public class CustomPasswordHasher : IPasswordHasher
	{
		public string HashPassword(string password)
		{
			return BCrypt.Net.BCrypt.HashPassword(password);
		}

		public bool VerifyHashedPassword(string providedPassword, string hashedPassword)
		{
			return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
		}
	}
}
