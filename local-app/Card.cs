using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalSolitaire
{
    class Card
    {
        // Card Data Model, holds the Suit(spades, hearts, clubs, diamonds) and Rank(1-10 etc)
        public string Rank { get; }
        public string Suit { get; }
        public bool IsFaceUp { get; set; } // Tracks if the card is hidden in the tableau

        // Set is declared on initialisation and never again
        public Card(string rank, string suit)
        {
            Rank = rank;
            Suit = suit;
            IsFaceUp = false;
        }

        // Retrieve the cards value in a visually nice way
        public override string ToString()
        {
            if (!IsFaceUp) return "??"; // Hidden card display

            // Unicode symbols for suits
            string suitSymbol = Suit switch
            {
                "Hearts" => "♥",
                "Diamonds" => "♦",
                "Clubs" => "♣",
                "Spades" => "♠",
                _ => "?"
            };

            return $"{Rank}{suitSymbol}"; ;
        }

    }
}
