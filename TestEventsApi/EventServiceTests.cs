using EventsApi.ModelDTO;
using EventsApi.Services;
using NuGet.Frameworks;

namespace TestEventsApi
{
  public class EventServiceTests
  {
    private EventService _service;
    private Guid _testEventId;

    public EventServiceTests()
    {
      // Очищаем список событий перед каждым тестом
      EventService.Events.Clear();

      // Создаем тестовое событие для некоторых тестов
      var input = new InputEventDTO
      {
        Title = "Test Event",
        Description = "Test description",
        StartAt = DateTime.Now.AddHours(1),
        EndAt = DateTime.Now.AddHours(5)
      };

      _service = new EventService();
      _testEventId = _service.AddEvent(input).Id;
    }

    /// <summary>
    /// тест: создание события
    /// </summary>
    [Fact]
    public void AddEvent_Success()
    {
      var input = new InputEventDTO
      {
        Title = "Новое мероприятие",
        Description = "Описание",
        StartAt = DateTime.Now,
        EndAt = DateTime.Now.AddHours(1)
      };

      var result = _service.AddEvent(input);

      Assert.NotNull(result);
      Assert.Equal(input.Title, result.Title);
      Assert.Equal(input.Description, result.Description);
      Assert.Equal(input.StartAt, result.StartAt);
      Assert.Equal(input.EndAt, result.EndAt);
    }

    /// <summary>
    /// тест: получение всех событий
    /// </summary>
    [Fact]
    public void GetEvents_AllEvents()
    {
      var result = _service.GetEvents(null, null, null, null, null);

      Assert.Equal(1, result.TotalItems);
      Assert.Single(result.Events);
    }

    /// <summary>
    /// тест: получение события по ID
    /// </summary>
    [Fact]
    public void GetEvent_ById_Success()
    {
      var result = _service.GetEvent(_testEventId);

      Assert.NotNull(result);
      Assert.Equal(_testEventId, result.Id);
    }

    /// <summary>
    /// тест: обновление существующего события
    /// </summary>
    [Fact]
    public void ChangeEvent_Success()
    {
      var updateInput = new InputEventDTO
      {
        Title = "Updated Title",
        Description = "Updated description",
        StartAt = DateTime.Now,
        EndAt = DateTime.Now.AddHours(2)
      };

      var success = _service.ChangeEvent(_testEventId, updateInput);
      var updatedEvent = _service.GetEvent(_testEventId);

      Assert.True(success);
      Assert.NotNull(updatedEvent);
      Assert.Equal(updateInput.Title, updatedEvent.Title);
      Assert.Equal(updateInput.Description, updatedEvent.Description);
    }

    /// <summary>
    /// тест:удаление существующего события
    /// </summary>
    [Fact]
    public void RemoveEvent_Success()
    {
      var success = _service.RemoveEvent(_testEventId);
      var result = _service.GetEvent(_testEventId);

      Assert.True(success);
      Assert.Null(result);
    }

