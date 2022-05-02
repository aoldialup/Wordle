using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Wordle
{
    class Program
    {
        static readonly Random random = new Random();

        static StreamReader inFile;
        static StreamWriter outFile;

        const string ANSWERS_DICTIONARY_PATH = "WordleAnswers.txt";
        const string EXTRAS_DICTIONARY_PATH = "WordleExtras.txt";

        const string STATS_PATH = "stats.txt";

        const ConsoleColor LETTER_IN_CORRECT_SLOT = ConsoleColor.Green;
        const ConsoleColor LETTER_IN_INCORRECT_SLOT = ConsoleColor.Yellow;
        const ConsoleColor LETTER_NOT_FOUND = ConsoleColor.DarkGray;

        const ConsoleColor LETTER_UNUSED = ConsoleColor.White;

        const int PLAY = 1;
        const int RESET_STATS = 2;
        const int EXIT_GAME = 3;

        const int BOARD_ROWS = 6;
        const int BOARD_COLS = 5;

        const int MAX_ATTEMPTS = BOARD_ROWS;

        const string WINDOW_TITLE = "Console Wordle";
        const int WINDOW_WIDTH = 45;
        const int WINDOW_HEIGHT = 30;

        static string answer = string.Empty;

        static char[,] board;

        static ConsoleColor[,] boardColors;

        static string[] answersDictionary;
        static string[] extrasDictionary;

        static int userOption;

        static string userInput = string.Empty;

        static int gamesPlayed = 0;
        static int gamesWon = 0;
        static float winPercentage = 0f;
        static int maxStreak = 0;
        static int currentStreak = 0;

        static int[] winDistribution = new int[MAX_ATTEMPTS];

        static int curRow = 0;
        static int curCol = 0;

        static bool isGameRunning = true;

        static bool isInputStageComplete = false;

        static bool hasUserWon = true;

        static ConsoleColor[] alphabetColors;

        const int ALPHABET_LENGTH = 26;

        static bool areStatsEnabled = true;

        static bool hasUserApprovedStatsFailure = false;

        static string statsErrorMessage = string.Empty;

        static void Main(string[] args)
        {
            InitGame();

            GameIntro();

            PlayGame();
        }

        static void PlayGame()
        {
            while (isGameRunning)
            {
                GenerateWord();

                GetWordInput();

                RecalculateStats();

                EndGameScreen();
            }
        }

        static void InitConsole()
        {
            Console.Title = WINDOW_TITLE;
            Console.WindowWidth = WINDOW_WIDTH;
            Console.WindowHeight = WINDOW_HEIGHT;
        }

        static void InitDictionaries()
        {
            Debug.Assert(File.Exists(ANSWERS_DICTIONARY_PATH), $"{ANSWERS_DICTIONARY_PATH} was not found.");
            Debug.Assert(File.Exists(EXTRAS_DICTIONARY_PATH), $"{EXTRAS_DICTIONARY_PATH} was not found.");

            answersDictionary = File.ReadAllLines(ANSWERS_DICTIONARY_PATH);
            extrasDictionary = File.ReadAllLines(EXTRAS_DICTIONARY_PATH);
        }

        static void InitBoard()
        {
            board = new char[BOARD_ROWS, BOARD_COLS];
            boardColors = new ConsoleColor[BOARD_ROWS, BOARD_COLS];
        }

        static void InitAlphabet()
        {
            alphabetColors = new ConsoleColor[ALPHABET_LENGTH];
            CreateDefaultAlphabet();
        }

        static void InitGame()
        {
            InitConsole();
            InitDictionaries();

            InitBoard();
            InitAlphabet();

            LoadStats();
        }

        static void CreateDefaultAlphabet()
        {
            for (int i = 0; i <= ALPHABET_LENGTH - 1; i++)
            {
                alphabetColors[i] = LETTER_UNUSED;
            }
        }

        static void GameIntro()
        {
            if (!areStatsEnabled)
            {
                DisplayStatsError();
            }

            if (isGameRunning)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Welcome to Wordle.\n");
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(
                    "This game is dedicated to the Mac users\n" +
                    "whose machines cannot handle\n" +
                    "more complex applications, such\n" +
                    "as Wordle inside of a web browser.\n");
                Console.ResetColor();

                Console.Write("Press ENTER to continue ");
                Console.ReadLine();

                Console.Clear();
            }
        }

        static bool IsGuessingDone()
        {
            return curRow == MAX_ATTEMPTS - 1 || hasUserWon;
        }

        static void DisplayStatsError()
        {
            if (!areStatsEnabled && !hasUserApprovedStatsFailure)
            {
                while (!isInputStageComplete)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Stats are disabled for the session due to an error.\n\n{statsErrorMessage}");
                    Console.ResetColor();
                    Console.WriteLine();

                    Console.WriteLine($"{PLAY}. Play");
                    Console.WriteLine($"{EXIT_GAME}. Exit");
                    Console.WriteLine();

                    Console.Write("Option: ");

                    if (int.TryParse(Console.ReadLine(), out userOption))
                    {
                        switch (userOption)
                        {
                            case PLAY:
                            isInputStageComplete = true;
                            hasUserApprovedStatsFailure = true;
                            break;

                            case EXIT_GAME:
                            isInputStageComplete = true;
                            isGameRunning = false;
                            break;
                        }
                    }

                    if (!isInputStageComplete)
                    {
                        Console.Write("Invalid option");
                        Console.ReadKey();
                    }

                    Console.Clear();
                }

                isInputStageComplete = false;
            }
        }

        static void DisplayAlphabet()
        {
            for (char c = 'A'; c <= 'Z'; c++)
            {
                Console.ForegroundColor = GetAlphabetColor(c);
                Console.Write(c);
            }

            Console.WriteLine();
            Console.ResetColor();
        }

        static void GetWordInput()
        {
            while (!isInputStageComplete)
            {
                PromptForWord();

                if (IsUserWordValid())
                {
                    UpdateGuess();

                    Console.Clear();
                    Console.ResetColor();
                }
                else
                {
                    ShowWordInputError();
                }
            }

            isInputStageComplete = false;
        }

        static void UpdateGuess()
        {
            AddGuessToBoard();

            if (IsGuessingDone())
            {
                isInputStageComplete = true;
            }
            else
            {
                curRow++;
            }
        }

        static void GenerateWord()
        {
            int randomIndex = random.Next(0, answersDictionary.Length);

            answer = answersDictionary[randomIndex].ToUpper();
        }

        static bool IsUserWordValid()
        {
            string userInputLower = string.Empty;

            if (userInput.Length == answer.Length && userInput.All(char.IsLetter))
            {
                userInputLower = userInput.ToLower();

                return answersDictionary.Contains(userInputLower) || extrasDictionary.Contains(userInputLower);
            }

            return false;
        }

        static int GetAlphabetIndex(char c)
        {
            return c - 'A';
        }

        static void PromptForWord()
        {
            DisplayAlphabet();

            DisplayBoard();

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.Write("Word: ");
            userInput = Console.ReadLine().ToUpper();

            Console.WriteLine();
        }

        static void ShowWordInputError()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.Write("Please enter a valid five-letter word");
            Console.ReadLine();

            Console.Clear();
            Console.ResetColor();
        }

        static void SetAlphabetColor(char letter, ConsoleColor color)
        {
            int index = GetAlphabetIndex(letter);

            alphabetColors[index] = color;
        }

        static ConsoleColor GetAlphabetColor(char letter)
        {
            return alphabetColors[GetAlphabetIndex(letter)];
        }

        static int GetCharCount(string s, char target)
        {
            int count = 0;

            foreach (char c in s)
            {
                if (c == target)
                {
                    count++;
                }
            }

            return count;
        }

        static ConsoleColor GetSquareColor()
        {
            if (userInput[curCol] == answer[curCol])
            {
                return LETTER_IN_CORRECT_SLOT;
            }
            else if (answer.Contains(userInput[curCol]))
            {
                int letterCount = GetCharCount(answer, answer[curCol]);

                int correctCount = 0;

                int highlightedCount = 0;

                for (int i = 0; i < answer.Length; i++)
                {
                    if (userInput[i] == userInput[curCol])
                    {
                        if (i <= curCol)
                        {
                            highlightedCount++;
                        }

                        if (userInput[curCol] == answer[i])
                        {
                            correctCount++;
                        }
                    }
                }

                if (letterCount - correctCount - highlightedCount >= 0)
                {
                    return LETTER_IN_INCORRECT_SLOT;
                }
            }

            return LETTER_NOT_FOUND;
        }

        static void UpdateAlphabet(ConsoleColor squareColor)
        {
            if (squareColor == LETTER_IN_INCORRECT_SLOT)
            {
                if (GetAlphabetColor(userInput[curCol]) != LETTER_IN_CORRECT_SLOT)
                {
                    SetAlphabetColor(userInput[curCol], LETTER_IN_INCORRECT_SLOT);
                }
            }
            else if (squareColor == LETTER_IN_CORRECT_SLOT)
            {
                SetAlphabetColor(userInput[curCol], LETTER_IN_CORRECT_SLOT);
            }
            else
            {
                SetAlphabetColor(userInput[curCol], LETTER_NOT_FOUND);
            }
        }

        static void AddGuessToBoard()
        {
            hasUserWon = true;

            for (curCol = 0; curCol <= BOARD_COLS - 1; curCol++)
            {
                board[curRow, curCol] = userInput[curCol];

                ConsoleColor squareColor = GetSquareColor();

                boardColors[curRow, curCol] = squareColor;

                UpdateAlphabet(squareColor);

                UpdateWinStatus(squareColor);
            }
        }

        static void UpdateWinStatus(ConsoleColor currentSquareColor)
        {
            if (currentSquareColor != LETTER_IN_CORRECT_SLOT)
            {
                hasUserWon = false;
            }
        }

        static void LoadStats()
        {
            try
            {
                string[] winDistributionRaw;

                inFile = File.OpenText(STATS_PATH);

                gamesPlayed = int.Parse(inFile.ReadLine());
                gamesWon = int.Parse(inFile.ReadLine());
                currentStreak = int.Parse(inFile.ReadLine());
                maxStreak = int.Parse(inFile.ReadLine());

                winDistributionRaw = inFile.ReadLine().Split(' ');

                for (int i = 0; i <= MAX_ATTEMPTS - 1; i++)
                {
                    winDistribution[i] = int.Parse(winDistributionRaw[i]);
                }

                inFile.Close();
            }
            catch (FormatException)
            {
                inFile.Close();
                ResetStats();
            }
            catch (FileNotFoundException)
            {
                inFile?.Close();
                SaveStats();
            }
            catch (ArgumentNullException)
            {
                inFile?.Close();
                SaveStats();
            }
            catch (Exception e)
            {
                areStatsEnabled = false;

                statsErrorMessage = e.Message;

                inFile?.Close();
            }
        }

        static void SaveStats()
        {
            try
            {
                outFile = File.CreateText(STATS_PATH);

                outFile.WriteLine(gamesPlayed);
                outFile.WriteLine(gamesWon);
                outFile.WriteLine(currentStreak);
                outFile.WriteLine(maxStreak);

                for (int i = 0; i <= MAX_ATTEMPTS - 1; i++)
                {
                    outFile.Write($"{winDistribution[i]} ");
                }
            }
            catch (Exception e)
            {
                areStatsEnabled = false;
                statsErrorMessage = e.Message;
            }
            finally
            {
                outFile?.Close();
            }
        }

        static void RecalculateStats()
        {
            if (areStatsEnabled)
            {
                gamesPlayed++;

                if (hasUserWon)
                {
                    currentStreak++;
                    gamesWon++;

                    if (currentStreak > maxStreak)
                    {
                        maxStreak = currentStreak;
                    }

                    winDistribution[curRow]++;
                }
                else
                {
                    currentStreak = 0;
                }

                winPercentage = (float)Math.Round(GetPercentage(gamesWon, gamesPlayed), 2);

                SaveStats();
            }
        }

        static float GetPercentage(float a, float b)
        {
            return (a / b) * 100;
        }

        static void EndGameScreen()
        {
            DisplayStatsError();

            while (!isInputStageComplete)
            {
                DisplayEndScreenStatistics();

                GetEndMenuInput();
            }

            isInputStageComplete = false;
        }

        static void DisplayStatisticsHeader()
        {
            Console.ForegroundColor = ConsoleColor.Blue;

            if (areStatsEnabled)
            {
                Console.WriteLine("\t\tSTATISTICS");
            }
            else
            {
                Console.WriteLine("\t\tSTATISTICS ARE UNAVAILABLE");
            }

            Console.ResetColor();
        }

        static void DisplayWinOrLoss()
        {
            if (hasUserWon)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\t\tYOU WON");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"\t\tYOU LOST");
            }
        }

        static void DisplayActiveWord()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\t\tANSWER: {answer}\n");

            Console.ResetColor();
        }

        static void DisplayStatsInfo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine($"{gamesPlayed}\t{winPercentage}\t{currentStreak}\t{maxStreak}");
            Console.WriteLine("Played\tWin %\tCurrent\tMax");
            Console.WriteLine("\t\tStreak\tStreak");

            Console.ResetColor();
        }

        static void DisplayWinDistribution()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nGuess Distribution");

            for (int i = 0; i < winDistribution.Length; i++)
            {
                Console.WriteLine($"{i + 1} : {winDistribution[i]}");
            }

            Console.ResetColor();
        }

        static void DisplayEndScreenStatistics()
        {
            DisplayStatisticsHeader();
            DisplayWinOrLoss();

            DisplayActiveWord();

            if (areStatsEnabled)
            {
                DisplayStatsInfo();

                DisplayWinDistribution();
            }

            Console.WriteLine();
        }

        static void DisplayEndMenuOptions()
        {
            Console.WriteLine($"{PLAY}. Play Again");

            if (areStatsEnabled)
            {
                Console.WriteLine($"{RESET_STATS}. Reset Stats");
            }

            Console.WriteLine($"{EXIT_GAME}. Exit");

            Console.WriteLine();
        }

        static void GetEndMenuInput()
        {
            DisplayEndMenuOptions();

            PerformEndMenuAction();
        }

        static void PerformEndMenuAction()
        {
            Console.Write("Option: ");
            if (int.TryParse(Console.ReadLine(), out userOption))
            {
                if (userOption == PLAY)
                {
                    PlayAgain();
                }
                else if (userOption == RESET_STATS)
                {
                    if (areStatsEnabled)
                    {
                        ResetStats();
                    }
                    else
                    {
                        Console.Clear();
                    }
                }
                else if (userOption == EXIT_GAME)
                {
                    ExitGame();
                }
            }

            Console.Clear();
        }

        static void ExitGame()
        {
            isGameRunning = false;
            isInputStageComplete = true;
        }

        static void PlayAgain()
        {
            for (int i = 0; i <= BOARD_ROWS - 1; i++)
            {
                for (int j = 0; j <= BOARD_COLS - 1; j++)
                {
                    board[i, j] = char.MinValue;
                    boardColors[i, j] = LETTER_UNUSED;
                }
            }

            CreateDefaultAlphabet();

            curRow = 0;
            userOption = -1;

            isInputStageComplete = true;

            Console.Clear();
        }


        static void DisplayBoard()
        {
            for (int i = 0; i <= BOARD_ROWS - 1; i++)
            {
                Console.WriteLine("------------------------");

                for (int j = 0; j <= BOARD_COLS - 1; j++)
                {
                    Console.Write(" |");

                    Console.ForegroundColor = boardColors[i, j];
                    Console.Write(board[i, j]);

                    Console.ResetColor();

                    Console.Write("| ");
                }

                Console.WriteLine();
            }

            Console.WriteLine("------------------------");
            Console.WriteLine();
        }

        static void ResetStats()
        {
            if (areStatsEnabled)
            {
                gamesPlayed = 0;
                gamesWon = 0;
                winPercentage = 0f;
                currentStreak = 0;
                maxStreak = 0;

                Array.Clear(winDistribution, 0, winDistribution.Length);

                SaveStats();

                Console.Clear();
            }
        }
    }
}