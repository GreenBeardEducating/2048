using System.Text.Json;

namespace _2048
{
    public partial class MainPage : ContentPage
    {
        private const int BoardSize = 4;
        private const string SaveKey = "2048_save";
        private readonly int[,] board = new int[BoardSize, BoardSize];
        private readonly Button[,] tiles = new Button[BoardSize, BoardSize];
        private readonly Random random = new();
        private int score;
        private bool gameStarted;

        public MainPage()
        {
            InitializeComponent();
            CreateTiles();
            LoadGame();
            UpdateBoard();
        }

        private void CreateTiles()
        {
            for (int row = 0; row < BoardSize; row++)
            {
                for (int column = 0; column < BoardSize; column++)
                {
                    var tile = new Button
                    {
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        Padding = 0,
                        CornerRadius = 5,
                        TextColor = Color.FromArgb("#776E65"),
                        BackgroundColor = Color.FromArgb("#CDC1B4")
                    };

                    tiles[row, column] = tile;
                    BoardGrid.Add(tile, column, row);
                }
            }
        }

        private void OnStartClicked(object? sender, EventArgs e)
        {
            if (!gameStarted)
                StartNewGame();

            MenuView.IsVisible = false;
            GameView.IsVisible = true;
            MessageLabel.Text = string.Empty;
            MenuMessageLabel.Text = string.Empty;
            UpdateBoard();
        }

        private void OnNewGameClicked(object? sender, EventArgs e)
        {
            StartNewGame();
            MessageLabel.Text = string.Empty;
            UpdateBoard();
        }

        private void StartNewGame()
        {
            Array.Clear(board);
            score = 0;
            gameStarted = true;
            AddRandomTile();
            AddRandomTile();
        }

        private void OnMenuClicked(object? sender, EventArgs e)
        {
            GameView.IsVisible = false;
            MenuView.IsVisible = true;
        }

        private void OnMenuSaveClicked(object? sender, EventArgs e)
        {
            if (!gameStarted)
            {
                MenuMessageLabel.Text = "Спочатку почніть гру";
                return;
            }

            SaveGame();
            MenuMessageLabel.Text = "Гру збережено";
        }

        private void OnSaveClicked(object? sender, EventArgs e)
        {
            if (!gameStarted)
            {
                MessageLabel.Text = "Спочатку почніть гру";
                return;
            }

            SaveGame();
            MessageLabel.Text = "Гру збережено";
        }

        private void SaveGame()
        {
            Preferences.Default.Set(SaveKey, JsonSerializer.Serialize(new GameSave(board.Cast<int>().ToArray(), score)));
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            Application.Current?.Quit();
        }

        private void LoadGame()
        {
            var savedGame = Preferences.Default.Get(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(savedGame))
                return;

            try
            {
                var save = JsonSerializer.Deserialize<GameSave>(savedGame);
                if (save?.Tiles is not { Length: 16 })
                    return;

                for (int index = 0; index < save.Tiles.Length; index++)
                    board[index / BoardSize, index % BoardSize] = save.Tiles[index];

                score = save.Score;
                gameStarted = true;
                MessageLabel.Text = "Збережену гру відновлено";
            }
            catch (JsonException)
            {
                Preferences.Default.Remove(SaveKey);
            }
        }

        private void OnUpClicked(object? sender, EventArgs e) => Move("up");
        private void OnDownClicked(object? sender, EventArgs e) => Move("down");
        private void OnLeftClicked(object? sender, EventArgs e) => Move("left");
        private void OnRightClicked(object? sender, EventArgs e) => Move("right");

        private void Move(string direction)
        {
            if (!gameStarted)
            {
                MessageLabel.Text = "Натисніть «Старт», щоб почати гру";
                return;
            }

            bool moved = false;
            for (int line = 0; line < BoardSize; line++)
            {
                var values = new List<int>();
                for (int position = 0; position < BoardSize; position++)
                {
                    var (row, column) = GetPosition(direction, line, position);
                    if (board[row, column] != 0)
                        values.Add(board[row, column]);
                }

                var mergedValues = new List<int>();
                for (int index = 0; index < values.Count; index++)
                {
                    if (index + 1 < values.Count && values[index] == values[index + 1])
                    {
                        int merged = values[index] * 2;
                        mergedValues.Add(merged);
                        score += merged;
                        index++;
                    }
                    else
                    {
                        mergedValues.Add(values[index]);
                    }
                }

                for (int position = 0; position < BoardSize; position++)
                {
                    var (row, column) = GetPosition(direction, line, position);
                    int value = position < mergedValues.Count ? mergedValues[position] : 0;
                    if (board[row, column] != value)
                        moved = true;
                    board[row, column] = value;
                }
            }

            if (moved)
                AddRandomTile();

            UpdateBoard();
            if (!CanMove())
                MessageLabel.Text = "Гру завершено! Натисніть «Старт», щоб почати знову.";
            else if (moved)
                MessageLabel.Text = string.Empty;
        }

        private static (int Row, int Column) GetPosition(string direction, int line, int position) => direction switch
        {
            "up" => (position, line),
            "down" => (BoardSize - 1 - position, line),
            "left" => (line, position),
            _ => (line, BoardSize - 1 - position)
        };

        private void AddRandomTile()
        {
            var emptyCells = new List<(int Row, int Column)>();
            for (int row = 0; row < BoardSize; row++)
                for (int column = 0; column < BoardSize; column++)
                    if (board[row, column] == 0)
                        emptyCells.Add((row, column));

            if (emptyCells.Count == 0)
                return;

            var cell = emptyCells[random.Next(emptyCells.Count)];
            board[cell.Row, cell.Column] = random.Next(10) == 0 ? 4 : 2;
        }

        private bool CanMove()
        {
            for (int row = 0; row < BoardSize; row++)
            {
                for (int column = 0; column < BoardSize; column++)
                {
                    if (board[row, column] == 0)
                        return true;
                    if (row + 1 < BoardSize && board[row, column] == board[row + 1, column])
                        return true;
                    if (column + 1 < BoardSize && board[row, column] == board[row, column + 1])
                        return true;
                }
            }

            return false;
        }

        private void UpdateBoard()
        {
            ScoreLabel.Text = $"Рахунок: {score}";
            for (int row = 0; row < BoardSize; row++)
            {
                for (int column = 0; column < BoardSize; column++)
                {
                    int value = board[row, column];
                    tiles[row, column].Text = value == 0 ? string.Empty : value.ToString();
                    tiles[row, column].BackgroundColor = GetTileColor(value);
                    tiles[row, column].TextColor = value <= 4 ? Color.FromArgb("#776E65") : Color.FromArgb("#F9F6F2");
                }
            }
        }

        private static Color GetTileColor(int value) => value switch
        {
            0 => Color.FromArgb("#CDC1B4"),
            2 => Color.FromArgb("#EEE4DA"),
            4 => Color.FromArgb("#EDE0C8"),
            8 => Color.FromArgb("#F2B179"),
            16 => Color.FromArgb("#F59563"),
            32 => Color.FromArgb("#F67C5F"),
            64 => Color.FromArgb("#F65E3B"),
            128 => Color.FromArgb("#EDCF72"),
            256 => Color.FromArgb("#EDCC61"),
            512 => Color.FromArgb("#EDC850"),
            _ => Color.FromArgb("#EDC53F")
        };

        private sealed record GameSave(int[] Tiles, int Score);
    }
}
