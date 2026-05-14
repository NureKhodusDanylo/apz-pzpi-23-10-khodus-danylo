using System.Text;
using System.Text.Json;
using WokwiLoader.Models;

namespace WokwiLoader;

class Program
{
    private static readonly HttpClient _httpClient = new();
    private const string WokwiApiUrl = "https://wokwi.com/api/projects/save";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Wokwi File Uploader ===\n");

        try
        {
            // Check for --reset-config argument
            if (args.Contains("--reset-config") || args.Contains("-r"))
            {
                const string configPath = "config.json";
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                    Console.WriteLine("Существующий config.json удален.\n");
                }
            }

            // Check for debug mode
            bool debugMode = args.Contains("--debug") || args.Contains("-d");

            var config = LoadConfig();

            if (string.IsNullOrEmpty(config.Cookie))
            {
                Console.WriteLine("Предупреждение: Cookie не указана в конфиге. Если требуется аутентификация, загрузка может не работать.\n");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Add("Cookie", config.Cookie);
            }

            // Получаем путь к папке из конфига или запрашиваем у пользователя
            string? folderPath = config.FolderPath;

            if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
            {
                Console.WriteLine($"Используется папка из конфига: {folderPath}");
                Console.Write("Использовать эту папку? (Y/n): ");
                string? useConfig = Console.ReadLine();

                if (useConfig?.ToLower() == "n" || useConfig?.ToLower() == "no")
                {
                    folderPath = null;
                }
            }
            else
            {
                folderPath = null;
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                Console.WriteLine("\nВыберите папку для загрузки файлов:");
                Console.Write("Введите полный путь к папке: ");
                folderPath = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                {
                    Console.WriteLine("Ошибка: Указанная папка не существует!");
                    Console.WriteLine("\nНажмите любую клавишу для выхода...");
                    Console.ReadKey();
                    return;
                }

                // Предлагаем сохранить путь в конфиг
                Console.Write("\nСохранить этот путь в конфиге для следующих запусков? (Y/n): ");
                string? savePath = Console.ReadLine();

                if (savePath?.ToLower() != "n" && savePath?.ToLower() != "no")
                {
                    config.FolderPath = folderPath;
                    SaveConfig(config);
                    Console.WriteLine("✓ Путь сохранен в конфигурации");
                }
            }

            Console.WriteLine($"\n1. Удаляю все файлы из проекта '{config.ProjectName}'...");
            await ClearProjectFiles(config, debugMode);
            Console.WriteLine("✓ Файлы удалены");

            Console.WriteLine($"\n2. Загружаю файлы из папки: {folderPath}");
            var files = LoadFilesRecursively(folderPath);
            Console.WriteLine($"   Найдено файлов: {files.Count}");

            foreach (var file in files)
            {
                Console.WriteLine($" - {file.Name}");
            }

            Console.WriteLine($"\n3. Отправляю файлы на Wokwi...");
            await UploadFiles(config, files, debugMode);
            Console.WriteLine("✓ Все файлы успешно загружены!");

            Console.WriteLine($"\nПроект доступен по адресу: https://wokwi.com/projects/{config.ProjectId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Ошибка: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }

    static WokwiConfig LoadConfig()
    {
        const string configPath = "config.json";

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Файл конфигурации '{configPath}' не найден!\n");
            Console.WriteLine("Давайте создадим конфигурацию:\n");
            return CreateConfigInteractively(configPath);
        }

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<WokwiConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
        {
            throw new InvalidOperationException("Не удалось прочитать конфигурацию");
        }

        return config;
    }

    static WokwiConfig CreateConfigInteractively(string configPath)
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("        СОЗДАНИЕ КОНФИГУРАЦИИ");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        // Project ID
        Console.WriteLine("1. Project ID");
        Console.WriteLine("   Найдите ID в URL вашего проекта:");
        Console.WriteLine("   https://wokwi.com/projects/449518275595938817");
        Console.WriteLine("                              ^^^^^^^^^^^^^^^^^^");
        Console.Write("\n   Введите Project ID: ");
        string? projectId = Console.ReadLine();
        while (string.IsNullOrWhiteSpace(projectId))
        {
            Console.Write("   Project ID не может быть пустым. Введите снова: ");
            projectId = Console.ReadLine();
        }

        // Project Name
        Console.WriteLine("\n2. Название проекта");
        Console.Write("   Введите название проекта: ");
        string? projectName = Console.ReadLine();
        while (string.IsNullOrWhiteSpace(projectName))
        {
            Console.Write("   Название не может быть пустым. Введите снова: ");
            projectName = Console.ReadLine();
        }

        // Unlisted
        Console.WriteLine("\n3. Видимость проекта");
        Console.Write("   Сделать проект скрытым (unlisted)? (y/N): ");
        string? unlistedInput = Console.ReadLine();
        bool unlisted = unlistedInput?.ToLower() == "y" || unlistedInput?.ToLower() == "yes";

