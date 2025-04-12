using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Diagnostics;

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
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Button cell = new Button();
                    cell.Tag = new Point(i, j);

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
                    Button cell = grid.Children[placeY * 10 + placeX] as Button;
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

            if (cell.Background == Brushes.Red || cell.Background == Brushes.Blue)
            {
                UpdateStatus("Вы уже стреляли в эту клетку!");
                return;
            }

            if (enemyShips[x, y] == 1)
            {
                cell.Background = Brushes.Red;
                cell.Content = "X";
                enemyShips[x, y] = 2;

                if (IsShipSunk(enemyShips, x, y))
                {
                    MarkSunkenShip(EnemyGrid, enemyShips, x, y);
                    enemyShipsRemaining--; // Уменьшаем только когда корабль полностью потоплен
                    UpdateStatus($"Вы потопили корабль! Осталось кораблей: {enemyShipsRemaining}");
                }
                else
                {
                    UpdateStatus($"Вы попали! Стреляйте еще раз. Осталось кораблей: {enemyShipsRemaining}");
                    return; // Позволяем игроку стрелять снова
                }
            }
            else
            {
                cell.Background = Brushes.Blue;
                cell.Content = "•";
                UpdateStatus("Промах! Ход противника.");
            }

            if (enemyShipsRemaining == 0)
            {
                GameOver(true);
                return;
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
                        // Поиск первой доступной клетки
                        for (x = 0; x < 10; x++)
                        {
                            for (y = 0; y < 10; y++)
                            {
                                if (playerShips[x, y] == 1) // Ищем непотопленные корабли
                                    goto FoundCell;
                            }
                        }
                        UpdateStatus("Компьютер не может найти клетку для выстрела");
                        isPlayerTurn = true;
                        return;
                    }
                } while (playerShips[x, y] == 2 || playerShips[x, y] == 3);

            FoundCell:
                Button cell = PlayerGrid.Children[y * 10 + x] as Button;
                if (cell == null) return;

                if (playerShips[x, y] == 1)
                {
                    cell.Background = Brushes.Red;
                    cell.Content = "X";
                    playerShips[x, y] = 2;

                    if (IsShipSunk(playerShips, x, y))
                    {
                        MarkSunkenShip(PlayerGrid, playerShips, x, y);
                        playerShipsRemaining--; // Уменьшаем только когда корабль полностью потоплен
                        UpdateStatus($"Компьютер потопил ваш корабль! Осталось кораблей: {playerShipsRemaining}");
                    }
                    else
                    {
                        UpdateStatus($"Компьютер попал в ваш корабль! Его ход продолжается. Осталось кораблей: {playerShipsRemaining}");
                        ComputerTurn(); // Компьютер стреляет снова
                        return;
                    }
                }
                else
                {
                    cell.Background = Brushes.LightBlue;
                    cell.Content = "•";
                    UpdateStatus("Компьютер промахнулся! Ваш ход.");
                    playerShips[x, y] = 3;
                }

                if (playerShipsRemaining == 0)
                {
                    GameOver(false);
                    return;
                }

                isPlayerTurn = true;
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
                if (ships[(int)p.X, (int)p.Y] != 2)
                    return false;
            }
            return true;
        }

        private void FindShipCells(int[,] ships, int x, int y, List<Point> shipCells)
        {
            if (x < 0 || x >= 10 || y < 0 || y >= 10) return;
            if (ships[x, y] == 0 || ships[x, y] == 3) return;
            if (shipCells.Contains(new Point(x, y))) return;

            shipCells.Add(new Point(x, y));

            FindShipCells(ships, x + 1, y, shipCells);
            FindShipCells(ships, x - 1, y, shipCells);
            FindShipCells(ships, x, y + 1, shipCells);
            FindShipCells(ships, x, y - 1, shipCells);
        }

        private void MarkSunkenShip(UniformGrid grid, int[,] ships, int x, int y)
        {
            List<Point> shipCells = new List<Point>();
            FindShipCells(ships, x, y, shipCells);

            foreach (Point p in shipCells)
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int checkX = (int)p.X + i;
                        int checkY = (int)p.Y + j;

                        if (checkX >= 0 && checkX < 10 && checkY >= 0 && checkY < 10)
                        {
                            if (ships[checkX, checkY] == 0)
                            {
                                ships[checkX, checkY] = 3;
                                Button cell = grid.Children[checkY * 10 + checkX] as Button;
                                if (cell != null)
                                {
                                    cell.Background = Brushes.LightBlue;
                                    cell.Content = "•";
                                }
                            }
                        }
                    }
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
            int x = (int)position.X;
            int y = (int)position.Y;

            if (playerShips[x, y] == 0)
            {
                playerShips[x, y] = 1;
                cell.Background = Brushes.Gray;
            }
            else
            {
                playerShips[x, y] = 0;
                cell.Background = Brushes.White;
            }
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }
    }
}