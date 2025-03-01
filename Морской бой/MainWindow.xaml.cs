using System;
using System.Windows;
using System.Windows.Controls;

namespace Морской_бой
{
    public partial class MainWindow : Window
    {
        private Button[,] buttons = new Button[10, 10];
        private bool isPlayerTurn = true;
        private Random random = new Random();
        private int playerHits = 0;
        private int computerHits = 0;
        private int[,] playerField = new int[10, 10];
        private int[,] computerField = new int[10, 10];

        private const int ShipCount = 10;
        private const int ShipSize = 1;

        public MainWindow()
        {
            InitializeComponent();
            PlaceComputerShips();
        }

        private void CreateGameGrid()
        {
            GameGrid.Children.Clear();
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    buttons[i, j] = new Button();
                    buttons[i, j].Content = "";
                    buttons[i, j].Width = 40;
                    buttons[i, j].Height = 40;
                    buttons[i, j].Margin = new Thickness(2);
                    buttons[i, j].Click += Button_Click;
                    Grid.SetRow(buttons[i, j], i);
                    Grid.SetColumn(buttons[i, j], j);
                    GameGrid.Children.Add(buttons[i, j]);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            if (isPlayerTurn && clickedButton.Content.ToString() == "")
            {
                clickedButton.Content = "X";
                playerHits++;

                if (CheckHit(clickedButton, computerField))
                {
                    StatusMessage.Text = "Попадание!";
                }
                else
                {
                    StatusMessage.Text = "Промах!";
                }

                if (CheckVictory(playerHits, ShipCount))
                {
                    StatusMessage.Text = "Вы победили!";
                    return;
                }

                isPlayerTurn = false;
                ComputerTurn();
            }
            else
            {
                StatusMessage.Text = "Выберите другую клетку!";
            }
        }

        private void ComputerTurn()
        {
            int x, y;
            do
            {
                x = random.Next(0, 10);
                y = random.Next(0, 10);
            } while (buttons[x, y].Content.ToString() == "X" || buttons[x, y].Content.ToString() == "O");

            buttons[x, y].Content = "O";
            computerHits++;

            if (CheckHit(buttons[x, y], playerField))
            {
                StatusMessage.Text = "Компьютер попал!";
            }
            else
            {
                StatusMessage.Text = "Компьютер промахнулся!";
            }

            if (CheckVictory(computerHits, ShipCount))
            {
                StatusMessage.Text = "Компьютер победил!";
                return;
            }

            StatusMessage.Text = "Сделал ход!";
            isPlayerTurn = true;
        }

        private bool CheckHit(Button button, int[,] field)
        {
            int row = Grid.GetRow(button);
            int col = Grid.GetColumn(button);
            return field[row, col] == 1;
        }

        private bool CheckVictory(int hits, int requiredHits)
        {
            return hits >= requiredHits;
        }

        private void PlaceComputerShips()
        {
            for (int i = 0; i < ShipCount; i++)
            {
                int x, y;
                do
                {
                    x = random.Next(0, 10);
                    y = random.Next(0, 10);
                } while (computerField[x, y] == 1);

                computerField[x, y] = 1;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StatusMessage.Text = "Игра началась!";
            playerHits = 0;
            computerHits = 0;
            CreateGameGrid();
            Array.Clear(playerField, 0, playerField.Length);
            Array.Clear(computerField, 0, computerField.Length);
            PlaceComputerShips();
        }
    }
}
