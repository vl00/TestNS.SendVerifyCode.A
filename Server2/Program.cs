using Common.Net;
using Common.Net.Virtuals;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics;
using TestNS;
using TestNS.Controllers;
using TestNS.ns2;
using WebAppl2.Common;

//-----------------------------------------------------------------------------------------------------------
var appBuilder = WebApplication.CreateBuilder(args);
ConfigureAppsettings(appBuilder.Environment, appBuilder.Configuration);
ConfigureLogging(appBuilder.Host, appBuilder.Logging, appBuilder.Configuration);
appBuilder.Services.AddHostedService(sp => 
{
    var f = new FuncHostedLifecycleService(sp);
    f.OnApplicationStarting = (s, _) => OnApplicationStarting(s);
    f.OnApplicationStopping = (s, _) => OnApplicationStopping(s);
    return f;
});
ConfigureServices(appBuilder.Services, appBuilder.Configuration);
//-----------------------------------------------------------------------------------------------------------
await using var app = appBuilder.Build();
ConfigureAppUse(app);
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
await app.RunAsync();
Log.CloseAndFlush();
//-----------------------------------------------------------------------------------------------------------

void ConfigureAppsettings(IWebHostEnvironment environment, IConfigurationBuilder configuration)
{
}

void ConfigureLogging(IHostBuilder hostBuilder, ILoggingBuilder builder, IConfiguration configuration)
{
    builder.ClearProviders();

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        //
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} ({SourceContext})] {Message:lj}{NewLine}{Exception}"
        )
        //      
        // 输出json. 该json的fields字段包括上下文的附加字段
        //.WriteTo.Console(new Serilog.Formatting.Elasticsearch.ElasticsearchJsonFormatter())
        //
        .CreateLogger();

    hostBuilder.UseSerilog();
}

void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    services.AddOptions();
    services.AddHttpContextAccessor();

    var address = config["namedpipe:address"].Replace("{rnd}", Guid.NewGuid().ToString("n")[..8]);

    services.AddControllersWithViews()
        //.AddJsonOptions(opts => { }) // utf8-json
        .AddNewtonsoftJson(opts => 
        {
            opts.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            opts.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            opts.SerializerSettings.Converters.Add(new Fn2ResultNewtJsonConverter());
        })
        ; //

    services.AddSwaggerEx(typeof(TestController), "Server2", "v1");

    // 
    services.AddTransient<IConnectionListener>(sp =>
    {
        return new Common.Net.Servers.NamedPipeConnectionListener(address, Convert.ToInt32(config["namedpipe:maxbody"]));
    });
    services.AddTransient(sp => new BytesPipelineOption
    {
        Options1 = new(minimumSegmentSize: 16, pauseWriterThreshold: 1024, resumeWriterThreshold: 512, useSynchronizationContext: false),
        Options2 = new(minimumSegmentSize: 16, pauseWriterThreshold: 1024, resumeWriterThreshold: 512, useSynchronizationContext: false),
    });

    services.AddVirtualConnection();
    services.AddTransient<IResolveHelper, ResolveHelper>();

    services.AddSingleton<WrapItems>();
    services.AddSingleton<JobWorking>();

    services.AddSingleton(sp =>
    {
        return ActivatorUtilities.CreateInstance(sp, typeof(ProcessManager), sp.GetService<IOptions<ProcessOption>>().Value) as ProcessManager;
    });
    services.AddOptions<ProcessOption>().Configure(opt =>
    {
        opt.Address = address;
        opt.Exe = Path.Combine(Directory.GetCurrentDirectory(), config["process:exe"]);
		opt.A = config["process:a"];
		if (opt.A is null or "") opt.A = Path.GetDirectoryName(opt.Exe);
        opt.Wd = config["process:wd"]?.Replace('\\', '/');
        if (opt.Wd is null) opt.Wd = Path.GetDirectoryName(opt.Exe);
        else if (opt.Wd is "" or ".") opt.Wd = Directory.GetCurrentDirectory();
        else opt.Wd = Path.Combine(Directory.GetCurrentDirectory(), opt.Wd.StartsWith("./") ? opt.Wd[2..] : opt.Wd);
    });
}

void ConfigureAppUse(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        // ...
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // ...
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    //app.UseAuthorization();


    app.MapGet("/", () => "Hello World");

    app.UseSwaggerEx("Server2", "v1");

    app.MapControllerRoute(
        name: "Area",
        pattern: "{area:exists}/{controller=Values}/{action=Test}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
}

async Task OnApplicationStarting(IServiceProvider services)
{
    services.GetService<ILoggerFactory>().CreateLogger("app").LogInformation("web app started");
    //Debugger.Break();
    await default(ValueTask);

    ActivatorUtilities.GetServiceOrCreateInstance<Program_test3_server>(services).OnRun();
}

async Task OnApplicationStopping(IServiceProvider services)
{
    services.GetService<ILoggerFactory>().CreateLogger("app").LogInformation("web app stopping");
    await default(ValueTask);
}

void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    var services = app.Services;
    var log = services.GetService<ILoggerFactory>().CreateLogger("app");

    log.LogDebug("AppDomain.CurrentDomain UnhandledException {IsTerminating}, {o}", e.IsTerminating, e.ExceptionObject?.GetType());
}
