using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

public class AIPlayer : MonoBehaviour
{
    [SerializeField]
    private GameSetter gameSetter;
    [SerializeField]
    private MoveGenerator moveGenerator;
    private Dictionary<uint, Point> idToPos;
    private GameObject[,] board;
    private List<MoveUpdater>[,] moveBoard;
    private Dictionary<string, float> actionValues;
    private Dictionary<string, int> pieceValues;

    private uint whiteKingId;
    private uint blackKingId;
    private Piece.PieceColor thisPlayer;
    private Piece.PieceColor opponentPlayer;
    private Move bestMove;
    private Piece.PieceColor playersTurn;

    private void Awake()
    {
        //gameSetter = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameSetter>();
        //moveGenerator = GameObject.FindGameObjectWithTag("MoveGenerator").GetComponent<MoveGenerator>();
        actionValues = new Dictionary<string, float>() { 
            { "checkmate", float.PositiveInfinity },{ "check", 2f }, { "draw", 0.0f }, { "pinned", 0.2f }, { "attackCloseToKing", 0.1f }, { "maxAttackAmount", 16f }
        };
        pieceValues = new Dictionary<string, int>()
        {
            { "white_pawn", 1 }, { "black_pawn", 1 }, { "white_knight", 3 }, { "black_knight", 3 },
            { "white_bishop", 3 }, { "black_bishop", 3 }, { "white_rook", 5 }, { "black_rook", 5 },
            { "white_queen", 9 }, { "black_queen", 9 }
        };
        whiteKingId = gameSetter.WHITE_KING_ID;
        blackKingId = gameSetter.BLACK_KING_ID;
    }

    private void MakeMoveAI(Move move)
    {
        gameSetter.MakeMove(move);
        board = gameSetter.GetBoard();
        idToPos = gameSetter.idToPos;
        moveBoard = moveGenerator.GetMoveBoard();
    }

    private void UnMakeMoveAI(Move move)
    {
        gameSetter.UnmakeMove(move);
        board = gameSetter.GetBoard();
        idToPos = gameSetter.idToPos;
        moveBoard = moveGenerator.GetMoveBoard();
    }

    public Move GetAIMove(Piece.PieceColor player, int depth)
    {
        board = gameSetter.GetBoard();
        idToPos = gameSetter.idToPos;
        moveBoard = moveGenerator.GetMoveBoard();
        thisPlayer = player;
        opponentPlayer = thisPlayer == Piece.PieceColor.White ? Piece.PieceColor.Black : Piece.PieceColor.White;

        float maxEval = Minimax(depth, float.NegativeInfinity, float.PositiveInfinity, 1);
        Debug.Log("max eval: " + maxEval);

        gameSetter.moveReady = true;
        gameSetter.AIMove(bestMove);
        return bestMove;
    }

    private void PrintMoveLog(Move move)
    {
        string moveLog = move.MovingPieceName + " (" + (char)('a'+move.From.X) + (1 + move.From.Y) + "), (" + (char)('a' + move.To.X) + (1 + move.To.Y) + ")";
        Debug.Log("move " + moveLog);
    }

    private float Minimax(int depth, float alpha, float beta, int maximizingPlayer)
    {
        if (depth == 0 || gameSetter.checkMated || gameSetter.drawn)
        {
            float eval = EvaluateBoard();
            Debug.Log("eval: " + eval);
            return eval;
        }

        if(maximizingPlayer == 1)
        {
            float maxEval = float.NegativeInfinity;
            Dictionary<uint, List<Move>> moves = moveGenerator.GetMoves(thisPlayer);
            //Debug.Log("Generated moves pos, depth=" + depth);
            //Debug.Log("Generated move count: " + moves.Count);
            foreach (uint id in moves.Keys)
            {
                foreach(Move move in moves[id])
                {
                    MakeMoveAI(move);
                    //PrintMoveLog(move);
                    float eval = Minimax(depth - 1, alpha, beta, -1);
                    UnMakeMoveAI(move);
                    Debug.Log(gameSetter.GetBoardLog());
                    if (eval > maxEval)
                    {
                        bestMove = move;
                    }
                    maxEval = Mathf.Max(maxEval, eval);
                    alpha = Mathf.Max(alpha, eval);
                    if (beta <= alpha)
                        break;
                }
                if (beta <= alpha)
                    break;
            }
            //Debug.Log("max eval minimax with depth=" + depth);
            //Debug.Log("max eval: " + maxEval);
            return maxEval;
        }
        else
        {
            float minEval = float.PositiveInfinity;
            Dictionary<uint, List<Move>> moves = moveGenerator.GetMoves(opponentPlayer);
            //Debug.Log("Generated moves neg, depth=" + depth);
            //Debug.Log("Generated move count: " + moves.Count);
            foreach (uint id in moves.Keys)
            {
                foreach (Move move in moves[id])
                {
                    MakeMoveAI(move);
                    PrintMoveLog(move);
                    float eval = Minimax(depth - 1, alpha, beta, 1);
                    UnMakeMoveAI(move);
                    minEval = Mathf.Min(minEval, eval);
                    beta = Mathf.Min(beta, eval);
                    if (beta <= alpha)
                        break;
                }
                if (beta <= alpha)
                    break;
            }
            //Debug.Log("min eval minimax with depth=" + depth);
            //Debug.Log("min eval: " + minEval);
            return minEval;
        }
    }

