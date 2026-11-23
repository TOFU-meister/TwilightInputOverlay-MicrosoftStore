using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: ReleaseDllCheck <dll> <assembly-and-guid> <version> <license-resource>");
    return 2;
}

try
{
    string path = args[0];
    string name = args[1];
    string version = args[2];
    using var stream = File.OpenRead(path);
    using var pe = new PEReader(stream);
    Require(pe.PEHeaders.CoffHeader.Machine == Machine.Amd64
        && pe.PEHeaders.PEHeader?.Magic == PEMagic.PE32Plus, "The DLL must target Windows x64.");

    var metadata = pe.GetMetadataReader();
    var assembly = metadata.GetAssemblyDefinition();
    Require(metadata.GetString(assembly.Name) == name, "Unexpected assembly name.");
    Require(FileVersionInfo.GetVersionInfo(path).ProductVersion == version,
        $"DLL ProductVersion must be {version}.");

    int pluginAttributes = 0;
    foreach (var typeHandle in metadata.TypeDefinitions)
    {
        foreach (var attributeHandle in metadata.GetTypeDefinition(typeHandle).GetCustomAttributes())
        {
            var attribute = metadata.GetCustomAttribute(attributeHandle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference)
                continue;
            var constructor = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            if (constructor.Parent.Kind != HandleKind.TypeReference)
                continue;
            var type = metadata.GetTypeReference((TypeReferenceHandle)constructor.Parent);
            if (metadata.GetString(type.Namespace) != "BepInEx" || metadata.GetString(type.Name) != "BepInPlugin")
                continue;

            pluginAttributes++;
            var blob = metadata.GetBlobReader(attribute.Value);
            Require(blob.ReadUInt16() == 1, "Invalid BepInPlugin attribute.");
            Require(blob.ReadSerializedString() == name, "Unexpected BepInEx plugin GUID.");
            _ = blob.ReadSerializedString(); // display name
            Require(blob.ReadSerializedString() == version, $"BepInEx plugin version must be {version}.");
        }
    }
    Require(pluginAttributes == 1, "Expected exactly one BepInPlugin attribute.");

    bool hasLicense = false;
    foreach (var handle in metadata.ManifestResources)
    {
        var resource = metadata.GetManifestResource(handle);
        if (resource.Implementation.IsNil && metadata.GetString(resource.Name) == args[3])
            hasLicense = true;
    }
    Require(hasLicense, "Missing embedded MIT license.");

    Console.WriteLine($"Verified x64 {name} {version}, GUID and embedded license.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidDataException(message);
}
