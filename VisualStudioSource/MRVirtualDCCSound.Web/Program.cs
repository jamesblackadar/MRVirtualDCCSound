using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Session;
using MRVirtualDCCSound.Core;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddLogging(l =>
{
    l.ClearProviders();
    l.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    l.AddNLogWeb("nlog.config");
});
builder.Host.UseNLog();

const string rootFolder = @"c:\dcc2";
var storageRepositoryGlobalSettings = new StorageRepository<GlobalSettings>(rootFolder);

var nlogFactory = NLogBuilder.ConfigureNLog("nlog.config");
var logger = nlogFactory.GetLogger("programmain");
var sch = new SerialConnectionHelper();
SerialConnectionSettings serialSettings=null;// = gs.Read(); GlobalSettings.GetDefaultSerialConnectionSettings();
try
{
    var globalSettings = storageRepositoryGlobalSettings.Read();
    serialSettings = globalSettings.ConnectionSettings;
    sch.Initialize(serialSettings);
}catch
{
    logger.Error("Couldn't initialize serial port with " + JsonConvert.SerializeObject(serialSettings));
    sch.Initialize(GlobalSettings.GetDefaultSerialConnectionSettings());    
}

builder.Services.AddSingleton<GlobalSettings>();
builder.Services.AddSingleton<SerialConnectionHelper, SerialConnectionHelper>(x=>sch);
builder.Services.AddSingleton<IMobileEventProvider, DCCArduinoSerialProvider>();
builder.Services.AddSingleton<IStorageRepository<GlobalSettings>>(x => storageRepositoryGlobalSettings);
builder.Services.AddSingleton<IStorageRepository<List<MobileSFXBase>>>(x => new StorageRepository<List<MobileSFXBase>>(rootFolder));
builder.Services.AddSingleton<RosterCollection>();



var app = builder.Build();
var roster = app.Services.GetRequiredService<RosterCollection>();
roster.Load();




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
