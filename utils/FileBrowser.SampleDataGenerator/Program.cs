using System.Text.Json;

const string settingsFileName = "appsettings.json";

try
{
    var settingsPath = Path.Combine(AppContext.BaseDirectory, settingsFileName);
    if (!File.Exists(settingsPath))
    {
        throw new FileNotFoundException($"Could not find {settingsFileName} at '{settingsPath}'.");
    }

    var json = await File.ReadAllTextAsync(settingsPath);
    var appSettings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? throw new InvalidOperationException($"{settingsFileName} is empty or invalid.");

    appSettings.SampleData.Validate();
    await new SampleDataGenerator(appSettings.SampleData).GenerateAsync();
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Sample data generation failed: {exception.Message}");
    Environment.ExitCode = 1;
}
