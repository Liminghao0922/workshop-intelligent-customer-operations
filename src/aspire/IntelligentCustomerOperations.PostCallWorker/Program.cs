using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IntelligentCustomerOperations.PostCallWorker;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddSingleton<CallAnalyzer>();
    })
    .Build();

host.Run();

