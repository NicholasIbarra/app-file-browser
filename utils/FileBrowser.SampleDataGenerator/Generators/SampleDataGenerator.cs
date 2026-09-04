using System.Text;
using Bogus;

internal sealed class SampleDataGenerator(SampleDataOptions options)
{
    private readonly Random _random = options.Seed is { } seed ? new Random(seed) : Random.Shared;
    private readonly Faker _faker = CreateFaker(options.Seed);

    public async Task GenerateAsync()
    {
        var destination = SampleDataDestination.Create(options);
        await destination.InitializeAsync();

        Console.WriteLine($"Generating {options.TotalFiles:N0} text files in {options.TotalFolders:N0} folders...");
        Console.WriteLine($"Provider: {options.Provider}");
        Console.WriteLine($"Output: {destination.Description}");

        var folders = await CreateFoldersAsync(destination);
        var fileCounts = AllocateFiles(folders.Count);
        var createdFiles = 0;

        for (var folderIndex = 0; folderIndex < folders.Count; folderIndex++)
        {
            var usedNames = (await destination.GetEntryNamesAsync(folders[folderIndex].Path))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var fileIndex = 0; fileIndex < fileCounts[folderIndex]; fileIndex++)
            {
                var fileName = UniqueName(usedNames, CreateFileName, ".txt");
                await destination.WriteTextAsync(CombinePath(folders[folderIndex].Path, fileName), CreateFileContent());
                createdFiles++;
            }

            if ((folderIndex + 1) % 25 == 0 || folderIndex == folders.Count - 1)
            {
                Console.Write($"\rFolders: {folderIndex + 1:N0}/{folders.Count:N0}  Files: {createdFiles:N0}/{options.TotalFiles:N0}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Sample data generation complete.");
    }

    private async Task<List<GeneratedFolder>> CreateFoldersAsync(ISampleDataDestination destination)
    {
        var folders = new List<GeneratedFolder>(options.TotalFolders);
        var namesByParent = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var parent = new GeneratedFolder(string.Empty, 0);

        // Establish a chain so the configured minimum recursion depth is always reached.
        for (var depth = 1; depth <= options.MinFolderDepth; depth++)
        {
            parent = await CreateFolderAsync(parent, namesByParent, destination);
            folders.Add(parent);
        }

        while (folders.Count < options.TotalFolders)
        {
            var possibleParents = folders.Where(folder => folder.Depth < options.MaxFolderDepth).ToList();
            possibleParents.Add(new GeneratedFolder(string.Empty, 0));
            parent = possibleParents[_random.Next(possibleParents.Count)];
            folders.Add(await CreateFolderAsync(parent, namesByParent, destination));
        }

        return folders;
    }

    private async Task<GeneratedFolder> CreateFolderAsync(
        GeneratedFolder parent,
        Dictionary<string, HashSet<string>> namesByParent,
        ISampleDataDestination destination)
    {
        if (!namesByParent.TryGetValue(parent.Path, out var usedNames))
        {
            usedNames = (await destination.GetEntryNamesAsync(parent.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            namesByParent[parent.Path] = usedNames;
        }

        var folderName = UniqueName(usedNames, CreateFolderName);
        var folder = new GeneratedFolder(CombinePath(parent.Path, folderName), parent.Depth + 1);
        await destination.CreateFolderAsync(folder.Path);
        return folder;
    }

    private int[] AllocateFiles(int folderCount)
    {
        var result = Enumerable.Repeat(options.MinFilesPerFolder, folderCount).ToArray();
        var remaining = options.TotalFiles - result.Sum();

        while (remaining > 0)
        {
            var index = _random.Next(folderCount);
            if (result[index] >= options.MaxFilesPerFolder)
            {
                continue;
            }

            result[index]++;
            remaining--;
        }

        return result;
    }

    private string CreateFolderName() => _random.Next(4) switch
    {
        0 => _faker.Commerce.Department(),
        1 => _faker.Company.CompanyName(),
        2 => $"{_faker.Date.Past(8).Year}-{_faker.Hacker.Noun()}",
        _ => $"{_faker.Address.City()}-{_faker.Hacker.Noun()}"
    };

    private string CreateFileName() => _random.Next(5) switch
    {
        0 => $"{_faker.Hacker.Adjective()}-{_faker.Hacker.Noun()}",
        1 => $"{_faker.Name.LastName()}-{_faker.Hacker.Noun()}",
        2 => $"{_faker.Company.CatchPhrase()}-{_faker.Date.Recent(730):yyyy-MM-dd}",
        3 => $"{_faker.Commerce.ProductName()}-{_faker.Random.AlphaNumeric(6)}",
        _ => $"{_faker.Lorem.Slug(3)}-{_faker.Random.Number(1000, 9999)}"
    };

    private string CreateFileContent()
    {
        var builder = new StringBuilder();
        builder.AppendLine(_faker.Company.CatchPhrase());
        builder.AppendLine($"Owner: {_faker.Name.FullName()} <{_faker.Internet.Email()}>");
        builder.AppendLine($"Host: {_faker.Internet.DomainName()}");
        builder.AppendLine($"Updated: {_faker.Date.Recent(730):O}");
        builder.AppendLine();

        builder.AppendLine(_faker.Lorem.Paragraphs(_random.Next(2, 7)));

        return builder.ToString();
    }

    private static Faker CreateFaker(int? seed)
    {
        var faker = new Faker("en");
        if (seed is { } value)
        {
            faker.Random = new Randomizer(value);
        }

        return faker;
    }

    private static string UniqueName(HashSet<string> usedNames, Func<string> factory, string extension = "")
    {
        for (var attempt = 0; ; attempt++)
        {
            var baseName = SanitizeName(factory());
            var suffix = attempt == 0 ? string.Empty : $"-{attempt}";
            var maxBaseLength = Math.Max(1, 100 - suffix.Length - extension.Length);
            if (baseName.Length > maxBaseLength)
            {
                baseName = baseName[..maxBaseLength].TrimEnd(' ', '.');
            }

            var name = $"{baseName}{suffix}{extension}";
            if (usedNames.Add(name))
            {
                return name;
            }
        }
    }

    private static string SanitizeName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray())
            .Trim(' ', '.');

        return string.IsNullOrWhiteSpace(sanitized) ? "item" : sanitized;
    }

    private static string CombinePath(string parent, string name) =>
        string.IsNullOrEmpty(parent) ? name : $"{parent}/{name}";

    private sealed record GeneratedFolder(string Path, int Depth);
}
