using Empire.Shared.Models;

public class JoinGameMove : GameMove
{
    public PlayerDeck PlayerDeck { get; set; } = new PlayerDeck(); // Initialize here
}