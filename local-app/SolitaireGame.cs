using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using static System.Formats.Asn1.AsnWriter;

namespace TerminalSolitaire
{
    public class SolitaireGame
    {
        // https://defbnszqe1hwm.cloudfront.net/images/Solitaire-play-are-set-up-2.png
        private List<List<Card>> tableau;
        private List<Card> stockpile;
        private List<List<Card>> foundations;

        // Scoring Variables
        private DateTime startTime;
        private int cardMovements;
        bool gameWon = false;

        // Which gametype it is
        private bool drawThree;
        // Fixed width for each column
        const int deckColumnWidth = 1;
        const int columnWidth = 3; 
        const int cardWidth = 3;

        // Track Position in interface
        private int selectedColumn = 0; 
        private int selectedRow = 0;
        private GameSection selectedSection = GameSection.Tableau; // Start at the tableau

        public SolitaireGame(bool drawThree)
        {
            this.drawThree = drawThree;
            InitializeGame();
        }

        private void InitializeGame()
        {
            tableau = new List<List<Card>>();
            stockpile = new List<Card>();
            // Foundation starts empty
            foundations = new List<List<Card>> { new List<Card>(), new List<Card>(), new List<Card>(), new List<Card>() };

            startTime = DateTime.Now; 
            cardMovements = 0;

            // Get a shuffled deck
            List<Card> deck = GenerateShuffledDeck();
            
            // Initialize tableau with 7 columns
            for (int i = 0; i < 7; i++)
                tableau.Add(new List<Card>());

            // Distribute cards with only the top one face-up
            for (int col = 0; col < 7; col++)
            {
                for (int row = 0; row <= col; row++)
                {
                    Card card = deck[deck.Count - 1];
                    deck.RemoveAt(deck.Count - 1);
                    if (row == col)
                        card.IsFaceUp = true; // Only top card is face-up
                    tableau[col].Add(card);
                }
            }

            // Remaining cards go to the stockpile, they can all be revealed
            foreach (Card card in deck)
            {
                card.IsFaceUp = true;
            }
            stockpile = deck;

        }

        private List<Card> GenerateShuffledDeck()
        {
            string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

            List<Card> deck = new List<Card>();

            foreach (string suit in suits)
                foreach (string rank in ranks)
                    deck.Add(new Card(rank, suit));

            // Shuffle deck
            Random rng = new Random();
            deck = deck.OrderBy(c => rng.Next()).ToList();

            return new List<Card>(deck);
        }

        public void Run()
        {
            ConsoleKeyInfo key;
            do
            {
                RenderGame();
                key = Console.ReadKey(true);
                HandleInput(key);
            } while (key.Key != ConsoleKey.Escape && !gameWon);

            if (gameWon)
            {
                // Instantiate the HighScores class
                HighScores highScores = new HighScores();

                string gameMode = drawThree ? "Three-Card Solitaire" : "One-Card Solitaire";

                // Create the high score for the active game and store it
                highScores.PostScore(CalculateScore(), gameMode);
            }
        }

        #region Scoring
        public void MoveCard()
        {
            cardMovements++;
            gameWon = CheckWinCondition();
        }
        private bool CheckWinCondition()
        {
            // Check if all four foundation piles have 13 cards each in the correct order
            foreach (List<Card> foundation in foundations)
            {
                if (foundation.Count != 13) return false;
                // If any foundation is not full then the game is not won
            }

            return true;
        }

        public int CalculateScore()
        {
            // Time-based score calculation (seconds since start)
            TimeSpan timeElapsed = DateTime.Now - startTime;
            int timePenalty = (int)timeElapsed.TotalSeconds / 60;  // e.g., 1 point per minute
            int movementPenalty = cardMovements * 5;  // e.g., 5 points per movement

            // Base score formula: 1000 - (time penalty + movement penalty)
            int score = 1000 - (timePenalty + movementPenalty);
            return Math.Max(score, 0);  // Ensure score doesn't go below zero
        }
        #endregion

