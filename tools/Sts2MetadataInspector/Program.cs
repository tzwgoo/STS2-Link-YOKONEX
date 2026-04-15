using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: Sts2MetadataInspector <assembly-path> [pattern1] [pattern2] ...");
    return 1;
}

var assemblyPath = args[0];
var patterns = args.Skip(1).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray();

using var stream = File.OpenRead(assemblyPath);
using var peReader = new PEReader(stream);
var metadataReader = peReader.GetMetadataReader();

Console.WriteLine($"Assembly: {Path.GetFileName(assemblyPath)}");
Console.WriteLine($"Patterns: {(patterns.Length == 0 ? "<none>" : string.Join(", ", patterns))}");

foreach (var typeHandle in metadataReader.TypeDefinitions)
{
    var typeDef = metadataReader.GetTypeDefinition(typeHandle);
    var ns = metadataReader.GetString(typeDef.Namespace);
    var name = metadataReader.GetString(typeDef.Name);
    var fullTypeName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

    var methodMatches = new List<string>();
    var propertyMatches = new List<string>();
    var fieldMatches = new List<string>();
    foreach (var methodHandle in typeDef.GetMethods())
    {
        var methodDef = metadataReader.GetMethodDefinition(methodHandle);
        var methodName = metadataReader.GetString(methodDef.Name);
        if (patterns.Length == 0 || patterns.Any(pattern => ContainsIgnoreCase(methodName, pattern) || ContainsIgnoreCase(fullTypeName, pattern)))
        {
            methodMatches.Add(methodName);
        }
    }

    foreach (var propertyHandle in typeDef.GetProperties())
    {
        var propertyDef = metadataReader.GetPropertyDefinition(propertyHandle);
        var propertyName = metadataReader.GetString(propertyDef.Name);
        if (patterns.Length == 0 || patterns.Any(pattern => ContainsIgnoreCase(propertyName, pattern) || ContainsIgnoreCase(fullTypeName, pattern)))
        {
            propertyMatches.Add(propertyName);
        }
    }

    foreach (var fieldHandle in typeDef.GetFields())
    {
        var fieldDef = metadataReader.GetFieldDefinition(fieldHandle);
        var fieldName = metadataReader.GetString(fieldDef.Name);
        if (patterns.Length == 0 || patterns.Any(pattern => ContainsIgnoreCase(fieldName, pattern) || ContainsIgnoreCase(fullTypeName, pattern)))
        {
            fieldMatches.Add(fieldName);
        }
    }

    if (patterns.Length == 0)
    {
        if (methodMatches.Count > 0 || propertyMatches.Count > 0 || fieldMatches.Count > 0)
        {
            Console.WriteLine(fullTypeName);
            foreach (var fieldName in fieldMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(20))
            {
                Console.WriteLine($"  # {fieldName}");
            }
            foreach (var propertyName in propertyMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(20))
            {
                Console.WriteLine($"  * {propertyName}");
            }
            foreach (var methodName in methodMatches.Take(20))
            {
                Console.WriteLine($"  - {methodName}");
            }
        }

        continue;
    }

    var typeMatched = patterns.Any(pattern => ContainsIgnoreCase(fullTypeName, pattern));
    if (!typeMatched && methodMatches.Count == 0 && propertyMatches.Count == 0 && fieldMatches.Count == 0)
    {
        continue;
    }

    Console.WriteLine(fullTypeName);
    foreach (var fieldName in fieldMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(40))
    {
        Console.WriteLine($"  # {fieldName}");
    }
    foreach (var propertyName in propertyMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(40))
    {
        Console.WriteLine($"  * {propertyName}");
    }
    foreach (var methodName in methodMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(40))
    {
        Console.WriteLine($"  - {methodName}");
    }
}

return 0;

static bool ContainsIgnoreCase(string source, string value) =>
    source.Contains(value, StringComparison.OrdinalIgnoreCase);
