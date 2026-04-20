using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Collections.Immutable;

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
            methodMatches.Add(BuildMethodSignature(metadataReader, methodDef, methodName));
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

static string BuildMethodSignature(MetadataReader metadataReader, MethodDefinition methodDef, string methodName)
{
    var signature = methodDef.DecodeSignature(new TypeNameProvider(metadataReader), genericContext: null);
    var parameters = signature.ParameterTypes.Length == 0
        ? string.Empty
        : string.Join(", ", signature.ParameterTypes);
    return $"{signature.ReturnType} {methodName}({parameters})";
}

file sealed class TypeNameProvider(MetadataReader metadataReader) : ISignatureTypeProvider<string, object?>
{
    public string GetArrayType(string elementType, ArrayShape shape) => $"{elementType}[{new string(',', shape.Rank - 1)}]";

    public string GetByReferenceType(string elementType) => $"{elementType}&";

    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
        $"{genericType}<{string.Join(", ", typeArguments)}>";

    public string GetGenericMethodParameter(object? genericContext, int index) => $"!!{index}";

    public string GetGenericTypeParameter(object? genericContext, int index) => $"!{index}";

    public string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired) => unmodifiedType;

    public string GetPinnedType(string elementType) => elementType;

    public string GetPointerType(string elementType) => $"{elementType}*";

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Void => "void",
        PrimitiveTypeCode.Boolean => "bool",
        PrimitiveTypeCode.Char => "char",
        PrimitiveTypeCode.SByte => "sbyte",
        PrimitiveTypeCode.Byte => "byte",
        PrimitiveTypeCode.Int16 => "short",
        PrimitiveTypeCode.UInt16 => "ushort",
        PrimitiveTypeCode.Int32 => "int",
        PrimitiveTypeCode.UInt32 => "uint",
        PrimitiveTypeCode.Int64 => "long",
        PrimitiveTypeCode.UInt64 => "ulong",
        PrimitiveTypeCode.Single => "float",
        PrimitiveTypeCode.Double => "double",
        PrimitiveTypeCode.String => "string",
        PrimitiveTypeCode.IntPtr => "nint",
        PrimitiveTypeCode.UIntPtr => "nuint",
        PrimitiveTypeCode.Object => "object",
        _ => typeCode.ToString()
    };

    public string GetSZArrayType(string elementType) => $"{elementType}[]";

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var definition = reader.GetTypeDefinition(handle);
        var ns = reader.GetString(definition.Namespace);
        var name = reader.GetString(definition.Name);
        return string.IsNullOrWhiteSpace(ns) ? name : $"{ns}.{name}";
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var reference = reader.GetTypeReference(handle);
        var ns = reader.GetString(reference.Namespace);
        var name = reader.GetString(reference.Name);
        return string.IsNullOrWhiteSpace(ns) ? name : $"{ns}.{name}";
    }

    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        var specification = reader.GetTypeSpecification(handle);
        return specification.DecodeSignature(this, genericContext);
    }
}
