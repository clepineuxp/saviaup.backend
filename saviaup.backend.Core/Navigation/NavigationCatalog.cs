namespace SaviaUp.Backend.Core.Navigation;

internal sealed record NavigationModuleDefinition(string Code, int Order);

internal sealed record NavigationOptionDefinition(
    string Code,
    string ModuleCode,
    string RequiredPermissionCode,
    int Order);

internal sealed record NavigationSectionDefinition(
    string Code,
    int Order,
    IReadOnlyCollection<NavigationModuleDefinition> Modules,
    IReadOnlyCollection<NavigationOptionDefinition> Options);

internal static class NavigationCatalog
{
    public static readonly IReadOnlyCollection<NavigationSectionDefinition> Sections =
    [
        new("sales", 1,
            [new("tables", 1)],
            []),
        new("operation", 2,
            [
                new("orders", 1),
                new("reports", 2),
                new("billing", 3)
            ],
            []),
        new("inventory", 3,
            [
                new("products", 1),
                new("categories", 2),
                new("inventory", 3),
                new("kitchen", 4)
            ],
            []),
        new("configuration", 4,
            [new("settings", 1)],
            [new("tables.manage", "tables", "tables.manage", 2)])
    ];

    public static readonly IReadOnlyDictionary<string, NavigationModuleDefinition> Modules = CreateModuleIndex();

    private static IReadOnlyDictionary<string, NavigationModuleDefinition> CreateModuleIndex()
    {
        EnsureUniqueStrings(Sections.Select(section => section.Code), "section code");
        EnsureUnique(Sections.Select(section => section.Order), "section order");
        EnsurePositive(Sections.Select(section => section.Order), "section order");

        foreach (var section in Sections)
        {
            EnsureUniqueStrings(section.Modules.Select(module => module.Code), $"module code in section '{section.Code}'");
            EnsureUnique(
                section.Modules.Select(module => module.Order).Concat(section.Options.Select(option => option.Order)),
                $"item order in section '{section.Code}'");
            EnsurePositive(
                section.Modules.Select(module => module.Order).Concat(section.Options.Select(option => option.Order)),
                $"item order in section '{section.Code}'");
            if (section.Options.Any(option => !option.RequiredPermissionCode.EndsWith(".manage", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Navigation options in section '{section.Code}' must require a .manage permission.");
            }
        }

        var modules = Sections
            .SelectMany(section => section.Modules)
            .ToDictionary(module => module.Code, StringComparer.OrdinalIgnoreCase);
        var options = Sections.SelectMany(section => section.Options).ToArray();
        EnsureUniqueStrings(options.Select(option => option.Code), "option code");
        var unknownOptionModule = options
            .FirstOrDefault(option => !modules.ContainsKey(option.ModuleCode));
        if (unknownOptionModule is not null)
        {
            throw new InvalidOperationException(
                $"Navigation option '{unknownOptionModule.Code}' references unknown module '{unknownOptionModule.ModuleCode}'.");
        }

        return modules;
    }

    private static void EnsureUnique<T>(IEnumerable<T> values, string label) where T : notnull
    {
        var items = values.ToArray();
        if (items.Distinct().Count() != items.Length)
            throw new InvalidOperationException($"NavigationCatalog contains a duplicate {label}.");
    }

    private static void EnsureUniqueStrings(IEnumerable<string> values, string label)
    {
        var items = values.ToArray();
        if (items.Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Length)
            throw new InvalidOperationException($"NavigationCatalog contains a duplicate {label}.");
    }

    private static void EnsurePositive(IEnumerable<int> values, string label)
    {
        if (values.Any(value => value <= 0))
            throw new InvalidOperationException($"NavigationCatalog requires a positive {label}.");
    }
}