        // Cookie
        Console.WriteLine("\n4. Cookie для аутентификации");
        Console.WriteLine("   Как получить Cookie:");
        Console.WriteLine("   1) Откройте https://wokwi.com и авторизуйтесь");
        Console.WriteLine("   2) Нажмите F12 (DevTools)");
        Console.WriteLine("   3) Вкладка Network → обновите страницу (F5)");
        Console.WriteLine("   4) Выберите любой запрос к wokwi.com");
        Console.WriteLine("   5) Скопируйте значение заголовка 'Cookie'");
        Console.Write("\n   Введите Cookie (или оставьте пустым): ");
        string? cookie = Console.ReadLine();

        // Folder Path
        Console.WriteLine("\n5. Путь к папке с файлами (опционально)");
        Console.Write("   Введите путь к папке (или оставьте пустым): ");
        string? folderPath = Console.ReadLine();

        // Create config object
        var config = new WokwiConfig
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Unlisted = unlisted,
            Cookie = string.IsNullOrWhiteSpace(cookie) ? null : cookie,
            FolderPath = string.IsNullOrWhiteSpace(folderPath) ? null : folderPath
        };

        // Save config
        SaveConfig(config, configPath);

        Console.WriteLine($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"✓ Конфигурация сохранена в '{configPath}'");
        Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        if (string.IsNullOrEmpty(config.Cookie))
        {
            Console.WriteLine("⚠ Предупреждение: Cookie не указана.");
            Console.WriteLine("  Если требуется аутентификация, загрузка может не работать.\n");
        }

        return config;
    }

    static void SaveConfig(WokwiConfig config, string configPath = "config.json")
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        File.WriteAllText(configPath, json);
    }

    static List<WokwiFile> LoadFilesRecursively(string folderPath)
    {
        var files = new List<WokwiFile>();
        var basePath = Path.GetFullPath(folderPath);

        var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        foreach (var filePath in allFiles)
        {
            var fullPath = Path.GetFullPath(filePath);
            var relativePath = Path.GetRelativePath(basePath, fullPath);

            // Заменяем все обратные слеши на прямые для совместимости с Wokwi
            relativePath = relativePath.Replace("\\", "/");

            string content;
            if (IsBinaryFile(filePath))
            {
                var bytes = File.ReadAllBytes(filePath);
                content = Convert.ToBase64String(bytes);
            }
            else
            {
                content = File.ReadAllText(filePath);
            }

            files.Add(new WokwiFile
            {
                Name = Path.GetFileName(relativePath),
                Content = content
            });
        }

        return files;
    }

    static bool IsBinaryFile(string filePath)
    {
        var binaryExtensions = new[] { ".bin", ".hex", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".wasm" };
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return binaryExtensions.Contains(ext);
    }

    static async Task ClearProjectFiles(WokwiConfig config, bool debugMode = false)
    {
        var project = new WokwiProject
        {
            Files = new List<WokwiFile>(),
            Id = config.ProjectId,
            Name = config.ProjectName,
            Unlisted = config.Unlisted
        };

        await SendToWokwi(project, debugMode);
    }

    static async Task UploadFiles(WokwiConfig config, List<WokwiFile> files, bool debugMode = false)
    {
        var project = new WokwiProject
        {
            Files = files,
            Id = config.ProjectId,
            Name = config.ProjectName,
            Unlisted = config.Unlisted
        };

        await SendToWokwi(project, debugMode);
    }

    static async Task SendToWokwi(WokwiProject project, bool debugMode = false)
    {
        var json = JsonSerializer.Serialize(project, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = debugMode // Красивый формат в debug режиме
        });

        // Логируем размер данных
        var jsonSize = Encoding.UTF8.GetByteCount(json);
        Console.WriteLine($"   Размер данных: {jsonSize / 1024.0:F2} KB");
        Console.WriteLine($"   Количество файлов: {project.Files.Count}");

        // В режиме отладки сохраняем запрос в файл
        if (debugMode)
        {
            var debugFileName = $"debug_request_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            File.WriteAllText(debugFileName, json);
            Console.WriteLine($"   🐛 DEBUG: Запрос сохранен в {debugFileName}");
        }

        // Проверяем размер - если слишком большой, предупреждаем
        if (jsonSize > 5 * 1024 * 1024) // 5 MB
        {
            Console.WriteLine("   ⚠ Предупреждение: Данные очень большие (>5MB), это может вызвать ошибку на сервере");
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(WokwiApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Добавляем более детальную информацию об ошибке
                var errorMessage = $"Ошибка при отправке на Wokwi\n" +
                                 $"Статус: {response.StatusCode} ({(int)response.StatusCode})\n" +
                                 $"Ответ сервера: {errorContent}\n" +
                                 $"Размер отправленных данных: {jsonSize / 1024.0:F2} KB\n" +
                                 $"Количество файлов: {project.Files.Count}";

                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    errorMessage += "\n\n💡 Возможные причины ошибки 500:\n" +
                                  "   1. Данные слишком большие (попробуйте загрузить меньше файлов)\n" +
                                  "   2. Некорректный формат данных\n" +
                                  "   3. Проблема с аутентификацией (проверьте Cookie)\n" +
                                  "   4. Временная проблема на стороне Wokwi\n" +
                                  "   5. Project ID не существует или у вас нет доступа";
                }

                throw new HttpRequestException(errorMessage);
            }

            var responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"   ✓ Ответ сервера: {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            throw; // Пробрасываем HTTP ошибки
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при отправке запроса: {ex.Message}", ex);
        }
    }
}
