using Empire.Shared.Models;

namespace Empire.Server.Services
{
    public class GameStateService
    {
        private readonly DeckLoaderService _deckLoader;
        private readonly Random _rng = new();

        public GameState GameState { get; private set; }

        public GameStateService(DeckLoaderService deckLoader)
        {
            _deckLoader = deckLoader;
            GameState = new GameState();
        }
        public void LoadTestDecks()
        {
            string civicDeckPath = "wwwroot/decks/Player1_Civic.csv";
            string militaryDeckPath = "wwwroot/decks/Player1_Military.csv";

            LoadDecks("Player1", civicDeckPath, militaryDeckPath);
        }

        public void LoadDecks(string player, string civicDeckPath, string militaryDeckPath)
        {
            Console.WriteLine($"Loading decks for {player}...");

            if (!File.Exists(civicDeckPath) || !File.Exists(militaryDeckPath))
            {
                Console.WriteLine("ERROR: One or both deck files are missing!");
                return;
            }

            var civicDeck = File.ReadAllLines(civicDeckPath)
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Select(line => int.Parse(line.Split(',')[0]))
                                .ToList();

            var militaryDeck = File.ReadAllLines(militaryDeckPath)
                                   .Where(line => !string.IsNullOrWhiteSpace(line))
                                   .Select(line => int.Parse(line.Split(',')[0]))
                                   .ToList();

            GameState.PlayerDecks[player] = new PlayerDeck(civicDeck, militaryDeck);

            Console.WriteLine($"Loaded {civicDeck.Count} Civic cards and {militaryDeck.Count} Military cards for {player}.");
        }



        public void DrawCard(string player, bool isCivic)
        {
            Console.WriteLine($"Attempting to draw a card for {player}");

            if (!GameState.PlayerDecks.ContainsKey(player))
            {
                Console.WriteLine($"ERROR: No deck found for {player}.");
                return;
            }

            var deck = isCivic ? GameState.PlayerDecks[player].CivicDeck : GameState.PlayerDecks[player].MilitaryDeck;

            if (deck == null || deck.Count == 0)
            {
                Console.WriteLine($"WARNING: {player}'s {(isCivic ? "Civic" : "Military")} deck is empty.");
                return;
            }

            int cardId = deck[_rng.Next(deck.Count)];
            deck.Remove(cardId);
            GameState.PlayerHands[player].Add(cardId);

            Console.WriteLine($"{player} drew card {cardId} from {(isCivic ? "Civic" : "Military")} deck.");
        }


        public void PlayCard(string player, int cardId)
        {
            if (!GameState.PlayerHands[player].Contains(cardId)) return;
            GameState.PlayerHands[player].Remove(cardId);
            GameState.PlayerBoard[player].Add(cardId);
        }
        private int ExtractCardId(string filename)
        {
            string[] parts = filename.Split(' ');
            if (int.TryParse(parts[0], out int cardId))
            {
                return cardId;
            }
            throw new Exception($"Invalid card format: {filename}");
        }
    }
}
