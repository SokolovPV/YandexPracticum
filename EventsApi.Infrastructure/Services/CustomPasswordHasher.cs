using EventsApi.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace EventsApi.Infrastructure.Services
{
	public class CustomPasswordHasher : IPasswordHasher
	{
		public string HashPassword(string password)
		{
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
			return Convert.ToHexString(bytes);
		}

		public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
		{
			string computedHash = HashPassword(providedPassword);
			return computedHash == hashedPassword; // Сравниваем строки
		}
	}
}
