using Application.Abstractions.Interfaces;
using Application.Services;
using Application.Services.PaymentServices;
using Entities.Config;
using Entities.Interfaces;
using Infrastructure;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace RobDeliveryAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var builder = WebApplication.CreateBuilder(args);

            Config config = new Config();
            builder.Configuration.Bind(config);
            builder.Services.AddSingleton(config);

            var connectionString = config.ConnectionStrings.DefaultConnection;

            // Handle relative SQLite paths by checking common locations
            if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Data Source=") && !connectionString.Contains(":\\") && !connectionString.Contains(":/"))
            {
                var dataSource = connectionString.Replace("Data Source=", "").Trim();
                if (!Path.IsPathRooted(dataSource))
                {
                    // 1. Try relative to content root
                    var path = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, dataSource));
                    if (!File.Exists(path))
                    {
                        // 2. Try one level up (for local development where Infrastructure is a sibling)
                        var parentDir = Directory.GetParent(builder.Environment.ContentRootPath)?.FullName;
                        if (parentDir != null)
                        {
                            var parentPath = Path.GetFullPath(Path.Combine(parentDir, dataSource));
                            if (File.Exists(parentPath))
                            {
                                path = parentPath;
                            }
                        }
                    }
                    connectionString = $"Data Source={path}";

                    // Ensure the directory exists
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
            }

            builder.Services.AddDbContext<MyDbContext>(options =>
                options.UseSqlite(connectionString));

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<INodeRepository, NodeRepository>();
            builder.Services.AddScoped<IRobotRepository, RobotRepository>();
            builder.Services.AddScoped<IFileRepository, FileRepository>();
            builder.Services.AddScoped<IAdminKeyRepository, AdminKeyRepository>();
            builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();

            // Services
            builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ITokenService, BaseTokenService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<INodeService, NodeService>();
            builder.Services.AddScoped<IRobotService, RobotService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IFriendshipService, FriendshipService>();
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IMapService, MapService>();
            builder.Services.AddScoped<IRoutingService, RoutingService>();

            // Utilities
            builder.Services.AddScoped<IPasswordHasher, Sha256PasswordHasher>();
            builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

            // IoT/Drone Communication
            builder.Services.AddHttpClient("DroneClient");
            builder.Services.AddScoped<IDroneConnectionService, DroneConnectionService>();

            // Payment services
            builder.Services.AddScoped<PayPalPaymentService>();
            builder.Services.AddScoped<GooglePayPaymentService>();
            builder.Services.AddScoped<StripePaymentService>();
            builder.Services.AddScoped<IPaymentProcessorService, PaymentProcessorService>();

            // Add HttpContextAccessor for accessing HTTP context in services
            builder.Services.AddHttpContextAccessor();

            // Add SignalR
            builder.Services.AddSignalR();

            // Configure JWT Authentication
            var jwtKey = config.Jwt.Key;
            var jwtIssuer = config.Jwt.Issuer;
            var jwtAudience = config.Jwt.Audience;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                // Allow SignalR to authenticate via query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.SetIsOriginAllowed(_ => true)  // Разрешает запросы с любого источника
                               .AllowAnyMethod()                // Разрешает любые HTTP-методы
                               .AllowAnyHeader()                // Разрешает любые HTTP-заголовки
                               .AllowCredentials();             // Разрешает передачу credentials для SignalR
                    });
            });

            builder.Services.AddAuthorization();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Serialize enums as strings instead of numbers
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "RobDelivery API",
                    Version = "v1",
                    Description = "API for robotic delivery system with JWT authentication"
                });

                // Add JWT Authentication to Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Support for IFormFile in Swagger
                options.MapType<IFormFile>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });

                options.MapType<IFormFileCollection>(() => new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                });
            });

            var app = builder.Build();

            // Apply migrations automatically
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                try
                {
                    dbContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
                    // Fallback for development if Migrate fails (e.g., if DB was created with EnsureCreated previously)
                    dbContext.Database.EnsureCreated();
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();
            app.UseCors("AllowAllOrigins");

            // Enable serving static files from Uploads directory
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(app.Environment.ContentRootPath, "Uploads")),
                RequestPath = "/Uploads"
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<RobDeliveryAPI.Hubs.MapHub>("/hubs/map");

            app.Run();
        }
    }
}