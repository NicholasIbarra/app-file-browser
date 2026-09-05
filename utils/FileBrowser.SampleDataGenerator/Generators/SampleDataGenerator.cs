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
                var document = CreateDocument(folders[folderIndex], fileIndex);
                var fileName = UniqueName(usedNames, () => document.Title, ".txt");
                await destination.WriteTextAsync(CombinePath(folders[folderIndex].Path, fileName), document.Content);
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
        var parent = new GeneratedFolder(string.Empty, 0, null, null, string.Empty);

        // Establish a chain so the configured minimum recursion depth is always reached.
        for (var depth = 1; depth <= options.MinFolderDepth; depth++)
        {
            parent = await CreateFolderAsync(parent, namesByParent, destination);
            folders.Add(parent);
        }

        while (folders.Count < options.TotalFolders)
        {
            var possibleParents = folders.Where(folder => folder.Depth < options.MaxFolderDepth
                && (folder.Depth != 1 || options.MaxFolderDepth == 2
                    || !namesByParent.TryGetValue(folder.Path, out var children)
                    || children.Count < folder.Department!.Teams.Length)).ToList();
            if (folders.Count(folder => folder.Depth == 1) < BusinessCatalog.Departments.Length || options.MaxFolderDepth == 1)
                possibleParents.Add(new GeneratedFolder(string.Empty, 0, null, null, string.Empty));
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

        var department = parent.Department ?? BusinessCatalog.Departments[namesByParent[parent.Path].Count % BusinessCatalog.Departments.Length];
        var team = parent.Team ?? department.Teams[usedNames.Count % department.Teams.Length];
        var owner = parent.Depth < 3 ? _faker.Name.FullName() : parent.Owner;
        var label = parent.Depth switch
        {
            0 => department.Name,
            1 => team.Name,
            2 => owner,
            3 => team.Initiative,
            4 => new[] { "Planning", "Delivery", "Reporting", "Governance" }[usedNames.Count % 4],
            _ => new[] { "Planning", "Working Papers", "Reviews", "Approvals", "Archive" }[usedNames.Count % 5]
        };
        var folderName = UniqueName(usedNames, () => label);
        var folder = new GeneratedFolder(CombinePath(parent.Path, folderName), parent.Depth + 1,
            department, parent.Depth == 0 ? null : team, owner);
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

    private (string Title, string Content) CreateDocument(GeneratedFolder folder, int index)
    {
        var department = folder.Department!;
        var team = folder.Team ?? department.Teams[index % department.Teams.Length];
        // Fixed reporting periods make seeded runs independent of the current date.
        var period = new DateTime(2025, 1, 1).AddMonths(index / 5);
        var kind = index % 5;
        var type = new[] { team.Deliverable, "Status Update", "Risk Register", "Decision Record", "Action Plan" }[kind];
        var title = $"{period:yyyy-MM} {team.Initiative} - {type}";
        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine("Company: Northstar Business Services (fictional sample data)");
        builder.AppendLine($"Department: {department.Name}");
        builder.AppendLine($"Team: {team.Name}");
        builder.AppendLine($"Accountable owner: {folder.Owner}");
        builder.AppendLine($"Location: {folder.Path}");
        builder.AppendLine($"Initiative: {team.Initiative}");
        builder.AppendLine($"Reporting period: {period:yyyy-MM}");
        builder.AppendLine();
        builder.AppendLine($"Purpose: {team.Purpose}");
        builder.AppendLine();
        builder.AppendLine(kind switch
        {
            0 => $"FINDINGS AND SUPPORTING EVIDENCE\n{team.Evidence}\n\nRECOMMENDATION\n{team.Decision}",
            1 => $"PROGRESS\n{team.Evidence}\n\nBLOCKER\n{team.Risk}\n\nNEXT MILESTONE\n{team.Action}",
            2 => $"RISK DESCRIPTION\n{team.Risk}\n\nEXPOSURE AND CONTEXT\n{team.Evidence}\n\nMITIGATION\n{team.Action}",
            3 => $"DECISION\n{team.Decision}\n\nRATIONALE\n{team.Evidence}\n\nCONSTRAINT\n{team.Risk}",
            _ => $"REQUIRED ACTION\n{team.Action}\n\nACCEPTANCE CRITERIA\n{team.Decision}\n\nDEPENDENCY\n{team.Risk}"
        });
        builder.AppendLine();
        builder.AppendLine($"Follow-up: {folder.Owner} will review the evidence on {period.AddDays(20):yyyy-MM-dd} and record unresolved items in the {team.Initiative} risk register.");
        builder.AppendLine($"Related records: {period:yyyy-MM} {team.Initiative} - {team.Deliverable}; {period:yyyy-MM} {team.Initiative} - Action Plan.");
        return (title, builder.ToString());
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

    private sealed record GeneratedFolder(string Path, int Depth, BusinessDepartment? Department, BusinessTeam? Team, string Owner);
}