    private float EvaluateBoard()
    {
        playersTurn = gameSetter.GetCurrentPlayer() == Piece.PieceColor.White ? Piece.PieceColor.Black : Piece.PieceColor.White;
        bool checkMate = gameSetter.checkMated;
        bool drawn = gameSetter.drawn;

        if(playersTurn == thisPlayer && checkMate)
        {
            return -1.0f * actionValues["checkmate"];
        }
        else if (checkMate)
        {
            return 1.0f * actionValues["checkmate"];
        }

        if (playersTurn == thisPlayer && drawn)
        {
            return -1.0f * actionValues["draw"];
        }
        else if (drawn)
        {
            return 1.0f * actionValues["draw"];
        }

        return EvaluateBoardOfOneSide(thisPlayer) - EvaluateBoardOfOneSide(opponentPlayer);
    }

    private float EvaluateBoardOfOneSide(Piece.PieceColor currPlayer)
    {
        bool check = gameSetter.check;
        return PiecePointTotal(currPlayer) + AttackingOpportunities(check, currPlayer) + PieceConnectedness(currPlayer) + KingSafety(check, currPlayer);
    }

    private float AttackingOpportunities(bool check, Piece.PieceColor currPlayer)
    {
        uint lowestIdOpponent = currPlayer == Piece.PieceColor.White ? (uint)16 : (uint)0;
        uint highestIdOpponent = lowestIdOpponent + 16;
        int attackedPieceValues = 0;
        HashSet<uint> attackedPieces = new();

        for (uint i = lowestIdOpponent; i < highestIdOpponent; i++)
        {
            if (i == whiteKingId || i == blackKingId)
                continue;
            Point pos = idToPos[i];
            if (pos != new Point(-1, -1))
            {
                foreach (MoveUpdater mu in moveBoard[pos.X, pos.Y])
                {
                    if (mu.attackable)
                    {
                        attackedPieceValues += pieceValues[board[pos.X, pos.Y].GetComponent<Piece>().name];
                        attackedPieces.Add(board[pos.X, pos.Y].GetComponent<Piece>().id);
                    }
                }
            }
        }

        float attackPotential = (attackedPieceValues * attackedPieces.Count) / (16 * TotalPieceValueCount());

        if (check && currPlayer == playersTurn) return attackPotential;
        return actionValues["check"] * attackPotential;
    }

    private int TotalPieceValueCount()
    {
        return 39;
    }

    private float PieceConnectedness(Piece.PieceColor currPlayer)
    {
        uint lowestId = currPlayer == Piece.PieceColor.White ? (uint)0 : (uint)16;
        uint highestId = lowestId + 16;

        HashSet<uint> connectedIds = new();
        int count = 0;
        int connectionCount = 0;

        for (uint i = lowestId; i < highestId; i++)
        {
            if (i == whiteKingId || i == blackKingId)
                continue;
            Point pos = idToPos[i];
            if (pos != new Point(-1, -1))
            {
                count++;
                foreach (MoveUpdater mu in moveBoard[pos.X, pos.Y])
                {
                    if (mu.protectable)
                    {
                        connectedIds.Add(mu.id);
                        connectionCount++;
                    }
                }
            }
        }

        return (1.0f * connectedIds.Count * connectionCount) / (count * count);
    }

    private float KingSafety(bool check, Piece.PieceColor currPlayer)
    {
        if (check && playersTurn == currPlayer)
        {
            return 0.0f;
        }
        Dictionary<uint, uint> pins = moveGenerator.GetPins();
        uint lowestId = currPlayer == Piece.PieceColor.White ? (uint)0 : (uint)16;
        uint highestId = lowestId + 16;
        int pinCount = 0;

        foreach (uint pinningPieceId in pins.Keys)
        {
            if (pins[pinningPieceId] >= lowestId && pins[pinningPieceId] < highestId)
            {
                pinCount++;
            }
        }
        
        float valueNormalized = 1.0f * (pins.Count * actionValues["pinned"]) / (actionValues["maxAttackAmount"] * actionValues["pinned"]);
        return (1.0f - valueNormalized);
        //int closeAttacksCount = 0;
        //uint kingId = currPlayer == "white" ? whiteKingId : blackKingId;
        //Point kingPos = idToPos[kingId];
        //for (int x = -1; x <= 1; x++)
        //{
        //    for (int y = -1; y <= 1; y++)
        //    {
        //        Point currentPos = new Point(kingPos.X + x, kingPos.Y + y);
        //        if (currentPos == kingPos)
        //            continue;
        //        if (gameSetter.PositionOnBoard(currentPos.X, currentPos.Y))
        //        {
        //            foreach (MoveUpdater mu in moveBoard[currentPos.X, currentPos.Y])
        //            {
        //                if (mu.id >= lowestIdOpponent && mu.id < highestIdOpponent && mu.attackable == true)
        //                {
        //                    closeAttacksCount++;
        //                }
        //            }
        //        }
        //    }
        //}
    }

    private float PiecePointTotal(Piece.PieceColor currPlayer)
    {
        uint lowestId = currPlayer == Piece.PieceColor.White ? (uint)0 : (uint)16;
        uint highestId = lowestId + 16;
        int totalValue = 0;

        for (uint i = lowestId; i < highestId; i++)
        {
            if (i == whiteKingId || i == blackKingId)
                continue;
            Point pos = idToPos[i];
            if (pos != new Point(-1, -1))
            {
                totalValue += pieceValues[board[pos.X, pos.Y].GetComponent<Piece>().name];
            }
        }
        return 1.0f * totalValue;
    }
}
