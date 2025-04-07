using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminalSolitaire;

class Program
{
    static string[] options = { "One-Card Solitaire", "Three-Card Solitaire", "High Scores", "Exit" };
    public static string[] GetOptions()
    {
        return options;
    }

    // Initialisation Class, starts the game. 
    // In future handles selection if other terminal games are added
    static void Main()
    {
        RenderMenu();
    }

    public static void RenderMenu()
    {
        Console.Clear();
        Console.WriteLine(writeWelcome());

        int selectedIndex = GetMenuSelection(options, () => Console.ReadKey(true));

        Console.Clear();

        // Map menu selections to corresponding actions
        var actions = new Dictionary<string, Action>
        {
            { "One-Card Solitaire", () => StartSolitaire(false) },
            { "Three-Card Solitaire", () => StartSolitaire(true) },
            { "High Scores", () => StartScores() },
            { "Exit", ExitGame }
        };

        actions[options[selectedIndex]].Invoke();

    }

    public static string writeWelcome()
    {
        // Get the width of the console window
        int consoleWidth = 60;

        // Define the text for the header
        string headerText = "Console Games";

        // Calculate the number of spaces to prepend for centering
        int padding = (consoleWidth - headerText.Length) / 2;

        // Print the centered header
        return $"{new string('=', padding)}{headerText}{new string('=', padding)}" +
            $"\nMenu Controls:" +
            $"\n  ▲/▼\t Move between options" +
            $"\n  ENTER\t Select" +
            $"\n{new string('=', consoleWidth - 1)}";
    }
    
    public static int GetMenuSelection(string[] options, Func<ConsoleKeyInfo> inputProvider, bool isTest = false)
    {
        int selectedIndex = 0;
        while (true)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"> {options[i]}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  {options[i]}");
                }
            }

            ConsoleKeyInfo key = inputProvider.Invoke();
            if (key.Key == ConsoleKey.UpArrow)
                selectedIndex = (selectedIndex == 0) ? options.Length - 1 : selectedIndex - 1;
            else if (key.Key == ConsoleKey.DownArrow)
                selectedIndex = (selectedIndex == options.Length - 1) ? 0 : selectedIndex + 1;
            else if (key.Key == ConsoleKey.Enter)
                break;

            // Only move the cursor in actual execution, NOT in unit tests, prevents duplicates
            if (!isTest) Console.SetCursorPosition(0, Console.CursorTop - options.Length);
        }
        return selectedIndex;
    }
    
    static void StartScores()
    {
        HighScores list = new HighScores();
        list.Run();
        RenderMenu();
    }

    static void StartSolitaire(bool drawThree)
    {
        SolitaireGame game = new SolitaireGame(drawThree);
        game.Run();
        RenderMenu();
    }

    static void ExitGame()
    {
        Console.WriteLine("\nThanks for playing!");
        // Pause for 1000 ms
        Thread.Sleep(1000);
        // Then exit
        Environment.Exit(0);
    }
}
