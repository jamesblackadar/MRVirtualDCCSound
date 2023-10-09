// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MRVirtualDCCSound.Core;
using NLog;
using NLog.Extensions.Logging;
using System.Text.Json;

Microsoft.Extensions.Logging.ILogger _il = null;
try
{
    var host = Host.CreateDefaultBuilder();
    //host.ConfigureServices(s => s.AddLogging(l => l    
    //.ClearProviders()
    //.AddNLog("nlog.config")
    //.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
    //));
    host.ConfigureLogging(x => x.AddNLog("nlog.config"));
    var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                //
                //.ClearProviders()
                .AddConsole()
                .AddNLog("nlog.config")
            .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    

        });
    //loggerFactory.ConfigureNLog("nlog.config");
  

    _il = loggerFactory.CreateLogger<Program>();
    _il.LogDebug("Starting test");

    const string rootFolder = @"c:\dcc2";
    var storageRepositoryGlobalSettings = new StorageRepository<GlobalSettings>(rootFolder);

    var sch = new SerialConnectionHelper();
    SerialConnectionSettings settings = null;// = gs.Read(); GlobalSettings.GetDefaultSerialConnectionSettings();
    try
    {
        var globalSettings = storageRepositoryGlobalSettings.Read();
        settings = globalSettings.ConnectionSettings;
        sch.Initialize(settings);
    }
    catch
    {
        if (settings == null)
        {
            _il.LogError("Couldn't initialize serial port with null settings");
        }
        else
        {
            _il.LogError("Couldn't initialize serial port with " + JsonSerializer.Serialize<SerialConnectionSettings>(settings));
        }
        sch.Initialize(GlobalSettings.GetDefaultSerialConnectionSettings());
    }


    // var sch = new SerialConnectionHelper();
    // sch.Initialize("com1", 9200, System.IO.Ports.Parity.None, 8);
    host.ConfigureServices(s => s
    .AddSingleton<GlobalSettings>()
    .AddSingleton<SerialConnectionHelper, SerialConnectionHelper>(x => sch)
    .AddSingleton<IMobileEventProvider, DCCArduinoSerialProvider>()
    .AddSingleton<IStorageRepository<GlobalSettings>>(x => storageRepositoryGlobalSettings)
    .AddSingleton<IStorageRepository<List<MobileSFXBase>>>(x => new StorageRepository<List<MobileSFXBase>>(rootFolder))
    .AddSingleton<RosterCollection>()
    );

    var app = host.Build();
    //_il = app.Services.GetRequiredService<ILogger<Program>>();
    _il.LogDebug("Starting test");
    var roster = app.Services.GetRequiredService<RosterCollection>();
    roster.Load();
    _il.LogInformation("Found the following decoders...which would you like to test (note use commas to specify more than one decoder)?" + string.Join(";",roster.Select(x=>x.Id.ToString())));
    var choice = Console.ReadLine();
    if(string.IsNullOrEmpty(choice)) { _il.LogInformation("No choice made...substituting default");choice = "7509,4245"; }
    var choices = choice?.Split(',');
    List<Task> tasks = new List<Task>();
    foreach(var c in choices)
    {
        var decoder = roster.Where(x => x.Id.ToString().Equals(c)).FirstOrDefault();
        if (decoder != null) tasks.Add(decoder.TestLoopAsync());
    }
    await Task.WhenAll(tasks);
    _il.LogDebug("Complete!");
}
catch (Exception ex)
{
    _il?.LogCritical(ex, "Error in Console Program Main");
    Console.WriteLine(ex.ToString());
}
Console.WriteLine("Done");
Console.ReadLine();
