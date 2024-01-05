using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class MovePlate : MonoBehaviour
{
    //Some functions will need reference to the controller
    public GameObject controller;

    public Move assignedMove;

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
            gameObject.GetComponent<SpriteRenderer>().color = new UnityEngine.Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
        else if (castle)
        {
            gameObject.GetComponent<SpriteRenderer>().color = new UnityEngine.Color(0.0f, 1.0f, 0.0f, 1.0f);
        }
        else
        {
            gameObject.GetComponent<SpriteRenderer>().color = new UnityEngine.Color(0.0f, 0.0f, 1.0f, 1.0f);
        }
    }

    public void OnMouseUp()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        GameSetter sc = controller.GetComponent<GameSetter>();
        if (sc.IsGameOver())
        {
            reference.GetComponent<Piece>().DestroyMovePlates();
            return;
        }
        sc.MakeMoveOnBoard(assignedMove);

        //Destroy the move plates including self
        reference.GetComponent<Piece>().DestroyMovePlates();
    }

    public void HandleDrop()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        GameSetter sc = controller.GetComponent<GameSetter>();
        if (sc.IsGameOver())
        {
            reference.GetComponent<Piece>().DestroyMovePlates();
            return;
        }
        sc.MakeMoveOnBoard(assignedMove);


        //Destroy the move plates including self
        reference.GetComponent<Piece>().DestroyMovePlates();
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