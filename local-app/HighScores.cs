using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using System.Xml.Linq;
using System.Security.AccessControl;

public class HighScores
{
    List<List<HighScore>> highScoresByGame = new List<List<HighScore>>();
    int listLimit = 5;

    private static readonly HttpClient client = new HttpClient();

    // Rails API database info
    private const string apiUrl = "http://44.211.129.207:80";

    // Local Backup database info
    private string dbPath;
    private string connectionString;
    public HighScores()
    {
        dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localdatabase.db");
        connectionString = $"Data Source={dbPath};";

        InitializeDatabaseAsync().GetAwaiter().GetResult();
#if DEBUG
        PopulateDatabaseAsync().GetAwaiter().GetResult();
#endif
    }
    public void Run()
    {
        // Loop through each game option in program.options, excluding the last two
        for (int i = 0; i < Program.GetOptions().Length - 2; i++)
        {
            string game = Program.GetOptions()[i];  // Get the game name from the options array
            // Display high scores for this game
            List<HighScore> highScores = FetchHighScoresAsync(game, 5).GetAwaiter().GetResult();

            if (highScores.Count > 0)
            {
                highScoresByGame.Add(highScores);
            }
        }

        // Render the fetched high scores
        RenderScores();

        ConsoleKeyInfo key;
        do { key = Console.ReadKey(true); } while (key.Key != ConsoleKey.Escape);
    }

    // Ensure that there's a local table at all times
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                CREATE TABLE IF NOT EXISTS HighScores (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Game TEXT NOT NULL,
                    Score INTEGER NOT NULL
                )";

                using (var command = new SqliteCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}");
        }
    }

    #region Create
    // Method to add a new score to Rails API
    public async Task SaveHighScoreToApiAsync(HighScore score)
    {
        try
        {
            await client.PostAsJsonAsync(apiUrl, score);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error posting to API: {ex.Message}.");
        }
    }

    // Method to add a new score to the local parallel database
    public async Task SaveHighScoreToLocalAsync(HighScore score)
    {
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO HighScores (Name, Game, Score) VALUES (@Name, @Game, @Score)";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", score.Name);
                    command.Parameters.AddWithValue("@Game", score.Game);
                    command.Parameters.AddWithValue("@Score", score.Score);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error posting to Local Database: {ex.Message}.");
        }
    }
    #endregion

    #region Read
    // Method to fetch high scores from the Rails API
    public async Task<List<HighScore>> FetchHighScoresAsync(string game, int limit = 5)
    {
        try
        {
            // Attempt to fetch from the Rails API
            string requestUrl = $"{apiUrl}/high_scores/top?game={Uri.EscapeDataString(game)}&limit={limit}";
            var response = await client.GetFromJsonAsync<List<HighScore>>(requestUrl);

            if (response != null && response.Count > 0)
            {
                return response;  // Return the scores fetched from Rails API
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching from Rails API: {ex.Message}, checking local backup.");
        }

        // If the API fetch failed, fetch from local SQLite database
        return await FetchHighScoresLocalAsync(game, limit);
    }

    // Method to fetch high scores from the Local Backup
    public async Task<List<HighScore>> FetchHighScoresLocalAsync(string game, int limit)
    {
        List<HighScore> highScores = new List<HighScore>();

        // Use SQLite to fetch the top X high scores for the given game
        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();

            // Define the query to get the top scores for a given game, ordered by score
            string query = "SELECT id, Name, Game, Score FROM HighScores WHERE Game = @Game ORDER BY Score DESC LIMIT @Limit";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Game", game);
                command.Parameters.AddWithValue("@Limit", limit);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Read data from the database
                        int id = Convert.ToInt32(reader["id"]);
                        string name = reader["Name"].ToString();
                        string gameName = reader["Game"].ToString();
                        int score = Convert.ToInt32(reader["Score"]);

                        // Add to the list of high scores
                        highScores.Add(new HighScore(id, name, gameName, score));
                    }
                }
            }
        }

        return highScores;
    }
    #endregion

    #region Update
    public async Task UpdateHighScoreAsync(HighScore score)
    {
        try
        {
            string requestUrl = $"{apiUrl}/high_scores/{score.Id}";
            await client.PutAsJsonAsync(requestUrl, score);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating API: {ex.Message}. Updating local backup.");
            await UpdateHighScoreLocalAsync(score);
        }
        await UpdateHighScoreLocalAsync(score);
    }

    public async Task UpdateHighScoreLocalAsync(HighScore score)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            string query = "UPDATE HighScores SET Name = @Name, Score = @Score WHERE id = @Id";
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Name", score.Name);
                command.Parameters.AddWithValue("@Score", score.Score);
                command.Parameters.AddWithValue("@Id", score.Id);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    #endregion

    #region Delete
    public async Task DeleteHighScoreAsync(int id)
    {
        try
        {
            string requestUrl = $"{apiUrl}/high_scores/{id}";
            await client.DeleteAsync(requestUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting from API: {ex.Message}. Deleting from local backup.");
        }
        await DeleteHighScoreLocalAsync(id);
    }

    public async Task DeleteHighScoreLocalAsync(int id)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            string query = "DELETE FROM HighScores WHERE id = @Id";
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    #endregion
    public void RenderScores()
    {
        // Tracks the currently highlighted game
        int selectedIndex = 0;

        while (true)
        {
            Console.Clear();
            // Get the width of the console window
            int consoleWidth = 60;
            // Define the text for the header
            string headerText = "Top 5 Scores";
            // Calculate the number of spaces to prepend for centering
            int padding = (consoleWidth - headerText.Length) / 2;
            // Print the centered "Top 5 Scores" header
            Console.WriteLine($"{new string('=', padding)}{headerText}{new string('=', padding)}");

            // Print the control instructions
            Console.WriteLine("Controls:\n"
                + "  ▲/▼\t Move between games\n"
                + "  ENTER\t View more detailed ranks for the selected Game\n"
                + "  ESC\t Return to the main menu");


            // Print the final line (separator)
            Console.WriteLine(new string('=', consoleWidth - 1));

            if (highScoresByGame.Count == 0)
            {
                Console.WriteLine("No high scores available.");
                return;
            }

            // Display each game and highlight the selected one
            for (int i = 0; i < highScoresByGame.Count; i++)
            {
                List<HighScore> group = highScoresByGame[i];

                if (group.Count == 0) continue;

                // Define the header text for each game
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"> {group[0].Game}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  {group[0].Game}");
                }


                // Display the top 5 scores for this game
                var topScores = group.OrderByDescending(score => score.Score);

                // Display each score with sequential numbering
                int rank = 1;
                foreach (HighScore score in topScores)
                {
                    Console.WriteLine($"{rank}. {score.Name}(#{score.Id})\t- {score.Score}pts ");
                    rank++;
                }

                // Add a line break between groups for readability
                Console.WriteLine();
            }

            // Call the input handler and check if the user wants to exit
            if (!HandleInput(ref selectedIndex))
                break;
        }
    }
    public void PostScore(int score, string source)
    {
        // If all foundations are complete then the game is won
        Console.WriteLine("You Win!");
        Console.WriteLine($"Your score: {score}");

        // Prompt the player for their name
        Console.Write("Please enter your name: ");
        string playerName = Console.ReadLine();

        // Create a new HighScore object with the player's name, game type, and score
        HighScore newScore = new HighScore(0, playerName, source, score);

        // Run the async methods inside a Task.Run()
        Task.Run(async () =>
        {
            // Attempt to save the high score to the API server
            await SaveHighScoreToApiAsync(newScore);
            // Save the high score to the local SQLite database as a backup
            await SaveHighScoreToLocalAsync(newScore);
        }).GetAwaiter().GetResult();  // Blocking call, but it's wrapped in Task.Run()
    }

    public async Task RenderScore()
    {

    #if DEBUG
        Console.WriteLine("Controls:\n"
            + "  BACKSPACE\t Delete Selected\n"
            + "  ENTER\t Add a new High-Score\n"
            + "  ESC\t Return to the main menu");
    #endif
    }
    private bool HandleInput(ref int selectedIndex)
    {
        ConsoleKeyInfo key = Console.ReadKey(true);

        if (key.Key == ConsoleKey.UpArrow)
        {
            selectedIndex = (selectedIndex > 0) ? selectedIndex - 1 : highScoresByGame.Count - 1;
        }
        else if (key.Key == ConsoleKey.DownArrow)
        {
            selectedIndex = (selectedIndex < highScoresByGame.Count - 1) ? selectedIndex + 1 : 0;
        }
        else if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine($"\nViewing details for: {highScoresByGame[selectedIndex][0].Game}");
            Console.ReadKey();  // Placeholder for detailed view logic
        }
        else if (key.Key == ConsoleKey.Escape)
        {
            return false;  // Exit the loop
        }

        return true;  // Continue looping
    }

