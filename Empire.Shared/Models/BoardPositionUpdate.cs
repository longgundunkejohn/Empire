public class BoardPositionUpdate
{
    public string GameId { get; set; }
    public string PlayerId { get; set; }
    public List<int> NewOrder { get; set; } = new();
}
