/*
Запити до ШІ : Створи три приклади коду на C# для демонстрації патерну Впровадження Залежностей (Dependency Injection). Кожен приклад повинен показувати різні підходи до реалізації:
1.	Напиши приклад сервісу CustomerService на мові C#, який використовує Constructor Injection (впровадження через конструктор). Сервіс повинен реалізовувати інтерфейс ICustomerService та залежати від ICustomerRepository і ILogger. Додай перевірку залежностей на null та асинхронний метод GetCustomerById, який логує процес отримання даних.
2.	Продемонструй приклад Setter Injection (впровадження через властивість) на C#. Створи інтерфейс IService, його реалізацію та клас Client, у якого є публічна властивість для цієї залежності. Покажи в методі Main, як вручну створити об'єкт клієнта та встановити залежність після його ініціалізації.
3.	Напиши приклад налаштування стандартного DI-контейнера (Inversion of Control container) у консольному додатку .NET за допомогою бібліотеки Microsoft.Extensions.DependencyInjection. Покажи, як зареєструвати сервіси з різними життєвими циклами (Transient та Scoped), як побудувати ServiceProvider та отримати з нього головний об'єкт додатка ClientApp з уже впровадженими залежностями.
*/


//В.1 Приклад програмного коду 
using System;
using CustomerApp.Core.Interfaces;
using CustomerApp.Core.Models;
using Microsoft.Extensions.Logging;

namespace CustomerApp.Core.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly ILogger<CustomerService> _logger;

        // Constructor with Dependencies Injected
        public CustomerService(ICustomerRepository repository, ILogger<CustomerService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("CustomerService initialized with {Repository}", _repository.GetType().Name);
        }

        public async Task<Customer> GetCustomerById(int id)
        {
            _logger.LogInformation("Fetching customer with ID: {Id}", id);
            var customer = await _repository.GetByIdAsync(id);

            if (customer == null)
            {
                _logger.LogWarning("Customer with ID {Id} not found.", id);
                return null;
            }

            return customer;
        }
    }
}

//В.2 Приклад програмного коду 
namespace SetterInjectionDemo
{
    // 1. Dependency Interface
    public interface IService {
        void DoSomething();
    }

    // 2. Implementation
    public class ServiceImplementation : IService {
        public void DoSomething() {
            Console.WriteLine("Service done.");
        }
    }

    public class Client {
        // 3. Setter Injection: The dependency property. It is set after instantiation.
        public IService Service { get; set; }

        public void PerformAction() {
            Service?.DoSomething();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // 4. Instantiate Client
            Client client = new Client();
            
            // 5. Manually inject the dependency.
            client.Service = new ServiceImplementation();
            client.PerformAction();
        }
    }
}

//В.3 Приклад програмного коду 
using Microsoft.Extensions.DependencyInjection;
using MyApp.Interfaces;
using MyApp.Services;

namespace MyApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //  1. Створюємо колекцію сервісів (DI-контейнер)
            var services = new ServiceCollection();

            // 2. Реєструємо наші залежності (Інжектор)
            services.AddTransient<IEmailService, EmailService>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Реєструємо нашого Клієнта
            services.AddTransient<ClientApp>();

            // 3. Будуємо провайдер сервісів
            var serviceProvider = services.BuildServiceProvider();

            // 4. Отримуємо готового Клієнта з усіма впровадженими залежностями
            var app = serviceProvider.GetRequiredService<ClientApp>();

            // 5. Запускаємо програму
            app.Run();
        }
    }
}
