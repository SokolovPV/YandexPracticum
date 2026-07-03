using EventsApi.Application.Interfaces;

namespace EventsApi.Infrastructure.Services
{
	/// <inheritdoc/>
	public class CustomPasswordHasher : IPasswordHasher
	{
		/// <inheritdoc/>
		public string HashPassword(string password)
		{
			return BCrypt.Net.BCrypt.HashPassword(password);
		}
		/// <inheritdoc/>
		public bool VerifyHashedPassword(string providedPassword, string hashedPassword)
		{
			return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
		}
	}
}
