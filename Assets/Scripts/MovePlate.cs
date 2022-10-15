using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlate : MonoBehaviour
{
    //Some functions will need reference to the controller
    public GameObject controller;

    //The Chesspiece that was tapped to create this MovePlate
    GameObject reference = null;

    //Location on the board
    int matrixX;
    int matrixY;

    //false: movement, true: attacking
    public bool attack = false;

    public bool castle = false;

    public void Start()
    {
        if (attack)
        {
            //Set to red
            gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
        if (castle)
        {
            gameObject.GetComponent<SpriteRenderer>().color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
        }
    }

    public void OnMouseUp()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        Game sc = controller.GetComponent<Game>();

        //Destroy the victim Chesspiece
        if (attack)
        {
            GameObject cp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);

            Destroy(cp);
        }

        else if (castle) 
        {
            if (sc.GetPosition(reference.GetComponent<ChessMan>().GetXBoard(), reference.GetComponent<ChessMan>().GetYBoard()).GetComponent<ChessMan>().GetPlayer() == "white")
            {
                if (matrixX == 2)
                {
                    GameObject rook = sc.GetPosition(0, 0);
                    rook.GetComponent<ChessMan>().SetXBoard(3);
                    rook.GetComponent<ChessMan>().SetCoords();
                    rook.GetComponent<ChessMan>().IncreaseMoveCount(1);
                    controller.GetComponent<Game>().SetPosition(rook);
                    controller.GetComponent<Game>().SetPositionEmpty(0, 0);
                }
                else 
                {
                    GameObject rook = sc.GetPosition(7, 0);
                    rook.GetComponent<ChessMan>().SetXBoard(5);
                    rook.GetComponent<ChessMan>().SetCoords();
                    rook.GetComponent<ChessMan>().IncreaseMoveCount(1);
                    controller.GetComponent<Game>().SetPosition(rook);
                    controller.GetComponent<Game>().SetPositionEmpty(7, 0);
                }
            }
            else 
            {
                if (matrixX == 2)
                {
                    GameObject rook = sc.GetPosition(0, 7);
                    rook.GetComponent<ChessMan>().SetXBoard(3);
                    rook.GetComponent<ChessMan>().SetCoords();
                    rook.GetComponent<ChessMan>().IncreaseMoveCount(1);
                    controller.GetComponent<Game>().SetPosition(rook);
                    controller.GetComponent<Game>().SetPositionEmpty(0, 7);
                }
                else
                {
                     GameObject rook = sc.GetPosition(7, 7);
                     rook.GetComponent<ChessMan>().SetXBoard(5);
                     rook.GetComponent<ChessMan>().SetCoords();
                     rook.GetComponent<ChessMan>().IncreaseMoveCount(1);
                     controller.GetComponent<Game>().SetPosition(rook);
                     controller.GetComponent<Game>().SetPositionEmpty(7, 7);
                }       
            }
        }

        bool promote = false;
        if ((reference.GetComponent<ChessMan>().name == "white_pawn" && matrixY == 7) || (reference.GetComponent<ChessMan>().name == "black_pawn" && matrixY == 0))
        {
            promote = true;
        }

        //Add this move to previously made move stack
        controller.GetComponent<Game>().AddToPreviousMoves(new Move(new Point(reference.GetComponent<ChessMan>().GetXBoard(),
            reference.GetComponent<ChessMan>().GetYBoard()), new Point(matrixX, matrixY), attack, castle, promote),
            attack ? controller.GetComponent<Game>().GetPosition(matrixX, matrixY).name : "",
            attack ? controller.GetComponent<Game>().GetPosition(matrixX, matrixY).GetComponent<ChessMan>().GetMoveCount() : 0,
            promote ? reference.GetComponent<ChessMan>().name : "",
            promote ? reference.GetComponent<ChessMan>().GetMoveCount() : 0);

        if (promote)
        {
            Destroy(reference);
            sc.EnableButtons();
            sc.SetPositionToAddNewPiece(new Point(matrixX, matrixY));
            reference.GetComponent<ChessMan>().DestroyMovePlates();
            return;
        }
        
        //Set the Chesspiece's original location to be empty
        controller.GetComponent<Game>().SetPositionEmpty(reference.GetComponent<ChessMan>().GetXBoard(),
            reference.GetComponent<ChessMan>().GetYBoard());

        //set reference piece as moved
        reference.GetComponent<ChessMan>().IncreaseMoveCount(1);
        //Move reference chess piece to this position
        reference.GetComponent<ChessMan>().SetXBoard(matrixX);
        reference.GetComponent<ChessMan>().SetYBoard(matrixY);
        reference.GetComponent<ChessMan>().SetCoords();

        //Update the matrix
        controller.GetComponent<Game>().SetPosition(reference);

        //Switch Current Player
        controller.GetComponent<Game>().NextTurn();

        //Destroy the move plates including self
        reference.GetComponent<ChessMan>().DestroyMovePlates();
    }

    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj)
    {
        reference = obj;
    }

    public GameObject GetReference()
    {
        return reference;
    }
}