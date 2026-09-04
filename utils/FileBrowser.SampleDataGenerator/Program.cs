using System.Reflection;
using Microsoft.Extensions.Configuration;

const string settingsFileName = "appsettings.json";

try
{
    var settingsPath = Path.Combine(AppContext.BaseDirectory, settingsFileName);
    if (!File.Exists(settingsPath))
    {
        throw new FileNotFoundException($"Could not find {settingsFileName} at '{settingsPath}'.");
    }

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile(settingsFileName, optional: false)
        .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
        .Build();

    var sampleData = configuration
        .GetRequiredSection("SampleData")
        .Get<SampleDataOptions>()
        ?? throw new InvalidOperationException($"{settingsFileName} is empty or invalid.");

    sampleData.Validate();
    await new SampleDataGenerator(sampleData).GenerateAsync();
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Sample data generation failed: {exception.Message}");
    Environment.ExitCode = 1;
}
