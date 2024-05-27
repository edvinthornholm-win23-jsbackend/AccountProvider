using Data.Contexts;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Messaging.ServiceBus;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddDbContext<DataContext>(x => x.UseSqlServer(context.Configuration.GetConnectionString("AccountDatabase")));

        services.AddIdentity<UserAccount, IdentityRole>(x =>
        {
            x.SignIn.RequireConfirmedAccount = true;
            x.User.RequireUniqueEmail = true;
            x.Password.RequiredLength = 8;
        }).AddEntityFrameworkStores<DataContext>();

        services.AddAuthentication();
        services.AddAuthorization();

        // Lägg till ServiceBusClient registrering här
        services.AddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration["ServiceBusConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Service Bus connection string cannot be null or empty.");
            }
            return new ServiceBusClient(connectionString);
        });

        // Registrera andra tjänster om det behövs
    })
    .Build();

host.Run();