    /// <summary>
    /// тест: фильтрация по названию
    /// </summary>
    [Fact]
    public void Filter_ByTitle()
    {
      const string filter = "Test";
      var result = _service.GetEvents(filter, null, null, null, null);

      Assert.NotEqual(0, result.TotalItems);
      Assert.All(result.Events, q => Assert.Contains(filter, q.Title, comparisonType: StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>
    /// тест: фильтрация по диапазону дат
    /// </summary>
    [Fact]
    public void Filter_ByDates()
    {
      var from = DateTime.Now;
      var to = DateTime.Now.AddDays(1);

      var result = _service.GetEvents(null, from, to, null, null);

      Assert.Equal(1, result.TotalItems);
    }

    /// <summary>
    /// тест: фильтрация по дате
    /// </summary>
    [Fact]
    public void Filter_ByDate_From()
    {
      var from = DateTime.Now;
      _service.AddEvent(new InputEventDTO() { Title = "Updated Event", StartAt = from.AddHours(1), EndAt = DateTime.Now.AddDays(1) });
      _service.AddEvent(new InputEventDTO() { Title = "Updated Event", StartAt = from.AddHours(2), EndAt = DateTime.Now.AddDays(2) });
      _service.AddEvent(new InputEventDTO() { Title = "Updated Event", StartAt = from.AddHours(3), EndAt = DateTime.Now.AddDays(3) });

      var result = _service.GetEvents(null, from, null, null, null);
      // 3 + 1 запись (при инициализации)
      Assert.Equal(4, result.TotalItems);
    }

    /// <summary>
    /// тест: фильтрация по дате
    /// </summary>
    [Fact]
    public void Filter_ByDate_To()
    {
      var to = DateTime.Now.AddDays(3);
      _service.AddEvent(new InputEventDTO() { Title = "Updated Event", StartAt = DateTime.Now.AddHours(1), EndAt = DateTime.Now.AddDays(1) });
      _service.AddEvent(new InputEventDTO() { Title = "Updated Event", StartAt = DateTime.Now.AddHours(2), EndAt = DateTime.Now.AddDays(2) });
      _service.AddEvent(new InputEventDTO() { Title = "Updated Event", StartAt = DateTime.Now.AddHours(3), EndAt = DateTime.Now.AddDays(3) });

      // должно быть 3 +1 запись(при инициализации)
      var result = _service.GetEvents(null, null, to, null, null);
      Assert.Equal(4, result.TotalItems);
    }



    /// <summary>
    /// тест: попытка получить событие с несуществующим ID
    /// </summary>
    [Fact]
    public void GetEventById_NonExistingId_ReturnsNull()
    {
      // Создаем несуществующий GUID
      var nonExistingId = Guid.NewGuid();

      var result = _service.GetEvent(nonExistingId);

      Assert.Null(result);
    }

    /// <summary>
    /// тест: обновление несуществующего события
    /// </summary>
    [Fact]
    public void UpdateEvent_NonExistingId_ReturnsFalse()
    {
      var nonExistingId = Guid.NewGuid();
      var updateData = new InputEventDTO
      {
        Title = "Updated Title",
        StartAt = DateTime.Now,
        EndAt = DateTime.Now.AddHours(1)
      };

      var result = _service.ChangeEvent(nonExistingId, updateData);

      Assert.False(result);
    }
    /// <summary>
    /// тест: обновление с некорректными датами
    /// </summary>
    [Fact]
    public void UpdateEvent_EndBeforeStart_ShouldFail()
    {

      var createdEvent = _service.GetEvent(_testEventId);

      // Пытаемся обновить с некорректными датами
      var invalidUpdate = new InputEventDTO
      {
        Title = "Updated Event",
        StartAt = createdEvent.EndAt.AddHours(1),
        EndAt = createdEvent.EndAt
      };

      var result = _service.ChangeEvent(createdEvent.Id, invalidUpdate);

      Assert.False(result);
    }

    /// <summary>
    /// тест: пагинация по умолчанию
    /// </summary>
    [Fact]
    public void Pagination_DefaultValues()
    {
      InitData(); //добавили 50 записей - всего будет 50 +1(при инициализации) 
      var result = _service.GetEvents(null, null, null, null, null);

      Assert.Equal(10, result.Events.Count);
      Assert.Equal(1, result.Page);
      Assert.Equal(10, result.PageSize);
      Assert.Equal(51, result.TotalItems);
    }

    /// <summary>
    ///  тест: пагинация - произвольная страница
    /// </summary>
    [Fact]
    public void Pagination_CustomPageSize()
    {
      InitData(); //добавили 50 записей - всего будет 50 +1(при инициализации) 
      var result = _service.GetEvents(null, null, null, 2, 5);

      Assert.Equal(5, result.Events.Count);
      Assert.Equal(2, result.Page);
      Assert.Equal(5, result.PageSize);
      Assert.Equal(51, result.TotalItems);
    }

    /// <summary>
    /// тест: комбинированная фильтрация
    /// </summary>
    [Fact]
    public void CombinedFiltering_TitleAndDates()
    {
      // Создаем событие с конкретным названием и датой
      var specialEvent = new InputEventDTO
      {
        Title = "Super Event",
        Description = "Test",
        StartAt = DateTime.Now.AddDays(10),
        EndAt = DateTime.Now.AddDays(10).AddHours(1)
      };
      _service.AddEvent(specialEvent);

      var from = DateTime.Now.AddDays(9);
      var to = DateTime.Now.AddDays(11);

      var result = _service.GetEvents("Super", from, to, null, null);

      Assert.Equal(1, result.TotalItems);
      Assert.Equal("Super Event", result.Events[0].Title);
      Assert.True(result.Events[0].StartAt >= from && result.Events[0].StartAt <= to);
    }

    private void InitData()
    {
      for (int i = 0; i < 50; i++)
      {
        _service.AddEvent(new InputEventDTO
        {
          Title = $"New Event #{i}",
          Description = "Updated description {}",
          StartAt = DateTime.Now.AddMinutes(1),
          EndAt = DateTime.Now.AddMinutes(i)
        });
      }
    }

    /*
    пагинация событий;
    комбинированная фильтрация.

    создание события с некорректными данными (если валидация в сервисе);
    обновление события с некорректными датами (EndAt раньше StartAt).
    */

    /*
    // Тестирование пагинации


    // Тестирование комбинированной фильтрации

        [Fact]
        public void CombinedFiltering_TitleAndDates()
        {
            // Создаем событие с конкретным названием и датой
            var specialEvent = new InputEventDTO
            {
                Title = "Special Event",
                Description = "Test",
                StartAt = DateTime.Now.AddDays(10),
                EndAt = DateTime.Now.AddDays(10).AddHours(1)
            };
            _service.AddEvent(specialEvent);

            var from = DateTime.Now.AddDays(9);
            var to = DateTime.Now.AddDays(11);

            var result = _service.GetEvents("Special", from, to, null, null);

            Assert.Equal(1, result.TotalItems);
            Assert.Equal("Special Event", result.Events[0].Title);
            Assert.Equal(specialEvent.StartAt, result.Events[0].StartAt);
        }


    // Комбинированная фильтрация по названию и датам
        [Fact]
        public void CombinedFiltering_TitleAndDates()
        {
            // Фильтруем по "Conference" и периоду между -5 и +5 дней
            var result = _service.GetEvents(
                title: "Conference",
                from: DateTime.Now.AddDays(-5),
                to: DateTime.Now.AddDays(5),
                page: null,
                pageSize: null);

            // Ожидаем только одно событие "Conference 2026"
            Assert.Equal(1, result.TotalItems);
            Assert.Equal("Conference 2026", result.Events[0].Title);
        }

        // Комбинированная фильтрация с несколькими критериями
        [Fact]
        public void CombinedFiltering_MultipleCriteria()
        {
            // Фильтруем по "Workshop" и периоду между -3 и +7 дней
            var result = _service.GetEvents(
                title: "Workshop",
                from: DateTime.Now.AddDays(-3),
                to: DateTime.Now.AddDays(7),
                page: null,
                pageSize: null);

            // Ожидаем два события: "Workshop Spring" и "Workshop Fall"
            Assert.Equal(2, result.TotalItems);
            Assert.Contains(result.Events, e => e.Title == "Workshop Spring");
            Assert.Contains(result.Events, e => e.Title == "Workshop Fall");
        }

        // Комбинированная фильтрация с пагинацией
        [Fact]
        public void CombinedFiltering_WithPagination()
        {
            // Фильтруем все события и применяем пагинацию
            var result = _service.GetEvents(
                title: null,
                from: null,
                to: null,
                page: 1,
                pageSize: 2);

            Assert.Equal(5, result.TotalItems);
            Assert.Equal(2, result.Events.Count);

            // Проверяем вторую страницу
            var resultPage2 = _service.GetEvents(
                title: null,
                from: null,
                to: null,
                page: 2,
                pageSize: 2);

            Assert.Equal(5, resultPage2.TotalItems);
            Assert.Equal(2, resultPage2.Events.Count);
        }

    */

    /*


            // Тест на создание события с некорректными датами
            [Fact]
            public void CreateEvent_EndBeforeStart_ShouldFail()
            {
                var invalidEvent = new InputEventDTO
                {
                    Title = "Invalid Event",
                    StartAt = DateTime.Now.AddHours(1),
                    EndAt = DateTime.Now
                };

                var result = _service.AddEvent(invalidEvent);

                // Проверяем, что событие не было добавлено
                Assert.Null(result);
            }

            // Тест на обновление с некорректными датами
            [Fact]
            public void UpdateEvent_EndBeforeStart_ShouldFail()
            {
                // Сначала создаем валидное событие
                var validEvent = new InputEventDTO
                {
                    Title = "Valid Event",
                    StartAt = DateTime.Now,
                    EndAt = DateTime.Now.AddHours(1)
                };
                var createdEvent = _service.AddEvent(validEvent);

                // Пытаемся обновить с некорректными датами
                var invalidUpdate = new InputEventDTO
                {
                    Title = "Updated Event",
                    StartAt = DateTime.Now.AddHours(1),
                    EndAt = DateTime.Now
                };

                var result = _service.ChangeEvent(createdEvent.Id, invalidUpdate);

                Assert.False(result);
            }

            // Тест на создание события с пустым названием
            [Fact]
            public void CreateEvent_EmptyTitle_ShouldFail()
            {
                var invalidEvent = new InputEventDTO
                {
                    Title = "",
                    StartAt = DateTime.Now,
                    EndAt = DateTime.Now.AddHours(1)
                };

                var result = _service.AddEvent(invalidEvent);

                Assert.Null(result);
            }

            // Тест на создание события с отсутствующими датами
            [Fact]
            public void CreateEvent_MissingDates_ShouldFail()
            {
                var invalidEvent = new InputEventDTO
                {
                    Title = "Test Event"
                    // StartAt и EndAt не заданы
                };

                var result = _service.AddEvent(invalidEvent);

                Assert.Null(result);
            }

    */


  }
}