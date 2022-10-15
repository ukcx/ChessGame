using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChessMan : MonoBehaviour
{
    //References to objects in our Unity Scene
    public GameObject controller;
    public GameObject movePlate;

    //Position for this Chesspiece on the Board
    //The correct position will be set later
    private int xBoard = -1;
    private int yBoard = -1;

    //Variable for keeping track of the player it belongs to "black" or "white"
    private string player;
    private int moveCount = 0;
    private Point lastClickedPiece = new Point(-1, -1);

    //References to all the possible Sprites that this Chesspiece could be
    public Sprite black_queen, black_knight, black_bishop, black_king, black_rook, black_pawn;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_rook, white_pawn;

    public void Activate()
    {
        //Get the game controller
        controller = GameObject.FindGameObjectWithTag("GameController");

        //Take the instantiated location and adjust transform
        SetCoords();

        //Choose correct sprite based on piece's name
        switch (this.name)
        {
            case "black_queen": this.GetComponent<SpriteRenderer>().sprite = black_queen; player = "black"; break;
            case "black_knight": this.GetComponent<SpriteRenderer>().sprite = black_knight; player = "black"; break;
            case "black_bishop": this.GetComponent<SpriteRenderer>().sprite = black_bishop; player = "black"; break;
            case "black_king": this.GetComponent<SpriteRenderer>().sprite = black_king; player = "black"; break;
            case "black_rook": this.GetComponent<SpriteRenderer>().sprite = black_rook; player = "black"; break;
            case "black_pawn": this.GetComponent<SpriteRenderer>().sprite = black_pawn; player = "black"; break;
            case "white_queen": this.GetComponent<SpriteRenderer>().sprite = white_queen; player = "white"; break;
            case "white_knight": this.GetComponent<SpriteRenderer>().sprite = white_knight; player = "white"; break;
            case "white_bishop": this.GetComponent<SpriteRenderer>().sprite = white_bishop; player = "white"; break;
            case "white_king": this.GetComponent<SpriteRenderer>().sprite = white_king; player = "white"; break;
            case "white_rook": this.GetComponent<SpriteRenderer>().sprite = white_rook; player = "white"; break;
            case "white_pawn": this.GetComponent<SpriteRenderer>().sprite = white_pawn; player = "white"; break;
        }
    }

    public void SetCoords()
    {
        //Get the board value in order to convert to xy coords
        float x = xBoard;
        float y = yBoard;

        //Adjust by variable offset
        x *= 0.66f;
        y *= 0.66f;

        //Add constants (pos 0,0)
        x += -2.3f;
        y += -2.3f;

        //Set actual unity values
        this.transform.position = new Vector3(x, y, -1.0f);
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

    public string GetPlayer() 
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

    private void OnMouseUp()
    {
        if (!controller.GetComponent<Game>().IsGameOver() && controller.GetComponent<Game>().GetCurrentPlayer() == player && lastClickedPiece != new Point(xBoard,yBoard))
        {
            //Remove all moveplates relating to previously selected piece
            DestroyMovePlates();

            lastClickedPiece = new Point(xBoard, yBoard);

            //Create new MovePlates
            InitiateMovePlates();
        }
        else if (!controller.GetComponent<Game>().IsGameOver() && controller.GetComponent<Game>().GetCurrentPlayer() == player && lastClickedPiece == new Point(xBoard, yBoard))
        {
            lastClickedPiece = new Point(-1, -1);
            DestroyMovePlates();
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
    }

    public void InitiateMovePlates()
    {
        Game sc = controller.GetComponent<Game>();
        Dictionary<Point, List<Point>> moves = sc.possibleMoves;
        Dictionary<Point, List<Point>> attacks = sc.possibleAttackMoves;
        Dictionary<Point, List<Point>> castlings = sc.possibleCastleMoves;
        Point thisPoint = new Point(this.xBoard, this.yBoard);

        if (moves != null && moves.ContainsKey(thisPoint))
        {
            foreach(Point p in moves[thisPoint])
            {
                MovePlateSpawn(p.GetX(), p.GetY());
            }
        }
        if (attacks != null && attacks.ContainsKey(thisPoint))
        {
            foreach (Point p in attacks[thisPoint])
            {
                MovePlateAttackSpawn(p.GetX(), p.GetY());
            }
        }
        if (castlings != null && castlings.ContainsKey(thisPoint))
        {
            foreach (Point p in castlings[thisPoint])
            {
                MovePlateSpawn(p.GetX(), p.GetY(), isThisCastling:true);
            }
        }
    }

    public void MovePlateSpawn(int matrixX, int matrixY, bool isThisCastling = false)
    {
        //Get the board value in order to convert to xy coords
        float x = matrixX;
        float y = matrixY;

        //Adjust by variable offset
        x *= 0.66f;
        y *= 0.66f;

        //Add constants (pos 0,0)
        x += -2.3f;
        y += -2.3f;

        //Set actual unity values
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.castle = isThisCastling;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public void SetLastClickedPiece(Point p)
    {
        lastClickedPiece = p;
    }

    public void MovePlateAttackSpawn(int matrixX, int matrixY)
    {
        //Get the board value in order to convert to xy coords
        float x = matrixX;
        float y = matrixY;

        //Adjust by variable offset
        x *= 0.66f;
        y *= 0.66f;

        //Add constants (pos 0,0)
        x += -2.3f;
        y += -2.3f;

        //Set actual unity values
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }
}