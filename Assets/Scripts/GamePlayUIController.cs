using System;
using UnityEditor.U2D.Path;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayUIController : MonoBehaviour
{
    public delegate void OneOfThePromotionOptionIsSelected(int index);
    public static event OneOfThePromotionOptionIsSelected PromotionOptionSelected;

    public delegate void UndoButtonClicked();
    public static event UndoButtonClicked UndoButtonIsClicked;
    public delegate void RestartButtonClicked();
    public static event RestartButtonClicked RestartButtonIsClicked;
    public delegate void ExitButtonClicked();
    public static event ExitButtonClicked ExitButtonIsClicked;

    [SerializeField]
    private GameObject movePanel;
    [SerializeField]
    private ScrollRect scrollRect;
    [SerializeField]
    private GameObject chesspiece;
    [SerializeField]
    private Sprite whiteBoardBorder, blackBoardBorder;
    [SerializeField]
    private GameObject boardBorder;
    [SerializeField]
    private GameObject rookButton;
    [SerializeField]
    private GameObject queenButton;
    [SerializeField]
    private GameObject bishopButton;
    [SerializeField]
    private GameObject knightButton;
    [SerializeField]
    private GameObject undoButton;
    [SerializeField]
    private GameObject restartButton;
    [SerializeField]
    private GameObject exitButton;
    [SerializeField]
    private Transform moveHolder;
    [SerializeField]
    private GameObject checkText;
    [SerializeField]
    private GameObject gameoverPanel;
    [SerializeField]
    private GameSetter gameSetter;
    [SerializeField]
    private Transform timerTextWhite;
    [SerializeField]
    private Transform timerTextBlack;
    [SerializeField]
    private Transform timerPanelWhite;
    [SerializeField]
    private Transform timerPanelBlack;

    private float timeLimit = 0;
    private float timeElapsedWhite = 0;
    private float timeElapsedBlack = 0;
    private bool timerIsOn = false;

    private void Update()
    {
        if (timerIsOn)
        {
            if (gameSetter.GetCurrentPlayer() == Piece.PieceColor.White)
            {
                timerPanelWhite.gameObject.SetActive(true);
                timerPanelBlack.gameObject.SetActive(false);
                timeElapsedWhite += Time.deltaTime;
                if (timeLimit - timeElapsedWhite < 0)
                {
                    gameSetter.TimeOut("white");
                    timerTextWhite.GetComponent<Text>().text = FormatTime(0).ToString();
                }
                else
                {
                    timerTextWhite.GetComponent<Text>().text = FormatTime(timeLimit - timeElapsedWhite).ToString();
                }
            }
            else
            {
                timerPanelWhite.gameObject.SetActive(false);
                timerPanelBlack.gameObject.SetActive(true);
                timeElapsedBlack += Time.deltaTime;
                if (timeLimit - timeElapsedBlack < 0)
                {
                    gameSetter.TimeOut("black");
                    timerTextBlack.GetComponent<Text>().text = FormatTime(0).ToString();
                }
                else
                {
                    timerTextBlack.GetComponent<Text>().text = FormatTime(timeLimit - timeElapsedBlack).ToString();
                }
            }
        }
    }

    public void OnPromotionButtonPressed(int index)
    {
        PromotionOptionSelected(index);
    }

    public void OnRestartButtonClicked()
    {
        RestartButtonIsClicked();
    }

    public void OnExitButtonClicked()
    {
        ExitButtonIsClicked();
    }

    public void OnUndoButtonClicked()
    {
        UndoButtonIsClicked();
    }

    public void Initialize(bool playAsWhite)
    {
        boardBorder.GetComponent<SpriteRenderer>().sprite = playAsWhite ? whiteBoardBorder : blackBoardBorder;
    }

    public GameObject[] CreateWholeChessBoardPieces()
    {
        return new GameObject[] { Create("white_rook", 0, 0, 0), Create("white_knight", 1, 0, 1),
            Create("white_bishop", 2, 0, 2), Create("white_queen", 3, 0, 3), Create("white_king", 4, 0, 4),
            Create("white_bishop", 5, 0, 5), Create("white_knight", 6, 0, 6), Create("white_rook", 7, 0, 7),
            Create("white_pawn", 0, 1, 8), Create("white_pawn", 1, 1, 9), Create("white_pawn", 2, 1, 10),
            Create("white_pawn", 3, 1, 11), Create("white_pawn", 4, 1, 12), Create("white_pawn", 5, 1, 13),
            Create("white_pawn", 6, 1, 14), Create("white_pawn", 7, 1, 15) , Create("black_rook", 0, 7, 16), Create("black_knight",1,7, 17),
            Create("black_bishop",2,7, 18), Create("black_queen",3,7, 19), Create("black_king",4,7, 20),
            Create("black_bishop",5,7, 21), Create("black_knight",6,7, 22), Create("black_rook",7,7, 23),
            Create("black_pawn", 0, 6, 24), Create("black_pawn", 1, 6, 25), Create("black_pawn", 2, 6, 26),
            Create("black_pawn", 3, 6, 27), Create("black_pawn", 4, 6, 28), Create("black_pawn", 5, 6, 29),
            Create("black_pawn", 6, 6, 30), Create("black_pawn", 7, 6, 31) };
    }

    private GameObject Create(string pieceName, int x, int y, uint id)
    {
        string[] playerInfo = pieceName.Split('_');
        string player = playerInfo[0];

        //Transform uiTrans = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform;
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
        Piece cm = obj.GetComponent<Piece>(); //We have access to the GameObject, we need the script
        cm.name = pieceName; //This is a built in variable that Unity has, so we did not have to declare it before
        cm.player = player == "white" ? Piece.PieceColor.White : Piece.PieceColor.Black;
        switch (playerInfo[1])
        {
            case "knight":
                cm.type = Piece.PieceType.Knight;
                break;
            case "bishop":
                cm.type = Piece.PieceType.Bishop;
                break;
            case "rook":
                cm.type = Piece.PieceType.Rook;
                break;
            case "queen":
                cm.type = Piece.PieceType.Queen;
                break;
            case "king":
                cm.type = Piece.PieceType.King;
                break;
            case "pawn":
                cm.type = Piece.PieceType.Pawn;
                break;
        }
        cm.id = id;
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate(); //It has everything set up so it can now Activate()
        return obj;
    }

    public void EnableUndoButton()
    {
        undoButton.SetActive(true);
    }

    public void DisableUndoButton()
    {
        undoButton.SetActive(false);
    }

    public void EnableRestartButton()
    {
        restartButton.SetActive(true);
    }

    public void DisableRestartButton()
    {
        restartButton.SetActive(false);
    }

    public void EnableExitButton()
    {
        exitButton.SetActive(true);
    }

    public void DisableExitButton()
    {
        exitButton.SetActive(false);
    }

    public void EnablePromotionButtons()
    {
        knightButton.SetActive(true);
        bishopButton.SetActive(true);
        rookButton.SetActive(true);
        queenButton.SetActive(true);
    }

    public void DisablePromotionButtons()
    {
        knightButton.SetActive(false);
        bishopButton.SetActive(false);
        rookButton.SetActive(false);
        queenButton.SetActive(false);
    }

    public void AddMoveToUI(string moveLog, int twoFoldMoveCount, bool gameOver, int moveNumber)
    {
       if (gameOver)
        {
            GameObject panel = Instantiate(movePanel, moveHolder);
            var texts = panel.GetComponentsInChildren<Text>();
            texts[1].text = moveLog;
            return;
        }
        if (twoFoldMoveCount % 2 == 1)
        {
            GameObject panel = Instantiate(movePanel, moveHolder);
            //movePanelLogs.Add(panel);
            var texts = panel.GetComponentsInChildren<Text>();
            texts[0].text = moveNumber + ".";
            texts[1].text = moveLog;
            panel.transform.SetAsLastSibling();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            var texts = moveHolder.GetChild(moveHolder.childCount - 1).gameObject.GetComponentsInChildren<Text>();
            texts[2].text = moveLog;
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void RemoveMoveFromUI(int twoFoldMoveCount)
    {
         if (twoFoldMoveCount % 2 == 1)
        {
            int childCount = moveHolder.childCount;
            if (childCount > 0)
            {
                Transform lastChild = moveHolder.GetChild(twoFoldMoveCount / 2);
                lastChild.GetComponentsInChildren<Text>()[2].text = "";
            }
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            int childCount = moveHolder.childCount;
            if (childCount > 0)
            {
                Transform lastChild = moveHolder.GetChild(childCount - 1);
                Destroy(lastChild.gameObject);
            }
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ActivateCheckText(string text)
    {
        checkText.SetActive(true);
        checkText.GetComponent<Text>().text = text;
    }

    public void DeActivateCheckText()
    {
        checkText.SetActive(false);
    }

    public void EnableGameOverPanel(string text, bool drawn)
    {
        gameoverPanel.SetActive(true);
        var texts = gameoverPanel.GetComponentsInChildren<Text>();
        texts[0].enabled = true;
        texts[1].enabled = true;
        if (!drawn)
            texts[1].text = "Winner is " + text;
        else
            texts[1].text = "Draw!!";
    }

    public void DisableGameOverPanel()
    {
        gameoverPanel.SetActive(false);
    }

    public string FormatTime(float time)
    {
        int hours = (int)time / 3600;
        int minutes = (int)time / 60;

        if (hours > 0)
            return FormatNumber(hours) + ":" + FormatNumber(minutes) + FormatNumber((int)time % 60) + ":" + FormatNumber((int)((time - (int)time) * 100));
        else
            return FormatNumber(minutes) + ":" + FormatNumber((int)time % 60) + ":" + FormatNumber((int)((time - (int)time) * 100));
    }

    private string FormatNumber(int number)
    {
        if (number < 10)
            return "0" + number.ToString();
        else
            return number.ToString();
    }

    public void StartTimers(float time)
    {
        timeLimit = time;
        timerTextBlack.GetComponent<Text>().text = FormatTime(timeLimit).ToString();
        timerTextWhite.GetComponent<Text>().text = FormatTime(timeLimit).ToString();
        timerIsOn = true;
    }

    public void StopTimers()
    {
        timerIsOn = false;
        timeElapsedBlack = 0;
        timeElapsedWhite = 0;
    }
}
