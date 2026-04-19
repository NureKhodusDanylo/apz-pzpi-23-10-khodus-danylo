/*
Запити до ШІ: 
1.	Під час підготовки доповіді на тему «Архітектура програмної системи SlashLIB» мені потрібно навести приклад реалізації мікросервісу. Зокрема, мене цікавить реалізація REST-контролера в ASP.NET Core для управління закладками (додавання закладки користувачем). Прошу продемонструвати приклад коду з урахуванням таких архітектурних принципів: клієнт-серверна архітектура, RESTful API, Dependency Injection, використання DTO та авторизація доступу до ресурсів.
2.	У межах підготовки доповіді про архітектуру SlashLIB мені потрібно продемонструвати асинхронну взаємодію компонентів. Наведи приклад сервісу завантаження розділів (ChapterService) мовою C#, який зберігає оригінали зображень у хмарному сховищі S3 та публікує подію у шину повідомлень RabbitMQ для подальшої фонової обробки зображень. Врахуй принципи слабкого зв'язування (Loose Coupling).
3.	Під час підготовки доповіді на тему архітектури SlashLIB мені потрібно показати, як оптимізується робота з даними та як реалізується принцип Polyglot Persistence. Наведи приклад коду на C#, який демонструє реалізацію стратегії кешування Cache-Aside для Redis під час отримання даних про тайтл (мангу) з основної бази даних PostgreSQL.
*/

// В.1 Приклад програмного коду мікросервіса (REST-контролер)
using Microsoft.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SlashLIB.BookmarkService.Controllers
{
    [ApiController]
    [Route("api/bookmarks")]
    [Authorize]
    public class BookmarksController : ControllerBase
    {
        private readonly IBookmarkService _service;

        public BookmarksController(IBookmarkService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddBookmark([FromBody] AddBookmarkDto dto)
        {
            var result = await _service.AddAsync(User.GetUserId(), dto);
            
            return result.Success ? Ok() : BadRequest(result.Error);
        }
    }
}

//В.2 Приклад програмного коду мікросервіса (взаємодія через RabbitMQ)
using SlashLIB.ReaderService.Events;
using SlashLIB.ReaderService.Interfaces;

namespace SlashLIB.ReaderService.Services
{
    public class ChapterService : IChapterService
    {
        private readonly IMessageBus _bus;
        private readonly IStorageService _storage;

        public ChapterService(IMessageBus bus, IStorageService storage)
        {
            _bus = bus;
            _storage = storage;
        }

        public async Task UploadChapter(ChapterUploadRequest request)
        {
            // 1. Збереження оригіналів у S3
            var storagePath = await _storage.SaveAsync(request.Files);
            
            // 2. Публікація події для обробки зображень
            await _bus.PublishAsync(new ChapterUploadedEvent 
            { 
                Id = request.Id, 
                Path = storagePath 
            });
        }
    }
}

//В.3 Приклад програмного коду (стратегія кешування Cache-Aside для Redis)
using Microsoft.EntityFrameworkCore;
using SlashLIB.CatalogService.DTOs;
using SlashLIB.CatalogService.Interfaces;
namespace SlashLIB.CatalogService.Services
{
    public class MangaCatalogService : IMangaCatalogService
    {
        private readonly ICacheService _cache;
        private readonly CatalogDbContext _db;

        public MangaCatalogService(ICacheService cache, CatalogDbContext db)
        {
            _cache = cache;
            _db = db;
        }

        public async Task<MangaDto> GetMangaAsync(int id)
        {
            var cacheKey = $"manga_{id}";
            
            return await _cache.GetOrSetAsync(cacheKey, async () => 
            {
                var manga = await _db.Manga
                    .Include(m => m.Chapters)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                return manga?.ToDto();
            }, TimeSpan.FromMinutes(30));
        }
    }
}
