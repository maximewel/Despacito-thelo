using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OthelloAI_Despacito
{
    // Tile states
    public enum TileState
    {
        EMPTY = -1,
        WHITE = 0,
        BLACK = 1
    }

    public class OthelloBoard_Despacito : IPlayable.IPlayable
    {
        const int BOARDSIZE_X = 9;
        const int BOARDSIZE_Y = 7;

        int[,] gameBoard = new int[BOARDSIZE_X, BOARDSIZE_Y];
        int whiteScore = 0;
        int blackScore = 0;
        public bool GameFinish { get; set; }

        private Random rnd = new Random();

        public OthelloBoard_Despacito()
        {
            InitBoard();
        }


        public void DrawBoard()
        {
            Console.WriteLine("REFERENCE" + "\tBLACK [X]:" + blackScore + "\tWHITE [O]:" + whiteScore);
            Console.WriteLine("  A B C D E F G H I");
            for (int line = 0; line < BOARDSIZE_Y; line++)
            {
                Console.Write($"{(line + 1)}");
                for (int col = 0; col < BOARDSIZE_X; col++)
                {
                    Console.Write((gameBoard[col, line] == (int)TileState.EMPTY) ? " -" : (gameBoard[col, line] == (int)TileState.WHITE) ? " O" : " X");
                }
                Console.Write("\n");
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        /// <summary>
        /// Returns the board game as a 2D array of int
        /// with following values
        /// -1: empty
        ///  0: white
        ///  1: black
        /// </summary>
        /// <returns></returns>
        public int[,] GetBoard()
        {
            return (int[,])gameBoard;
        }

        #region MinMax

        /**
         * Return the next best possible move using min_max algorithm and the alpha-beta optimisation
         */
        private (int, (int, int)?) MinMax(int[,] field, int depth, int minOrMax, int parentValue, bool isWhite)
        {
            //stopping conditions
            if(depth == 0 || IsFinished(isWhite, field))
                return (Heuristic(field, isWhite), null);

            int optimalValue = int.MinValue * minOrMax;
            (int, int)? optimalMove = null;

            Console.WriteLine($"Searching best move whith max = {minOrMax} at dept {depth}...");
            //Iterate over each possible move for that field
            foreach(var move in this.GetPossibleMove(isWhite, field))
            {
                //Create an identical field
                int[,] playedField = (int[,]) field.Clone();
                //Play the move on this field
                this.PlayMove(move, isWhite, playedField);
                var result = MinMax(playedField, depth-1, -minOrMax, optimalValue, !isWhite);
                int val = result.Item1; 
                Console.WriteLine($"found value : {val}");
                if (val * minOrMax > optimalValue * minOrMax)
                {
                    Console.WriteLine($"kept value as best");
                    optimalValue = val;
                    optimalMove = move;
                    //Pruning of the tree
                    if (optimalValue * minOrMax > parentValue * minOrMax) { }
                        //break;
                }
            }
            return (optimalValue, optimalMove);
        }

        /**
         * Evaluate the given field
         */
        private int Heuristic(int[,] field, bool isWhite)
        {
            return CountPions(field, isWhite);
        }

        private int CountPions(int[,] field, bool isWhite)
        {
            TileState player = isWhite ? TileState.WHITE : TileState.BLACK;
            TileState opponent = !isWhite ? TileState.WHITE : TileState.BLACK;
            int count = 0;
            foreach(var elem in field)
            {
                if (elem == (int)player)
                    count++;
                else if (elem == (int)opponent)
                    count--;
            }
            return count;
        }

        /**
         * Return wether a field is still playable or not
         */
        private bool IsFinished(bool isWhite, int[,] field)
        {
            //TODO
            return GetPossibleMove(isWhite, field).Count <= 0;
        }

        #endregion

        #region IPlayable
        public int GetWhiteScore() { return whiteScore; }
        public int GetBlackScore() { return blackScore; }
        public string GetName() { return "🎵🤘🎸🌮🌮Despacito'thello🌮🌮🎸🤘🎵"; }

        /// <summary>
        /// Maximizes the number of flipped slots at each move
        /// </summary>
        /// <param name="game"></param>
        /// <param name="level"></param>
        /// <param name="whiteTurn"></param>
        /// <returns>The move it will play, will return {P,0} if it has to PASS its turn (no move is possible)</returns>
        public Tuple<int, int> GetNextMove(int[,] game, int level, bool whiteTurn)
        {
            (int, (int,int)?) results = MinMax(gameBoard, 3, 1, int.MaxValue, whiteTurn);
            (int, int)? move = results.Item2;
            if (move.HasValue)
            {
                Console.WriteLine($"Found optimal value {results.Item1} with move {move.Value}");
                return Tuple.Create(move.Value.Item1, move.Value.Item2);
            }  else
                return Tuple.Create(-1, -1);
        }

        public bool PlayMove((int, int) move, bool isWhite, int[,] field)
        {
            int column = move.Item1, line = move.Item2;
            //0. Verify if indices are valid
            if ((column < 0) || (column >= BOARDSIZE_X) || (line < 0) || (line >= BOARDSIZE_Y))
                return false;
            //1. Verify if it is playable
            if (IsPlayable(column, line, isWhite) == false)
                return false;

            //2. Create a list of directions {dx,dy,length} where tiles are flipped
            int c = column, l = line;
            bool playable = false;
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();

            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (field[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0))
                                   && (field[c, l] == (int)opponent)) // pour éviter les trous
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (field[c, l] == (int)ownColor)
                            {
                                playable = true;
                                field[column, line] = (int)ownColor;
                                catchDirections.Add(new Tuple<int, int, int>(dCol, dLine, counter));
                            }
                        }
                    }
                }
            }
            // 3. Flip ennemy tiles
            foreach (var v in catchDirections)
            {
                int counter = 0;
                l = line;
                c = column;
                while (counter++ < v.Item3)
                {
                    c += v.Item1;
                    l += v.Item2;
                    field[c, l] = (int)ownColor;
                }
            }
            //Console.WriteLine("CATCH DIRECTIONS:" + catchDirections.Count);
            ComputeScore();
            return playable;
        }

        //Function that satisfies the interface IsPlayable
        public bool PlayMove(int column, int line, bool isWhite)
        {
            return this.PlayMove((column, line), isWhite, gameBoard);
        }

        /// <summary>
        /// More convenient overload to verify if a move is possible
        /// </summary>
        /// <param name=""></param>
        /// <param name="isWhite"></param>
        /// <returns></returns>
        public bool IsPlayable((int, int) move, bool isWhite, int[,] field)
        {
            //retrieve tuple values
            int column = move.Item1, line = move.Item2;
            //1. Verify if the tile is empty !
            if (field[column, line] != (int)TileState.EMPTY)
                return false;
            //2. Verify if at least one adjacent tile has an opponent tile
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            int c = column, l = line;
            bool playable = false;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();
            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (field[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0)))
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (field[c, l] == (int)ownColor)
                            {
                                playable = true;
                                break;
                            }
                            else if (field[c, l] == (int)opponent)
                                continue;
                            else if (field[c, l] == (int)TileState.EMPTY)
                                break;  //empty slot ends the search
                        }
                    }
                }
            }
            return playable;
        }

        public bool IsPlayable(int column, int line, bool isWhite)
        {
            return this.IsPlayable((column, line), isWhite, gameBoard);
        }

       
        public int GetFlipNumber(int column, int line, bool isWhite)
        {
            if (gameBoard[column, line] != (int)TileState.EMPTY)
                return 0;
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            int c = column, l = line;
            bool playable = false;
            int counter = 0;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();
            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (gameBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0)))
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (gameBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                break;
                            }
                            else if (gameBoard[c, l] == (int)opponent)
                                continue;
                            else if (gameBoard[c, l] == (int)TileState.EMPTY)
                                break;  //empty slot ends the search
                        }
                    }
                }
            }
            return playable ? counter : 0;
        }

        #endregion


        /// <summary>
        /// Returns all the playable moves in a computer readable way (e.g. "<3, 0>")
        /// </summary>
        /// <param name="v"></param>
        /// <param name="whiteTurn"></param>
        /// <returns></returns>
        public List<(int, int)> GetPossibleMove(bool whiteTurn, int[,] field)
        {
            char[] colonnes = "ABCDEFGHIJKL".ToCharArray();
            List<(int, int)> possibleMoves = new List<(int, int)>();
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                {
                    if (IsPlayable((i, j), whiteTurn, field))
                    {
                        possibleMoves.Add((i, j));
                    }
                }
            return possibleMoves;
        }

        private void InitBoard()
        {
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                    gameBoard[i, j] = (int)TileState.EMPTY;

            gameBoard[3, 3] = (int)TileState.WHITE;
            gameBoard[4, 4] = (int)TileState.WHITE;
            gameBoard[3, 4] = (int)TileState.BLACK;
            gameBoard[4, 3] = (int)TileState.BLACK;

            ComputeScore();
        }

        private void ComputeScore()
        {
            whiteScore = 0;
            blackScore = 0;
            foreach (var v in gameBoard)
            {
                if (v == (int)TileState.WHITE)
                    whiteScore++;
                else if (v == (int)TileState.BLACK)
                    blackScore++;
            }
            GameFinish = ((whiteScore == 0) || (blackScore == 0) ||
                        (whiteScore + blackScore == 63));
        }
    }

}
