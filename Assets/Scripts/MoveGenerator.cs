using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveGenerator : MonoBehaviour
{
    [SerializeField]
    private GameSetter game;
    private Dictionary<uint, Point> idToPos;
    private GameObject[,] board;
    private List<MoveUpdater>[,] moveBoard;
    private Queue<uint> whiteUpdatesQueue;
    private List<uint> enPassants;
    private Queue<uint> blackUpdatesQueue;
    private Piece.PieceColor currentPlayer;
    private uint whiteKingId;
    private uint blackKingId;

    private Dictionary<uint, List<Point>> moveupdatePositionsOfPieces;
    private Dictionary<uint, uint> pins;
    private Dictionary<uint, List<Move>> allWhiteMoves;
    private Dictionary<uint, List<Move>> allBlackMoves;
    private Dictionary<uint, List<Move>> legalWhiteMoves;
    private Dictionary<uint, List<Move>> legalBlackMoves;
    private Dictionary<string, string> logNames = new Dictionary<string, string>();

    private bool check = false;
    private bool doublecheck = false;

    public Dictionary<uint, uint> GetPins()
    {
        return pins;
    }

    public List<MoveUpdater>[,] GetMoveBoard()
    {
        return moveBoard;
    }

    public Dictionary<uint, List<Move>> GetMoves(Piece.PieceColor player)
    {
        return player == Piece.PieceColor.White ? legalWhiteMoves : legalBlackMoves;
    }

    public bool GetCheck()
    {
        return check;
    }

    public bool GetDoubleCheck()
    {
        return doublecheck;
    }

    private void DeepCopyMoves(ref Dictionary<uint, List<Move>> originalDict, ref Dictionary<uint, List<Move>> newDict)
    {
        newDict = new Dictionary<uint, List<Move>>();
        foreach(var key in originalDict.Keys)
        {
            var move = originalDict[key];
            if (move != null)
            {
                newDict.Add(key, move);
            }
        }
    }
    public void Initialize()
    {
        whiteKingId = game.WHITE_KING_ID;
        blackKingId = game.BLACK_KING_ID;
        legalWhiteMoves = new Dictionary<uint, List<Move>>();
        legalBlackMoves = new Dictionary<uint, List<Move>>();
        allWhiteMoves = new Dictionary<uint, List<Move>>();
        allBlackMoves = new Dictionary<uint, List<Move>>();
        whiteUpdatesQueue = new Queue<uint>();
        blackUpdatesQueue = new Queue<uint>();
        pins = new Dictionary<uint, uint>();
        enPassants = new List<uint>();

        uint _id = 0;
        while(_id < 16)
        {
            whiteUpdatesQueue.Enqueue(_id);
            _id++;
        }
        while (_id < 32)
        {
            blackUpdatesQueue.Enqueue(_id);
            _id++;
        }

        moveBoard = new List<MoveUpdater>[8, 8];
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                moveBoard[row, col] = new List<MoveUpdater>();
            }
        }

        moveupdatePositionsOfPieces = new Dictionary<uint, List<Point>>();
        for (uint id = 0; id < 32; id++)
        {
            moveupdatePositionsOfPieces[id] = new List<Point>();
        }

        logNames = new Dictionary<string, string>() {
            {"white_pawn", "" },  {"black_pawn" , "" }, {"white_rook", "R" },  {"black_rook" , "R" }, {"white_bishop", "B" },  {"black_bishop" , "B" },
            {"white_queen", "Q" },  {"black_queen" , "Q" }, {"white_knight", "N" },  {"black_knight" , "N" }, {"white_king", "K" },  {"black_king" , "K" }};
    }

    public void GenerateMoves(Piece.PieceColor player)
    {
        board = game.GetBoard();
        idToPos = game.GetPiecePositions();
        currentPlayer = player;

        if (player == Piece.PieceColor.White) legalWhiteMoves = new Dictionary<uint, List<Move>>();
        else legalBlackMoves = new Dictionary<uint, List<Move>>();

        //keep track of pieces in other way
        //if (check) return;
        //uint start_id = (uint)(currentPlayer.Equals("white") ? 0 : 16);
        //uint end_id = start_id + 16;

        Queue<uint> updatesQueue = new Queue<uint>(currentPlayer == Piece.PieceColor.White ? whiteUpdatesQueue : blackUpdatesQueue);
        Queue<uint> updatesQueue2 = new Queue<uint>();

        //////Debug.Log("currentPlayer: " + currentPlayer);
        while (updatesQueue.Count > 0)
        {
            uint id = updatesQueue.Dequeue();
            updatesQueue2.Enqueue(id);
            foreach (Point pt in moveupdatePositionsOfPieces[id])
            {
                RemoveFromMoveBoard(pt, id);
            }
            moveupdatePositionsOfPieces[id] = new List<Point>();
            if (pins.ContainsKey(id))
            {   
                pins.Remove(id);
            }
        }

        while (updatesQueue2.Count > 0)
        {
            uint id = updatesQueue2.Dequeue();
            Point p = new Point(-1, -1);

            if (currentPlayer == Piece.PieceColor.White && allWhiteMoves.ContainsKey(id)) allWhiteMoves.Remove(id);
            else if(allBlackMoves.ContainsKey(id)) allBlackMoves.Remove(id);
            if (idToPos[id] != p)
            {
                GenerateMovesForAChessPiece(idToPos[id], id);
            }        
        }

        if (currentPlayer == Piece.PieceColor.White) whiteUpdatesQueue.Clear(); else blackUpdatesQueue.Clear();

        if (player == Piece.PieceColor.White)DeepCopyMoves(ref allWhiteMoves, ref legalWhiteMoves);
        else DeepCopyMoves(ref allBlackMoves, ref legalBlackMoves);

        EliminateKingMoves();
        EliminatePinnedMoves();

        if (doublecheck)
        {
            EliminateOtherMovesThanKingMove();
        }
        else if (check)
        {
            EliminateIllegalMoves();
        }

        RemoveEmptyMoves();
    }

    private void RemoveEmptyMoves()
    {
        if(currentPlayer == Piece.PieceColor.White)
        {
            List<uint> keysToRemove = new List<uint>();
            foreach(KeyValuePair<uint, List<Move>> pair in legalWhiteMoves)
            {
                if (pair.Value.Count == 0)
                    keysToRemove.Add(pair.Key);
            }
            foreach(uint key in keysToRemove)
            {
                legalWhiteMoves.Remove(key);
            }
        }
        else
        {
            List<uint> keysToRemove = new List<uint>();
            foreach (KeyValuePair<uint, List<Move>> pair in legalBlackMoves)
            {
                if (pair.Value.Count == 0)
                    keysToRemove.Add(pair.Key);
            }
            foreach (uint key in keysToRemove)
            {
                legalBlackMoves.Remove(key);
            }
        }
    }

    private void EliminateIllegalMoves()
    {
        Dictionary<uint, List<Move>> movesAll = currentPlayer == Piece.PieceColor.White ? legalWhiteMoves : legalBlackMoves;
        uint kingId = currentPlayer == Piece.PieceColor.White ? whiteKingId : blackKingId;
        uint attackingPieceId = 0;

        foreach (MoveUpdater mu in moveBoard[idToPos[kingId].X, idToPos[kingId].Y]) {
            if (mu.attackable == true)
            {
                attackingPieceId = mu.id; 
                break;
            }
        }

        if (idToPos[attackingPieceId] == new Point(-1, -1))
            return;

        Dictionary<uint, List<Move>> movesToAdd = new Dictionary<uint, List<Move>>();
        List<string> attackPieceNames = new List<string>() {"white_rook", "white_bishop", "white_queen", "black_rook", "black_bishop", "black_queen" };
        //add stuff about rays
        if(attackPieceNames.Contains(board[idToPos[attackingPieceId].X, idToPos[attackingPieceId].Y].GetComponent<Piece>().name)){
            List<Point> rayOfPoints = new List<Point>();
            Point start = idToPos[attackingPieceId];
            Point end = idToPos[kingId];

            if (start.X == end.X)
            {
                int yInc = (end.Y - start.Y) / Math.Abs(end.Y - start.Y);
                int yAxis = start.Y;
                while(yAxis != end.Y)
                {
                    rayOfPoints.Add(new Point(start.X, yAxis));
                    yAxis += yInc;
                }
            }
            else if(start.Y == end.Y)
            {
                int xInc = (end.X - start.X) / Math.Abs(end.X - start.X);
                int xAxis = start.X;
                while (xAxis != end.X)
                {
                    rayOfPoints.Add(new Point(xAxis, start.Y));
                    xAxis += xInc;
                }
            }
            else
            {
                int xInc = (end.X - start.X) / Math.Abs(end.X - start.X);
                int xAxis = start.X;
                int yInc = (end.Y - start.Y) / Math.Abs(end.Y - start.Y);
                int yAxis = start.Y;
                while (xAxis != end.X)
                {
                    rayOfPoints.Add(new Point(xAxis, yAxis));
                    xAxis += xInc;
                    yAxis += yInc;
                }
            }

            foreach(Point point in rayOfPoints)
            {
                List<MoveUpdater> moveUpdates = moveBoard[point.X, point.Y];
                foreach (MoveUpdater moveUpdater in moveUpdates)
                {
                    if (movesAll.ContainsKey(moveUpdater.id))
                    {
                        if(!movesToAdd.ContainsKey(moveUpdater.id))
                            movesToAdd.Add(moveUpdater.id, new List<Move>());
                        foreach (Move m in movesAll[moveUpdater.id])
                        {
                            if(m.To == point)
                                movesToAdd[moveUpdater.id].Add(m);
                        }
                    }
                }
            }
        }
        else
        {
            Point attackingPiecePos = idToPos[attackingPieceId];
            List<MoveUpdater> moveUpdates = moveBoard[attackingPiecePos.X, attackingPiecePos.Y];
            foreach (MoveUpdater moveUpdater in moveUpdates)
            {
                if (movesAll.ContainsKey(moveUpdater.id))
                {
                    if (!movesToAdd.ContainsKey(moveUpdater.id))
                        movesToAdd.Add(moveUpdater.id, new List<Move>());
                    foreach (Move m in movesAll[moveUpdater.id])
                    {
                        if (m.To == attackingPiecePos)
                            movesToAdd[moveUpdater.id].Add(m);
                    }
                }
            }

        }
        EliminateOtherMovesThanKingMove();
        if (movesToAdd.ContainsKey(kingId))
            movesToAdd.Remove(kingId);
        if (currentPlayer == Piece.PieceColor.White)
        {
            foreach (uint p in movesToAdd.Keys)
            {
                legalWhiteMoves.Add(p, movesToAdd[p]);
            }
        }
        else
        {
            foreach (uint p in movesToAdd.Keys)
            {
                legalBlackMoves.Add(p, movesToAdd[p]);
            }
        }
    }

    private void EliminateOtherMovesThanKingMove()
    {
        Dictionary<uint, List<Move>> movesAll = currentPlayer == Piece.PieceColor.White ? legalWhiteMoves : legalBlackMoves;
        uint kingId = currentPlayer == Piece.PieceColor.White ? whiteKingId : blackKingId;
        Point posking = idToPos[kingId];
        Dictionary<uint, List<Move>> newMovesAll = new Dictionary<uint, List<Move>>();
        if (movesAll.ContainsKey(kingId))
        {
            newMovesAll.Add(kingId, movesAll[kingId]);
        }

        if (currentPlayer == Piece.PieceColor.White)
            DeepCopyMoves(ref newMovesAll, ref legalWhiteMoves);
        else
            DeepCopyMoves(ref newMovesAll, ref legalBlackMoves);
    }

    private void EliminatePinnedMoves()
    {
        Dictionary<uint, List<Move>> movesAll = currentPlayer == Piece.PieceColor.White ? legalWhiteMoves : legalBlackMoves;
        foreach (KeyValuePair<uint,uint> pin in pins)
        {
            uint pinnedId = pin.Value;
            if (board[idToPos[pinnedId].X, idToPos[pinnedId].Y].GetComponent<Piece>().player != currentPlayer)
                continue;
            if(!movesAll.ContainsKey(pinnedId))
                continue;
            List<Move> moves = movesAll[pinnedId];
            List<Move> newMoves = new List<Move>();

            foreach (Move move in moves)
            {
                Point pinnerPos = idToPos[pin.Key];
                //////Debug.Log("pinner pos: " + pinnerPos.X + ", " + pinnerPos.Y);
                if ((move.To.X == move.From.X && pinnerPos.X == move.To.X) ||
                    (move.To.Y == move.From.Y && pinnerPos.Y == move.To.Y) ||
                    (move.To == pinnerPos))
                {
                    newMoves.Add(move);
                    //////Debug.Log("Aligned!!");
                }
                else if (move.To.X == move.From.X || move.To.Y == move.From.Y || move.To.X == pinnerPos.X || pinnerPos.Y == move.To.Y) { }
                else
                {
                    double ratio1 = 1.0 * (move.To.X - move.From.X) / (move.To.Y - move.From.Y);
                    double ratio2 = 1.0 * (pinnerPos.X - move.To.X) / (pinnerPos.Y - move.To.Y);

                    if (ratio1 == ratio2)
                    {
                        //////Debug.Log("Aligned In both directions!!");
                        newMoves.Add(move);
                    }
                }
            }
            if (newMoves.Count > 0)
                movesAll[pinnedId] = newMoves;
            else
                movesAll.Remove(pinnedId);
        }
    }

    private void EliminateKingMoves()
    {
        uint kingId = currentPlayer == Piece.PieceColor.White ? whiteKingId : blackKingId;
        Point kingPos = idToPos[kingId];
        Dictionary<uint, List<Move>> movesAll = currentPlayer == Piece.PieceColor.White ? legalWhiteMoves : legalBlackMoves;
        if (!movesAll.ContainsKey(kingId)) return;
        List<Move> moves = movesAll[kingId];
        if (moves.Count == 0) return;
        List<Move> newMoves = new List<Move>();

        foreach(Move move in moves)
        {
            List<MoveUpdater> updater = moveBoard[move.To.X, move.To.Y];
            bool moveAddable = true;
            foreach(MoveUpdater mu in updater)
            {
                if((mu.attackable || mu.protectable) && board[idToPos[mu.id].X, idToPos[mu.id].Y].GetComponent<Piece>().player != currentPlayer)
                {
                    moveAddable = false;
                    break;
                }
            }
            if(moveAddable)
                newMoves.Add(move);
        }
        if (newMoves.Count == 0) movesAll.Remove(kingId);
        else movesAll[kingId] = newMoves;

        //int end = movesAll.ContainsKey(kingId) ? newMoves.Count : 0;
        //////Debug.Log(currentPlayer + " king move count: " + end);
        //////Debug.Log(currentPlayer + " king move pos: " + kingPos.X + ", " + kingPos.Y);
    }

    //private Check IsOpponentKingChecked(Move move)
    //{
    //    currentPlayer = currentPlayer == "white" ? "black" : "white";

    //    opponentPossibleAttackMoves = new Dictionary<Point, List<Move>>();
    //    GameObject oldPos = board[move.From.X, move.From.Y];
    //    GameObject newPos = board[move.To.X, move.To.Y];

    //    idToPos[board[move.From.X, move.From.Y].GetComponent<Piece>().id] = new Point(move.To.X, move.To.Y);
    //    if(newPos != null)
    //        idToPos[board[move.To.X, move.To.Y].GetComponent<Piece>().id] = new Point(-1, -1);

    //    board[move.From.X, move.From.Y] = null;
    //    board[move.To.X, move.To.Y] = oldPos;

    //    bool ischecked = IsKingChecked();

    //    GenerateMovesForOpponent();
    //    bool isCheckMated = false;
    //    bool isDrawn = false;
    //    if(possibleMoves.Count == 0 && ischecked) 
    //    {
    //        isCheckMated = true;
    //    }
    //    else if(possibleMoves.Count == 0) {
    //        isDrawn = true;
    //    }

    //    board[move.From.X, move.From.Y] = oldPos;
    //    board[move.To.X, move.To.Y] = newPos;


    //    idToPos[board[move.From.X, move.From.Y].GetComponent<Piece>().id] = new Point(move.From.X, move.From.Y);
    //    if (newPos != null)
    //        idToPos[board[move.To.X, move.To.Y].GetComponent<Piece>().id] = new Point(move.To.X, move.To.Y);

    //    return new Check(ischecked, isCheckMated, isDrawn);
    //}

    //private void GenerateMovesForOpponent()
    //{
    //    //keep track of pieces in other way
    //    //if (check) return;
    //    uint start_id = (uint)(currentPlayer.Equals("white") ? 0 : 16);
    //    uint end_id = start_id + 16;
    //    
    //    

    //    for (uint i = start_id; i < end_id; i++)
    //    {
    //        Point p = new Point(-1, -1);
    //        if (idToPos[i] != p)
    //        {
    //            GenerateMovesForAChessPiece(idToPos[i], board[idToPos[i].X, idToPos[i].Y].GetComponent<Piece>().name);
    //        }
    //    }
    //    
    //}

    private void GenerateMovesForAChessPiece(Point p, uint id)
    {
        string name = board[idToPos[id].X, idToPos[id].Y].GetComponent<Piece>().name;
        switch (name)
        {
            case "black_queen":
            case "white_queen":
                LineMove(p, 1, 0);
                LineMove(p, 0, 1);
                LineMove(p, 1, 1);
                LineMove(p, -1, 0);
                LineMove(p, 0, -1);
                LineMove(p, -1, -1);
                LineMove(p, -1, 1);
                LineMove(p, 1, -1);
                break;
            case "black_knight":
            case "white_knight":
                LMove(p);
                break;
            case "black_bishop":
            case "white_bishop":
                LineMove(p, 1, 1);
                LineMove(p, 1, -1);
                LineMove(p, -1, 1);
                LineMove(p, -1, -1);
                break;
            case "black_king":
            case "white_king":
                SurroundMove(p);
                CastleMove(p);
                break;
            case "black_rook":
            case "white_rook":
                LineMove(p, 1, 0);
                LineMove(p, 0, 1);
                LineMove(p, -1, 0);
                LineMove(p, 0, -1);
                break;
            case "black_pawn":
                PawnMove(p, p.X, p.Y - 1);
                break;
            case "white_pawn":
                PawnMove(p, p.X, p.Y + 1);
                break;
        }
    }
    private void LineMove(Point p, int xIncrement, int yIncrement)
    {
        int x = p.X + xIncrement;
        int y = p.Y + yIncrement;
        bool isKingInRay = false;
        bool potentialPin = false;
        uint potentialPinId = 0;
        uint kingId = currentPlayer == Piece.PieceColor.White ? blackKingId : whiteKingId;

        while (game.PositionOnBoard(x, y) && board[x, y] == null)
        {
            Move move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, null, null, null, null);
            AddToPossibleMoves(currentPlayer, move);
            AddToMoveBoard(move.To, new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, true));

            x += xIncrement;
            y += yIncrement;
        }

        if (!game.PositionOnBoard(x, y))
            return;

        if (board[x, y].GetComponent<Piece>().GetPlayer() != currentPlayer)
        {
            Move move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x, y), board[x, y].GetComponent<Piece>().name, board[x, y].GetComponent<Piece>().id), null, null, null);
            AddToPossibleMoves(currentPlayer, move);
            AddToMoveBoard(move.To, new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, true));
            if(board[x, y].GetComponent<Piece>().id == kingId)
            {
                isKingInRay = true;
            }
            else
            {
                potentialPin = true;
                potentialPinId = board[x, y].GetComponent<Piece>().id;
            }
        }
        else
        {
            AddToMoveBoard(new Point(x, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false, true));
            return;
        }
        
        x += xIncrement;
        y += yIncrement;
        while (game.PositionOnBoard(x, y) && board[x, y] == null)
        {
            if(isKingInRay)
                AddToMoveBoard(new Point(x, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, true));
            else
                AddToMoveBoard(new Point(x, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false));

            x += xIncrement;
            y += yIncrement;
        }

        if (game.PositionOnBoard(x, y) && board[x, y].GetComponent<Piece>().id == kingId)
        {
            if(potentialPin == true)
            {
                pins[board[p.X, p.Y].GetComponent<Piece>().id] = potentialPinId;
                //////Debug.Log("pinner id: " + board[p.X, p.Y].GetComponent<Piece>().id);
                //////Debug.Log("pinned id: " + potentialPinId);
                //////Debug.Log("pinned again");
                //////Debug.Log("line move dir: " + xIncrement + ", " + yIncrement);
            }
        }
    }

    private void LMove(Point p)
    {
        PointMove(p, p.X + 1, p.Y + 2);
        PointMove(p, p.X - 1, p.Y + 2);
        PointMove(p, p.X + 2, p.Y + 1);
        PointMove(p, p.X + 2, p.Y - 1);
        PointMove(p, p.X + 1, p.Y - 2);
        PointMove(p, p.X - 1, p.Y - 2);
        PointMove(p, p.X - 2, p.Y + 1);
        PointMove(p, p.X - 2, p.Y - 1);
    }

    private void SurroundMove(Point p)
    {
        PointMove(p, p.X, p.Y + 1);
        PointMove(p, p.X, p.Y - 1);
        PointMove(p, p.X - 1, p.Y + 0);
        PointMove(p, p.X - 1, p.Y - 1);
        PointMove(p, p.X - 1, p.Y + 1);
        PointMove(p, p.X + 1, p.Y + 0);
        PointMove(p, p.X + 1, p.Y - 1);
        PointMove(p, p.X + 1, p.Y + 1);
    }

    private bool IsThisSquareChecked(Point p)
    {
        foreach(MoveUpdater mu in moveBoard[p.X, p.Y])
        {
            if (mu.attackable == true && board[idToPos[mu.id].X, idToPos[mu.id].Y].GetComponent<Piece>().player != currentPlayer)
                return true;
        }
        return false;
    }

    private void CastleMove(Point p)
    {
        //check if king played before
        //check if rooks played before
        //check if king is under attack at any of the points king passes through
        int x = p.X;
        int y = p.Y;

        if (currentPlayer == Piece.PieceColor.White)
        {
            if (x == 4 && y == 0 && board[4, 0].GetComponent<Piece>().GetMoveCount() == 0)
            {
                GameObject cp0 = board[0, y];
                GameObject cp1 = board[1, y];
                GameObject cp2 = board[2, y];
                GameObject cp3 = board[3, y];

                if(cp0 != null && cp0.GetComponent<Piece>().name == "white_rook" && cp0.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    AddToMoveBoard(new Point(1, y), new MoveUpdater(whiteKingId, false));
                    AddToMoveBoard(new Point(2, y), new MoveUpdater(whiteKingId, false));
                    AddToMoveBoard(new Point(3, y), new MoveUpdater(whiteKingId, false));
                    AddToMoveBoard(new Point(4, y), new MoveUpdater(whiteKingId, false));
                }

                if (cp1 == null && cp2 == null && cp3 == null && cp0 != null && cp0.GetComponent<Piece>().name == "white_rook" && cp0.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    if (!IsThisSquareChecked(new Point(2, y)) && !IsThisSquareChecked(new Point(3, y)) && !IsThisSquareChecked(new Point(4, y)))
                    {
                        Move move = new Move(p, new Point(2, y), board[p.X, p.Y].GetComponent<Piece>().name, null, new Castle(new Point(0, 0), new Point(3, 0), false), null, null);
                        AddToPossibleMoves(currentPlayer, move);
                    }
                }

                GameObject cp5 = board[x + 1, y];
                GameObject cp6 = board[x + 2, y];
                GameObject cp7 = board[x + 3, y];

                if (cp7 != null && cp7.GetComponent<Piece>().name == "white_rook" && cp7.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    AddToMoveBoard(new Point(6, y), new MoveUpdater(whiteKingId, false));
                    AddToMoveBoard(new Point(5, y), new MoveUpdater(whiteKingId, false));
                    AddToMoveBoard(new Point(4, y), new MoveUpdater(whiteKingId, false));
                }
                if (cp5 == null && cp6 == null && cp7 != null && cp7.GetComponent<Piece>().name == "white_rook" && cp7.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    if (!IsThisSquareChecked(new Point(5, y)) && !IsThisSquareChecked(new Point(6, y)) && !IsThisSquareChecked(new Point(4, y)))
                    {
                        Move move = new Move(p, new Point(6, y), board[p.X, p.Y].GetComponent<Piece>().name, null, new Castle(new Point(7, 0), new Point(5, 0), true), null, null);
                        AddToPossibleMoves(currentPlayer, move);
                    }
                }
            }
        }
        else
        {
            if (x == 4 && y == 7 && board[4, 7].GetComponent<Piece>().GetMoveCount() == 0)
            {
                GameObject cp0 = board[0, y];
                GameObject cp1 = board[1, y];
                GameObject cp2 = board[2, y];
                GameObject cp3 = board[3, y];

                if (cp0 != null && cp0.GetComponent<Piece>().name == "black_rook" && cp0.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    AddToMoveBoard(new Point(1, y), new MoveUpdater(blackKingId, false));
                    AddToMoveBoard(new Point(2, y), new MoveUpdater(blackKingId, false));
                    AddToMoveBoard(new Point(3, y), new MoveUpdater(blackKingId, false));
                    AddToMoveBoard(new Point(4, y), new MoveUpdater(blackKingId, false));
                }
                if (cp1 == null && cp2 == null && cp3 == null && cp0 != null && cp0.GetComponent<Piece>().name == "black_rook" && cp0.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    if (!IsThisSquareChecked(new Point(2, y)) && !IsThisSquareChecked(new Point(3, y)) && !IsThisSquareChecked(new Point(4, y)))
                    {
                        Move move = new Move(p, new Point(2, y), board[p.X, p.Y].GetComponent<Piece>().name, null, new Castle(new Point(0, 7), new Point(3, 7), false), null, null);
                        AddToPossibleMoves(currentPlayer, move);
                    }
                }

                GameObject cp5 = board[x + 1, y];
                GameObject cp6 = board[x + 2, y];
                GameObject cp7 = board[x + 3, y];

                if (cp7 != null && cp7.GetComponent<Piece>().name == "black_rook" && cp7.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    AddToMoveBoard(new Point(6, y), new MoveUpdater(blackKingId, false));
                    AddToMoveBoard(new Point(5, y), new MoveUpdater(blackKingId, false));
                    AddToMoveBoard(new Point(4, y), new MoveUpdater(blackKingId, false));
                }
                if (cp5 == null && cp6 == null && cp7 != null && cp7.GetComponent<Piece>().name == "black_rook" && cp7.GetComponent<Piece>().GetMoveCount() == 0)
                {
                    if (!IsThisSquareChecked(new Point(5, y)) && !IsThisSquareChecked(new Point(6, y)) && !IsThisSquareChecked(new Point(4, y)))
                    {
                        Move move = new Move(p, new Point(6, y), board[p.X, p.Y].GetComponent<Piece>().name, null, new Castle(new Point(7, 7), new Point(5, 7), true), null, null);
                        AddToPossibleMoves(currentPlayer, move);
                    }
                }
            }
        }
    }

    private void PointMove(Point p, int x, int y)
    {
        if (game.PositionOnBoard(x, y))
        {
            GameObject cp = board[x, y];

            if (cp == null)
            {
                Move move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, null, null, null, null);
                AddToPossibleMoves(currentPlayer, move);
                AddToMoveBoard(new Point(x, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, true));
            }
            else if (cp.GetComponent<Piece>().GetPlayer() != currentPlayer)
            {
                Move move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x, y), board[x, y].GetComponent<Piece>().name, board[x, y].GetComponent<Piece>().id), null, null, null);
                AddToPossibleMoves(currentPlayer, move);
                AddToMoveBoard(new Point(x, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, true));
            }
            else
            {
                AddToMoveBoard(new Point(x, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false, true));
            }
        }
    }

    private void PawnMove(Point p, int x, int y)
    {
        if (game.PositionOnBoard(x, y))
        {
            AddToMoveBoard(new Point(x, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false));
            if (y == 5 && currentPlayer == Piece.PieceColor.Black && game.PositionOnBoard(x, y - 1))
                AddToMoveBoard(new Point(x, y - 1), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false));
            if (y == 2 && currentPlayer == Piece.PieceColor.White && game.PositionOnBoard(x, y + 1))
                AddToMoveBoard(new Point(x, y + 1), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false));
            
            if (board[x, y] == null)
            {
                if(y == 0 || y == 7)
                {
                    Move move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, null, null, y == 0 || y == 7 ? new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_queen") : null, null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, null, null, y == 0 || y == 7 ? new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_rook") : null, null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, null, null, y == 0 || y == 7 ? new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_bishop") : null, null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, null, null, y == 0 || y == 7 ? new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_knight") : null, null);
                    AddToPossibleMoves(currentPlayer, move);
                }
                else
                {
                    Move move = new Move(p, new Point(x, y), board[p.X, p.Y].GetComponent<Piece>().name, null, null, null, null);
                    AddToPossibleMoves(currentPlayer, move);
                }

                //

                //double move
                if (y == 5 && currentPlayer == Piece.PieceColor.Black && game.PositionOnBoard(x, y - 1) && board[x, y - 1] == null)
                {
                    Move move = new Move(p, new Point(x, y - 1), board[p.X, p.Y].GetComponent<Piece>().name, null, null, null, null);
                    AddToPossibleMoves(currentPlayer, move);
                }
                if (y == 2 && currentPlayer == Piece.PieceColor.White && game.PositionOnBoard(x, y + 1) && board[x, y + 1] == null)
                {
                    Move move = new Move(p, new Point(x, y + 1), board[p.X, p.Y].GetComponent<Piece>().name, null, null, null, null);
                    AddToPossibleMoves(currentPlayer, move);
                }
            }

            //move timing is not checked  // en passant
            if (y == 2 && currentPlayer == Piece.PieceColor.Black && game.PositionOnBoard(x - 1, y) && board[x - 1, y] == null && board[x - 1, y + 1] != null && board[x - 1, y + 1].GetComponent<Piece>().GetPlayer() != currentPlayer && board[x - 1, y + 1].GetComponent<Piece>().name == "white_pawn" && board[x - 1, y + 1].GetComponent<Piece>().GetMoveCount() == 1 && game.GetTwoFoldMoveCount() - board[x - 1, y + 1].GetComponent<Piece>().GetLastMoveNumber() <= 1)
            {
                Move move = new Move(p, new Point(x - 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x - 1, y + 1), board[x - 1, y + 1].GetComponent<Piece>().name, board[x - 1, y + 1].GetComponent<Piece>().id), null, null, null);
                AddToPossibleMoves(currentPlayer, move);
                enPassants.Add(board[p.X, p.Y].GetComponent<Piece>().id);
            }
            if (y == 2 && currentPlayer == Piece.PieceColor.Black && game.PositionOnBoard(x + 1, y) && board[x + 1, y] == null && board[x + 1, y + 1] != null && board[x + 1, y + 1].GetComponent<Piece>().GetPlayer() != currentPlayer && board[x + 1, y + 1].GetComponent<Piece>().name == "white_pawn" && board[x + 1, y + 1].GetComponent<Piece>().GetMoveCount() == 1 && game.GetTwoFoldMoveCount() - board[x + 1, y + 1].GetComponent<Piece>().GetLastMoveNumber() <= 1)
            {
                Move move = new Move(p, new Point(x + 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x + 1, y + 1), board[x + 1, y + 1].GetComponent<Piece>().name, board[x + 1, y + 1].GetComponent<Piece>().id), null, null, null);
                AddToPossibleMoves(currentPlayer, move);
                enPassants.Add(board[p.X, p.Y].GetComponent<Piece>().id);
            }
            if (y == 5 && currentPlayer == Piece.PieceColor.White && game.PositionOnBoard(x - 1, y) && board[x - 1, y] == null && board[x - 1, y - 1] != null && board[x - 1, y - 1].GetComponent<Piece>().GetPlayer() != currentPlayer && board[x - 1, y - 1].GetComponent<Piece>().name == "black_pawn" && board[x - 1, y - 1].GetComponent<Piece>().GetMoveCount() == 1 && game.GetTwoFoldMoveCount() - board[x - 1, y - 1].GetComponent<Piece>().GetLastMoveNumber() <= 1)
            {
                Move move = new Move(p, new Point(x - 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x - 1, y - 1), board[x - 1, y - 1].GetComponent<Piece>().name, board[x - 1, y - 1].GetComponent<Piece>().id), null, null, null);
                AddToPossibleMoves(currentPlayer, move);
                enPassants.Add(board[p.X, p.Y].GetComponent<Piece>().id);
            }
            if (y == 5 && currentPlayer == Piece.PieceColor.White && game.PositionOnBoard(x + 1, y) && board[x + 1, y] == null && board[x + 1, y - 1] != null && board[x + 1, y - 1].GetComponent<Piece>().GetPlayer() != currentPlayer && board[x + 1, y - 1].GetComponent<Piece>().name == "black_pawn" && board[x + 1, y - 1].GetComponent<Piece>().GetMoveCount() == 1 && game.GetComponent<GameSetter>().GetTwoFoldMoveCount() - board[x + 1, y - 1].GetComponent<Piece>().GetLastMoveNumber() <= 1)
            {
                Move move = new Move(p, new Point(x + 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x + 1, y - 1), board[x + 1, y - 1].GetComponent<Piece>().name, board[x + 1, y - 1].GetComponent<Piece>().id), null, null, null);
                AddToPossibleMoves(currentPlayer, move);
                enPassants.Add(board[p.X, p.Y].GetComponent<Piece>().id);
            }

            if (game.PositionOnBoard(x + 1, y) && board[x + 1, y] != null && board[x + 1, y].GetComponent<Piece>().GetPlayer() == currentPlayer)
            {
                AddToMoveBoard(new Point(x + 1, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false, true));
            }
            else if (game.PositionOnBoard(x + 1, y))
            {
                AddToMoveBoard(new Point(x + 1, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, true));
            }
            if (game.PositionOnBoard(x - 1, y) && board[x - 1, y] != null && board[x - 1, y].GetComponent<Piece>().GetPlayer() == currentPlayer)
            {
                AddToMoveBoard(new Point(x - 1, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, false, true));
            }
            else if (game.PositionOnBoard(x - 1, y))
            {
                AddToMoveBoard(new Point(x - 1, y), new MoveUpdater(board[p.X, p.Y].GetComponent<Piece>().id, true));
            }

            if (game.PositionOnBoard(x + 1, y) && board[x + 1, y] != null && board[x + 1, y].GetComponent<Piece>().GetPlayer() != currentPlayer)
            {
                if (y == 0 || y == 7)
                {
                    Move move = new Move(p, new Point(x + 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x + 1, y), board[x + 1, y].GetComponent<Piece>().name, board[x + 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_queen"), null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x + 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x + 1, y), board[x + 1, y].GetComponent<Piece>().name, board[x + 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_bishop"), null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x + 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x + 1, y), board[x + 1, y].GetComponent<Piece>().name, board[x + 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_knight"), null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x + 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x + 1, y), board[x + 1, y].GetComponent<Piece>().name, board[x + 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_rook"), null);
                    AddToPossibleMoves(currentPlayer, move);
                }
                else
                {
                    Move move = new Move(p, new Point(x + 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x + 1, y), board[x + 1, y].GetComponent<Piece>().name, board[x + 1, y].GetComponent<Piece>().id), null, null, null);
                    AddToPossibleMoves(currentPlayer, move);
                }
            }

            if (game.PositionOnBoard(x - 1, y) && board[x - 1, y] != null && board[x - 1, y].GetComponent<Piece>().GetPlayer() != currentPlayer)
            {
                if (y == 0 || y == 7)
                {
                    Move move = new Move(p, new Point(x - 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x - 1, y), board[x - 1, y].GetComponent<Piece>().name, board[x - 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_queen"), null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x - 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x - 1, y), board[x - 1, y].GetComponent<Piece>().name, board[x - 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_rook"), null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x - 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x - 1, y), board[x - 1, y].GetComponent<Piece>().name, board[x - 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_bishop"), null);
                    AddToPossibleMoves(currentPlayer, move);
                    move = new Move(p, new Point(x - 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x - 1, y), board[x - 1, y].GetComponent<Piece>().name, board[x - 1, y].GetComponent<Piece>().id), null, new Promote((currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_knight"), null);
                    AddToPossibleMoves(currentPlayer, move);
                }
                else
                {
                    Move move = new Move(p, new Point(x - 1, y), board[p.X, p.Y].GetComponent<Piece>().name, new Attack(new Point(x - 1, y), board[x - 1, y].GetComponent<Piece>().name, board[x - 1, y].GetComponent<Piece>().id), null, null, null);
                    AddToPossibleMoves(currentPlayer, move);
                }
            }
        }
    }

    private void AddToPossibleMoves(Piece.PieceColor player, Move move)
    {
        if (player == Piece.PieceColor.White)
            AddToWhiteMoves(move);
        else
            AddToBlackMoves(move);
    }

    private void AddToWhiteMoves(Move move)
    {
        uint id = board[move.From.X, move.From.Y].GetComponent<Piece>().id;
        if (allWhiteMoves.ContainsKey(id))
        {
            List<Move> pointsToMove = allWhiteMoves[id];
            pointsToMove.Add(move);
            allWhiteMoves[id] = pointsToMove;
        }
        else
        {
            List<Move> pointsToMove = new List<Move> { move };
            allWhiteMoves.Add(id, pointsToMove);
        }
    }

    private void AddToBlackMoves(Move move)
    {
        uint id = board[move.From.X, move.From.Y].GetComponent<Piece>().id;
        if (allBlackMoves.ContainsKey(id))
        {
            List<Move> pointsToMove = allBlackMoves[id];
            pointsToMove.Add(move);
            allBlackMoves[id] = pointsToMove;
        }
        else
        {
            List<Move> pointsToMove = new List<Move> { move };
            allBlackMoves.Add(id, pointsToMove);
        }
    }

    private void AddToMoveBoard(Point pos, MoveUpdater moveUpdate)
    {
        if(!moveBoard[pos.X, pos.Y].Contains(moveUpdate))
        {
            moveBoard[pos.X, pos.Y].Add(moveUpdate);
            moveupdatePositionsOfPieces[moveUpdate.id].Add(pos);
        }
            
    }

    private void RemoveFromMoveBoard(Point pos, uint id)
    {
        MoveUpdater mu1 = new MoveUpdater(id, true, true);
        MoveUpdater mu2 = new MoveUpdater(id, true, false);
        MoveUpdater mu3 = new MoveUpdater(id, false, true);
        MoveUpdater mu4 = new MoveUpdater(id, false, false);
        if (moveBoard[pos.X, pos.Y].Contains(mu1))
            moveBoard[pos.X, pos.Y].Remove(mu1);
        if (moveBoard[pos.X, pos.Y].Contains(mu2))
            moveBoard[pos.X, pos.Y].Remove(mu2);
        if (moveBoard[pos.X, pos.Y].Contains(mu3))
            moveBoard[pos.X, pos.Y].Remove(mu3);
        if (moveBoard[pos.X, pos.Y].Contains(mu4))
            moveBoard[pos.X, pos.Y].Remove(mu4);
    }

    public void ForwardMoveUpdate(Move move)
    {
        board = game.GetBoard();
        check = false;
        uint kingIdOfPlayer = board[move.To.X, move.To.Y].GetComponent<Piece>().player == Piece.PieceColor.White ? whiteKingId : blackKingId;
        uint kingIdOfOpponent = board[move.To.X, move.To.Y].GetComponent<Piece>().player == Piece.PieceColor.White ? blackKingId : whiteKingId;

        HashSet<uint> ids = new HashSet<uint>();
        if (move.Attack != null)
        {
            uint attackedId = move.Attack.Value.CapturedPieceID;
            ids.Add(attackedId);
            foreach (Point pt in moveupdatePositionsOfPieces[attackedId])
            {
                RemoveFromMoveBoard(pt, attackedId);
            }
            moveupdatePositionsOfPieces[attackedId] = new List<Point>();
            if (pins.ContainsKey(attackedId))
            {
                //////Debug.Log("pin removed");
                pins.Remove(attackedId);
            }
        }
        if((board[move.To.X, move.To.Y].GetComponent<Piece>().name == "white_pawn" || board[move.To.X, move.To.Y].GetComponent<Piece>().name == "black_pawn") && Math.Abs(move.From.Y - move.To.Y) == 2)
        {
            foreach (MoveUpdater mu in moveBoard[move.From.X, (move.From.Y + move.To.Y) / 2])
            {
                if (board[idToPos[mu.id].X, idToPos[mu.id].Y].GetComponent<Piece>().name == "white_pawn" || board[idToPos[mu.id].X, idToPos[mu.id].Y].GetComponent<Piece>().name == "black_pawn")
                {
                    ids.Add(mu.id);
                }
            }        
        }

        foreach (uint id in enPassants)
            ids.Add(id);
        foreach (MoveUpdater mu in moveBoard[move.From.X, move.From.Y])
            ids.Add(mu.id);
        foreach (MoveUpdater mu in moveBoard[move.To.X, move.To.Y])
            ids.Add(mu.id);

        foreach (uint id in ids)
        {
            if(id < 16)
                whiteUpdatesQueue.Enqueue(id);
            else
                blackUpdatesQueue.Enqueue(id);
        }

        GenerateMoves(currentPlayer);
        foreach (uint id in enPassants)
            ids.Add(id);
        foreach (uint id in ids)
        {
            if (id < 16)
                whiteUpdatesQueue.Enqueue(id);
            else
                blackUpdatesQueue.Enqueue(id);
        }
        enPassants = new List<uint>();

        if (doublecheck)
        {
            //king run

        }
        else if (check)
        {
            //block, capture, king run
            //king run, no block etc.
            check = false;
            if (move.Attack == null)
            {
                uint pinningPieceId = 0;
                foreach (MoveUpdater mu in moveBoard[idToPos[kingIdOfPlayer].X, idToPos[kingIdOfPlayer].Y])
                {
                    if (mu.attackable == true)
                    {
                        pinningPieceId = mu.id;
                        break;
                    }
                }
                pins.Add(pinningPieceId, board[move.To.X, move.To.Y].GetComponent<Piece>().id);
                //////Debug.Log("pinned!!");  
            }
        }
        int checkCount = 0;

        foreach (MoveUpdater mu in moveBoard[idToPos[kingIdOfOpponent].X, idToPos[kingIdOfOpponent].Y])
        {
            //////Debug.Log(mu.id);
            if (mu.attackable == true)
            {
                checkCount++;
            }
        }
        check = checkCount > 0;
        doublecheck = checkCount > 1;
        if (doublecheck) { }////Debug.Log("double check"); }
    }

    public List<uint> GetEnPassants()
    {
        return enPassants;
    }

    public void BackwardMoveUpdate(Move move, Piece.PieceColor player, bool isInitialPos = false)
    {
        board = game.GetBoard();
        currentPlayer = player == Piece.PieceColor.White ? Piece.PieceColor.Black : Piece.PieceColor.White;
        if (isInitialPos)
        {
            Initialize();
            GenerateMoves(Piece.PieceColor.White);
            return;
        }

        if (check)
            check = false;
        if (doublecheck)
            doublecheck = false;
        GameObject from = board[move.From.X, move.From.Y];

        uint kingIdOfPlayer = player == Piece.PieceColor.White ?  blackKingId : whiteKingId;

        HashSet<uint> ids = new HashSet<uint>();
        ids.Add(from.GetComponent<Piece>().id);
        if (move.Attack != null)
            ids.Add(move.Attack.Value.CapturedPieceID);
        foreach (uint id in enPassants)
            ids.Add(id);
        foreach (MoveUpdater mu in moveBoard[move.From.X, move.From.Y])
            ids.Add(mu.id);
        foreach (MoveUpdater mu in moveBoard[move.To.X, move.To.Y])
            ids.Add(mu.id);

        for(int i = 0; i < 8; i++)
        {
            GameObject go1 = board[i, 4];
            GameObject go2 = board[i, 3];
            if (go1 != null && go1.GetComponent<Piece>().name == "white_pawn")
                ids.Add(go1.GetComponent<Piece>().id);
            if (go2 != null && go2.GetComponent<Piece>().name == "black_pawn")
                ids.Add(go2.GetComponent<Piece>().id);
        }

        foreach (uint id in ids)
        {
            if (id < 16)
                whiteUpdatesQueue.Enqueue(id);
            else
                blackUpdatesQueue.Enqueue(id);
        }

        enPassants = new List<uint>();

        GenerateMoves(player == Piece.PieceColor.White ? Piece.PieceColor.Black : Piece.PieceColor.White);

        //block, capture, king run
        //king run, no block etc.
        int checkCount = 0;
        uint attackingPieceId = 0;
        foreach (MoveUpdater mu in moveBoard[idToPos[kingIdOfPlayer].X, idToPos[kingIdOfPlayer].Y])
        {
            //////Debug.Log(mu.id);
            if (mu.attackable == true)
            {
                checkCount++;
                attackingPieceId = mu.id;
            }
        }
        check = checkCount > 0;
        doublecheck = checkCount > 1;

        ids = new HashSet<uint>();
        if (check)
        {
            ////Debug.Log("attackingPieceId: " + attackingPieceId);
            ////Debug.Log("here");
            List<string> attackPieceNames = new List<string>() { "white_rook", "white_bishop", "white_queen", "black_rook", "black_bishop", "black_queen" };
            if (attackPieceNames.Contains(board[idToPos[attackingPieceId].X, idToPos[attackingPieceId].Y].GetComponent<Piece>().name))
            {
                ////Debug.Log("here2");
                List<Point> rayOfPoints = new List<Point>();
                Point start = idToPos[attackingPieceId];
                Point end = idToPos[kingIdOfPlayer];

                if (start.X == end.X)
                {
                    int yInc = (end.Y - start.Y) / Math.Abs(end.Y - start.Y);
                    int yAxis = start.Y;
                    while (yAxis != end.Y)
                    {
                        rayOfPoints.Add(new Point(start.X, yAxis));
                        yAxis += yInc;
                    }
                }
                else if (start.Y == end.Y)
                {
                    int xInc = (end.X - start.X) / Math.Abs(end.X - start.X);
                    int xAxis = start.X;
                    while (xAxis != end.X)
                    {
                        rayOfPoints.Add(new Point(xAxis, start.Y));
                        xAxis += xInc;
                    }
                }
                else
                {
                    int xInc = (end.X - start.X) / Math.Abs(end.X - start.X);
                    int xAxis = start.X;
                    int yInc = (end.Y - start.Y) / Math.Abs(end.Y - start.Y);
                    int yAxis = start.Y;
                    while (xAxis != end.X)
                    {
                        rayOfPoints.Add(new Point(xAxis, yAxis));
                        xAxis += xInc;
                        yAxis += yInc;
                    }
                }
                ////Debug.Log("rayOfPoints count: " + rayOfPoints.Count);
                foreach(Point point in rayOfPoints)
                {
                    foreach (MoveUpdater mu in moveBoard[point.X, point.Y])
                    {
                        if (board[idToPos[mu.id].X, idToPos[mu.id].Y].GetComponent<Piece>().player != currentPlayer)
                        {
                            ////Debug.Log("HERE3");
                            ////Debug.Log(mu.id);
                            ids.Add(mu.id);
                        }
                        ////Debug.Log("here4");
                    }                
                }
                foreach (uint id in ids)
                {
                    ////Debug.Log("here5");
                    if (id < 16)
                        whiteUpdatesQueue.Enqueue(id);
                    else
                        blackUpdatesQueue.Enqueue(id);
                }
            }
        }

        GenerateMoves(player);
        //int pinningPieceId = -1;
        //foreach (MoveUpdater mu in moveBoard[idToPos[kingIdOfOpponent].X, idToPos[kingIdOfOpponent].Y])
        //{
        //    if (mu.attackable == true)
        //    {
        //        pinningPieceId = (int)mu.id;
        //        break;
        //    }
        //}
        //if (pinningPieceId >= 0)
        //{
        //    pins.Remove((uint)pinningPieceId);
        //}
        //////Debug.Log("pinned!!");
    }

    public string GetMoveLogFirstPart(Move move)
    {
        if (move.Castle != null)
        {
            if (move.Castle.Value.IsShortCastle)
                return "O-O";
            else
                return "O-O-O";
        }
        List<MoveUpdater> mu = moveBoard[move.From.X, move.From.Y];
        board = game.GetBoard();
        idToPos = game.idToPos;
        uint movingPieceId = board[move.From.X, move.From.Y].GetComponent<Piece>().id;
        string fromPosLogX = "";
        string fromPosLogY = "";
        if(!(move.MovingPieceName == "white_pawn" || move.MovingPieceName == "black_pawn"))
        {
            foreach (MoveUpdater moveUpdater in mu)
            {
                if (moveUpdater.id != movingPieceId && board[idToPos[moveUpdater.id].X, idToPos[moveUpdater.id].Y].GetComponent<Piece>().name == move.MovingPieceName && ((moveUpdater.attackable == true && move.Attack != null) || (moveUpdater.attackable == false && move.Attack == null)))
                {
                    if (idToPos[moveUpdater.id].X == move.From.X)
                        fromPosLogY = (move.From.Y + 1).ToString();
                    else
                        fromPosLogX = Char.ToString((char)('a' + move.From.X));
                }
            }
        }
        else
        {
            if (move.Attack != null)
                fromPosLogX = Char.ToString((char)('a' + move.From.X));
        }

        ////Debug.Log("here " + Char.ToString((char)('a' + move.From.X)));
        string fromPosLog = fromPosLogX + fromPosLogY;

        return logNames[move.MovingPieceName] + fromPosLog + (move.Attack == null ? "" : "x" + logNames[move.Attack.Value.CapturedPieceName]) + GetPieceCoordLogs(move.To) + (move.Promote == null ? "" : "=" + logNames[move.Promote.Value.PromotedTo]);
    }

    public string GetPieceName(Move move)
    {
        return logNames[move.MovingPieceName];
    }

    public string GetMoveLogFully(string logFirstPart, bool check, bool checkmate = false)
    {
        return logFirstPart + (checkmate ? "#" : check ? "+" : "");
    }

    public string GetPieceCoordLogs(Point point)
    {
        char letterCoord = (char)('a' + point.X);
        return letterCoord.ToString() + (point.Y + 1).ToString();
    }
}