#if DEBUG
    // Debug Methods to allow CRUD operations without having to actually complete a game
    public async Task PopulateDatabaseAsync()
    {
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if the table already has content
                string checkQuery = "SELECT COUNT(*) FROM HighScores";
                using (var command = new SqliteCommand(checkQuery, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    if (Convert.ToInt32(result) > 0)
                    {
                        // Table already has content, so do nothing
                        return;
                    }
                }

                string query = @"
                INSERT INTO HighScores (Name, Game, Score) VALUES
                ('Alice', 'One-Card Solitaire', 500),
                ('Bob', 'One-Card Solitaire', 750),
                ('Charlie', 'One-Card Solitaire', 600),
                ('David', 'One-Card Solitaire', 800),
                ('Eve', 'One-Card Solitaire', 700),
                ('Frank', 'Three-Card Solitaire', 1000),
                ('Grace', 'Three-Card Solitaire', 950),
                ('Hank', 'Three-Card Solitaire', 1100),
                ('Ivy', 'Three-Card Solitaire', 1200),
                ('Jack', 'Three-Card Solitaire', 900)";

                using (var command = new SqliteCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error populating database: {ex.Message}");
        }
    }

#endif

#if RELEASE
    // Dud copies of the Debug access methods

#endif
}
public class HighScore
{
    public int Id { get; set; }   // This is the auto-incremented ID from the database
    public string Name { get; set; }
    public string Game { get; set; }
    public int Score { get; set; }

    public HighScore(int id, string name, string game, int score)
    {
        this.Id = id;
        Name = name;
        Game = game;
        Score = score;
    }
}