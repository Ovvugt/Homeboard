using System.Net;
using System.Net.Http.Json;
using Homeboard.API.Tests.Fixtures;
using Homeboard.Boards.Dtos;
using Homeboard.Boards.Entities;
using NUnit.Framework;

// ReSharper disable once UnusedType.Global

namespace Homeboard.API.Tests.Controllers;

[TestFixture]
public sealed class BoardsControllerTests
{
    private HomeboardApiFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly Guid HomeBoardId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [OneTimeSetUp]
    public void Init()
    {
        _factory = new HomeboardApiFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void Done()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [SetUp]
    public async Task ResetData()
    {
        var connStr = _factory.ConnectionString;
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM tile_status_snapshots; DELETE FROM tiles; DELETE FROM widgets; DELETE FROM boards WHERE id <> '00000000-0000-0000-0000-000000000001';";
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task ListBoards_returns_seeded_home_board()
    {
        var boards = await _client.GetFromJsonAsync<List<BoardSummaryDto>>("/api/boards", TestJson.Options);
        Assert.That(boards, Is.Not.Null);
        Assert.That(boards!, Has.Count.EqualTo(1));
        Assert.That(boards[0].Slug, Is.EqualTo("home"));
    }

    [Test]
    public async Task GetHome_initially_has_no_tiles()
    {
        var board = await _client.GetFromJsonAsync<BoardDetailDto>("/api/boards/home", TestJson.Options);
        Assert.That(board, Is.Not.Null);
        Assert.That(board!.Tiles, Is.Empty);
        Assert.That(board.Widgets, Is.Empty);
    }

    [Test]
    public async Task Create_tile_then_get_home_returns_it()
    {
        var dto = new CreateTileDto(
            HomeBoardId, "TestTile", "https://example.com", null, TileIconKind.Url,
            null, null, 1, 2, 3, 2,
            TileStatusType.None, null, null, null, null);
        var resp = await _client.PostAsJsonAsync("/api/tiles", dto, TestJson.Options);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var board = await _client.GetFromJsonAsync<BoardDetailDto>("/api/boards/home", TestJson.Options);
        Assert.That(board!.Tiles.Any(t => t.Name == "TestTile"), Is.True);
    }

    [Test]
    public async Task SaveLayout_updates_positions()
    {
        // Seed a tile to move
        var create = new CreateTileDto(
            HomeBoardId, "Movable", "https://example.com", null, TileIconKind.Url,
            null, null, 0, 0, 2, 2,
            TileStatusType.None, null, null, null, null);
        var createResp = await _client.PostAsJsonAsync("/api/tiles", create, TestJson.Options);
        var tile = await createResp.Content.ReadFromJsonAsync<TileDto>(TestJson.Options);
        Assert.That(tile, Is.Not.Null);

        var layout = new SaveLayoutDto(new List<LayoutItemDto>
        {
            new(tile!.Id, LayoutItemKind.Tile, 5, 4, 2, 2),
        });
        var saveResp = await _client.PostAsJsonAsync($"/api/boards/{HomeBoardId}/layout", layout, TestJson.Options);
        Assert.That(saveResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var board = await _client.GetFromJsonAsync<BoardDetailDto>("/api/boards/home", TestJson.Options);
        var moved = board!.Tiles.Single(t => t.Id == tile.Id);
        Assert.That(moved.GridX, Is.EqualTo(5));
        Assert.That(moved.GridY, Is.EqualTo(4));
    }

    [Test]
    public async Task Create_board_with_duplicate_slug_returns_409()
    {
        var dto = new CreateBoardDto("Home Two", "home", null);
        var resp = await _client.PostAsJsonAsync("/api/boards", dto, TestJson.Options);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }
}
