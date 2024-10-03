using AIForgedPollingExample;
using AIForgedPollingExample.Models;

using System.Text.Json;

//Using the builder pattern to configure our worker service
var builder = Host.CreateDefaultBuilder(args);

//Add different configuration methods via builder pattern
builder.ConfigureHostConfiguration(config =>
{
    //You can enable environment variables from the container if you want to configure your service that way.
    //config.AddEnvironmentVariables();

    //You can enable loading config from user secrets if you want to configure your service that way.
    //config.AddUserSecrets("[UserSecretsId]");
});

//Configure our app services
builder.ConfigureServices((hostContext, config) =>
{
    //Get application configuration from the various configuration methods
    IConfiguration configuration = hostContext.Configuration;

    //Build our app Config from appsettings.json
    Config configFile = new Config()
    {
        EndPoint = configuration.GetRequiredSection("Config:Endpoint").Value,
        Username = configuration.GetRequiredSection("Config:Username").Value,
        Password = configuration.GetRequiredSection("Config:Password").Value,
        ApiKey = configuration.GetRequiredSection("Config:ApiKey").Value,
        ProjectId = int.Parse(configuration.GetRequiredSection("Config:ProjectId").Value),
        ServiceId = int.Parse(configuration.GetRequiredSection("Config:ServiceId").Value),
        StartDateTimeSpan = TimeSpan.Parse(configuration.GetRequiredSection("Config:StartDateTimeSpan").Value),
        Interval = TimeSpan.Parse(configuration.GetRequiredSection("Config:Interval").Value)
    };

    //Add our config as a an app lifetime service
    config.AddSingleton<IConfig>(configFile);

    //Add our worker
    config.AddHostedService<Worker>();

    //Add some logging
    config.AddLogging(l =>
    {
        l.AddDebug();
    });
});

var host = builder.Build();
host.Run();
