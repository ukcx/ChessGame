using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.EventSystems;
using UnityEditor.Experimental.GraphView;

public class Piece : MonoBehaviour
{
    public enum PieceType
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    public enum PieceColor
    {
        White,
        Black
    }

    public PieceType type;
    public PieceColor player;
    //References to objects in our Unity Scene
    public GameObject movePlate;
    private GameSetter controller;
    public uint id;
    private Vector3 boardPos;

    private bool isDragging = false;
    private Vector3 offset;

    //private static Point lastClickedPiece = new Point(-1, -1);
    private static uint lastClickedPieceId = 0;

    private List<MovePlate> moveplateRefs = new List<MovePlate>();
    private bool playAsWhite = true;
    //Position for this Chesspiece on the Board
    //The correct position will be set later
    private int xBoard = -1;
    private int yBoard = -1;

    //Variable for keeping track of the player it belongs to "black" or "white"
    private int moveCount = 0;
    private int lastMoveNumber = 0;

    //References to all the possible Sprites that this Chesspiece could be
    public Sprite black_queen, black_knight, black_bishop, black_king, black_rook, black_pawn;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_rook, white_pawn;

    private void Awake()
    {
        boardPos = GameObject.FindGameObjectWithTag("BoardBorder").transform.position;
        playAsWhite = GameManager.instance.isWhiteSelected;
        controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameSetter>();
    }

    public void Activate()
    {
        
        //Take the instantiated location and adjust transform
        SetCoords();
        //Choose correct sprite based on piece's name
        switch (this.name)
        {
            case "black_queen": this.GetComponent<SpriteRenderer>().sprite = black_queen; player = PieceColor.Black; break;
            case "black_knight": this.GetComponent<SpriteRenderer>().sprite = black_knight; player = PieceColor.Black; break;
            case "black_bishop": this.GetComponent<SpriteRenderer>().sprite = black_bishop; player = PieceColor.Black; break;
            case "black_king": this.GetComponent<SpriteRenderer>().sprite = black_king; player = PieceColor.Black; break;
            case "black_rook": this.GetComponent<SpriteRenderer>().sprite = black_rook; player = PieceColor.Black; break;
            case "black_pawn": this.GetComponent<SpriteRenderer>().sprite = black_pawn; player = PieceColor.Black; break;
            case "white_queen": this.GetComponent<SpriteRenderer>().sprite = white_queen; player = PieceColor.White; break;
            case "white_knight": this.GetComponent<SpriteRenderer>().sprite = white_knight; player = PieceColor.White; break;
            case "white_bishop": this.GetComponent<SpriteRenderer>().sprite = white_bishop; player = PieceColor.White; break;
            case "white_king": this.GetComponent<SpriteRenderer>().sprite = white_king; player = PieceColor.White; break;
            case "white_rook": this.GetComponent<SpriteRenderer>().sprite = white_rook; player = PieceColor.White; break;
            case "white_pawn": this.GetComponent<SpriteRenderer>().sprite = white_pawn; player = PieceColor.White; break;
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
        bool isDroppable = false;
        //Debug.Log("mouse up");
        foreach(MovePlate mp in moveplateRefs)
        {
            if(Vector2.Distance(mp.transform.position, this.transform.position) < (1.138f / 2))
            {
                mp.HandleDrop();
                isDroppable = true;
            }
        }
        if(!isDroppable || controller.GetComponent<GameSetter>().IsGameOver())
        {
            SetCoords();
        }
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            if (controller.GetComponent<GameSetter>().IsGameOver())
            {
                SetCoords();
                return;
            }
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
            transform.position = newPosition;
            controller.SetLastClickedPiecePos(new Point(-1, -1));
        }
    }

