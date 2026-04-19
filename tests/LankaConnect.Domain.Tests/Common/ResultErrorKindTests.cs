using FluentAssertions;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Domain.Tests.Common;

/// <summary>
/// Slice 5 Chunk 1: Result pattern gains an ErrorKind discriminator so API layer can map
/// handler outcomes to HTTP status codes without string-sniffing. Default kind is Validation
/// (maps to 400), preserving behavior of the existing ~500 Result.Failure(string) callers.
/// </summary>
public class ResultErrorKindTests
{
    [Fact]
    public void Failure_Without_Kind_Should_Default_To_Validation()
    {
        var r = Result.Failure("bad input");

        r.IsFailure.Should().BeTrue();
        r.ErrorKind.Should().Be(ErrorKind.Validation);
        r.Error.Should().Be("bad input");
    }

    [Fact]
    public void Success_Should_Have_None_ErrorKind()
    {
        var r = Result.Success();

        r.IsSuccess.Should().BeTrue();
        r.ErrorKind.Should().Be(ErrorKind.None);
    }

    [Fact]
    public void Forbidden_Factory_Should_Set_Kind_To_Forbidden()
    {
        var r = Result.Forbidden("not your layout");

        r.IsFailure.Should().BeTrue();
        r.ErrorKind.Should().Be(ErrorKind.Forbidden);
        r.Error.Should().Be("not your layout");
    }

    [Fact]
    public void NotFound_Factory_Should_Set_Kind_To_NotFound()
    {
        var r = Result.NotFound("layout missing");

        r.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public void Conflict_Factory_Should_Set_Kind_To_Conflict()
    {
        var r = Result.Conflict("stale RowVersion");

        r.ErrorKind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public void StructuralEditRejected_Factory_Should_Set_Kind_To_StructuralEditRejected()
    {
        var r = Result.StructuralEditRejected("seats reserved");

        r.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
    }

    [Fact]
    public void Generic_Result_Forbidden_Should_Propagate_Kind()
    {
        var r = Result<string>.Forbidden("no access");

        r.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public void Generic_Result_NotFound_Should_Propagate_Kind()
    {
        var r = Result<int>.NotFound("missing");

        r.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public void Generic_Result_Conflict_Should_Propagate_Kind()
    {
        var r = Result<int>.Conflict("conflict");

        r.ErrorKind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public void Generic_Result_StructuralEditRejected_Should_Propagate_Kind()
    {
        var r = Result<int>.StructuralEditRejected("reserved");

        r.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
    }

    [Fact]
    public void Explicit_Kind_Overload_Should_Set_Kind()
    {
        var r = Result.Failure("custom", ErrorKind.Forbidden);

        r.ErrorKind.Should().Be(ErrorKind.Forbidden);
    }
}
