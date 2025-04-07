using NUnit.Framework;
using NUnit.Framework.Legacy;
using TerminalSolitaire;

#if DEBUG  // Ensure tests are only included in Debug builds

namespace TerminalSolitaire.Tests
{
    [TestFixture]
    public class GameLogicTests
    {
        [Test]
        public void WriteWelcome_ContainsExpectedText()
        {
            // Act
            string output = Program.writeWelcome();

            // Assert
            StringAssert.Contains("Console Games", output);
            StringAssert.Contains("Menu Controls:", output);
            StringAssert.Contains("▲/▼\t Move between options", output);
            StringAssert.Contains("ENTER\t Select", output);
        }

        [Test]
        public void GetMenuSelection_SelectsCorrectIndex()
        {
            // Arrange
            Queue<ConsoleKeyInfo> inputQueue = new Queue<ConsoleKeyInfo>();
            inputQueue.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
            inputQueue.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));

            // Act
            int selection = Program.GetMenuSelection(
                new string[] { "One-Card Solitaire", "Three-Card Solitaire", "High Scores", "Exit" },
                () => inputQueue.Dequeue(),
                true // It is a test
            );

            // Assert
            Assert.That(selection, Is.EqualTo(1));  // Should select "Three-Card Solitaire"
        }
    }
}

#endif