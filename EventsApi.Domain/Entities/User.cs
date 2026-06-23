using System.ComponentModel.DataAnnotations;

namespace EventsApi.Domain.Entities
{
    /// <summary>
    /// Модель пользователя
    /// </summary>
    public class User
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string Login { get; private set; }
        /// <summary>
        /// Хэш пароля пользователя
        /// </summary>
        public string PasswordHash { get; private set; }
        /// <summary>
        /// Роль пользователя
        /// </summary>
        public RoleType Role { get; private set; }
        /// <summary>
        /// Коллекция бронирований пользователя
        /// </summary>
        public ICollection<Booking> Bookings { get; private set; } = [];

        private User(string login, string passwordHash, RoleType role)
        {
            Id = Guid.NewGuid();
            Login = login;
            PasswordHash = passwordHash;
            Role = role;
        }

        public static User Create(string login, string passwordHash, RoleType role)
        {
            if (string.IsNullOrEmpty(login))
                throw new ValidationException(nameof(User), Guid.Empty.ToString(), "Логин пользователя не задан.");

            if (string.IsNullOrEmpty(passwordHash))
                throw new ValidationException(nameof(User), Guid.Empty.ToString(), "Пароль пользователя не задан.");

            return new User(login, passwordHash, role);
        }

    }
}