using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;

namespace SabberStonePython
{
    public static class SabberHelpers
    {
        public static class ManagedObjects
        {
            public static Dictionary<int, Game> Games = new Dictionary<int, Game>();
        }

        public static API.Game GenerateGameAPI(string deckString1, string deckString2)
        {
            Deck deck1, deck2;

            try
            {
                deck1 = Deserialise(deckString1);
            } 
            catch (Exception e)
            {
                Console.WriteLine("Deckstring #1 is not a valid deckstring");
                throw e;
            }

            try
            {
                deck2 = Deserialise(deckString2);
            }
            catch (Exception e)
            {
                Console.WriteLine("Deckstring #2 is not a valid deckstring");
                throw e;
            }



            var game = new Game(new SabberStoneCore.Config.GameConfig
            {
                StartPlayer = -1,
                Player1HeroClass = deck1.Class,
                Player1Deck = deck1.GetCardList(),
                Player2HeroClass = deck2.Class,
                Player2Deck = deck2.GetCardList(),

                Logging = false,
                History = false,
                FillDecks = false,
                Shuffle = true,
                SkipMulligan = true,
            });
            game.StartGame();

            Console.WriteLine(game.FullPrint());

            return new API.Game(game);
        }

        public class Deck
        {
            private readonly IReadOnlyDictionary<int, int> _cards;

            public CardClass Class { get; }
            public FormatType Format { get; }
            public string Name { get; }

            public Deck(int heroId, IReadOnlyDictionary<int, int> idsAndCounts, FormatType format, string name)
            {
                Name = name;
                Format = format;
                Class = Cards.FromAssetId(heroId).Class;
                _cards = idsAndCounts;
            }

            public List<Card> GetCardList()
            {
                var result = new List<Card>(30);
                foreach (KeyValuePair<int, int> item in _cards)
                {
                    Card card = Cards.FromAssetId(item.Key);
                    for (int i = 0; i < item.Value; i++)
                        result.Add(card);
                }
                return result;
            }
        }

        public static Deck Deserialise(string deckString, string deckName = null)
        {
            string[] lines = deckString.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith('#'))
                {
                    if (deckName == null && line.StartsWith("###"))
                        deckName = line.Substring(3).Trim();
                    continue;
                }

                byte[] bytes = Convert.FromBase64String(line);
                // Leading 0
                int offset = 1;
                int length;

                // Version
                ReadVarint(bytes, ref offset, out length);

                FormatType format = (FormatType)ReadVarint(bytes, ref offset, out length);

                ReadVarint(bytes, ref offset, out length);

                int heroId = ReadVarint(bytes, ref offset, out length);

                Dictionary<int, int> cardIdsAndCountPairs = new Dictionary<int, int>();
                for (int j = ReadVarint(bytes, ref offset, out length); j > 0; --j)
                    cardIdsAndCountPairs.Add(ReadVarint(bytes, ref offset, out length), 1);
                for (int j = ReadVarint(bytes, ref offset, out length); j > 0; --j)
                    cardIdsAndCountPairs.Add(ReadVarint(bytes, ref offset, out length), 2);
                for (int j = ReadVarint(bytes, ref offset, out length); j > 0; --j)
                {
                    int id = ReadVarint(bytes, ref offset, out length);
                    int count = ReadVarint(bytes, ref offset, out length);
                    cardIdsAndCountPairs.Add(id, count);
                }

                return new Deck(heroId, cardIdsAndCountPairs, format, deckName);
            }

            throw new ArgumentException();
        }

        private static int ReadVarint(byte[] bytes, ref int offset, out int length)
        {
            int result = length = 0;
            for (int i = offset; i < bytes.Length; i++)
            {
                int value = (int) bytes[i] & 0x7f;
                result |= value << length * 7;
                if ((bytes[i] & 0x80) != 0x80)
                    break;
                length++;
            }

            length++;

            offset += length;
            return result;
        }
    }
}