        #region Input Handling
        private void HandleInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    NavigateLeft();
                    break;
                case ConsoleKey.RightArrow:
                    NavigateRight();
                    break;
                case ConsoleKey.UpArrow:
                    NavigateUp();
                    break;
                case ConsoleKey.DownArrow:
                    NavigateDown();
                    break;
                case ConsoleKey.Spacebar:
                    CycleStockPile();
                    break;
                case ConsoleKey.Enter:
                    AttemptAutoMove();
                    break;
            }
        }

        private void NavigateLeft()
        {
            switch (selectedSection)
            {
                case GameSection.Tableau:
                    if (selectedColumn == 0)
                    {
                        selectedSection = GameSection.Stockpile;
                        // Move to the stockpile from the first tableau column
                    }
                    else
                    {
                        selectedColumn--;
                        // Move left within the tableau

                        selectedRow = Math.Min(selectedRow, tableau[selectedColumn].Count - 1);
                        // Ensure row is within bounds
                    }
                    break;

                case GameSection.Foundation:
                    if (selectedColumn == 0)
                    {
                        selectedSection = GameSection.Tableau;
                        // Move to the last tableau column

                        selectedColumn = tableau.Count - 1;
                        selectedRow = tableau[selectedColumn].Count - 1;
                    }
                    else
                    {
                        selectedColumn--;
                        // Move left within the foundation
                    }
                    break;

                case GameSection.Stockpile:
                    selectedSection = GameSection.Foundation;
                    // Move to the last foundation column

                    selectedColumn = foundations.Count - 1;
                    break;
            }
        }

        private void NavigateRight()
        {
            switch (selectedSection)
            {
                case GameSection.Tableau:
                if (selectedColumn == tableau.Count - 1)
                {
                    selectedSection = GameSection.Foundation; 
                    // Move to the first foundation column
                    selectedColumn = 0;
                }
                else
                {
                    // Move right within the tableau
                    selectedColumn++; 
                    // Ensure row is within bounds
                    selectedRow = Math.Min(selectedRow, tableau[selectedColumn].Count - 1);
                }
                break;

            case GameSection.Foundation:
                if (selectedColumn == foundations.Count - 1)
                {
                    selectedSection = GameSection.Stockpile; 
                    selectedColumn = 0;
                }
                else
                { 
                    // Move right within the foundation
                    selectedColumn++;
                }
                break;

            case GameSection.Stockpile:
                selectedSection = GameSection.Tableau;
                selectedColumn = 0;
                selectedRow = tableau[selectedColumn].Count - 1;
                break;
            }
        }

        private void NavigateUp()
        {
            if (selectedSection == GameSection.Tableau && selectedRow > 0)
            {
                selectedRow--;
            }
        }

        private void NavigateDown()
        {
            if (selectedSection == GameSection.Tableau && selectedRow < tableau[selectedColumn].Count - 1)
            {
                selectedRow++;
            }
        }
        #endregion

        #region Movement Handlers
        private void CycleStockPile()
        {
            int cardsToDraw = drawThree ? 3 : 1; // Determine how many cards to cycle

            if (stockpile.Count == 0) return; // No cards to cycle

            List<Card> movedCards = new List<Card>();

            // Move cards from the top to a temporary list
            for (int i = 0; i < cardsToDraw && stockpile.Count > 0; i++)
            {
                movedCards.Add(stockpile[stockpile.Count - 1]); // Get the last card
                stockpile.RemoveAt(stockpile.Count - 1); // Remove the last card
            }

            // Preserve Ordering
            movedCards.Reverse();
            // Reinsert moved cards at the bottom in the same order
            stockpile.InsertRange(0, movedCards);
        }

        private void AttemptAutoMove()
        {
            Card selectedCard = null;

            if (selectedSection == GameSection.Stockpile)
            {
                if (stockpile.Count == 0) return;
                selectedCard = stockpile[0];
            } 
            else if (selectedSection == GameSection.Tableau)
            {
                if (tableau[selectedColumn].Count == 0) return;

                // Get the selected card and all face-up cards below it
                int startIndex = selectedRow;
                List<Card> movingStack = tableau[selectedColumn].GetRange(startIndex, tableau[selectedColumn].Count - startIndex);

                // Ensure we are only moving face-up cards
                if (!movingStack[0].IsFaceUp) return;

                selectedCard = movingStack[0];

                foreach (var column in tableau)
                {
                    if (column != tableau[selectedColumn] && CanMoveToTableau(selectedCard, column))
                    {
                        column.AddRange(movingStack);  // Move all selected cards
                        tableau[selectedColumn].RemoveRange(startIndex, movingStack.Count);
                        CheckCardFlips();
                        MoveCard();
                        return;
                    }
                }
            }
            else if (selectedSection == GameSection.Foundation)
            {
                if (foundations[selectedColumn].Count == 0) return;
                selectedCard = foundations[selectedColumn][0];
            }


            // Ensure the card at the selected row is face-up
            if (selectedCard == null || !selectedCard.IsFaceUp) return;

            // If moving from stockpile, we don't care about the selected column for tableau
            if (selectedSection == GameSection.Stockpile)
            {
                foreach (var column in tableau)
                {
                    if (CanMoveToTableau(selectedCard, column))
                    {
                        column.Add(selectedCard);  // Move the card to the tableau column
                        stockpile.RemoveAt(0);     // Remove the card from stockpile
                        CheckCardFlips();
                        MoveCard();
                        return;
                    }
                }
            }

            // Try moving to another tableau column
            if (selectedSection == GameSection.Tableau)
            {
                foreach (var column in tableau)
                {
                    if (column != tableau[selectedColumn] && CanMoveToTableau(selectedCard, column))
                    {
                        column.Add(selectedCard);
                        tableau[selectedColumn].RemoveAt(selectedRow);
                        CheckCardFlips();
                        MoveCard();
                        return;
                    }
                }
            }

            foreach (var foundation in foundations)
            {
                if (CanMoveToFoundation(selectedCard, foundation))
                {
                    if (selectedSection == GameSection.Tableau)
                    {
                        foundation.Add(selectedCard);
                        tableau[selectedColumn].RemoveAt(selectedRow);  // This assumes the card is from the tableau
                    }
                    else if (selectedSection == GameSection.Stockpile)
                    {
                        foundation.Add(selectedCard);  // Directly add from stockpile to foundation
                        stockpile.RemoveAt(0);
                    }
                    CheckCardFlips();
                    MoveCard();
                    return;
                }
            }

        }

        private void CheckCardFlips()
        {
            // Check and flip the bottom of stacks after movement
            foreach (var column in tableau)
            {
                // Check if the column has any cards
                if (column.Count > 0)
                {
                    // Check if the bottom card is face-down and flip it face-up
                    Card bottomCard = column[column.Count - 1];
                    if (!bottomCard.IsFaceUp)
                    {
                        bottomCard.IsFaceUp = true;
                    }
                }
            }
        }

        private bool CanMoveToTableau(Card card, List<Card> column)
        {
            if (column.Count == 0) return card.Rank == "K" && card.IsFaceUp; // Only Kings start empty columns
            Card topCard = column[column.Count - 1];
            return IsOppositeColor(card, topCard) && GetCardValue(card.Rank) == GetCardValue(topCard.Rank) - 1 && card.IsFaceUp;
        }

        private bool CanMoveToFoundation(Card card, List<Card> foundation)
        {
            if (foundation.Count == 0) return card.Rank == "A" && card.IsFaceUp;
            Card topCard = foundation[foundation.Count - 1];
            return card.Suit == topCard.Suit && GetCardValue(card.Rank) == GetCardValue(topCard.Rank) + 1 && card.IsFaceUp;
        }
        #endregion

        #region Display
        // Display the game itself
        private void RenderGame()
        {
            Console.Clear();
            // Get the width of the console window
            int consoleWidth = 60;

            // Define the text for the header
            string headerText = $"{(drawThree ? "Three-Card-Draw Solitaire" : "One-Card-Draw Solitaire")}";

            // Calculate the number of spaces to prepend for centering
            int padding = (consoleWidth - headerText.Length) / 2;

            // Print the centered header
            Console.WriteLine($"{new string('=', padding)}{headerText}{new string('=', padding)}");
            // Print the control instructions
            Console.WriteLine("Game Controls:\n"
                + "  ◄/►\t Move between columns, foundations, and stockpile\n"
                + "  ▲/▼\t Move up and down along tableau columns\n"
                + "  ENTER\t Select a card to move it to another pile\n"
                + "  SPACE\t Cycle the stockpile\n"
                + "  ESC\t Return to the main menu");
            // Print the final line (separator)
            Console.WriteLine(new string('=', consoleWidth - 1));

            RenderDeck();
            Console.WriteLine();
            RenderTableau();
        }

        private void RenderTableau()
        {
            int maxHeight = tableau.Max(column => column.Count); // Find the tallest column

            for (int row = 0; row < maxHeight; row++)
            {
                for (int col = 0; col < tableau.Count; col++)
                {
                    if (row < tableau[col].Count) // Only print if this row exists
                    {
                        Card card = tableau[col][row];

                        PrintCard(card);
                        if ((col == selectedColumn) && (row == selectedRow) && (selectedSection == GameSection.Tableau)) Console.Write(" ← "); else PrintSpace(columnWidth);
                    }
                    else
                    {
                        PrintEmpty(ConsoleColor.Black);
                        PrintSpace(columnWidth);
                    }
                }
                Console.WriteLine();
            }
        }

        private void RenderDeck()
        {
            RenderFoundation();
            Console.Write(" |  ");
            RenderStockpile();

            // Break Line for Tableau
            Console.Write("\n");
        }

        private void RenderFoundation()
        {
            // Print Foundations
            for (int i = 0; i < foundations.Count; i++)
            {
                if (foundations[i].Count > 0)
                {
                    PrintCard(foundations[i][^1]); // Show final card of foundation
                }
                else
                {
                    PrintEmpty(ConsoleColor.DarkGray);
                }
                if (i == selectedColumn && selectedSection == GameSection.Foundation) Console.Write(" ← "); else PrintSpace(columnWidth);
            }
        }

        private void RenderStockpile()
        {
            int cardsToShow = drawThree ? Math.Min(3, stockpile.Count) : 1;

            // Print empty spaces for missing cards to maintain alignment
            for (int i = 0; i < 3 - cardsToShow; i++)
            {
                PrintEmpty(ConsoleColor.Black);
                PrintSpace(deckColumnWidth);
            }

            // Print extracted cards (right-aligned, last X cards)
            for (int i = cardsToShow - 1; i >= 0; i--)
            {
                PrintCard(stockpile[i]);
                PrintSpace(deckColumnWidth);
            }

            if (selectedSection == GameSection.Stockpile) Console.Write("← "); else PrintSpace(columnWidth);
        }
        #endregion


        #region Card Handling

        // Assigns a numerical value to non-numerical cards for sorting purposes
        private static int GetCardValue(string rank)
        {
            return rank switch { "A" => 1, "J" => 11, "Q" => 12, "K" => 13, _ => int.Parse(rank) };
        }

        // Ensures that only alternating colours are valid in the deck
        private static bool IsOppositeColor(Card c1, Card c2)
        {
            bool isRed = c1.Suit == "Hearts" || c1.Suit == "Diamonds";
            bool isBlack = c2.Suit == "Clubs" || c2.Suit == "Spades";
            return isRed == isBlack;
            // Bug elsewhere in code but inverting it here fixes
        }

        // Prints the card in color
        private static void PrintCard(Card c)
        {
            string cardValue = c.ToString();
            if (c.IsFaceUp)
            {
                // Set the color for red suits
                if (c.Suit == "Hearts" || c.Suit == "Diamonds")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.BackgroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                }
            } 
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray; //Grey
                Console.BackgroundColor = ConsoleColor.White;
            }

            Console.Write((c.ToString()).PadRight(cardWidth));
            Console.ResetColor(); // Reset the console color after printing
        }

        // Insert a designated spacer instead of tab
        private static void PrintEmpty(ConsoleColor color)
        {
            Console.BackgroundColor = color;
            //Console.Write(("").PadRight(cardWidth));
            Console.Write(new string(' ', cardWidth)); // Ensures uniform spacing
            Console.ResetColor(); // Reset the console color after printing
        }

        private static void PrintSpace(int width)
        {
            Console.Write(new string(' ', width));
        }
        #endregion
    }

    // Enum to define the sections
    public enum GameSection
    {
        Tableau,
        Stockpile,
        Foundation
    }
}