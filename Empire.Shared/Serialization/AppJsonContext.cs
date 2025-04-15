using System.Text.Json.Serialization;
using Empire.Shared.DTOs;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;

namespace Empire.Shared.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GameStartRequest))]
[JsonSerializable(typeof(GameMove))]
[JsonSerializable(typeof(GamePreview))]
[JsonSerializable(typeof(List<GamePreview>))]
[JsonSerializable(typeof(GameState))]
[JsonSerializable(typeof(PlayerDeck))]
[JsonSerializable(typeof(List<PlayerDeck>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Card))]
[JsonSerializable(typeof(List<Card>))]
[JsonSerializable(typeof(int))]
public partial class AppJsonContext : JsonSerializerContext { }