    private void OnMouseDown()
    {
        isDragging = true;
        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (controller.IsGameOver()) { return; }   
        if (controller.GetAIsTurn()) { return; }
        if (controller.IsPromoting()) { return; }
        if (controller.GetCurrentPlayer() != player) { return; }
        if (controller.GetLastClickedPiecePos() != new Point(xBoard, yBoard))
        {
            //Remove all moveplates relating to previously selected piece
            DestroyMovePlates();

            controller.SetLastClickedPiecePos(new Point(xBoard, yBoard));
            lastClickedPieceId = id;

            //Create new MovePlates
            InitiateMovePlates();
        }
        else if (controller.GetLastClickedPiecePos() == new Point(xBoard, yBoard))
        {
            controller.SetLastClickedPiecePos(new Point(-1, -1));
            DestroyMovePlates();
        }
    }

    public static uint GetLastClickedPieceId()
    {
        return lastClickedPieceId;
    }

    public void InitiateMovePlates()
    {
        GameSetter sc = controller.GetComponent<GameSetter>();
        Dictionary<uint, List<Move>> allMoves = sc.possibleMoves;
        List<Move> moves = new List<Move>();
        if (!allMoves.ContainsKey(id))
            return;
        moves = allMoves[id];

        HashSet<Point> promotePts = new HashSet<Point>();
        foreach (Move m in moves)
        {
            if(m.Promote != null && promotePts.Contains(m.To)) { }
            else if(m.Promote != null) { 
                promotePts.Add(m.To);
                MovePlateSpawn(m, m.To.X, m.To.Y, m.Attack != null, m.Castle != null);
            }
            else {
                MovePlateSpawn(m, m.To.X, m.To.Y, m.Attack != null, m.Castle != null);
            }
        }
    }

    public void DestroyMovePlates()
    {
        //Destroy old MovePlates
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
        {
            Destroy(movePlates[i]); //Be careful with this function "Destroy" it is asynchronous
        }
        moveplateRefs = new List<MovePlate>();
    }

    private int InvertPosIfNecessary(int x) {
        if (playAsWhite)
            return x;
        else
            return 7 - x;
    }

    public void MovePlateSpawn(Move move, int matrixX, int matrixY, bool attacking, bool castling)
    {
        //Get the board value in order to convert to xy coords
        float x = InvertPosIfNecessary(matrixX);
        float y = InvertPosIfNecessary(matrixY);

        //Adjust by variable offset
        x *= 1.138f;
        y *= 1.138f;

        //Add constants (pos 0,0)
        x += -3.8543f;
        y += -3.8301f;

        x += boardPos.x;
        y += boardPos.y;

        //Set actual unity values
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        moveplateRefs.Add(mpScript);
        mpScript.attack = attacking;
        mpScript.castle = castling;
        mpScript.assignedMove = move;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public void SetCoords()
    {
        
        //Get the board value in order to convert to xy coords
        float x = InvertPosIfNecessary(xBoard);
        float y = InvertPosIfNecessary(yBoard);

        //Adjust by variable offset
        x *= 1.138f;
        y *= 1.138f;

        //Add constants (pos 0,0)
        x += -3.8543f;
        y += -3.8301f;

        x += boardPos.x;
        y += boardPos.y;

        //Set actual unity values
        this.transform.position = new Vector3(x, y, -1.0f);
    }

    public void SetLastMoveNumber(int moveNumber)
    {
        lastMoveNumber = moveNumber;
    }

    public int GetLastMoveNumber()
    {
        return lastMoveNumber;
    }

    public int GetXBoard()
    {
        return xBoard;
    }

    public int GetYBoard()
    {
        return yBoard;
    }

    public void SetXBoard(int x)
    {
        xBoard = x;
    }

    public void SetYBoard(int y)
    {
        yBoard = y;
    }

    public PieceColor GetPlayer()
    {
        return player;
    }

    public int GetMoveCount()
    {
        return moveCount;
    }

    public void SetMoveCount(int count)
    {
        moveCount = count;
    }

    public void IncreaseMoveCount(int incerement)
    {
        moveCount += incerement;
    }
}
