﻿using System;
using System.Collections.Generic;

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
        //base class attributes from othello OHU
        const int BOARDSIZE_X = 9, BOARDSIZE_Y = 7;
        int[,] gameBoard = new int[BOARDSIZE_X, BOARDSIZE_Y];
        int whiteScore = 0;
        int blackScore = 0;
        public bool GameFinish { get; set; }

        //added MinMax attributes
        private bool isPlayerWhite;
        private int LATEGAME_EMPTY_TRESHOLD = (int) (0.10 * (BOARDSIZE_X * BOARDSIZE_Y)); //<10% empty -> late game
        private static readonly int[,] weightedBoard = { 
                                        {20,-4, 4, 3, 2, 3, 4, -4, 20},
                                        {-4,-5,-1,-1,-1,-1,-1,-5, -4},
                                        {4,-1, 1, 0, 0, 1, 2, -1, 4},
                                        {2,-1, 0, 1, 1, -1, 2, -1, 2},
                                        {4,-1, 0, 1, 1, -1, 2, -1, 4},
                                        {-4,-5,-1,-1,-1,-1,-1,-5,-4},
                                        {20,-4, 4, 3, 2, 3, 4, -4, 20}};

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
        /// <returns>The board</returns>
        public int[,] GetBoard()
        {
            return gameBoard;
        }

        #region MinMax
        /*Region Minmax : in this region are placed all MinMax related methods */


        /// <summary>
        /// Recusrive function. Min max algorithm : uses itself to play @level rounds and find the best move amongst them
        /// </summary>
        /// <param name="field">the feld</param>
        /// <param name="depth">number of rounds to play</param>
        /// <param name="minOrMax">Wether this node is trying to maximize or minimize</param>
        /// <param name="parentValue">the current value of the parent (alpha-beta)</param>
        /// <param name="whiteTurn">wether this is white player's turn</param>
        /// <returns>the optimal value and its associated move</returns>
        private (int, (int, int)?) MinMax(int[,] field, int depth, int minOrMax, int? parentValue, bool whiteTurn)
        {
            //get possible moves for this field and player/opponent (to avoid multiple calculations)
            var movesPlayer = GetPossibleMove(whiteTurn, field);
            var movesOpponent = GetPossibleMove(!whiteTurn, field);

            //stopping conditions : game finished, depth is 0. return relevant heuristic
            if (depth == 0)
            {
                if (movesPlayer.Count == 0 && movesOpponent.Count == 0)
                    return (HeuristicEndGame(field), null); //end of game
                else
                    return (Heuristic(field), null); //just out of depth
            }

            //special case : this player can not play. Either game finished or pass turn
            if (movesPlayer.Count == 0)
            {
                if(movesOpponent.Count == 0)
                    return (HeuristicEndGame(field), null); //end of game detected
                else
                    return MinMax(field, depth, -minOrMax, parentValue, !whiteTurn); //simulate "pass turn"
            }

            //init optimal values kept in memory for alpha-beta and min-max algorithm
            int optimalValue = int.MinValue+100 * minOrMax; //+100 to avoid overflow !
            (int, int)? optimalMove = null;
            double optimalMobility = -1.0; //discriminating factor

            //Iterate over each possible move for that field
            foreach (var move in movesPlayer)
            {
                //Create an identical field
                int[,] playedField = (int[,]) field.Clone();
                //Play the move on this field, calculate its value using recursive minmax
                PlayMove(move, whiteTurn, playedField);
                var result = MinMax(playedField, depth-1, -minOrMax, optimalValue, !whiteTurn);
                int val = result.Item1;
                //check if it is better using mobility as a tie-breaker
                double mobility = HeuristicMobility(field);
                if ((val * minOrMax > optimalValue * minOrMax) || (val == optimalValue && mobility > optimalMobility))
                {
                    optimalValue = val;
                    optimalMove = move;
                    optimalMobility = mobility;
                    //Pruning of the tree
                    if (parentValue.HasValue && (optimalValue * minOrMax > parentValue * minOrMax))
                        break;
                }
            }
            //return the worst/best one !
            return (optimalValue, optimalMove);
        }

        /// <summary>
        /// Evaluate the given field using different factors (static & dynamics)
        /// </summary>
        /// <param name="field">field to evaluate</param>
        /// <returns>the evaluation for this field</returns>
        private int Heuristic(int[,] field)
        {
            //start by defining a static base value and the factors to give to the dynamic values
            int baseValue;
            int mobFactor, parityFactor, borderFactor, cornerFactor;
            if (IsLateGame(field))
            {
                //late game focuses more on the pawns count to try and "steal" the game if possible
                baseValue = 10000;
                (mobFactor, cornerFactor, borderFactor, parityFactor) = (10, 0, 0, 100);
            }
            else
            {
                //realy game focuses on mobility & stability
                baseValue = HeuristicStaticWeightedBoard(field);
                (mobFactor, cornerFactor, borderFactor, parityFactor) = (100, 100, 20, 1);
            }

            //calcul all the dynamic values multiplied by their factors
            double mobility = HeuristicMobility(field) * mobFactor;
            double parity = HeuristicParity(field) * parityFactor;
            double borders = HeuristicBorders(field) * borderFactor;
            double corners = HeuristicCorners(field) * cornerFactor;
            //calculate the total normalized factor and its final boosting value
            double totalFactor = (mobility + borders + corners + parity) / (double)(mobFactor + parityFactor + borderFactor + cornerFactor);
            int totalBooster = (int)Math.Round(totalFactor * Math.Abs(baseValue));

            return baseValue + totalBooster;
        }

        /// <summary>
        /// Return wether this field is in its 'late game' phase according to its empty cells count
        /// </summary>
        /// <param name="field">The field to evaluate</param>
        /// <returns>true if late game, false otherwise</returns>
        private bool IsLateGame(int[,] field)
        {
            return CountEmptyCells(field) <= LATEGAME_EMPTY_TRESHOLD;
        }

        /// <summary>
        /// Count the empty cells in the field (not to any player)
        /// </summary>
        /// <param name="field">The field to evaluate</param>
        /// <returns>The count of empty cells</returns>
        private int CountEmptyCells(int[,] field)
        {
            int count = 0;
            int emptyTile = (int)TileState.EMPTY;

            foreach (var elem in field)
            {
                if (elem == emptyTile)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Very usefull for debug purposes ! Print the field in a human readable way
        /// </summary>
        /// <param name="arr">Field to display</param>
        private void PrintField(int[,] arr)
        {
            Dictionary<int, String> dict = new Dictionary<int, String> { { (int)TileState.BLACK, "b" }, { (int)TileState.WHITE, "w" }, { (int)TileState.EMPTY, "o" } };
            var rowCount = arr.GetLength(0);
            var colCount = arr.GetLength(1);
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                    Console.Write(String.Format("{0}\t", dict[arr[row, col]]));
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Heuristic : Static evaluation of the field according to a board
        /// </summary>
        /// <param name="field">Field to evaluate</param>
        /// <returns>The value of the board, as an integer</returns>
        private int HeuristicStaticWeightedBoard(int[,] field)
        {
            int sumPlayer = 0;
            int sens = isPlayerWhite ? -1 : 1;
            int[] vals = { 0, -sens, sens }; //trick to multiply easily the weight by 1 or -1

            //uses double loop to fetch the values in the field & its corresponding static weighted value
            for (int x = 0; x < field.GetLength(0); x++)
            {
                for (int y = 0; y < field.GetLength(1); y++)
                {
                    sumPlayer += weightedBoard[y, x] * vals[field[x, y] + 1];
                }
            }
            return sumPlayer;
        }


        /// <summary>
        /// Heuristic that evaluates the field according to its corners
        /// </summary>
        /// <param name="field">Field to evaluate</param>
        /// <returns>a normalized value between [-1,1], -1 is bad, 1 is good</returns>
        private double HeuristicCorners(int[,] field)
        {
            //prepare evaluation
            int rows = field.GetLength(0)-1;
            int cols = field.GetLength(1)-1;
            int[] vals = {0, 0, 0};

            //evaluate the corners
            vals[field[0, 0] + 1]++;
            vals[field[rows, 0] + 1]++;
            vals[field[0, cols] + 1]++;
            vals[field[rows, cols] + 1]++;

            //fetch values
            int maxPlayer = isPlayerWhite ? vals[(int)TileState.WHITE+1] : vals[(int)TileState.BLACK+1];
            int minPlayer = !isPlayerWhite ? vals[(int)TileState.WHITE+1] : vals[(int)TileState.BLACK+1];

            //return the ratio
            if ((maxPlayer + minPlayer) == 0) return 0;
            return (double)(maxPlayer - minPlayer) / (double)(maxPlayer + minPlayer);
        }

        /// <summary>
        /// Heuristic that evaluates the field according to its borders
        /// </summary>
        /// <param name="field">Field to evaluate</param>
        /// <returns>a normalized value between [-1,1], -1 is bad, 1 is good</returns>
        private double HeuristicBorders(int[,] field)
        {
            return (EvaluateHorizontal(field) + EvaluateVertical(field)) / 2.0;
        }

        /// <summary>
        /// Sub-heuristic used by border - evaluates the top and bottom horizontal borders by counting consecutive pawns
        /// </summary>
        /// <param name="field">Field to evaluate</param>
        /// <returns>a normalized value between [-1,1], -1 is bad, 1 is good</returns>
        private double EvaluateHorizontal(int[,] field)
        {
            int sum = 0, consecutives;
            int playerColor = (int)(isPlayerWhite ? TileState.WHITE : TileState.BLACK);
            int opponentColor = (int)(isPlayerWhite ? TileState.BLACK : TileState.WHITE);
            int[] vertiDimensions = { 0, field.GetLength(1)-1 }; //y is static

            foreach (int y in vertiDimensions)
            {
                consecutives = 0;
                int last = field[0, y];
                for (int i = 1; i < field.GetLength(0); i++) //x changes
                {
                    int current = field[i, y];
                    if (current == last)
                        consecutives++;
                    else
                    {
                        int pawnSerie = consecutives > 0 ? consecutives + 1 : 0;
                        int sign = (last == playerColor ? 1 : (last == opponentColor ? -1 : 0));
                        sum += pawnSerie * sign;
                        consecutives = 0;
                        last = current;
                    }
                }
                //last check - necessary because we calculate "only" when the pawn serie changes during the loop
                if (consecutives != 0)
                {
                    int pawnSerie = consecutives > 0 ? consecutives + 1 : 0;
                    int sign = (last == playerColor ? 1 : (last == opponentColor ? -1 : 0));
                    sum += pawnSerie * sign;
                }
            }

            //normalized, as max is when the 2 borders are full - 2*length
            return (double)sum / (double)(2*field.GetLength(0));
        }


        /// <summary>
        /// Sub-heuristic used by border - evaluates the left and right vertical borders by counting consecutive pawns
        /// </summary>
        /// <param name="field">Field to evaluate</param>
        /// <returns>a normalized value between [-1,1], -1 is bad, 1 is good</returns>
        private double EvaluateVertical(int[,] field)
        {
            int sum = 0, consecutives;
            int playerColor = (int)(isPlayerWhite ? TileState.WHITE : TileState.BLACK);
            int opponentColor = (int)(isPlayerWhite ? TileState.BLACK : TileState.WHITE);
            int[] vertiDimensions = { 0, field.GetLength(0)-1 }; //x is static

            foreach (int x in vertiDimensions)
            {
                consecutives = 0;
                int last = field[x, 0];
                for (int j = 1; j < field.GetLength(1); j++) //y changes
                {
                    int current = field[x, j];
                    if (current == last)
                        consecutives++;
                    else
                    {
                        int pawnSerie = consecutives > 0 ? consecutives + 1 : 0;
                        int sign = (last == playerColor ? 1 : (last == opponentColor ? -1 : 0));
                        sum += pawnSerie * sign;
                        consecutives = 0;
                        last = current;
                    }
                }
                if (consecutives != 0)
                {
                    int pawnSerie = consecutives > 0 ? consecutives + 1 : 0;
                    int sign = (last == playerColor ? 1 : (last == opponentColor ? -1 : 0));
                    sum += pawnSerie * sign;
                }
            }

            //normalize
            return (double)sum / (double)(2 * field.GetLength(1));
        }


        /// <summary>
        /// Heuristic that evaluates the field according to its mobility value - the number of moves the player & opponents can do
        /// </summary>
        /// <param name="field">Field to evaluate</param>
        /// <returns>a normalized value between [-1,1], -1 is bad, 1 is good</returns>
        private double HeuristicMobility(int[,] field)
        {
            //get both move count
            int player = GetPossibleMove(isPlayerWhite, field).Count;
            int opponent = GetPossibleMove(!isPlayerWhite, field).Count;
            //player=0 -> pass turn, very bad in case of mobility !
            if (player == 0)
                return -1;

            if ((player + opponent) == 0) 
                return 0;
            return (double)(player - opponent) / (double)(player + opponent);
        }

        /// <summary>
        /// Heuristic that evaluates the field according to its parity value - difference of pawns
        /// </summary>
        /// <param name="field">Field to evaluate</param>
        /// <returns>a normalized value between [-1,1], -1 is bad, 1 is good</returns>
        private double HeuristicParity(int[,] field)
        {
            int player = isPlayerWhite ? (int)TileState.WHITE : (int)TileState.BLACK;
            int opponent = isPlayerWhite ? (int)TileState.BLACK : (int)TileState.WHITE;
            int maxPlayer = 0, minPlayer = 0;

            foreach(var elem in field)
            {
                if (elem == player)
                    maxPlayer++;
                else if (elem == opponent)
                    minPlayer++;
            }
            if ((maxPlayer + minPlayer) == 0) return 0;
            return (double)(maxPlayer-minPlayer) / (double)(maxPlayer+minPlayer);
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
            int player = (int)(isPlayerWhite ? TileState.WHITE : TileState.BLACK);
            int opponent = (int)(isPlayerWhite ? TileState.BLACK : TileState.WHITE);
            //do a simple, basic count of pions
            foreach (var elem in field)
            {
                if (elem == player)
                    count++;
                else if (elem == opponent)
                    count--;
            }
            //draw
            if (count == 0)
                return 0;
            //win/loose
            return count + (count > 0 ? int.MaxValue / 2 : int.MinValue / 2);
        }

        #endregion

        #region IPlayable
        /* GetNextMove is in-house, the rest is heavily taken from the given AI exemple (othello rules for exemples) : Othello_OHU */
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
            //update isPlayerWhite (Should be in a "init game", but it is not :-()
            isPlayerWhite = whiteTurn;

            //if player must pass his turn : pass turn
            if(GetPossibleMove(whiteTurn, gameBoard).Count == 0)
                return Tuple.Create(-1, -1);

            //ask the MinMax IA to play
            (int, (int,int)?) results = MinMax(gameBoard, level, 1, null, whiteTurn);
            (int, int)? move = results.Item2;
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
            return PlayMove((column, line), isWhite, gameBoard);
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
            return IsPlayable((column, line), isWhite, gameBoard); //field default to gameboard
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