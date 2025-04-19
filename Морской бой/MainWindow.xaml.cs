using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Морской_бой
{
    public partial class MainWindow : Window
    {
        private bool isPlayerTurn = true;
        private int[,] playerShips = new int[10, 10];
        private int[,] enemyShips = new int[10, 10];
        private int playerShipsRemaining = 10;
        private int enemyShipsRemaining = 10;
        private Random rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
            InitializeGrid(PlayerGrid, true);
            InitializeGrid(EnemyGrid, false);
            StartNewGame();
        }

        private void InitializeGrid(UniformGrid grid, bool isPlayerGrid)
        {
            grid.Children.Clear();
            grid.Rows = 10;
            grid.Columns = 10;

            for (int y = 0; y < 10; y++) // строки
            {
                for (int x = 0; x < 10; x++) // столбцы
                {
                    Button cell = new Button();
                    // Важно: сохраняем координаты как (X,Y)
                    cell.Tag = new Point(x, y);

                    if (isPlayerGrid)
                        cell.Click += PlayerCell_Click;
                    else
                        cell.Click += EnemyCell_Click;

                    grid.Children.Add(cell);
                }
            }
        }

        private void StartNewGame()
        {
            ClearGrid(PlayerGrid);
            ClearGrid(EnemyGrid);

            playerShips = new int[10, 10];
            enemyShips = new int[10, 10];
            playerShipsRemaining = 10;
            enemyShipsRemaining = 10;
            isPlayerTurn = true;

            PlaceShipsRandomly(PlayerGrid, playerShips);
            PlaceShipsRandomly(EnemyGrid, enemyShips);

            UpdateStatus("Ваш ход. Стреляйте по полю противника!");
        }

        private void ClearGrid(UniformGrid grid)
        {
            foreach (Button cell in grid.Children)
            {
                cell.Background = Brushes.White;
                cell.Content = "";
                cell.IsEnabled = true;
            }
        }

        private void PlaceShipsRandomly(UniformGrid grid, int[,] ships)
        {
            int[] shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            foreach (int size in shipSizes)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 100)
                {
                    attempts++;
                    int x = rand.Next(10);
                    int y = rand.Next(10);
                    bool horizontal = rand.Next(2) == 0;

                    if (CanPlaceShip(ships, x, y, size, horizontal))
                    {
                        PlaceShip(grid, ships, x, y, size, horizontal);
                        placed = true;
                    }
                }
            }
        }

        private bool CanPlaceShip(int[,] ships, int x, int y, int size, bool horizontal)
        {
            for (int i = 0; i < size; i++)
            {
                int checkX = x + (horizontal ? i : 0);
                int checkY = y + (horizontal ? 0 : i);

                if (checkX >= 10 || checkY >= 10 || ships[checkX, checkY] != 0)
                    return false;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = checkX + dx;
                        int ny = checkY + dy;

                        if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10 && ships[nx, ny] != 0)
                            return false;
                    }
                }
            }
            return true;
        }

        private void PlaceShip(UniformGrid grid, int[,] ships, int x, int y, int size, bool horizontal)
        {
            for (int i = 0; i < size; i++)
            {
                int placeX = x + (horizontal ? i : 0);
                int placeY = y + (horizontal ? 0 : i);

                ships[placeX, placeY] = 1;

                if (grid == PlayerGrid)
                {
                    // Правильное преобразование координат в индекс
                    int index = placeY * 10 + placeX;
                    Button cell = grid.Children[index] as Button;
                    cell.Background = Brushes.Gray;
                }
            }
        }


        private void EnemyCell_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlayerTurn) return;

            Button cell = sender as Button;
            if (cell == null) return;

            Point position = (Point)cell.Tag;
            int x = (int)position.X;
            int y = (int)position.Y;

            // Проверяем, не стреляли ли уже в эту клетку
            if (cell.Background == Brushes.Red || cell.Background == Brushes.Blue || cell.Background == Brushes.LightBlue)
            {
                UpdateStatus("Вы уже стреляли в эту клетку!");
                return;
            }

            if (enemyShips[x, y] == 1) // Попадание в корабль
            {
                cell.Background = Brushes.Red;
                cell.Content = "X";
                enemyShips[x, y] = 2; // Помечаем как подбитую часть корабля

                if (IsShipSunk(enemyShips, x, y))
                {
                    MarkSunkenShip(EnemyGrid, enemyShips, x, y);
                    enemyShipsRemaining--;
                    UpdateStatus($"Вы потопили корабль! Осталось кораблей: {enemyShipsRemaining}");

                    if (enemyShipsRemaining == 0)
                    {
                        GameOver(true);
                        return;
                    }
                }
                else
                {
                    UpdateStatus($"Вы попали! Стреляйте еще раз. Осталось кораблей: {enemyShipsRemaining}");
                    return;
                }
            }
            else // Промах
            {
                cell.Background = Brushes.Blue;
                cell.Content = "•";
                enemyShips[x, y] = 3; // Помечаем как промах
                UpdateStatus("Промах! Ход противника.");
            }

            isPlayerTurn = false;
            ComputerTurn();
        }

        private void ComputerTurn()
        {
            try
            {
                int x, y;
                int attempts = 0;

                do
                {
                    x = rand.Next(10);
                    y = rand.Next(10);
                    attempts++;

                    if (attempts > 100)
                    {
                        // Поиск оставшихся кораблей
                        for (x = 0; x < 10; x++)
                        {
                            for (y = 0; y < 10; y++)
                            {
                                if (playerShips[x, y] == 1) // Ищем неповрежденные части кораблей
                                {
                                    goto FoundCell;
                                }
                            }
                        }
                        UpdateStatus("Компьютер не может найти клетку для выстрела");
                        isPlayerTurn = true;
                        return;
                    }
                } while (playerShips[x, y] >= 2); // Пропускаем уже атакованные клетки (2 - попадание, 3 - промах)

            FoundCell:
                Button cell = PlayerGrid.Children[y * 10 + x] as Button;
                if (cell == null) return;

                if (playerShips[x, y] == 1) // Попадание в корабль
                {
                    cell.Background = Brushes.Red;
                    cell.Content = "X";
                    playerShips[x, y] = 2; // Помечаем как подбитую часть

                    Debug.WriteLine($"Компьютер попал в {x},{y}");
                    UpdateStatus("Компьютер попал!");

                    if (IsShipSunk(playerShips, x, y))
                    {
                        // Добавляем небольшую задержку для лучшей видимости сообщения
                        Task.Delay(500).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MarkSunkenShip(PlayerGrid, playerShips, x, y);
                                playerShipsRemaining--;
                                UpdateStatus($"Компьютер потопил ваш корабль! Осталось: {playerShipsRemaining}");

                                if (playerShipsRemaining == 0)
                                {
                                    GameOver(false);
                                    return;
                                }
                                isPlayerTurn = true;
                            });
                        });
                    }
                    else
                    {
                        // Добавляем задержку перед повторным выстрелом
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                UpdateStatus("Компьютер попал в ваш корабль! Он стреляет снова.");
                                ComputerTurn(); // Повторный выстрел
                            });
                        });
                        return;
                    }
                }
                else // Промах
                {
                    cell.Background = Brushes.LightBlue;
                    cell.Content = "•";
                    playerShips[x, y] = 3;
                    UpdateStatus("Компьютер промахнулся! Ваш ход.");
                    isPlayerTurn = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в ComputerTurn: {ex}");
                isPlayerTurn = true;
            }
        }

        private bool IsShipSunk(int[,] ships, int x, int y)
        {
            List<Point> shipCells = new List<Point>();
            FindShipCells(ships, x, y, shipCells);

            foreach (Point p in shipCells)
            {
                if (ships[(int)p.X, (int)p.Y] != 2) // Все клетки должны быть подбиты
                    return false;
            }
            return true;
        }

        private void FindShipCells(int[,] ships, int x, int y, List<Point> shipCells)
        {
            if (x < 0 || x >= 10 || y < 0 || y >= 10) return;
            if (ships[x, y] == 0 || ships[x, y] == 3) return; // 0 - пусто, 3 - промах
            if (shipCells.Contains(new Point(x, y))) return;

            shipCells.Add(new Point(x, y));

            // Проверяем только соседние клетки (не диагонали)
            FindShipCells(ships, x + 1, y, shipCells);
            FindShipCells(ships, x - 1, y, shipCells);
            FindShipCells(ships, x, y + 1, shipCells);
            FindShipCells(ships, x, y - 1, shipCells);
        }

        private void MarkSunkenShip(UniformGrid grid, int[,] ships, int x, int y)
        {
            // 1. Находим все клетки потопленного корабля
            List<Point> shipCells = new List<Point>();
            FindShipCells(ships, x, y, shipCells);

            // 2. Собираем все соседние клетки вокруг всего корабля
            HashSet<Point> surroundingCells = new HashSet<Point>();

            foreach (Point shipCell in shipCells)
            {
                // Проверяем всех 8 соседей для каждой клетки корабля
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Пропускаем саму клетку корабля

                        int nx = (int)shipCell.X + dx;
                        int ny = (int)shipCell.Y + dy;

                        // Если клетка в пределах поля и ПУСТАЯ (0)
                        if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10 && ships[nx, ny] == 0)
                        {
                            surroundingCells.Add(new Point(nx, ny));
                        }
                    }
                }
            }

            // 3. Помечаем клетки корабля на поле
            foreach (Point p in shipCells)
            {
                int px = (int)p.X;
                int py = (int)p.Y;

                Button cell = grid.Children[py * 10 + px] as Button;
                if (cell != null)
                {
                    cell.Background = Brushes.Red;
                    cell.Content = "X";
                    cell.IsEnabled = false;
                    ships[px, py] = 2; // Помечаем как подбитую часть
                }
            }

            // 4. Помечаем клетки ВОКРУГ корабля
            foreach (Point p in surroundingCells)
            {
                int px = (int)p.X;
                int py = (int)p.Y;

                ships[px, py] = 3; // Помечаем как "блокированную"

                Button cell = grid.Children[py * 10 + px] as Button;
                if (cell != null && cell.Background != Brushes.Blue)
                {
                    cell.Background = Brushes.LightBlue;
                    cell.Content = "•";
                    cell.IsEnabled = false;
                }
            }
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private void GameOver(bool playerWon)
        {
            if (playerWon)
                MessageBox.Show("Поздравляем! Вы выиграли!", "Игра окончена", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("К сожалению, вы проиграли.", "Игра окончена", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PlayerCell_Click(object sender, RoutedEventArgs e)
        {
            Button cell = sender as Button;
            Point position = (Point)cell.Tag;
            int x = (int)position.X; // Столбец (0-9)
            int y = (int)position.Y; // Строка (0-9)

            // Проверяем границы массива
            if (x < 0 || x >= 10 || y < 0 || y >= 10) return;

            // Если клетка пустая - пробуем поставить корабль
            if (playerShips[x, y] == 0)
            {
                // Временная проверка размещения
                if (CanPlaceSingleCell(x, y))
                {
                    playerShips[x, y] = 1;
                    cell.Background = Brushes.Gray;
                }
                else
                {
                    MessageBox.Show("Нельзя размещать корабли рядом друг с другом!");
                }
            }
            else // Если клетка занята - очищаем
            {
                playerShips[x, y] = 0;
                cell.Background = Brushes.White;
            }
        }

        private bool CanPlaceSingleCell(int x, int y)
        {
            // Проверяем саму клетку
            if (playerShips[x, y] != 0) return false;

            // Проверяем соседей (8-связность)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10)
                    {
                        if (playerShips[nx, ny] != 0)
                            return false;
                    }
                }
            }
            return true;
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }
        private void RulesButton_Click(object sender, RoutedEventArgs e)
        {
            Window rulesWindow = new Window
            {
                Title = "Правила игры Морской бой",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = this.Left + 20,
                Top = this.Top + 20,
                ResizeMode = ResizeMode.NoResize
            };

            TextBlock rulesText = new TextBlock
            {
                Text = "Правила игры:\n\n" +
                       "1. Каждый игрок имеет 10 кораблей:\n" +
                       "   - 1 корабль на 4 клетки\n" +
                       "   - 2 корабля на 3 клетки\n" +
                       "   - 3 корабля на 2 клетки\n" +
                       "   - 4 корабля на 1 клетку\n\n" +
                       "2. Корабли не могут касаться друг друга\n" +
                       "3. Игроки по очереди стреляют по клеткам\n" +
                       "4. Попадание дает право на дополнительный выстрел\n" +
                       "5. Побеждает тот, кто первым потопит все корабли противника",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                FontSize = 14
            };

            rulesWindow.Content = rulesText;
            rulesWindow.Show();
        }

    }
}