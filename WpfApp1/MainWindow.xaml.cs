using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XO;
using XOPT1;

namespace WpfApp1
{

    public partial class MainWindow : Window
    {
        private readonly Dictionary<Igrac, ImageSource> imageSources = new()
        {
            {Igrac.X, new BitmapImage(new Uri("pack://application:,,,/Modeli/X15.png")) },
            {Igrac.O, new BitmapImage(new Uri("pack://application:,,,/Modeli/O15.png")) }

        };
        private readonly Dictionary<Igrac, ObjectAnimationUsingKeyFrames> animations = new()
        {
            {Igrac.X, new ObjectAnimationUsingKeyFrames() },
            {Igrac.O, new ObjectAnimationUsingKeyFrames()  }
        };

        private readonly DoubleAnimation fadeoutAnimation = new DoubleAnimation
        {
            Duration = TimeSpan.FromSeconds(0.5),
            From = 1,
            To = 0
        };

        private readonly DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            Duration = TimeSpan.FromSeconds(0.5),
            From = 0,
            To = 1
        };
        private readonly Image[,] imageControls = new Image[3, 3];
        private readonly GameState gameState = new GameState();

        public MainWindow()
        {
            InitializeComponent();
            SetupGameGrid();
            SetupAnimations();

            gameState.MoveMade += OnMoveMade;
            gameState.GameEnded += OnGameEnded;
            gameState.GameRestarted += OnGameRestarted;
        }
        private void SetupGameGrid()
        {
            for (int r = 0; r < 3; r++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Image imageControl = new Image();
                    GameGrid.Children.Add(imageControl);
                    imageControls[r, k] = imageControl;
                }
            }
        }
        private void SetupAnimations()
        {
            animations[Igrac.X].Duration = TimeSpan.FromSeconds(.25);
            animations[Igrac.O].Duration = TimeSpan.FromSeconds(.25);
            for(int i=0;i<16;i++)
            {
                Uri xUri = new Uri($"pack://application:,,,/Modeli/X{i}.png");
                BitmapImage xImg = new BitmapImage(xUri);
                DiscreteObjectKeyFrame xKeyFrame = new DiscreteObjectKeyFrame(xImg);
                animations[Igrac.X].KeyFrames.Add(xKeyFrame);

                Uri oUri = new Uri($"pack://application:,,,/Modeli/O{i}.png");
                BitmapImage oImg = new BitmapImage(oUri);
                DiscreteObjectKeyFrame oKeyFrame = new DiscreteObjectKeyFrame(oImg);
                animations[Igrac.O].KeyFrames.Add(oKeyFrame);
            }    
        }
        private async Task Fadeout(UIElement uiElement)
        {
            uiElement.BeginAnimation(OpacityProperty, fadeoutAnimation);
            await Task.Delay(fadeoutAnimation.Duration.TimeSpan);
            uiElement.Visibility = Visibility.Hidden;
        }
        private async Task FadeIn(UIElement uiElement)
        {
            uiElement.Visibility = Visibility.Visible;
            uiElement.BeginAnimation(OpacityProperty, fadeInAnimation);
            await Task.Delay(fadeInAnimation.Duration.TimeSpan);
        }
        private async Task TransitionToEndScreen(string text, ImageSource winnerImage)
        {
            await Task.WhenAll(Fadeout(TurnPanel), Fadeout(GameCanvas));
            ResultText.Text = text;
            WinnerImage.Source = winnerImage;
            await FadeIn(EndScreen);
        }
        private async Task TransitionToGameScreen()
        {
            await Fadeout(EndScreen);
            Linija.Visibility = Visibility.Hidden;
            await Task.WhenAll(FadeIn(TurnPanel), FadeIn(GameCanvas));
        }
        private (Point, Point) FindLinePoints(PobedaInfo winInfo)
        {
            double squareSize = GameGrid.Width / 3;
            double margin = squareSize / 2;
            if(winInfo.Type ==PobedaTip.Red)
            {
                double y = (winInfo.Broj * squareSize) + margin;
                return ( new Point(0,y), new Point(GameGrid.Width,y));
            }
            if (winInfo.Type == PobedaTip.Kolona)
            {
                double x = (winInfo.Broj * squareSize) + margin;
                return (new Point(x, 0), new Point(x,GameGrid.Height));
            }
            if (winInfo.Type == PobedaTip.Diagonala)
            {
                
                return (new Point(0, 0), new Point(GameGrid.Width, GameGrid.Height));
            }
            return (new Point(GameGrid.Width, 0), new Point(0, GameGrid.Height));
        }
        private async Task ShowLine(PobedaInfo winInfo)
        {
            (Point start, Point end) = FindLinePoints(winInfo);
            Linija.X1 = start.X;
            Linija.Y1 = start.Y;

            DoubleAnimation x2Animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.25),
                From = start.X,
                To = end.X

            };
            DoubleAnimation y2Animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.25),
                From = start.Y,
                To = end.Y
            };
            Linija.Visibility = Visibility.Visible;
            Linija.BeginAnimation(Line.X2Property, x2Animation);
            Linija.BeginAnimation(Line.Y2Property, y2Animation);
            await Task.Delay(x2Animation.Duration.TimeSpan);
        }
        private void OnMoveMade(int r, int k)
        {
            Igrac player = gameState.GameGrid[r, k];
            imageControls[r, k].BeginAnimation(Image.SourceProperty, animations[player]);
            PlayerImage.Source = imageSources[gameState.CurrentPlayer];
        }   
        private async void OnGameEnded(Rezultat rezultat)
        {
            await Task.Delay(1000);
            if(rezultat.Winner==Igrac.Nikoj)
            {
                await TransitionToEndScreen("Ne reseno!", null);
            }
            else
            {
                ShowLine(rezultat.PobedaInfo);
                await Task.Delay(1000);
                await TransitionToEndScreen("Pobednik:", imageSources[rezultat.Winner]);
            }
        } 
        private async void OnGameRestarted()
        {
            for (int r = 0; r < 3; r++)
            {
                for (int k = 0; k < 3; k++)
                {
                    imageControls[r, k].BeginAnimation(Image.SourceProperty, null);
                    imageControls[r, k].Source = null;
                }
            }
            PlayerImage.Source = imageSources[gameState.CurrentPlayer];
            await TransitionToGameScreen();
        }
        private void GameGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            double squareSize = GameGrid.Width / 3;
            Point clickPosition = e.GetPosition(GameGrid);
            int red = (int)(clickPosition.Y / squareSize);
            int kolona = (int)(clickPosition.X / squareSize);
            gameState.MakeMove(red, kolona);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (gameState.GameOver)
            {
                gameState.Reset();
            }
        }
    }
}