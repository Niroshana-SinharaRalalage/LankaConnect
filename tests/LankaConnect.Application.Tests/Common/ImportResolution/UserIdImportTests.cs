using FluentAssertions;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LankaConnect.Application.Tests.Common.ImportResolution;

/// <summary>
/// TDD RED Phase: Test to verify Application layer can properly resolve UserId from Domain layer
/// This test should FAIL until we add proper using statements to Application layer files
/// </summary>
public class UserIdImportTests
{
    [Fact]
    public void Application_Should_Resolve_UserId_From_Domain_Layer()
    {
        // Arrange
        var applicationSourceFiles = GetApplicationSourceFiles();
        var syntaxTrees = applicationSourceFiles.Select(file => 
            CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(file)));
        
        // Act - Attempt compilation
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references: GetRequiredReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            
        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Where(d => d.GetMessage().Contains("UserId"))
            .ToList();
        
        // Assert - This should FAIL initially (RED phase)
        diagnostics.Should().BeEmpty("Application layer should be able to resolve UserId from Domain layer");
    }
    
    private string[] GetApplicationSourceFiles()
    {
        // Use relative paths that work on both Windows and Linux
        var testAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var testDirectory = System.IO.Path.GetDirectoryName(testAssemblyLocation);
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(testDirectory!, "..", "..", "..", "..", "..", "src", "LankaConnect.Application"));

        return System.IO.Directory.GetFiles(projectRoot, "*.cs", System.IO.SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToArray();
    }

    private MetadataReference[] GetRequiredReferences()
    {
        // Use relative paths that work on both Windows and Linux
        var testAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var testDirectory = System.IO.Path.GetDirectoryName(testAssemblyLocation);
        var domainPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(testDirectory!, "..", "..", "..", "..", "..", "src", "LankaConnect.Domain", "bin", "Debug", "net8.0", "LankaConnect.Domain.dll"));

        var systemReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        var references = systemReferences.ToList();
        if (System.IO.File.Exists(domainPath))
        {
            references.Add(MetadataReference.CreateFromFile(domainPath));
        }

        return references.ToArray();
    }
}