public class BoardPositionUpdate
{
    public required string GameId { get; set; }
    public required string PlayerId { get; set; }
    public List<int> NewOrder { get; set; } = new();
}
