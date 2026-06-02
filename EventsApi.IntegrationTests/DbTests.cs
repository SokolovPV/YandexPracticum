using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.IntegrationTests
{

    public class DbTests : IClassFixture<PostgreSqlFixture>
    {

        private readonly PostgreSqlFixture postgreSqlFixture;
        public DbTests(PostgreSqlFixture _postgreSqlFixture) => postgreSqlFixture = _postgreSqlFixture;


        [Fact]
        public async Task EfCoreModelEvent_ShouldHaveCorrectConfigurationAsync()
        {
            // Act
            await postgreSqlFixture.ResetDatabaseAsync();
            using var context = await postgreSqlFixture.CreateContextAsync();
            var entityType = context.Model.FindEntityType(typeof(Event));

            // Assert
            Assert.NotNull(entityType);
            Assert.Equal("events", entityType.GetTableName());

            // Проверка первичного ключа
            var primaryKey = entityType.FindPrimaryKey();
            Assert.NotNull(primaryKey);
            Assert.Single(primaryKey.Properties);
            Assert.Equal("Id", primaryKey.Properties[0].Name);

            // Проверка свойств
            var idProperty = entityType.FindProperty("Id");
            Assert.False(idProperty!.IsNullable);
            Assert.Equal(typeof(Guid), idProperty.ClrType);

            var titleProperty = entityType.FindProperty("Title");
            Assert.False(titleProperty!.IsNullable);
            Assert.Equal(typeof(string), titleProperty.ClrType);
            Assert.Equal("title", titleProperty.GetColumnName());

            var descriptionProperty = entityType.FindProperty("Description");
            Assert.True(descriptionProperty!.IsNullable);
            Assert.Equal(typeof(string), descriptionProperty.ClrType);
            Assert.Equal("description", descriptionProperty.GetColumnName());



            var startAtProperty = entityType.FindProperty("StartAt");
            Assert.False(startAtProperty!.IsNullable);
            Assert.Equal(typeof(DateTime), startAtProperty.ClrType);
            Assert.Equal("start_at", startAtProperty.GetColumnName());


            var endAtProperty = entityType.FindProperty("EndAt");
            Assert.False(endAtProperty!.IsNullable);
            Assert.Equal(typeof(DateTime), endAtProperty.ClrType);
            Assert.Equal("end_at", endAtProperty.GetColumnName());

            var totalSeatsProperty = entityType.FindProperty("TotalSeats");
            Assert.False(totalSeatsProperty!.IsNullable);
            Assert.Equal(typeof(int), totalSeatsProperty.ClrType);
            Assert.Equal("total_seats", totalSeatsProperty.GetColumnName());


            var availableSeatsProperty = entityType.FindProperty("AvailableSeats");
            Assert.False(availableSeatsProperty!.IsNullable);
            Assert.Equal(typeof(int), availableSeatsProperty.ClrType);
            Assert.Equal("available_seats", availableSeatsProperty.GetColumnName());

                // Проверка индексов
                var indexes = entityType.GetIndexes().ToList();
                Assert.NotEmpty(indexes);
        }


        [Fact]
        public async Task EfCoreModelBooking_ShouldHaveCorrectConfigurationAsync()
        {
            // Act
            await postgreSqlFixture.ResetDatabaseAsync();
            using var context = await postgreSqlFixture.CreateContextAsync();
            var entityType = context.Model.FindEntityType(typeof(Booking));

            // Assert
            Assert.NotNull(entityType);
            Assert.Equal("bookings", entityType.GetTableName());

            // Проверка первичного ключа
            var primaryKey = entityType.FindPrimaryKey();
            Assert.NotNull(primaryKey);
            Assert.Single(primaryKey.Properties);
            Assert.Equal("Id", primaryKey.Properties[0].Name);

            // Проверка свойств
            var idProperty = entityType.FindProperty("Id");
            Assert.False(idProperty!.IsNullable);
            Assert.Equal(typeof(Guid), idProperty.ClrType);

            var statusProperty = entityType.FindProperty("Status");
            Assert.False(statusProperty!.IsNullable);
            Assert.Equal(typeof(BookingStatus), statusProperty.ClrType);
            Assert.Equal("status", statusProperty.GetColumnName());

            var createdAtProperty = entityType.FindProperty("CreatedAt");
            Assert.False(createdAtProperty!.IsNullable);
            Assert.Equal(typeof(DateTime), createdAtProperty.ClrType);
            Assert.Equal("created_at", createdAtProperty.GetColumnName());

            var processedAtProperty = entityType.FindProperty("ProcessedAt");
            Assert.True(processedAtProperty!.IsNullable);
            Assert.Equal(typeof(DateTime?), processedAtProperty.ClrType);
            Assert.Equal("processed_at", processedAtProperty.GetColumnName());


            // Проверка внешних ключей
            var foreignKeys = entityType.GetForeignKeys().ToList();
            Assert.Single(foreignKeys);

            Assert.Contains(foreignKeys, fk =>
                fk.PrincipalEntityType.ClrType == typeof(Event) &&
                fk.Properties.Any(p => p.Name == "EventId"));


            // Проверка индексов
            var indexes = entityType.GetIndexes().ToList();
            Assert.NotEmpty(indexes);
        }
    }
}