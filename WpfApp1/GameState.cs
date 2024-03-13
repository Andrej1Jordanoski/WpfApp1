using System;
using XOPT1;


namespace XO
{
    public class GameState
    {
        public Igrac[,] GameGrid { get; private set; }
        public Igrac CurrentPlayer { get; private set; }
        public int TurnsPassed { get; private set; }
        public bool GameOver { get; private set; }

        public event Action<int, int> MoveMade;
        public event Action<Rezultat> GameEnded;
        public event Action GameRestarted;

        public GameState()
        {
            GameGrid = new Igrac[3, 3];
            CurrentPlayer = Igrac.X;
            TurnsPassed = 0;
            GameOver = false;
        }

        private bool CanMakeMove(int r, int k)
        {
            return !GameOver && GameGrid[r, k] == Igrac.Nikoj;
        }

        private bool IsGridFull()
        {
            return TurnsPassed == 9;
        }

        private void SwitchPlayer()
        {
            if (CurrentPlayer == Igrac.X)
            {
                CurrentPlayer = Igrac.O;
            }
            else
            {
                CurrentPlayer = Igrac.X;
            }
        }

        private bool AreSquaresMarked((int, int)[] squares, Igrac player)
        {
            foreach ((int r, int k) in squares)
            {
                if (GameGrid[r, k] != player)
                {
                    return false;
                }
            }

            return true;
        }

        private bool DidMoveWin(int r, int k, out PobedaInfo winInfo)
        {
            (int, int)[] red = new[] { (r, 0), (r, 1), (r, 2) };
            (int, int)[] kolona = new[] { (0, k), (1, k), (2, k) };
            (int, int)[] diag = new[] { (0, 0), (1, 1), (2, 2) };
            (int, int)[] antidiag = new[] { (0, 2), (1, 1), (2, 0) };

            if (AreSquaresMarked(red, CurrentPlayer))
            {
                winInfo = new PobedaInfo { Type = PobedaTip.Red, Broj = r };
                return true;
            }


            if (AreSquaresMarked(kolona, CurrentPlayer))
            {
                winInfo = new PobedaInfo { Type = PobedaTip.Kolona, Broj = k };
                return true;
            }

            if (AreSquaresMarked(diag, CurrentPlayer))
            {
                winInfo = new PobedaInfo { Type = PobedaTip.Diagonala };
                return true;

            }

            if (AreSquaresMarked(antidiag, CurrentPlayer))
            {
                winInfo = new PobedaInfo { Type = PobedaTip.AntiDiagonala };
                return true;

            }
            winInfo = null;
            return false;
        }

        private bool DidMoveEndGame(int r, int k, out Rezultat gameResult)
        {
            if (DidMoveWin(r, k, out PobedaInfo winInfo))
            {
                gameResult = new Rezultat { Winner = CurrentPlayer, PobedaInfo = winInfo };
                return true;
            }
            if (IsGridFull())
            {
                gameResult = new Rezultat { Winner = Igrac.Nikoj };
                return true;
            }

            gameResult = null;
            return false;

        }
        public void MakeMove(int r, int k)
        {
            if (!CanMakeMove(r, k))
            {
                return;
            }
            GameGrid[r, k] = CurrentPlayer;
            TurnsPassed++;

            if (DidMoveEndGame(r, k, out Rezultat gameResult))
            {
                GameOver = true;
                MoveMade?.Invoke(r, k);
                GameEnded?.Invoke(gameResult);
            }
            else
            {
                SwitchPlayer();
                MoveMade?.Invoke(r, k);
            }
        }

        public void Reset()
        {
            GameGrid = new Igrac[3, 3];
            CurrentPlayer = Igrac.X;
            TurnsPassed = 0;
            GameOver = false;
            GameRestarted?.Invoke();
        }

    }

}
