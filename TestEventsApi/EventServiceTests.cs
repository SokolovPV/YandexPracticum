using EventsApi.Application.CustomException;
using EventsApi.Application.Services;
using EventsApi.ModelDTO;
using System.ComponentModel.DataAnnotations;

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
    public void RemoveEvent_Success_ThrowsKeyNotExistException()
    {
      // удаляем мероприятие 
      var success = _service.RemoveEvent(_testEventId);

      // пытыемся получить удаленное мероприятие 
      var exception = Assert.Throws<KeyNotExistException>(() => _service.GetEvent(_testEventId));

      Assert.True(success);
      Assert.Contains("Идентификатор мероприятия не найден", exception.Message);
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
      var to = DateTime.Now.AddDays(4);
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
    public void GetEventById_NonExistingId_ThrowsKeyNotExistException()
    {
      // Создаем несуществующий GUID
      var nonExistingId = Guid.NewGuid();
      var exception = Assert.Throws<KeyNotExistException>(() => _service.GetEvent(nonExistingId));      
      Assert.Contains("Идентификатор мероприятия не найден", exception.Message);
    }

    /// <summary>
    /// тест: обновление несуществующего события
    /// </summary>
    [Fact]
    public void UpdateEvent_NonExistingId_ThrowsKeyNotExistException()
    {
      var nonExistingId = Guid.NewGuid();
      var updateData = new InputEventDTO
      {
        Title = "Updated Title",
        StartAt = DateTime.Now,
        EndAt = DateTime.Now.AddHours(1)
      };

      var exception = Assert.Throws<KeyNotExistException>(() => _service.ChangeEvent(nonExistingId, updateData));
      Assert.Contains("Идентификатор мероприятия не найден", exception.Message);
    }
    
    /// <summary>
    /// тест: обновление с некорректными датами
    /// </summary>
    [Fact]
    public void UpdateEvent_EndBeforeStart_ThrowsValidationEception()
    {

      var createdEvent = _service.GetEvent(_testEventId);
      // Пытаемся обновить с некорректными датами
      var invalidUpdate = new InputEventDTO
      {
        Title = "Updated Event",
        StartAt = createdEvent!.EndAt.AddHours(1),
        EndAt = createdEvent.EndAt
      };
      var exception = Assert.Throws<ValidationException>(() => _service.ChangeEvent(createdEvent.Id, invalidUpdate));
      Assert.Contains("Дата начала мероприятия больше даты завершения", exception.Message);
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
      Assert.True(result.Events[0].StartAt >= from && result.Events[0].EndAt <= to);
    }

    private void InitData()
    {
      for (int i = 0; i < 50; i++)
      {
        _service.AddEvent(new InputEventDTO
        {
          Title = $"New Event #{i}",
          Description = "Updated description {}",
          StartAt = DateTime.Now.AddMinutes(i),
          EndAt = DateTime.Now.AddMinutes(i+3)
        });
      }
    }
  }
}