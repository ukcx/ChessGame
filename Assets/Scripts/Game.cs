using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public struct Point
{
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int GetX() { return X; }
    public int GetY() { return Y; }
    private int X { get; }
    private int Y { get; }

    public static bool operator ==(Point obj1, Point obj2)
    {
        return obj1.X == obj2.X && obj1.Y == obj2.Y;
    }
    public static bool operator !=(Point obj1, Point obj2)
    {
        return obj1.X != obj2.X || obj1.Y != obj2.Y;
    }
    public override int GetHashCode()
    {
        return (X << 2) ^ Y;
    }
}

public struct Move
{
    public Move(Point _from, Point _to, bool _attacking=false, bool _castling=false, bool _promote=false)
    {
        from = _from;
        to = _to;
        attacking = _attacking;
        castling = _castling;
        promoting = _promote;
    }

    public Point GetFrom() { return from; }
    public Point GetTo() { return to; }
    public bool GetAttacking() { return attacking; }
    public bool GetCastling() { return castling; }
    public bool GetPromoting() { return promoting; }
    private Point from { get; }
    private Point to { get; }
    private bool attacking { get; }
    private bool castling { get;  }
    private bool promoting { get; }

    public static bool operator ==(Move obj1, Move obj2)
    {
        return obj1.from == obj2.from && obj1.to == obj2.to && obj1.attacking == obj2.attacking && obj1.castling == obj2.castling && obj1.promoting == obj2.promoting;
    }
    public static bool operator !=(Move obj1, Move obj2)
    {
        return obj1.from != obj2.from || obj1.to != obj2.to || obj1.attacking != obj2.attacking || obj1.castling != obj2.castling || obj1.promoting != obj2.promoting;
    }
}

public class Game : MonoBehaviour
{
    //Reference from Unity IDE
    public GameObject chesspiece;
    public Dictionary<Point, List<Point>> possibleMoves;
    public Dictionary<Point, List<Point>> possibleAttackMoves;
    public Dictionary<Point, List<Point>> possibleCastleMoves;
    public Dictionary<Point, List<Point>> opponentPossibleAttackMoves;

