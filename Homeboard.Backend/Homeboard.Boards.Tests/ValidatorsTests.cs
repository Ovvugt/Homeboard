using Homeboard.Boards.Dtos;
using Homeboard.Boards.Entities;
using Homeboard.Boards.Validators;
using NUnit.Framework;

namespace Homeboard.Boards.Tests;

[TestFixture]
public sealed class ValidatorsTests
{
    [Test]
    public void CreateTile_requires_name_and_url()
    {
        var v = new CreateTileDtoValidator();
        var dto = new CreateTileDto(
            Guid.NewGuid(), "", "", null, TileIconKind.Url, null, null,
            0, 0, 1, 1, TileStatusType.None, null, null, null, null);
        var result = v.Validate(dto);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Select(e => e.PropertyName), Contains.Item("Name").And.Contains("Url"));
    }

    [Test]
    public void CreateTile_rejects_oversized_grid()
    {
        var v = new CreateTileDtoValidator();
        var dto = new CreateTileDto(
            Guid.NewGuid(), "x", "https://x", null, TileIconKind.Url, null, null,
            0, 0, 99, 1, TileStatusType.None, null, null, null, null);
        var result = v.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void SaveLayout_rejects_negative_coords()
    {
        var v = new SaveLayoutDtoValidator();
        var dto = new SaveLayoutDto([
            new LayoutItemDto(Guid.NewGuid(), LayoutItemKind.Tile, -1, 0, 1, 1)
        ]);
        var result = v.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }
}
