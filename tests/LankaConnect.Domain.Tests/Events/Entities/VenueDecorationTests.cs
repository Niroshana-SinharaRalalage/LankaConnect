using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class VenueDecorationTests
{
    private readonly Guid _layoutId = Guid.NewGuid();

    [Fact]
    public void Create_Stage_WithoutLabel_Should_Succeed()
    {
        var result = VenueDecoration.Create(_layoutId, DecorationKind.Stage, label: null, sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(DecorationKind.Stage);
        result.Value.Label.Should().BeNull();
    }

    [Fact]
    public void Create_Text_WithoutLabel_Should_Fail()
    {
        var result = VenueDecoration.Create(_layoutId, DecorationKind.Text, label: null, sortOrder: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("label");
    }

    [Fact]
    public void Create_Text_WithLabel_Should_Succeed()
    {
        var result = VenueDecoration.Create(_layoutId, DecorationKind.Text, label: "RESERVED", sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Label.Should().Be("RESERVED");
    }

    [Fact]
    public void Create_WithNegativeSortOrder_Should_Fail()
    {
        var result = VenueDecoration.Create(_layoutId, DecorationKind.Stage, null, sortOrder: -1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyLayoutId_Should_Fail()
    {
        var result = VenueDecoration.Create(Guid.Empty, DecorationKind.Stage, null, 0);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_InvalidJson_Falls_Back_To_Empty()
    {
        var result = VenueDecoration.Create(_layoutId, DecorationKind.Stage, null, 0,
            geometry: "not-json",
            properties: "also-not-json");

        result.IsSuccess.Should().BeTrue();
        result.Value.Geometry.Should().Be("{}");
        result.Value.Properties.Should().Be("{}");
    }

    [Fact]
    public void Create_With_Valid_Json_Geometry_Should_Preserve_It()
    {
        var geometry = "{\"x\":10,\"y\":20,\"width\":100,\"height\":50,\"rotation\":0}";
        var properties = "{\"color\":\"#ff0000\"}";

        var result = VenueDecoration.Create(_layoutId, DecorationKind.DanceFloor, null, 0, geometry, properties);

        result.IsSuccess.Should().BeTrue();
        result.Value.Geometry.Should().Be(geometry);
        result.Value.Properties.Should().Be(properties);
    }

    [Fact]
    public void Update_Should_Modify_Properties()
    {
        var decoration = VenueDecoration.Create(_layoutId, DecorationKind.Stage, null, 0).Value;

        var result = decoration.Update(
            DecorationKind.Text,
            label: "Main Stage",
            sortOrder: 2,
            geometry: "{\"x\":0,\"y\":0}",
            properties: "{\"fontSize\":20}");

        result.IsSuccess.Should().BeTrue();
        decoration.Kind.Should().Be(DecorationKind.Text);
        decoration.Label.Should().Be("Main Stage");
        decoration.SortOrder.Should().Be(2);
        decoration.UpdatedAt.Should().NotBeNull();
    }
}
