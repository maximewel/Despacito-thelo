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
        const int BOARDSIZE_X = 9, BOARDSIZE_Y = 7;
        int[,] gameBoard = new int[BOARDSIZE_X, BOARDSIZE_Y];
        int whiteScore = 0;
        int blackScore = 0;
        private bool isPlayerWhite;
        public bool GameFinish { get; set; }

        public int[,] weightedBoard = { {10,-8, 4, 3, 2, 3, 4, -8, 10},
                                        {-8,-8,-1,-1,-1,-1,-1,-8, -8},
                                        {4,-1, 1, 0, 0, 1, 2, -1, 4},
                                        {2,-1, 0, 1, 1, -1, 2, -1, 2},
                                        {4,-1, 0, 1, 1, -1, 2, -1, 4},
                                        {-8,-8,-1,-1,-1,-1,-1,-8,-8},
                                        {10,-8, 4, 3, 2, 3, 4, -8, 10}};

        public OthelloBoard_Despacito()
        {
            InitBoard();
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
            return gameBoard;
        }

        #region MinMax

        /**
         * Return the next best possible move using min_max algorithm and the alpha-beta optimisation
         */
        private (int, (int, int)?) MinMax(int[,] field, int depth, int minOrMax, int parentValue, bool isWhite)
        {
            //get possible moves for this field
            var moves = GetPossibleMove(isWhite, field);

            //stopping conditions : game finished, depth is 0
            if (moves.Count == 0 && GetPossibleMove(!isWhite, field).Count == 0)
                return (HeuristicEndGame(field), null);
            if (depth == 0)
                return (Heuristic(field, isWhite, moves), null);

            //init optimal values kept in memory for alpha-beta and min-max algorithm
            int optimalValue = int.MinValue * minOrMax;
            (int, int)? optimalMove = null;

            //Iterate over each possible move for that field
            foreach (var move in moves)
            {
                //Create an identical field
                int[,] playedField = (int[,]) field.Clone();
                //Play the move on this field

                PlayMove(move, isWhite, playedField);
                var result = MinMax(playedField, depth-1, -minOrMax, optimalValue, !isWhite);
                int val = result.Item1; 
                if (val * minOrMax > optimalValue * minOrMax)
                {
                    optimalValue = val;
                    optimalMove = move;
                    //Pruning of the tree
                    if (optimalValue * minOrMax > parentValue * minOrMax)
                        break;
                }
            }
            return (optimalValue, optimalMove);
        }

        /**
         * Evaluate the given field
         */
        private int Heuristic(int[,] field, bool isWhite, List<(int, int)> moves)
        {
            int boardValue = HeuristicStaticWeightedBoard(field);
            int mobility = HeuristicMobility(field, isWhite, moves);
            int mobilityWeighted = (int)(mobility * boardValue / 100.0);
            return boardValue + mobilityWeighted;
        }

        private void PrintField(int[,] arr)
        {
            var rowCount = arr.GetLength(0);
            var colCount = arr.GetLength(1);
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                    Console.Write(String.Format("{0}\t", arr[row, col]));
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Heuristic used to count if the game will be won or lost in case of end game
        /// </summary>
        /// <param name="field"></param>
        /// <returns>+/- infinity (as int.max/minValue) to indicate win or loose. 0 if draw.</returns>
        private int HeuristicEndGame(int[,] field)
        {
            //init counting variables
            int count = 0;
            TileState player = isPlayerWhite ? TileState.WHITE : TileState.BLACK;
            TileState opponent = isPlayerWhite ? TileState.BLACK : TileState.WHITE;
            //do a simple, basic count of pions
            foreach (var elem in field)
            {
                if (elem == (int)player)
                    count++;
                else if (elem == (int)opponent)
                    count--;
            }
            //draw
            if (count == 0)
                return 0;
            //win/loose
            return count > 0 ? int.MaxValue/2 : int.MinValue/2;
        }


        private int HeuristicCorners(int[,] field)
        {
            int rows = field.GetLength(0)-1;
            int cols = field.GetLength(1)-1;
            int[] vals =  {0, 0, 0};

            vals[field[0, 0] + 1]++;
            vals[field[rows, 0] + 1]++;
            vals[field[0, cols] + 1]++;
            vals[field[rows, cols] + 1]++;

            int maxPlayer = isPlayerWhite ? vals[(int)TileState.WHITE+1] : vals[(int)TileState.BLACK+1];
            int minPlayer = !isPlayerWhite ? vals[(int)TileState.WHITE+1] : vals[(int)TileState.BLACK+1];

            if ((maxPlayer + minPlayer) == 0) return 0;
            return 100 * (maxPlayer - minPlayer) / (maxPlayer + minPlayer);
        }

        private int HeuristicStaticWeightedBoard(int[,] field)
        {
            int sum = 0;
            int sens = isPlayerWhite ? -1 : 1;
            int[] vals = { 0, -sens, sens};

            for (int x = 0; x < field.GetLength(0); x++)
            {
                for (int y = 0; y < field.GetLength(1); y++)
                {
                    sum += weightedBoard[y, x] * vals[field[x, y]+1];
                }
            }
            return sum;
        }

        private int HeuristicMobility(int[,] field, bool isWhite, List<(int, int)> moves)
        {
            int IsMaxFactor = isWhite == isPlayerWhite ? 1 : -1;

            int maxPlayer = moves.Count * IsMaxFactor;
            int minPlayer = GetPossibleMove(!isWhite, field).Count * IsMaxFactor;
            if ((maxPlayer + minPlayer) == 0) return 0;
            return (int)Math.Round(100 * ((double)(maxPlayer - minPlayer) / (double)(maxPlayer + minPlayer)));
        }

        private int CountParity(int[,] field)
        {
            int player = isPlayerWhite ? (int)TileState.WHITE : (int)TileState.BLACK;
            int opponent = isPlayerWhite ? (int)TileState.BLACK : (int)TileState.WHITE;
            int maxPlayer = 0;
            int minPlayer = 0;

            foreach(var elem in field)
            {
                if (elem == player)
                    maxPlayer++;
                else if (elem == opponent)
                    minPlayer++;
            }
            if ((maxPlayer + minPlayer) == 0) return 0;
            return 100*(maxPlayer-minPlayer)/(maxPlayer+minPlayer);
        }

        #endregion

        #region IPlayable
        public int GetWhiteScore() { return whiteScore; }
        public int GetBlackScore() { return blackScore; }
        public string GetName() { return "🎵🤘🎸🌮🌮Despacito'thello🌮🌮🎸🤘🎵"; }

        /// <summary>
        /// Call the MinMax method
        /// </summary>
        /// <param name="game"></param>
        /// <param name="level"></param>
        /// <param name="whiteTurn"></param>
        /// <returns>The move it will play, will return {P,0} if it has to PASS its turn (no move is possible)</returns>
        public Tuple<int, int> GetNextMove(int[,] game, int level, bool whiteTurn)
        {
            isPlayerWhite = whiteTurn;
            (int, (int,int)?) results = MinMax(gameBoard, level, 1, int.MaxValue, whiteTurn);
            (int, int)? move = results.Item2;
            Console.WriteLine($"found best value at : {results.Item1}");
            if (move.HasValue)
                return Tuple.Create(move.Value.Item1, move.Value.Item2);
            //no value : Pass turn
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
            return IsPlayable((column, line), isWhite, gameBoard);
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
        }

    }

}