    //Matrices needed, positions of each of the GameObjects
    //Also separate arrays for the players in order to easily keep track of them all
    //Keep in mind that the same objects are going to be in "positions" and "playerBlack"/"playerWhite"
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];
    private Stack<Move> previousMoves = new Stack<Move>();
    private Stack<string> previouslyEatenPieces = new Stack<string>();
    private Stack<int> previouslyEatenPiecesMoveCounts = new Stack<int>();

    //current turn
    private string currentPlayer = "white";

    //Game Ending
    private bool gameOver = false;
    private Point pointToAddNewPiece = new Point(-1, -1);
    private bool check = false;
    private bool promoting = false;

    //Unity calls this right when the game starts, there are a few built in functions
    //that Unity can call for you
    public void Start()
    {
        playerWhite = new GameObject[] { Create("white_rook", 0, 0), Create("white_knight", 1, 0),
            Create("white_bishop", 2, 0), Create("white_queen", 3, 0), Create("white_king", 4, 0),
            Create("white_bishop", 5, 0), Create("white_knight", 6, 0), Create("white_rook", 7, 0),
            Create("white_pawn", 0, 1), Create("white_pawn", 1, 1), Create("white_pawn", 2, 1),
            Create("white_pawn", 3, 1), Create("white_pawn", 4, 1), Create("white_pawn", 5, 1),
            Create("white_pawn", 6, 1), Create("white_pawn", 7, 1) };
        playerBlack = new GameObject[] { Create("black_rook", 0, 7), Create("black_knight",1,7),
            Create("black_bishop",2,7), Create("black_queen",3,7), Create("black_king",4,7),
            Create("black_bishop",5,7), Create("black_knight",6,7), Create("black_rook",7,7),
            Create("black_pawn", 0, 6), Create("black_pawn", 1, 6), Create("black_pawn", 2, 6),
            Create("black_pawn", 3, 6), Create("black_pawn", 4, 6), Create("black_pawn", 5, 6),
            Create("black_pawn", 6, 6), Create("black_pawn", 7, 6) };

        //Set all piece positions on the positions board
        for (int i = 0; i < playerBlack.Length; i++)
        {
            SetPosition(playerBlack[i]);
            SetPosition(playerWhite[i]);
        }
        DisableButtons();
        GameObject.FindGameObjectWithTag("KnightButton").GetComponent<Button>().onClick.AddListener(delegate { ReplacePawn(0); });
        GameObject.FindGameObjectWithTag("BishopButton").GetComponent<Button>().onClick.AddListener(delegate { ReplacePawn(1); });
        GameObject.FindGameObjectWithTag("RookButton").GetComponent<Button>().onClick.AddListener(delegate { ReplacePawn(2); });
        GameObject.FindGameObjectWithTag("QueenButton").GetComponent<Button>().onClick.AddListener(delegate { ReplacePawn(3); });
        GameObject.FindGameObjectWithTag("UndoButton").GetComponent<Button>().onClick.AddListener(delegate { UndoFunction(); });

        previousMoves = new Stack<Move>();
        previouslyEatenPieces = new Stack<string>(); 
        previouslyEatenPiecesMoveCounts = new Stack<int>();
        possibleMoves = new Dictionary<Point, List<Point>>();
        possibleAttackMoves = new Dictionary<Point, List<Point>>();
        possibleCastleMoves = new Dictionary<Point, List<Point>>();
        opponentPossibleAttackMoves = new Dictionary<Point, List<Point>>();
        check = false;
        GenerateMoves();   
    }

    public GameObject Create(string name, int x, int y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
        ChessMan cm = obj.GetComponent<ChessMan>(); //We have access to the GameObject, we need the script
        cm.name = name; //This is a built in variable that Unity has, so we did not have to declare it before
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate(); //It has everything set up so it can now Activate()
        return obj;
    }

    public void ReplacePawn(int index)
    {
        switch (index)
        {
            case 0:
                SetPosition(Create(currentPlayer + "_knight", pointToAddNewPiece.GetX(), pointToAddNewPiece.GetY()));
                break;
            case 1:
                SetPosition(Create(currentPlayer + "_bishop", pointToAddNewPiece.GetX(), pointToAddNewPiece.GetY()));
                break;
            case 2:
                SetPosition(Create(currentPlayer + "_rook", pointToAddNewPiece.GetX(), pointToAddNewPiece.GetY()));
                break;
            case 3:
                SetPosition(Create(currentPlayer + "_queen", pointToAddNewPiece.GetX(), pointToAddNewPiece.GetY()));
                break;
        }
        DisableButtons();
        EnableUndoButton();
        promoting = false;
        NextTurn();
    }

    public void UndoFunction()
    {
        if(previousMoves.Count != 0)
        {
            Move m = previousMoves.Pop();
            Point from = m.GetFrom();
            Point to = m.GetTo();
            
            GameObject reference = GetPosition(to.GetX(), to.GetY());
            reference.GetComponent<ChessMan>().DestroyMovePlates();
            SetPositionEmpty(to.GetX(), to.GetY());

            if (m.GetPromoting())
            {
                Destroy(reference);
                string eatenPiece = previouslyEatenPieces.Pop();
                GameObject reference2 = Create(eatenPiece, from.GetX(), from.GetY());
                reference2.GetComponent<ChessMan>().SetXBoard(from.GetX());
                reference2.GetComponent<ChessMan>().SetYBoard(from.GetY());
                reference2.GetComponent<ChessMan>().SetCoords();
                reference2.GetComponent<ChessMan>().SetMoveCount(previouslyEatenPiecesMoveCounts.Pop());
                SetPosition(reference2);
            }
            else
            {
                reference.GetComponent<ChessMan>().SetXBoard(from.GetX());
                reference.GetComponent<ChessMan>().SetYBoard(from.GetY());
                reference.GetComponent<ChessMan>().SetCoords();
                reference.GetComponent<ChessMan>().SetLastClickedPiece(new Point(-1, -1));
                reference.GetComponent<ChessMan>().IncreaseMoveCount(-1);
                SetPosition(reference);
            }

            if (m.GetAttacking())
            {
                string eatenPiece = previouslyEatenPieces.Pop();
                GameObject reference2 = Create(eatenPiece, to.GetX(), to.GetY());
                reference2.GetComponent<ChessMan>().SetXBoard(to.GetX());
                reference2.GetComponent<ChessMan>().SetYBoard(to.GetY());
                reference2.GetComponent<ChessMan>().SetCoords();
                reference2.GetComponent<ChessMan>().SetMoveCount(previouslyEatenPiecesMoveCounts.Pop());
                SetPosition(reference2);
            }
            else if (m.GetCastling())
            {
                if(to.GetX() == 6)
                {
                    GameObject reference2 = GetPosition(5, to.GetY());
                    SetPositionEmpty(5, to.GetY());
                    reference2.GetComponent<ChessMan>().SetXBoard(7);
                    reference2.GetComponent<ChessMan>().SetYBoard(to.GetY());
                    reference2.GetComponent<ChessMan>().SetCoords();
                    reference2.GetComponent<ChessMan>().IncreaseMoveCount(-1);
                    SetPosition(reference2);
                }
                else
                {
                    GameObject reference2 = GetPosition(3, to.GetY());
                    SetPositionEmpty(3, to.GetY());
                    reference2.GetComponent<ChessMan>().SetXBoard(0);
                    reference2.GetComponent<ChessMan>().SetYBoard(to.GetY());
                    reference2.GetComponent<ChessMan>().SetCoords();
                    reference2.GetComponent<ChessMan>().IncreaseMoveCount(-1);
                    SetPosition(reference2);
                }

            }

            NextTurn();
        }
    }

    public void SetPositionToAddNewPiece(Point p)
    {
        pointToAddNewPiece = p;
    }

    public void SetPosition(GameObject obj)
    {
        ChessMan cm = obj.GetComponent<ChessMan>();

        //Overwrites either empty space or whatever was there
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
    }

    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y)
    {
        return positions[x, y];
    }

    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }

    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public void SetCheck(bool isChecked)
    {
        check = isChecked;
    }

    public bool GetCheck()
    {
        return check;
    }

    public void AddToPreviousMoves(Move move, string attackedPiece="", int attackedPieceMoveCount=0, string promotedPiece="", int promotedPieceMoveCount=0)
    {
        previousMoves.Push(move);
        if (move.GetAttacking())
        {
            previouslyEatenPieces.Push(attackedPiece);
            previouslyEatenPiecesMoveCounts.Push(attackedPieceMoveCount);
        }
        if (move.GetPromoting())
        {
            previouslyEatenPieces.Push(promotedPiece);
            previouslyEatenPiecesMoveCounts.Push(promotedPieceMoveCount);
        }
    }

    public void NextTurn()
    {
        if (currentPlayer == "white")
        {
            currentPlayer = "black";
        }
        else
        {
            currentPlayer = "white";
        }
        possibleMoves = new Dictionary<Point, List<Point>>();
        possibleAttackMoves = new Dictionary<Point, List<Point>>();
        possibleCastleMoves = new Dictionary<Point, List<Point>>();
        opponentPossibleAttackMoves = new Dictionary<Point, List<Point>>();
        check = IsKingChecked(ref positions);
        GenerateMoves();

        int moveCount = 0;
        moveCount += possibleMoves != null ? possibleMoves.Count : 0;
        moveCount += possibleAttackMoves != null ? possibleAttackMoves.Count : 0;
        moveCount += possibleCastleMoves != null ? possibleCastleMoves.Count : 0;

        if (moveCount == 0)
        {
            previousMoves = new Stack<Move>();
            previouslyEatenPieces = new Stack<string>();
            previouslyEatenPiecesMoveCounts = new Stack<int>();
            if (check)
            {
                if (currentPlayer == "white") Winner();
                if (currentPlayer == "black") Winner();
            }
            else
            {
                Winner(draw:true);
            }
        }
    }

    public void Update()
    {
        if(previousMoves.Count == 0 || promoting)
        {
            DisableUndoButton();
        }
        else
        {
            EnableUndoButton();
        }
        if (gameOver == true && Input.GetMouseButtonDown(0))
        {
            gameOver = false;

            //Using UnityEngine.SceneManagement is needed here
            SceneManager.LoadScene("Game"); //Restarts the game by loading the scene over again
        }
        else if(gameOver == false && check)
        {
            GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().enabled = true;
            GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().text = "Check";
        }
        else if(gameOver == false && !check)
        {
            GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().enabled = false;
        }
    }

    public void DisableButtons()
    {
        GameObject.FindGameObjectWithTag("KnightButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = false;
        GameObject.FindGameObjectWithTag("BishopButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = false;
        GameObject.FindGameObjectWithTag("RookButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = false;
        GameObject.FindGameObjectWithTag("QueenButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = false;

        GameObject.FindGameObjectWithTag("KnightButton").GetComponent<Button>().enabled = false;
        GameObject.FindGameObjectWithTag("KnightButton").GetComponent<Image>().enabled = false;
        GameObject.FindGameObjectWithTag("BishopButton").GetComponent<Button>().enabled = false;
        GameObject.FindGameObjectWithTag("BishopButton").GetComponent<Image>().enabled = false;
        GameObject.FindGameObjectWithTag("RookButton").GetComponent<Button>().enabled = false;
        GameObject.FindGameObjectWithTag("RookButton").GetComponent<Image>().enabled = false;
        GameObject.FindGameObjectWithTag("QueenButton").GetComponent<Button>().enabled = false;
        GameObject.FindGameObjectWithTag("QueenButton").GetComponent<Image>().enabled = false;
    }

    public void EnableButtons()
    {
        promoting = true;
        GameObject.FindGameObjectWithTag("KnightButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = true;
        GameObject.FindGameObjectWithTag("BishopButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = true;
        GameObject.FindGameObjectWithTag("RookButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = true;
        GameObject.FindGameObjectWithTag("QueenButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = true;

        GameObject.FindGameObjectWithTag("KnightButton").GetComponent<Button>().enabled = true;
        GameObject.FindGameObjectWithTag("KnightButton").GetComponent<Image>().enabled = true;
        GameObject.FindGameObjectWithTag("BishopButton").GetComponent<Button>().enabled = true;
        GameObject.FindGameObjectWithTag("BishopButton").GetComponent<Image>().enabled = true;
        GameObject.FindGameObjectWithTag("RookButton").GetComponent<Button>().enabled = true;
        GameObject.FindGameObjectWithTag("RookButton").GetComponent<Image>().enabled = true;
        GameObject.FindGameObjectWithTag("QueenButton").GetComponent<Button>().enabled = true;
        GameObject.FindGameObjectWithTag("QueenButton").GetComponent<Image>().enabled = true;
    }

    public void EnableUndoButton()
    {
        GameObject.FindGameObjectWithTag("UndoButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = true;
        GameObject.FindGameObjectWithTag("UndoButton").GetComponent<Image>().enabled = true;
        GameObject.FindGameObjectWithTag("UndoButton").GetComponent<Button>().enabled = true;
    }

    public void DisableUndoButton()
    {
        GameObject.FindGameObjectWithTag("UndoButton").GetComponent<Button>().GetComponentInChildren<Text>().enabled = false;
        GameObject.FindGameObjectWithTag("UndoButton").GetComponent<Image>().enabled = false;
        GameObject.FindGameObjectWithTag("UndoButton").GetComponent<Button>().enabled = false;
    }

    public void Winner(bool draw=false)
    {
        gameOver = true;

        //Using UnityEngine.UI is needed here
        if (draw)
        {
            GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().enabled = true;
            GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().text = "Game is over. It is a draw!";
        }
        else
        {
            string winner = currentPlayer == "black" ? "White" : "Black";
            GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().enabled = true;
            GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().text = "Game is over. " + winner + " is the winner";
        }
        GameObject.FindGameObjectWithTag("RestartText").GetComponent<Text>().enabled = true;
    }

    public void GenerateMoves()
    {
        //if (check) return;
        for (int iter_x = 0; iter_x < 8; iter_x++)
        {
            for (int iter_y = 0; iter_y < 8; iter_y++)
            {
                if(positions[iter_x, iter_y] != null && positions[iter_x, iter_y].GetComponent<ChessMan>().GetPlayer() == currentPlayer)
                {
                    GenerateMovesForAChessPiece( new Point(iter_x, iter_y) , positions[iter_x, iter_y].name);
                }
            }
        }
    }

    public void GenerateMovesForAChessPiece(Point p, string name)
    {
        switch (name)
        {
            case "black_queen":
            case "white_queen":
                LineMove(p, 1, 0, ref possibleMoves);
                LineMove(p, 0, 1, ref possibleMoves);
                LineMove(p, 1, 1, ref possibleMoves);
                LineMove(p, -1, 0, ref possibleMoves);
                LineMove(p, 0, -1, ref possibleMoves);
                LineMove(p, -1, -1, ref possibleMoves);
                LineMove(p, -1, 1, ref possibleMoves);
                LineMove(p, 1, -1, ref possibleMoves);
                break;
            case "black_knight":
            case "white_knight":
                LMove(p, ref possibleMoves);
                break;
            case "black_bishop":
            case "white_bishop":
                LineMove(p, 1, 1, ref possibleMoves);
                LineMove(p, 1, -1, ref possibleMoves);
                LineMove(p, -1, 1, ref possibleMoves);
                LineMove(p, -1, -1, ref possibleMoves);
                break;
            case "black_king":
            case "white_king":
                SurroundMove(p, ref possibleMoves);
                CastleMove(p, ref possibleMoves);
                break;
            case "black_rook":
            case "white_rook":
                LineMove(p, 1, 0, ref possibleMoves);
                LineMove(p, 0, 1, ref possibleMoves);
                LineMove(p, -1, 0, ref possibleMoves);
                LineMove(p, 0, -1, ref possibleMoves);
                break;
            case "black_pawn":
                PawnMove(p, p.GetX(), p.GetY() - 1, ref possibleMoves);
                break;
            case "white_pawn":
                PawnMove(p, p.GetX(), p.GetY() + 1, ref possibleMoves);
                break;
        }
    }

    public void AddToPossibleMoves(Point p, int x, int y, ref Dictionary<Point, List<Point>> possibleMoves)
    {
        if (possibleMoves.ContainsKey(p))
        {
            List<Point> pointsToMove = possibleMoves[p];
            pointsToMove.Add(new Point(x, y));
            possibleMoves[p] = pointsToMove;
        }
        else
        {
            List<Point> pointsToMove = new List<Point> { new Point(x, y) };
            possibleMoves.Add(p, pointsToMove);
        }
    }

    public void AddToPossibleAttackMoves(Point p, int x, int y)
    {
        if (possibleAttackMoves.ContainsKey(p))
        {
            List<Point> pointsToAttack = possibleAttackMoves[p];
            pointsToAttack.Add(new Point(x, y));
            possibleAttackMoves[p] = pointsToAttack;
        }
        else
        {
            List<Point> pointsToAttack = new List<Point> { new Point(x, y) };
            possibleAttackMoves.Add(p, pointsToAttack);
        }
    }

    public void AddToPossibleCastleMoves(Point p, int x, int y)
    {
        if (possibleCastleMoves.ContainsKey(p))
        {
            List<Point> pointsToCastle = possibleCastleMoves[p];
            pointsToCastle.Add(new Point(x, y));
            possibleCastleMoves[p] = pointsToCastle;
        }
        else
        {
            List<Point> pointsToCastle = new List<Point> { new Point(x, y) };
            possibleCastleMoves.Add(p, pointsToCastle);
        }
    }

    public void AddToOpponentPossibleAttackMoves(Point p, int x, int y)
    {
        if (opponentPossibleAttackMoves.ContainsKey(p))
        {
            List<Point> pointsToAttack = opponentPossibleAttackMoves[p];
            pointsToAttack.Add(new Point(x, y));
            opponentPossibleAttackMoves[p] = pointsToAttack;
        }
        else
        {
            List<Point> pointsToAttack = new List<Point> { new Point(x, y) };
            opponentPossibleAttackMoves.Add(p, pointsToAttack);
        }
    }

    public void LineMove(Point p, int xIncrement, int yIncrement, ref Dictionary<Point, List<Point>> possibleMoves)
    {
        int x = p.GetX() + xIncrement;
        int y = p.GetY() + yIncrement;

        while (PositionOnBoard(x, y) && GetPosition(x, y) == null)
        {
            if (IsThisMoveLegal(p, x, y))
            {
                AddToPossibleMoves(p, x, y, ref possibleMoves);
            }
            else if(!check)
                return;
            
            x += xIncrement;
            y += yIncrement;
            //Debug.Log("From X: " + p.GetX() + " Y: " + p.GetY() + " To X: " + x + " Y: " + y);
        }
        
        if (PositionOnBoard(x, y) && GetPosition(x, y).GetComponent<ChessMan>().GetPlayer() != currentPlayer && IsThisMoveLegal(p, x, y))
        {
            AddToPossibleAttackMoves(p, x, y);
        }
    }

    public void LMove(Point p, ref Dictionary<Point, List<Point>> possibleMoves)
    {
        PointMove(p, p.GetX() + 1, p.GetY() + 2, ref possibleMoves);
        PointMove(p, p.GetX() - 1, p.GetY() + 2, ref possibleMoves);
        PointMove(p, p.GetX() + 2, p.GetY() + 1, ref possibleMoves);
        PointMove(p, p.GetX() + 2, p.GetY() - 1, ref possibleMoves);
        PointMove(p, p.GetX() + 1, p.GetY() - 2, ref possibleMoves);
        PointMove(p, p.GetX() - 1, p.GetY() - 2, ref possibleMoves);
        PointMove(p, p.GetX() - 2, p.GetY() + 1, ref possibleMoves);
        PointMove(p, p.GetX() - 2, p.GetY() - 1, ref possibleMoves);
    }

    public void SurroundMove(Point p, ref Dictionary<Point, List<Point>> possibleMoves)
    {
        PointMove(p, p.GetX(), p.GetY() + 1, ref possibleMoves);
        PointMove(p, p.GetX(), p.GetY() - 1, ref possibleMoves);
        PointMove(p, p.GetX() - 1, p.GetY() + 0, ref possibleMoves);
        PointMove(p, p.GetX() - 1, p.GetY() - 1, ref possibleMoves);
        PointMove(p, p.GetX() - 1, p.GetY() + 1, ref possibleMoves);
        PointMove(p, p.GetX() + 1, p.GetY() + 0, ref possibleMoves);
        PointMove(p, p.GetX() + 1, p.GetY() - 1, ref possibleMoves);
        PointMove(p, p.GetX() + 1, p.GetY() + 1, ref possibleMoves);
    }

    public void CastleMove(Point p, ref Dictionary<Point, List<Point>> possibleMoves)
    {
        //check if king played before
        //check if rooks played before
        //check if king is under attack at any of the points king passes through
        int x = p.GetX();
        int y = p.GetY();

        if (check)
            return;

        if (currentPlayer == "white")
        {
            if (x == 4 && y == 0 && positions[4, 0].GetComponent<ChessMan>().GetMoveCount() == 0)
            {
                GameObject cp0 = positions[0, y];
                GameObject cp1 = positions[1, y];
                GameObject cp2 = positions[2, y];
                GameObject cp3 = positions[3, y];

                if (cp1 == null && cp2 == null && cp3 == null && cp0 != null && cp0.GetComponent<ChessMan>().name == "white_rook" && cp0.GetComponent<ChessMan>().GetMoveCount() == 0)
                {
                    if (IsThisMoveLegal(p, 2, y) && IsThisMoveLegal(p, 3, y))
                    {
                        AddToPossibleCastleMoves(p, 2, y);
                    }
                }

                GameObject cp5 = positions[x+1, y];
                GameObject cp6 = positions[x+2, y];
                GameObject cp7 = positions[x+3, y];

                if (cp5 == null && cp6 == null && cp7 != null && cp7.GetComponent<ChessMan>().name == "white_rook" && cp7.GetComponent<ChessMan>().GetMoveCount() == 0)
                {
                    if (IsThisMoveLegal(p, 6, y) && IsThisMoveLegal(p, 5, y))
                    {
                        AddToPossibleCastleMoves(p, 6, y);
                    }
                }
            }
        }
        else
        {
            if (x == 4 && y == 7 && positions[4, 7].GetComponent<ChessMan>().GetMoveCount() == 0)
            {
                GameObject cp0 = positions[0, y];
                GameObject cp1 = positions[1, y];
                GameObject cp2 = positions[2, y];
                GameObject cp3 = positions[3, y];

                if (cp1 == null && cp2 == null && cp3 == null && cp0 != null && cp0.GetComponent<ChessMan>().name == "black_rook" && cp0.GetComponent<ChessMan>().GetMoveCount() == 0)
                {
                    if (IsThisMoveLegal(p, 2, y) && IsThisMoveLegal(p, 3, y))
                    {
                        AddToPossibleCastleMoves(p, 2, y);
                    }
                }

                GameObject cp5 = positions[x + 1, y];
                GameObject cp6 = positions[x + 2, y];
                GameObject cp7 = positions[x + 3, y];

                if (cp5 == null && cp6 == null && cp7 != null && cp7.GetComponent<ChessMan>().name == "black_rook" && cp7.GetComponent<ChessMan>().GetMoveCount() == 0)
                {
                    if (IsThisMoveLegal(p, 5, y) && IsThisMoveLegal(p, 6, y))
                    {
                        AddToPossibleCastleMoves(p, 6, y);
                    }
                }
            }
        }
    }

    public void PointMove(Point p, int x, int y, ref Dictionary<Point, List<Point>> possibleMoves)
    {
        if (PositionOnBoard(x, y))
        {
            GameObject cp = positions[x, y];

            if (cp == null)
            {
                if (IsThisMoveLegal(p, x, y))
                {
                    AddToPossibleMoves(p, x, y, ref possibleMoves);
                }
            }
            else if (cp.GetComponent<ChessMan>().GetPlayer() != currentPlayer)
            {
               if (IsThisMoveLegal(p, x, y))
               {
                    AddToPossibleAttackMoves(p, x, y);
               }
            }
        }
    }

    public void PawnMove(Point p, int x, int y, ref Dictionary<Point, List<Point>> possibleMoves)
    {
        if (PositionOnBoard(x, y))
        {
            if (positions[x, y] == null)
            {
                if (IsThisMoveLegal(p, x, y))
                {
                    AddToPossibleMoves(p, x, y, ref possibleMoves);
                }
                    //Debug.Log("Old position of the pawn: (" + p.GetX() + ", " + p.GetY() + "), New position: (" + x + ", " + y + ")"  );
                    
                
                if (y == 5 && currentPlayer == "black" && PositionOnBoard(x, y - 1) && GetPosition(x, y - 1) == null)
                {
                    if (IsThisMoveLegal(p, x, y - 1))
                    {
                        AddToPossibleMoves(p, x, y - 1, ref possibleMoves);
                    }
                }
                if (y == 2 && currentPlayer == "white" && PositionOnBoard(x, y + 1) && GetPosition(x, y + 1) == null)
                {
                    if (IsThisMoveLegal(p, x, y + 1))
                    {
                        AddToPossibleMoves(p, x, y + 1, ref possibleMoves);
                    }
                }
            }
            
            if (PositionOnBoard(x + 1, y) && GetPosition(x + 1, y) != null && GetPosition(x + 1, y).GetComponent<ChessMan>().GetPlayer() != currentPlayer)
            {
                if (IsThisMoveLegal(p, x + 1, y))
                {
                    AddToPossibleAttackMoves(p, x + 1, y);
                }
            }

            if (PositionOnBoard(x - 1, y) && GetPosition(x - 1, y) != null && GetPosition(x - 1, y).GetComponent<ChessMan>().GetPlayer() != currentPlayer)
            {
                if (IsThisMoveLegal(p, x - 1, y))
                {
                    AddToPossibleAttackMoves(p, x - 1, y);
                }   
            }
        }
    }

    public bool IsKingChecked(ref GameObject[,] given_positions)
    {
        for (int iter_x = 0; iter_x < 8; iter_x++)
        {
            for (int iter_y = 0; iter_y < 8; iter_y++)
            {
                if (given_positions[iter_x, iter_y] != null && given_positions[iter_x, iter_y].GetComponent<ChessMan>().GetPlayer() != currentPlayer)
                {
                    GenerateAttacks(ref given_positions, given_positions[iter_x, iter_y].name, new Point(iter_x, iter_y));
                }
            }
        }

        int king_x = 0, king_y = 0;
        FindKing(currentPlayer, ref given_positions, ref king_x, ref king_y);
        Point kingsPos = new Point(king_x, king_y);

        if (opponentPossibleAttackMoves != null)
        {
            foreach (KeyValuePair<Point, List<Point>> entry in opponentPossibleAttackMoves)
            {
                foreach (Point p in entry.Value)
                {
                    if (kingsPos == p)
                        return true;
                }
            }
        }
        return false;
    }

        /*
        Debug.Log("For player " + currentPlayer + " number of attackable positions: " + opponentPossibleAttackMoves.Count);
        if (opponentPossibleAttackMoves != null)
        {
            foreach (KeyValuePair<Point, List<Point>> entry in opponentPossibleAttackMoves)
            {
                Debug.Log("For the piece in the coordinates x = " + entry.Key.GetX() + " and y = " + entry.Key.GetY() + " the points");
                foreach (Point p in entry.Value)
                {
                    Debug.Log("(" + p.GetX() + ", " + p.GetY() + ")");
                }
                Debug.Log("are attackable");
            }
        }*/    

    public bool IsThisMoveLegal(Point p, int newX, int newY)
    {
        opponentPossibleAttackMoves = new Dictionary<Point, List<Point>>();
        
        GameObject oldPos = positions[p.GetX(), p.GetY()];
        GameObject newPos = positions[newX, newY];

        positions[p.GetX(), p.GetY()] = null;
        positions[newX, newY] = oldPos;

        bool res = !IsKingChecked(ref positions);

        positions[p.GetX(), p.GetY()] = oldPos;
        positions[newX, newY] = newPos;

        return res;

        /*
        GameObject[,] newPositions = new GameObject[8, 8];
        for (int iter_x = 0; iter_x < 8; iter_x++)
        {
            for (int iter_y = 0; iter_y < 8; iter_y++)
            {
                if (iter_x == p.GetX() && iter_y == p.GetY())
                    newPositions[iter_x, iter_y] = null;
                else if (iter_x == newX && iter_y == newY)
                    newPositions[iter_x, iter_y] = GetPosition(p.GetX(), p.GetY());
                else
                    newPositions[iter_x, iter_y] = GetPosition(iter_x, iter_y);
            }
        }
        return !IsKingChecked(ref newPositions);
        */
    }

    public void FindKing(string kingsColor, ref GameObject[,] newPositions, ref int x, ref int y)
    {
        string kingsName = kingsColor == "white" ? "white_king" : "black_king";

        for (int iter_x = 0; iter_x < 8; iter_x++)
        {
            for (int iter_y = 0; iter_y < 8; iter_y++)
            {
                if (newPositions[iter_x, iter_y] != null && newPositions[iter_x, iter_y].name == kingsName)
                {
                    x = iter_x;
                    y = iter_y;
                    return;
                }
            }
        }
    }

    public void GenerateAttacks(ref GameObject[,] given_positions, string name, Point p)
    {
        switch (name)
        {
            case "black_queen":
            case "white_queen":
                LineAttackableMoves(ref given_positions, p, 1, 0);
                LineAttackableMoves(ref given_positions, p, 0, 1);
                LineAttackableMoves(ref given_positions, p, 1, 1);
                LineAttackableMoves(ref given_positions, p, -1, 0);
                LineAttackableMoves(ref given_positions, p, 0, -1);
                LineAttackableMoves(ref given_positions, p, -1, -1);
                LineAttackableMoves(ref given_positions, p, -1, 1);
                LineAttackableMoves(ref given_positions, p, 1, -1);
                break;
            case "black_knight":
            case "white_knight":
                LAttackableMoves(ref given_positions, p);
                break;
            case "black_bishop":
            case "white_bishop":
                LineAttackableMoves(ref given_positions, p, 1, 1);
                LineAttackableMoves(ref given_positions, p, 1, -1);
                LineAttackableMoves(ref given_positions, p, -1, 1);
                LineAttackableMoves(ref given_positions, p, -1, -1);
                break;
            case "black_king":
            case "white_king":
                SurroundAttackableMoves(ref given_positions, p);
                break;
            case "black_rook":
            case "white_rook":
                LineAttackableMoves(ref given_positions, p, 1, 0);
                LineAttackableMoves(ref given_positions, p, 0, 1);
                LineAttackableMoves(ref given_positions, p, -1, 0);
                LineAttackableMoves(ref given_positions, p, 0, -1);
                break;
            case "black_pawn":
                PawnAttackableMoves(ref given_positions, p, p.GetX(), p.GetY() - 1);
                break;
            case "white_pawn":
                PawnAttackableMoves(ref given_positions, p, p.GetX(), p.GetY() + 1);
                break;
        }
    }

    public void LineAttackableMoves(ref GameObject[,] given_positions, Point p, int xIncrement, int yIncrement)
    {
        int x = p.GetX() + xIncrement;
        int y = p.GetY() + yIncrement;

        while (PositionOnBoard(x, y))
        {
            if (given_positions[x, y] != null)
            {
                if (given_positions[x, y].GetComponent<ChessMan>().GetPlayer() == currentPlayer)
                {
                    AddToOpponentPossibleAttackMoves(p, x, y);
                    break;
                }
                else
                    break;
            }

            x += xIncrement;
            y += yIncrement;
        }
    }

    public void LAttackableMoves(ref GameObject[,] given_positions, Point p)
    {
        PointAttackableMoves(ref given_positions, p, p.GetX() + 1, p.GetY() + 2);
        PointAttackableMoves(ref given_positions, p, p.GetX() - 1, p.GetY() + 2);
        PointAttackableMoves(ref given_positions, p, p.GetX() + 2, p.GetY() + 1);
        PointAttackableMoves(ref given_positions, p, p.GetX() + 2, p.GetY() - 1);
        PointAttackableMoves(ref given_positions, p, p.GetX() + 1, p.GetY() - 2);
        PointAttackableMoves(ref given_positions, p, p.GetX() - 1, p.GetY() - 2);
        PointAttackableMoves(ref given_positions, p, p.GetX() - 2, p.GetY() + 1);
        PointAttackableMoves(ref given_positions, p, p.GetX() - 2, p.GetY() - 1);
    }

    public void SurroundAttackableMoves(ref GameObject[,] given_positions, Point p)
    {
        PointAttackableMoves(ref given_positions, p, p.GetX(), p.GetY() + 1);
        PointAttackableMoves(ref given_positions, p, p.GetX(), p.GetY() - 1);
        PointAttackableMoves(ref given_positions, p, p.GetX() - 1, p.GetY() + 0);
        PointAttackableMoves(ref given_positions, p, p.GetX() - 1, p.GetY() - 1);
        PointAttackableMoves(ref given_positions, p, p.GetX() - 1, p.GetY() + 1);
        PointAttackableMoves(ref given_positions, p, p.GetX() + 1, p.GetY() + 0);
        PointAttackableMoves(ref given_positions, p, p.GetX() + 1, p.GetY() - 1);
        PointAttackableMoves(ref given_positions, p, p.GetX() + 1, p.GetY() + 1);
    }

    public void PointAttackableMoves(ref GameObject[,] given_positions, Point p, int x, int y)
    {
        if (PositionOnBoard(x, y) && given_positions[x, y] != null && given_positions[x, y].GetComponent<ChessMan>().GetPlayer() == currentPlayer)
        {
            AddToOpponentPossibleAttackMoves(p, x, y);
        }
    }

    public void PawnAttackableMoves(ref GameObject[,] given_positions, Point p, int x, int y)
    {
        if (PositionOnBoard(x, y))
        {
            if (PositionOnBoard(x + 1, y) && given_positions[x + 1, y] != null && given_positions[x + 1, y].GetComponent<ChessMan>().GetPlayer() == currentPlayer)
            {
                AddToOpponentPossibleAttackMoves(p, x + 1, y);
            }
            if (PositionOnBoard(x - 1, y) && given_positions[x - 1, y] != null && given_positions[x - 1, y].GetComponent<ChessMan>().GetPlayer() == currentPlayer)
            {
                AddToOpponentPossibleAttackMoves(p, x - 1, y);
            }
        }
    }
}