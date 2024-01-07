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
    private Dictionary<string, float> evaluationCoefficients;
    private Dictionary<Piece.PieceType, int> pieceValues;

    private uint whiteKingId;
    private uint blackKingId;
    private Piece.PieceColor thisPlayer;
    private Piece.PieceColor opponentPlayer;
    private Move bestMove;
    private Piece.PieceColor lastPlayedPlayer;

    private void Awake()
    {
        //gameSetter = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameSetter>();
        //moveGenerator = GameObject.FindGameObjectWithTag("MoveGenerator").GetComponent<MoveGenerator>();
        actionValues = new Dictionary<string, float>() { 
            { "checkmate", float.PositiveInfinity },{ "check", 2f }, { "draw", 0.0f }, { "pinned", 0.2f }, { "attackCloseToKing", 0.1f }, { "maxAttackAmount", 16f }
        };
        evaluationCoefficients = new Dictionary<string, float>() {
            {"attackPotential", 4f }, {"pieceMobility", .4f}, {"connectedPawns", 3f}, {"kingSafety", 2f}, {"openLine", 2f}, {"pawnChain", .5f}, {"isolatedPawn", -0.2f}
        };
        pieceValues = new Dictionary<Piece.PieceType, int>()
        {
            { Piece.PieceType.Pawn, 1 }, { Piece.PieceType.Knight, 3 },
            { Piece.PieceType.Bishop, 3 }, { Piece.PieceType.Rook, 5 },
            { Piece.PieceType.Queen, 9 }
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
        //Debug.Log("max eval: " + maxEval);

        gameSetter.moveReady = true;
        gameSetter.AIMove(bestMove);
        return bestMove;
    }

    private void PrintMoveLog(Move move)
    {
        string moveLog = move.MovingPieceName + " (" + (char)('a'+move.From.X) + (1 + move.From.Y) + "), (" + (char)('a' + move.To.X) + (1 + move.To.Y) + ")";
        //Debug.Log("move " + moveLog);
    }

    private float Minimax(int depth, float alpha, float beta, int maximizingPlayer)
    {
        if (depth == 0 || gameSetter.checkMated || gameSetter.drawn)
        {
            float eval = EvaluateBoard();
            //Debug.Log("eval: " + eval);
            return eval;
        }

        if(maximizingPlayer == 1)
        {
            float maxEval = float.NegativeInfinity;
            Dictionary<uint, List<Move>> moves = moveGenerator.GetMoves(thisPlayer);
            ////Debug.Log("Generated moves pos, depth=" + depth);
            ////Debug.Log("Generated move count: " + moves.Count);
            foreach (uint id in moves.Keys)
            {
                foreach(Move move in moves[id])
                {
                    MakeMoveAI(move);
                    //PrintMoveLog(move);
                    float eval = Minimax(depth - 1, alpha, beta, -1);
                    UnMakeMoveAI(move);
                    //Debug.Log(gameSetter.GetBoardLog());
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
            ////Debug.Log("max eval minimax with depth=" + depth);
            ////Debug.Log("max eval: " + maxEval);
            return maxEval;
        }
        else
        {
            float minEval = float.PositiveInfinity;
            Dictionary<uint, List<Move>> moves = moveGenerator.GetMoves(opponentPlayer);
            ////Debug.Log("Generated moves neg, depth=" + depth);
            ////Debug.Log("Generated move count: " + moves.Count);
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
            ////Debug.Log("min eval minimax with depth=" + depth);
            ////Debug.Log("min eval: " + minEval);
            return minEval;
        }
    }

    private float EvaluateBoard()
    {
        lastPlayedPlayer = gameSetter.GetCurrentPlayer() == Piece.PieceColor.White ? Piece.PieceColor.Black : Piece.PieceColor.White;
        bool checkMate = gameSetter.checkMated;
        bool drawn = gameSetter.drawn;

        if(lastPlayedPlayer == thisPlayer && checkMate)
        {
            return 1.0f * actionValues["checkmate"];
        }
        else if (checkMate)
        {
            return -1.0f * actionValues["checkmate"];
        }

        if (lastPlayedPlayer == thisPlayer && drawn)
        {
            return 1.0f * actionValues["draw"];
        }
        else if (drawn)
        {
            return -1.0f * actionValues["draw"];
        }

        return EvaluateBoardForOneSide(thisPlayer) - EvaluateBoardForOneSide(opponentPlayer);
    }

    private float EvaluateBoardForOneSide(Piece.PieceColor playerToEvaluate)
    {
        bool check = gameSetter.check;
        return EvaluateMaterialBalanceOfAPlayer(playerToEvaluate) + EvaluateAttackPotentialOfAPlayer(check, playerToEvaluate) 
            + EvaluatePieceMobilityOfAPlayer(check, playerToEvaluate) + EvaluatePieceConnectedness(playerToEvaluate) 
            + EvaluateKingSafety(check, playerToEvaluate) + EvaluatePawnStructure(playerToEvaluate);
    }

    private float EvaluatePawnStructure(Piece.PieceColor playerToEvaluate)
    {
        uint lowestId = playerToEvaluate == Piece.PieceColor.White ? (uint)0 : (uint)16;
        uint highestId = lowestId + 16;
        int chainLength = 0;
        int isolatedPawnCount = 0;

        for(uint i = lowestId;  i < highestId; i++)
        {
            Point pos = idToPos[i];
            if(pos != new Point(-1, -1))
            {
                if (board[pos.X, pos.Y].GetComponent<Piece>().type == Piece.PieceType.Pawn)
                {
                    bool isIsolated = true;
                    List<MoveUpdater> muList = moveBoard[pos.X, pos.Y];
                    foreach(MoveUpdater mu in muList)
                    {
                        Point posOfOtherPiece = idToPos[mu.id];
                        if (mu.protectable && board[posOfOtherPiece.X, posOfOtherPiece.Y].GetComponent<Piece>().type == Piece.PieceType.Pawn)
                        {
                            isIsolated = false;
                            chainLength++;
                        }
                    }
                    if(isIsolated)
                        isolatedPawnCount++;
                }
            }
        }

        return chainLength * evaluationCoefficients["pawnChain"] + isolatedPawnCount * evaluationCoefficients["isolatedPawn"];
    }

    private float EvaluateAttackPotentialOfAPlayer(bool check, Piece.PieceColor playerToEvaluate)
    {
        uint lowestIdOpponent = playerToEvaluate == Piece.PieceColor.White ? (uint)16 : (uint)0;
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
                        attackedPieceValues += pieceValues[board[pos.X, pos.Y].GetComponent<Piece>().type];
                        attackedPieces.Add(board[pos.X, pos.Y].GetComponent<Piece>().id);
                    }
                }
            }
        }

        float attackPotential = (1.0f * CheckNumberOfPassedPawns(playerToEvaluate) + attackedPieceValues * attackedPieces.Count) / (16 * TotalPieceValueCount());

        if (check && playerToEvaluate == lastPlayedPlayer) attackPotential = actionValues["check"] * attackPotential;
        return evaluationCoefficients["attackPotential"] * attackPotential;
    }

    private int CheckNumberOfPassedPawns(Piece.PieceColor playerToEvaluate)
    {
        Piece.PieceColor opponentColor = playerToEvaluate == Piece.PieceColor.White ? Piece.PieceColor.Black : Piece.PieceColor.White;  
        int passedPawnCount = 0;
        int lowerBoardBound = 0, upperBoardBound = 8;
        int increment = playerToEvaluate == Piece.PieceColor.White ? 1 : -1;

        uint lowestId = playerToEvaluate == Piece.PieceColor.White ? (uint)0 : (uint)16;
        uint highestId = lowestId + 16;
        for(uint i = lowestId; i < highestId; i++) {
            Point pos = idToPos[i];
            if(pos != new Point(-1, -1) && board[pos.X, pos.Y].GetComponent<Piece>().type == Piece.PieceType.Pawn)
            {
                int checkPosX = pos.X, checkPosY = pos.Y + increment;
                bool passedPawn = true;
                for(int m = -1; m <= 1; m++)
                {
                    if (pos.X + m < lowerBoardBound || pos.X + m >= upperBoardBound)
                        continue;
                    checkPosX = pos.X + m;
                    while (checkPosY >= lowerBoardBound && checkPosY < upperBoardBound)
                    {
                        if (board[checkPosX, checkPosY] != null && board[checkPosX, checkPosY].GetComponent<Piece>().player == opponentColor &&  board[checkPosX, checkPosY].GetComponent<Piece>().type == Piece.PieceType.Pawn)
                        {
                            passedPawn = false;
                            break;
                        }
                        checkPosY += increment;
                    }
                }

                if(passedPawn)
                    passedPawnCount++;
            }
        }

        return passedPawnCount;
    }

    private float EvaluatePieceMobilityOfAPlayer(bool check, Piece.PieceColor playerToEvaluate)
    {
        return evaluationCoefficients["pieceMobility"] * moveGenerator.GetMoves(playerToEvaluate).Count;
    }

    private int TotalPieceValueCount()
    {
        return 39;
    }

    private float EvaluatePieceConnectedness(Piece.PieceColor playerToEvaluate)
    {
        uint lowestId = playerToEvaluate == Piece.PieceColor.White ? (uint)0 : (uint)16;
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

    private float EvaluateKingSafety(bool check, Piece.PieceColor playerToEvaluate)
    {
        if (check && lastPlayedPlayer == playerToEvaluate)
        {
            return 0.0f;
        }
        Dictionary<uint, uint> pins = moveGenerator.GetPins();
        uint lowestId = playerToEvaluate == Piece.PieceColor.White ? (uint)0 : (uint)16;
        uint highestId = lowestId + 16;
        int pinCount = 0;

        foreach (uint pinningPieceId in pins.Keys)
        {
            if (pins[pinningPieceId] >= lowestId && pins[pinningPieceId] < highestId)
            {
                pinCount++;
            }
        }

        uint lowestIdOpponent = playerToEvaluate == Piece.PieceColor.White ? (uint)16 : (uint)0;
        uint highestIdOpponent = lowestId + 16;
        int closeAttacksCount = 0;
        uint kingId = playerToEvaluate == Piece.PieceColor.White ? whiteKingId : blackKingId;
        Point kingPos = idToPos[kingId];
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Point currentPos = new Point(kingPos.X + x, kingPos.Y + y);
                if (currentPos == kingPos)
                    continue;
                if (gameSetter.PositionOnBoard(currentPos.X, currentPos.Y))
                {
                    foreach (MoveUpdater mu in moveBoard[currentPos.X, currentPos.Y])
                    {
                        if (mu.id >= lowestIdOpponent && mu.id < highestIdOpponent && mu.attackable == true)
                        {
                            closeAttacksCount++;
                        }
                    }
                }
            }
        }

        float valueNormalized = 1.0f * (pins.Count * actionValues["pinned"]) / (actionValues["maxAttackAmount"]);
        valueNormalized += 1.0f * (closeAttacksCount * actionValues["attackCloseToKing"]) / (actionValues["maxAttackAmount"]);
        return (1.0f - valueNormalized) * evaluationCoefficients["kingSafety"];
    }

    private float EvaluateMaterialBalanceOfAPlayer(Piece.PieceColor playerToEvaluate)
    {
        uint lowestId = playerToEvaluate == Piece.PieceColor.White ? (uint)0 : (uint)16;
        uint highestId = lowestId + 16;
        int totalValue = 0;

        for (uint i = lowestId; i < highestId; i++)
        {
            if (i == whiteKingId || i == blackKingId)
                continue;
            Point pos = idToPos[i];
            if (pos != new Point(-1, -1))
            {
                totalValue += pieceValues[board[pos.X, pos.Y].GetComponent<Piece>().type];
            }
        }
        return 1.0f * totalValue;
    }
}
