using Empire.Shared.Models;

namespace Empire.Client.Services
{
    public class MockCardDataService
    {
        private static readonly List<Card> _allCards = new()
        {
            // AMALI FACTION
            new Card { CardId = 1, Name = "Conscript", Cost = 2, Tier = 4, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/ngOsVIx.jpg" },
            new Card { CardId = 2, Name = "Knights of Songdu", Cost = 3, Tier = 4, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/LL93WhK.jpg" },
            new Card { CardId = 3, Name = "Consecrated Paladin", Cost = 6, Tier = 4, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/573z9Qu.jpg" },
            new Card { CardId = 4, Name = "High Priestess N'Thalla", Cost = 5, Tier = 4, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/7LFmYWE.jpg" },
            new Card { CardId = 5, Name = "Issa and Chinara", Cost = 5, Tier = 4, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/G7vAFB5.jpg" },
            new Card { CardId = 6, Name = "Iru, Champion of Tembe Field", Cost = 4, Tier = 4, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/jYwTPvs.jpg" },
            new Card { CardId = 7, Name = "Inspiring Elephantry", Cost = 6, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/nfdrtlu.jpg" },
            new Card { CardId = 8, Name = "Aissata, Night's Vanguard", Cost = 5, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/GVxJU1W.jpg" },
            new Card { CardId = 9, Name = "Marun, Feared Captain", Cost = 5, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/aHsdO7d.jpg" },
            new Card { CardId = 10, Name = "Nthalla's Oracle", Cost = 3, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/Qho7ohH.jpg" },
            new Card { CardId = 11, Name = "Hamadou Janissary", Cost = 4, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/0c8qyni.jpg" },
            new Card { CardId = 12, Name = "Giltplate Kingsguard", Cost = 4, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/s5xzGZG.jpg" },
            new Card { CardId = 13, Name = "Borderland Sentry", Cost = 1, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/X02cgu5.jpg" },
            new Card { CardId = 14, Name = "Dauntless Lancer", Cost = 2, Tier = 3, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/Evdf1hH.jpg" },
            new Card { CardId = 15, Name = "Hamadou Cavalier", Cost = 5, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/KmQtq5Q.jpg" },
            new Card { CardId = 16, Name = "Tilsi Noble", Cost = 4, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/YRZ9xmq.jpg" },
            new Card { CardId = 17, Name = "Songdu Glaivemaster", Cost = 4, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/N2PT30O.jpg" },
            new Card { CardId = 18, Name = "Tilsi Gatekeeper", Cost = 3, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/LYYm8OH.jpg" },
            new Card { CardId = 19, Name = "Songu Patriot", Cost = 3, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/4YdV2UP.jpg" },
            new Card { CardId = 20, Name = "Headstrong Cuirassier", Cost = 3, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/hAhZrVV.jpg" },
            new Card { CardId = 21, Name = "Amali Warrior Poet", Cost = 3, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/vQIN8ZV.jpg" },
            new Card { CardId = 22, Name = "Songdu Oathkeeper", Cost = 2, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/fhgKowR.jpg" },
            new Card { CardId = 23, Name = "Ejiroghene, Venerated Orator", Cost = 2, Tier = 2, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/CmWPYHN.jpg" },
            new Card { CardId = 24, Name = "Hamadou Partisan", Cost = 3, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/EjMBXwk.jpg" },
            new Card { CardId = 25, Name = "Amali Astronomer", Cost = 3, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/doBLYOl.jpg" },
            new Card { CardId = 26, Name = "Amali Outriders", Cost = 3, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/S74uXPS.jpg" },
            new Card { CardId = 27, Name = "Shields of N'Thalla", Cost = 2, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/FXZe1Lg.jpg" },
            new Card { CardId = 28, Name = "Highland Operative", Cost = 2, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/oPnF6ya.jpg" },
            new Card { CardId = 29, Name = "Iru's Aspirant", Cost = 1, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/41YiWAX.jpg" },
            new Card { CardId = 30, Name = "Songdu Militia", Cost = 1, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/KlL45l5.jpg" },
            new Card { CardId = 31, Name = "Tilsi Hoplite", Cost = 2, Tier = 1, Faction = "Amali", CardType = "Unit", ImagePath = "https://i.imgur.com/6pOCik4.jpg" },

            // AMALI TACTICS
            new Card { CardId = 32, Name = "Call the Cavalry", Cost = 6, Tier = 4, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/pRL9Jhl.jpg" },
            new Card { CardId = 33, Name = "Embarrassment of Riches", Cost = 7, Tier = 3, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/M6nfOUp.jpg" },
            new Card { CardId = 34, Name = "Incongrous End", Cost = 5, Tier = 3, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/XV3BW6n.jpg" },
            new Card { CardId = 35, Name = "Portend", Cost = 3, Tier = 3, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/9XnFa1d.jpg" },
            new Card { CardId = 36, Name = "Aurar Studies", Cost = 3, Tier = 2, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/nY8N8Bo.jpg" },
            new Card { CardId = 37, Name = "Bribe", Cost = 1, Tier = 2, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/zVEo0Jv.jpg" },
            new Card { CardId = 38, Name = "Amali Standard", Cost = 2, Tier = 2, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/L7iPCA3.jpg" },
            new Card { CardId = 39, Name = "Boon of the Djinn", Cost = 2, Tier = 1, Faction = "Amali", CardType = "Tactic", ImagePath = "https://i.imgur.com/QClw7md.jpg" },

            // AMALI BATTLE TACTICS
            new Card { CardId = 40, Name = "Charging Tembe Field", Cost = 5, Tier = 3, Faction = "Amali", CardType = "Battle Tactic", ImagePath = "https://i.imgur.com/TV6y6be.jpg" },
            new Card { CardId = 41, Name = "Tawari Smiles", Cost = 3, Tier = 3, Faction = "Amali", CardType = "Battle Tactic", ImagePath = "https://i.imgur.com/mjSsJVn.jpg" },
            new Card { CardId = 42, Name = "Victuals", Cost = 2, Tier = 2, Faction = "Amali", CardType = "Battle Tactic", ImagePath = "https://i.imgur.com/ZDgv5zm.jpg" },

            // AMALI CHRONICLES
            new Card { CardId = 43, Name = "Conquering the Clifflands", Cost = 4, Tier = 4, Faction = "Amali", CardType = "Chronicle", ImagePath = "https://i.imgur.com/Ai6UQcq.jpg" },
            new Card { CardId = 44, Name = "Encirclement of Baraba", Cost = 3, Tier = 3, Faction = "Amali", CardType = "Chronicle", ImagePath = "https://i.imgur.com/UNpOfQu.jpg" },
            new Card { CardId = 45, Name = "Fill the Coffers", Cost = 5, Tier = 2, Faction = "Amali", CardType = "Chronicle", ImagePath = "https://i.imgur.com/PcbqLVZ.jpg" },
            new Card { CardId = 46, Name = "Martial Tradition", Cost = 3, Tier = 2, Faction = "Amali", CardType = "Chronicle", ImagePath = "https://i.imgur.com/ypaplEC.jpg" },
            new Card { CardId = 47, Name = "Cosmic Imprisonment", Cost = 3, Tier = 1, Faction = "Amali", CardType = "Chronicle", ImagePath = "https://i.imgur.com/9QQtmjl.jpg" },

            // AMALI VILLAGERS
            new Card { CardId = 48, Name = "Tilsi Preacher", Cost = 1, Tier = 1, Faction = "Amali", CardType = "Villager", ImagePath = "https://i.imgur.com/JjBkJsV.jpg" },

            // AMALI SKIRMISHERS
            new Card { CardId = 49, Name = "Regimental Drummer", Cost = 3, Tier = 3, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/7zYDx22.jpg" },
            new Card { CardId = 50, Name = "Amali Field Marshal", Cost = 5, Tier = 3, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/4DFLlOG.jpg" },
            new Card { CardId = 51, Name = "Levied Spearmen", Cost = 4, Tier = 2, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/SfbWRkf.jpg" },
            new Card { CardId = 52, Name = "Call to Arms", Cost = 6, Tier = 2, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/hOkypXC.jpg" },
            new Card { CardId = 53, Name = "Sanctum Keeper", Cost = 3, Tier = 1, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/dvEzb5r.jpg" },
            new Card { CardId = 54, Name = "Legion Colonist", Cost = 3, Tier = 1, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/ecKzBOK.jpg" },
            new Card { CardId = 55, Name = "Lantern Bearer", Cost = 2, Tier = 1, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/zZ1mW1B.jpg" },
            new Card { CardId = 56, Name = "Amali Lookout", Cost = 2, Tier = 1, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/vwQBrlS.jpg" },
            new Card { CardId = 57, Name = "Amali Scout", Cost = 1, Tier = 1, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/4mJSVAO.jpg" },
            new Card { CardId = 58, Name = "Outland Stalwart", Cost = 3, Tier = 2, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/TQWfSiK.jpg" },
            new Card { CardId = 59, Name = "Conqueror of the West", Cost = 2, Tier = 3, Faction = "Amali", CardType = "Skirmisher", ImagePath = "https://i.imgur.com/cnv3bsX.jpg" },

            // AMALI SETTLEMENTS
            new Card { CardId = 60, Name = "Amali Potters", Cost = 0, Tier = 0, Faction = "Amali", CardType = "Settlement", ImagePath = "https://i.imgur.com/rWc4Yri.jpg" },
            new Card { CardId = 61, Name = "Seb'an Palace Minstrel", Cost = 0, Tier = 0, Faction = "Amali", CardType = "Settlement", ImagePath = "https://i.imgur.com/ndjUJ8k.jpg" },
            new Card { CardId = 62, Name = "Escape Tunnels", Cost = 0, Tier = 0, Faction = "Amali", CardType = "Settlement", ImagePath = "https://i.imgur.com/6wXn6Gk.jpg" },
            new Card { CardId = 63, Name = "Tulum Palace", Cost = 0, Tier = 0, Faction = "Amali", CardType = "Settlement", ImagePath = "https://i.imgur.com/g9a9rW8.jpg" },
            new Card { CardId = 64, Name = "Trebuchet Emplacement", Cost = 0, Tier = 0, Faction = "Amali", CardType = "Settlement", ImagePath = "https://i.imgur.com/0kPjr0o.jpg" },
            new Card { CardId = 65, Name = "Tilsi Fortress", Cost = 0, Tier = 0, Faction = "Amali", CardType = "Settlement", ImagePath = "https://i.imgur.com/kvVNKrL.jpg" },
            new Card { CardId = 66, Name = "Frontier Outpost", Cost = 0, Tier = 0, Faction = "Amali", CardType = "Settlement", ImagePath = "https://i.imgur.com/pw95RfK.jpg" },

            // HORUDJET FACTION
            new Card { CardId = 67, Name = "Sun Worshipper", Cost = 2, Tier = 0, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/5POxfkS.jpg" },
            new Card { CardId = 68, Name = "Sekhemne, the Usurper", Cost = 10, Tier = 4, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/4jg1dud.jpg" },
            new Card { CardId = 69, Name = "Pteth, God of Plague", Cost = 10, Tier = 4, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/1oBsRI4.jpg" },
            new Card { CardId = 70, Name = "Anan-Khunaten, XII", Cost = 7, Tier = 4, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/1p6HVHc.jpg" },
            new Card { CardId = 71, Name = "Sphinx of Dawn's Splendor", Cost = 5, Tier = 4, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/C8S8qV2.jpg" },
            new Card { CardId = 72, Name = "Aspect of Khutum", Cost = 4, Tier = 4, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/tFPYrbn.jpg" },
            new Card { CardId = 73, Name = "Utnapishtu, the Ageless", Cost = 5, Tier = 3, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/IJPXj9B.jpg" },
            new Card { CardId = 74, Name = "Shefferet Demisphinx", Cost = 4, Tier = 3, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/fDOZ3kv.jpg" },
            new Card { CardId = 75, Name = "Keeper of the Secret Sun", Cost = 4, Tier = 3, Faction = "Horudjet", CardType = "Unit", ImagePath = "https://i.imgur.com/1OVJYC9.jpg" },

            // LYRIA FACTION
            new Card { CardId = 76, Name = "Blightghoul", Cost = 4, Tier = 0, Faction = "Lyria", CardType = "Unit", ImagePath = "https://i.imgur.com/leunSFI.jpg" },
            new Card { CardId = 77, Name = "Skrogwurm", Cost = 6, Tier = 4, Faction = "Lyria", CardType = "Unit", ImagePath = "https://i.imgur.com/ozi8aCr.jpg" },
            new Card { CardId = 78, Name = "Sana, Matron of Unrest", Cost = 5, Tier = 4, Faction = "Lyria", CardType = "Unit", ImagePath = "https://i.imgur.com/9nwoDPQ.jpg" },
            new Card { CardId = 79, Name = "Balefiend", Cost = 6, Tier = 4, Faction = "Lyria", CardType = "Unit", ImagePath = "https://i.imgur.com/8UVxBH4.jpg" },
            new Card { CardId = 80, Name = "Lyrian Bishop", Cost = 4, Tier = 4, Faction = "Lyria", CardType = "Unit", ImagePath = "https://i.imgur.com/302WNav.jpg" },

            // NDEMBE FACTION
            new Card { CardId = 81, Name = "Vine!", Cost = 3, Tier = 0, Faction = "Ndembe", CardType = "Token", ImagePath = "https://i.imgur.com/DFXjriC.jpg" },
            new Card { CardId = 82, Name = "Avatar of the Mother Tree", Cost = 10, Tier = 4, Faction = "Ndembe", CardType = "Unit", ImagePath = "https://i.imgur.com/IHmtPq2.jpg" },
            new Card { CardId = 83, Name = "M'bku, the Indomitable", Cost = 6, Tier = 4, Faction = "Ndembe", CardType = "Unit", ImagePath = "https://i.imgur.com/6CEK9x2.jpg" },
            new Card { CardId = 84, Name = "Wetland Imperator", Cost = 6, Tier = 4, Faction = "Ndembe", CardType = "Unit", ImagePath = "https://i.imgur.com/cCu7WK0.jpg" },
            new Card { CardId = 85, Name = "Youmbe, Death's Shadow", Cost = 4, Tier = 4, Faction = "Ndembe", CardType = "Unit", ImagePath = "https://i.imgur.com/xoyMA7j.jpg" },

            // CHUGUDAI FACTION
            new Card { CardId = 86, Name = "Spectral Beast", Cost = 4, Tier = 0, Faction = "Chugudai", CardType = "Token", ImagePath = "https://i.imgur.com/49fRyOH.jpg" },
            new Card { CardId = 87, Name = "Ögelei, Khan of the Chugudai", Cost = 6, Tier = 4, Faction = "Chugudai", CardType = "Unit", ImagePath = "https://i.imgur.com/xko0Zoq.jpg" },
            new Card { CardId = 88, Name = "Tengai of the Eastern Wind", Cost = 6, Tier = 4, Faction = "Chugudai", CardType = "Unit", ImagePath = "https://i.imgur.com/xs1AcFJ.jpg" },
            new Card { CardId = 89, Name = "Üguluk, the Calamity", Cost = 5, Tier = 4, Faction = "Chugudai", CardType = "Unit", ImagePath = "https://i.imgur.com/tUMzQid.jpg" },
            new Card { CardId = 90, Name = "Austere Wanderer", Cost = 4, Tier = 4, Faction = "Chugudai", CardType = "Unit", ImagePath = "https://i.imgur.com/ZTrGuAJ.jpg" },

            // OHOTEC FACTION
            new Card { CardId = 91, Name = "Ancestral Spirit", Cost = 2, Tier = 0, Faction = "Ohotec", CardType = "Token", ImagePath = "https://i.imgur.com/pTd9ECE.jpg" },
            new Card { CardId = 92, Name = "Otoc, the Serpent King", Cost = 5, Tier = 4, Faction = "Ohotec", CardType = "Unit", ImagePath = "https://i.imgur.com/kCUwck8.jpg" },
            new Card { CardId = 93, Name = "Skyborne Coatl", Cost = 5, Tier = 4, Faction = "Ohotec", CardType = "Unit", ImagePath = "https://i.imgur.com/EoPRdr7.jpg" },
            new Card { CardId = 94, Name = "Tlacan Regent", Cost = 4, Tier = 4, Faction = "Ohotec", CardType = "Unit", ImagePath = "https://i.imgur.com/9fvL3fj.jpg" },
            new Card { CardId = 95, Name = "Huatemoc, Twinjade Feather", Cost = 3, Tier = 4, Faction = "Ohotec", CardType = "Unit", ImagePath = "https://i.imgur.com/XLYz4Ac.jpg" },

            // NEUTRAL CARDS
            new Card { CardId = 96, Name = "Nomads", Cost = 0, Tier = 0, Faction = "Neutral", CardType = "Settlement", ImagePath = "https://i.imgur.com/7KRo0Gg.jpg" }
        };

        public static List<Card> GetAllCards() => _allCards;

        public static Card? GetCardById(int cardId) => _allCards.FirstOrDefault(c => c.CardId == cardId);

        public static List<Card> GetCardsByFaction(string faction) => 
            _allCards.Where(c => c.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase)).ToList();

        public static List<Card> GetCardsByType(string cardType) => 
            _allCards.Where(c => c.CardType.Equals(cardType, StringComparison.OrdinalIgnoreCase)).ToList();

        public static List<Card> GetCivicCards() => 
            _allCards.Where(c => c.CardType is "Settlement" or "Villager").ToList();

        public static List<Card> GetMilitaryCards() => 
            _allCards.Where(c => c.CardType is "Unit" or "Tactic" or "Battle Tactic" or "Chronicle" or "Skirmisher").ToList();

        // Sample deck configurations for testing
        public static List<int> GetSampleAmaliCivicDeck() => new()
        {
            60, 61, 62, 63, 64, 65, 66, // Settlements
            48, 48, 48, 48, 48, 48, 48, 48 // Villagers (repeated for deck size)
        };

        public static List<int> GetSampleAmaliMilitaryDeck() => new()
        {
            // Low cost units
            29, 29, 30, 30, 31, 31, 27, 27, 28, 28,
            // Mid cost units  
            22, 22, 23, 23, 18, 18, 19, 19, 20, 20,
            // Tactics
            37, 37, 38, 38, 39, 39,
            // Higher tier units
            13, 14, 15, 16
        };

        public static List<int> GetSampleHorudjetCivicDeck() => new()
        {
            // Add Horudjet settlements when available
            60, 61, 62, 63, 64, 65, 66, // Placeholder with Amali settlements
            67, 67, 67, 67, 67, 67, 67, 67 // Sun Worshippers
        };

        public static List<int> GetSampleHorudjetMilitaryDeck() => new()
        {
            // Horudjet units
            67, 67, 67, 67, 71, 71, 72, 72, 73, 73,
            74, 74, 75, 75, 68, 69, 70, 70, 71, 72,
            73, 74, 75, 67, 67, 67, 67, 67, 67, 67
        };
    }
}
