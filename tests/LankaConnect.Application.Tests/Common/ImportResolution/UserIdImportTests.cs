using FluentAssertions;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LankaConnect.Application.Tests.Common.ImportResolution;

/// <summary>
/// TDD RED Phase: Test to verify Application layer can properly resolve UserId from Domain layer
/// This test should FAIL until we add proper using statements to Application layer files.
/// Wave 4.7.b (2026-06-25): expanded GetRequiredReferences() to include the
/// Identity.Contracts assembly so the standalone compilation can see
/// PasswordResetInitiatedDto.UserId / EmailVerificationInitiatedDto.UserId
/// fields referenced by the W4.7.b Communications handler rewrites.
/// </summary>
public class UserIdImportTests
{
    [Fact]
    public void Application_Should_Resolve_UserId_From_Domain_Layer()
    {
        var applicationSourceFiles = GetApplicationSourceFiles();
        var syntaxTrees = applicationSourceFiles.Select(file =>
            CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(file)));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references: GetRequiredReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Where(d => d.GetMessage().Contains("UserId"))
            .ToList();

        diagnostics.Should().BeEmpty("Application layer should be able to resolve UserId from Domain layer");
    }

    private string[] GetApplicationSourceFiles()
    {
        var testAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var testDirectory = System.IO.Path.GetDirectoryName(testAssemblyLocation);
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(testDirectory!, "..", "..", "..", "..", "..", "src", "LankaConnect.Application"));

        return System.IO.Directory.GetFiles(projectRoot, "*.cs", System.IO.SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToArray();
    }

    private MetadataReference[] GetRequiredReferences()
    {
        var testAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var testDirectory = System.IO.Path.GetDirectoryName(testAssemblyLocation);

        string ResolveAssembly(string relativeSrcSubPath, string assemblyName)
        {
            var releasePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                testDirectory!, "..", "..", "..", "..", "..", "src", relativeSrcSubPath, "bin", "Release", "net8.0", assemblyName + ".dll"));
            var debugPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                testDirectory!, "..", "..", "..", "..", "..", "src", relativeSrcSubPath, "bin", "Debug", "net8.0", assemblyName + ".dll"));
            return System.IO.File.Exists(releasePath) ? releasePath : debugPath;
        }

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        var domainPath = ResolveAssembly("LankaConnect.Domain", "LankaConnect.Domain");
        if (System.IO.File.Exists(domainPath))
            references.Add(MetadataReference.CreateFromFile(domainPath));

        var identityContractsPath = ResolveAssembly(System.IO.Path.Combine("Modules", "Identity", "Identity.Contracts"), "LankaConnect.Modules.Identity.Contracts");
        if (System.IO.File.Exists(identityContractsPath))
            references.Add(MetadataReference.CreateFromFile(identityContractsPath));

        // Wave 5.1.a-α.3 (2026-06-27): Event family moved to Products/LankaEvents.Domain.
        // Application files reference Event/Registration/EventPass/etc. types now in
        // LankaConnect.Products.LankaEvents.Domain.
        var lankaEventsDomainPath = ResolveAssembly(System.IO.Path.Combine("Products", "LankaEvents", "LankaEvents.Domain"), "LankaConnect.Products.LankaEvents.Domain");
        if (System.IO.File.Exists(lankaEventsDomainPath))
            references.Add(MetadataReference.CreateFromFile(lankaEventsDomainPath));

        return references.ToArray();
    }
}
