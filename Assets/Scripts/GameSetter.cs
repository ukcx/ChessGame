using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSetter : MonoBehaviour
{
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] allPieces = new GameObject[32];
    public Dictionary<uint, List<Move>> possibleMoves;
    private Stack<Move> moveStack = new Stack<Move>();
    private Stack<string> moveLogStack = new Stack<string>();
    
    public uint WHITE_KING_ID = 4;
    public uint BLACK_KING_ID = 20;

    public bool moveReady = false;
    private float aiTimeDelay = 3.0f;
    private string winner = "";
    public bool aiTimerOn = false;
    public static float aiTimer = 0f;
    private bool aisTurn = false;
    public Move aiMove;

    public int moveNumber = 0;
    private int twoFoldMoveNumber = 0;
    private Stack<int> lastBreakMoveNumber = new Stack<int>();

    public Dictionary<uint, Point> idToPos = new Dictionary<uint, Point>();
    private Dictionary<string, int> boardLogCounts = new Dictionary<string, int>();

    //current turn
    private Piece.PieceColor currentPlayer = Piece.PieceColor.White;

    [SerializeField]
    private GamePlayUIController gamePlayUIController;
    [SerializeField]
    private MoveGenerator moveGenerator;
    [SerializeField]
    private AudioController audioController;
    [SerializeField]
    private AIPlayer aiPlayer;

    private Point lastClickedPiecePos;
    private Move lastMove;
    private string lastMoveLog;

    private int depthOfAI = 3;
    
    private bool whiteShortCastleRight = true;
    private bool whiteLongCastleRight = true;
    private bool blackShortCastleRight = true;
    private bool blackLongCastleRight = true;
    public bool check = false;
    public bool checkMated = false;
    public bool drawn = false;
    private bool promotionInProcess = false;
    private string promotingTo = "";
    private bool isOpponentHuman = false;
    private bool playAsWhite = true;
    private float timeLimit = 0;

    //Game Ending
    private bool gameOver = false;

    private void OnEnable()
    {
        GamePlayUIController.PromotionOptionSelected += ReplacePawn;
        GamePlayUIController.UndoButtonIsClicked += UndoMove;
        GamePlayUIController.RestartButtonIsClicked += RestartGame;
        GamePlayUIController.ExitButtonIsClicked += ExitGame;
    }

    private void OnDisable()
    {
        GamePlayUIController.PromotionOptionSelected -= ReplacePawn;
        GamePlayUIController.UndoButtonIsClicked -= UndoMove;
        GamePlayUIController.RestartButtonIsClicked -= RestartGame;
        GamePlayUIController.ExitButtonIsClicked -= ExitGame;
    }

    //Unity calls this right when the game starts, there are a few built in functions
    //that Unity can call for you
    public void Start()
    {
        isOpponentHuman = GameManager.instance.isOpponentHuman;
        playAsWhite = GameManager.instance.isWhiteSelected;
        timeLimit = GameManager.instance.playTimeAmount;

        gamePlayUIController.Initialize(playAsWhite);
        allPieces = gamePlayUIController.CreateWholeChessBoardPieces();

        //Set all piece positions on the positions board
        for (int i = 0; i < allPieces.Length; i++)
        {
            SetPosition(allPieces[i]);
        }

        moveStack = new();
        moveLogStack = new();
        gamePlayUIController.DisablePromotionButtons();
        gamePlayUIController.DisableUndoButton();
        gamePlayUIController.DisableGameOverPanel();
        
        //DisableRestartButton();
        //DisableExitButton();
        EmptyMoves();

        lastClickedPiecePos = new Point(-1, -1);
        lastBreakMoveNumber.Push(0);
        
        moveGenerator.Initialize();
        moveGenerator.GenerateMoves(currentPlayer);
        possibleMoves = moveGenerator.GetMoves(currentPlayer);
        string lastLog = UpdateBoardLogs(false);

        gamePlayUIController.StartTimers(timeLimit);

        lastMove = new Move();
        aisTurn = !isOpponentHuman && !playAsWhite;
        if (aisTurn)
        {
            aiTimer = 0f;
            aiTimerOn = true;
            StartCoroutine(WaitForTimer());
        }
    }

    public GameObject[] GetChessPieces()
    {
        return allPieces;
    }

    public void Update()
    {
        if (aiTimerOn)
        {
            aiTimer += Time.deltaTime;
        }
        else
        {
            aiTimer = 0f;
        }
        if (gameOver)
        {
            gamePlayUIController.StopTimers();
            gamePlayUIController.EnableRestartButton();
            gamePlayUIController.DisableUndoButton();
            gamePlayUIController.EnableExitButton();
            gamePlayUIController.EnableGameOverPanel(winner, drawn);
            if(lastClickedPiecePos != new Point(-1, -1))
                GetPosition(lastClickedPiecePos.X, lastClickedPiecePos.Y).GetComponent<Piece>().DestroyMovePlates();
            return;
        }
        
        if (moveStack.Count == 0 || promotionInProcess || aisTurn || (!isOpponentHuman && moveStack.Count <= 1))
        {
            gamePlayUIController.DisableUndoButton();
        }
        else
        {
            gamePlayUIController.EnableUndoButton();
        }
    }

    public void TimeOut(string player)
    {
        if(player == "white")
        {
            winner = "black";
        }
        else
        {
            winner = "white";
        }
        gameOver = true;
        UpdateMoveLog(false);
    }

    public int GetTwoFoldMoveCount()
    {
        return twoFoldMoveNumber;
    }

    public ref GameObject[,] GetBoard()
    {
        return ref positions;
    }

    public ref Dictionary<uint, Point> GetPiecePositions()
    {
        return ref idToPos;
    }

    public void EmptyPreviousMoves()
    {
        moveStack = new Stack<Move>();
    }

    public void EmptyMoves()
    {
        possibleMoves = new Dictionary<uint, List<Move>>();
        check = false;
        checkMated = false;
        drawn = false;
    }

    //public void UndoFunction()
    //{
    //    if (chosenOpponentIsAI)
    //    {
    //        if (UndoOnBoard())
    //        {
    //            if (UndoOnBoard())
    //            {
    //                ChangePlayer();
    //            }
    //            NextTurn();
    //        }
    //    }
    //    else
    //    {
    //        if (UndoOnBoard())
    //        {
    //            NextTurn();
    //        }
    //    }
    //}

    public void ReplacePawn(int index)
    {
        switch (index)
        {
            case 0:
                promotingTo = (currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_knight";
                break;
            case 1:
                promotingTo = (currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_bishop";
                break;
            case 2:
                promotingTo = (currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_rook";
                break;
            case 3:
                promotingTo = (currentPlayer == Piece.PieceColor.White ? "white" : "black") + "_queen";
                break;
        }
        promotionInProcess = false;
    }

    public void UndoMove()
    {
        if (aisTurn)
            return;
        if (!isOpponentHuman && moveStack.Count <= 1)
            return;
        if(moveStack.Count > 0)
        {
            uint lastClickedPieceId = Piece.GetLastClickedPieceId();
            allPieces[lastClickedPieceId].GetComponent<Piece>().DestroyMovePlates();
            SetLastClickedPiecePos(new Point(-1, -1));

            UnmakeMoveOnBoard(moveStack.Peek());
            
            if (!isOpponentHuman)
            {
                UnmakeMoveOnBoard(moveStack.Peek());
            }
        }
    }

    public Point GetLastClickedPiecePos()
    {
        return lastClickedPiecePos;
    }

    public void SetLastClickedPiecePos(Point p)
    {
        lastClickedPiecePos = p;
    }

    public void ChangePlayer()
    {
        if (currentPlayer == Piece.PieceColor.White)
        {
            currentPlayer = Piece.PieceColor.Black;
        }
        else
        {
            currentPlayer = Piece.PieceColor.White;
        }
    }

    public void NextTurn(bool movedBackwards = false)
    {
        if (gameOver)
            return;

        if (lastMove != new Move())
        {
            PlaySound(lastMove);
        }
        UpdateMoveLog(movedBackwards);
        aisTurn = !isOpponentHuman && (playAsWhite ? currentPlayer == Piece.PieceColor.Black : currentPlayer == Piece.PieceColor.White);

        if (!movedBackwards)
        {
            if (aisTurn)
            {
                aiTimer = 0f;
                aiTimerOn = true;
                StartCoroutine(WaitForTimer());
                //aiPlayer.GetAIMove(playAsWhite ? Piece.PieceColor.Black : Piece.PieceColor.White, depthOfAI);
            }
            else if (!isOpponentHuman && !aisTurn)
            {
                aiTimerOn = false;
                aiTimer = 0f;
            }
        }
        else
        {
            aiTimerOn = false;
            aiTimer = 0f;
        }

        if (gameOver)
            moveStack = new Stack<Move>();
    }

    public void UpdateMoveLog(bool movedBackwards)
    {
        if (gameOver)
        {
            gamePlayUIController.AddMoveToUI(currentPlayer == Piece.PieceColor.White ? "0-1" : "1-0", twoFoldMoveNumber, gameOver, moveNumber);
            moveLogStack = new Stack<string>();
            return;
        }
        
        if (!movedBackwards)
        {
            if (moveStack.Count > 0)
            {
                moveLogStack.Push(moveGenerator.GetMoveLogFully(lastMoveLog, check, checkMated));
                gamePlayUIController.AddMoveToUI(moveLogStack.Peek(), twoFoldMoveNumber, gameOver, moveNumber);
            }
            if (checkMated)
            {
                gameOver = true;
                gamePlayUIController.AddMoveToUI(currentPlayer == Piece.PieceColor.White ? "0-1" : "1-0", twoFoldMoveNumber, gameOver, moveNumber);
                //Debug.Log("Checkmated!!");
                gamePlayUIController.ActivateCheckText("CheckMate!!");// + currentPlayer == "white" ? "Black" : "White" + " is the winner.";
                moveStack = new Stack<Move>();
                moveLogStack = new Stack<string>();
            }
            else if (drawn)
            {
                gameOver = true;
                gamePlayUIController.AddMoveToUI("1/2-1/2", twoFoldMoveNumber, gameOver, moveNumber);
                //Debug.Log("Drawn!!");
                gamePlayUIController.ActivateCheckText("Draw!!");
                moveStack = new Stack<Move>();
                moveLogStack = new Stack<string>();
            }
            else if (check)
            {
                gamePlayUIController.ActivateCheckText("Check");
            }
            else
            {
                gamePlayUIController.DeActivateCheckText();
            }
        }
        else
        {
            if (check)
            {
                gamePlayUIController.ActivateCheckText("Check");
            }
            else
                gamePlayUIController.DeActivateCheckText();
            if (moveLogStack.Count > 0)
                gamePlayUIController.RemoveMoveFromUI(twoFoldMoveNumber);
        }
    }

    public void PlaySound(Move move)
    {
        audioController.PlaySound(move, check);
    }

    public bool GetAIsTurn()
    {
        return aisTurn;
    }

    public void RestartGame()
    {
        gameOver = false;
        //Using UnityEngine.SceneManagement is needed here
        SceneManager.LoadScene("GamePlay"); //Restarts the game by loading the scene over again
    }

    public void ExitGame()
    {
        gameOver = false;
        SceneManager.LoadScene("MainMenu"); //Exits the game by loading the scene over again
    }

    public void SetPosition(GameObject obj)
    {
        Piece cm = obj.GetComponent<Piece>();

        //Overwrites either empty space or whatever was there
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
        idToPos[obj.GetComponent<Piece>().id] = new Point(cm.GetXBoard(), cm.GetYBoard());
    }

    public void SetPositionEmpty(int x, int y)
    {
        uint cm_id = GetPosition(x, y).GetComponent<Piece>().id;
        positions[x, y] = null;
        idToPos[cm_id] = new Point(-1, -1);
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
    public Piece.PieceColor GetCurrentPlayer()
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

    public bool IsPromoting()
    {
        return promotionInProcess;
    }

    IEnumerator WaitForPromotion(Move move)
    {
        yield return new WaitUntil(() => promotionInProcess == false);
        
        move.Promote = new Promote(promotingTo);
        GameObject movingPiece = GetPosition(move.From.X, move.From.Y);

        MakeMove(move);
        
        movingPiece.GetComponent<Piece>().Activate();

        gamePlayUIController.DisablePromotionButtons();
        gamePlayUIController.EnableUndoButton();
        NextTurn();
    }

    public void AIMove(Move move)
    {
        //Debug.Log("moving piece coords: " + aiMove.From.X + ", " + aiMove.From.Y);
        if (aisTurn && !gameOver)
        {
            MakeMoveOnBoard(move);
        }
        aiTimerOn = false;
        moveReady = false;
        aiTimer = 0f;
    }

    IEnumerator WaitForTimer()
    {
        //Debug.Log("moveReady: " + moveReady);
        if (aisTurn && !gameOver)
        {
            aiMove = aiPlayer.GetAIMove(playAsWhite ? Piece.PieceColor.Black : Piece.PieceColor.White, depthOfAI);
        }
        //yield return new WaitUntil(() => moveReady);
        yield return new WaitUntil(() => moveReady);
        //Debug.Log("moving piece coords: " + aiMove.From.X + ", " + aiMove.From.Y);
        if (aisTurn && !gameOver)
        {
            MakeMoveOnBoard(aiMove);
        }
        aiTimerOn = false;
        moveReady = false;
        aiTimer = 0f;
    }

    public void AssignCheckValues(string lastBoardLog)
    {
        if(possibleMoves.Count == 0)
        {
            if (check)
                checkMated = true;
            else
                drawn = true; //stalemate
        }
        if (checkMated)
            winner = currentPlayer == Piece.PieceColor.White ? "black" : "white";
        else
        {
            if (boardLogCounts.ContainsKey(lastBoardLog) && boardLogCounts[lastBoardLog] >= 3)
                drawn = true; //3-fold
            if (twoFoldMoveNumber - lastBreakMoveNumber.Peek() >= 100)
                drawn = true;
            if (InsufficientMaterial())
                drawn = true;
            if (drawn)
                winner = "";
        }
    }

    public void MakeMoveOnBoard(Move move)
    {
        if (move == null) { return; }

        GameObject movingPiece = GetPosition(move.From.X, move.From.Y);
        GameObject toPiece;
        if (move.Attack != null)
        {
            toPiece = GetPosition(move.Attack.Value.CapturedPieceCoords.X, move.Attack.Value.CapturedPieceCoords.Y);
            toPiece.SetActive(false);
        }

        if (!aisTurn && move.Promote != null)
        {
            promotionInProcess = true;

            gamePlayUIController.EnablePromotionButtons();
            StartCoroutine(WaitForPromotion(move));
        }
        else if(move.Promote != null)
        {
            MakeMove(move);
            movingPiece.GetComponent<Piece>().Activate();
            NextTurn();
        }
        else
        {
            MakeMove(move);
            NextTurn();
        }
    }


    public void MakeMove(Move move)
    {
        MoveUpdate(true, move);

        Point from = move.From;
        Point to = move.To;
        if (move.Attack != null)
        {
            GameObject capturedPiece = GetPosition(move.Attack.Value.CapturedPieceCoords.X, move.Attack.Value.CapturedPieceCoords.Y);
            capturedPiece.GetComponent<Piece>().SetXBoard(-1);
            capturedPiece.GetComponent<Piece>().SetYBoard(-1);
            SetPositionEmpty(move.Attack.Value.CapturedPieceCoords.X, move.Attack.Value.CapturedPieceCoords.Y);
        }
        else if (move.Castle != null)
        {
            Point rookFrom = new Point(move.Castle.Value.RookFrom.X, move.Castle.Value.RookFrom.Y);
            Point rookTo = new Point(move.Castle.Value.RookTo.X, move.Castle.Value.RookTo.Y);
            GameObject rook = GetPosition(rookFrom.X, rookFrom.Y);
            SetPositionEmpty(rook.GetComponent<Piece>().GetXBoard(), rook.GetComponent<Piece>().GetYBoard());
            rook.GetComponent<Piece>().SetXBoard(rookTo.X);
            rook.GetComponent<Piece>().SetYBoard(rookTo.Y);
            rook.GetComponent<Piece>().SetCoords();
            rook.GetComponent<Piece>().IncreaseMoveCount(1);
            SetPosition(rook);
            rook.GetComponent<Piece>().SetLastMoveNumber(twoFoldMoveNumber);

        }
        GameObject reference = GetPosition(from.X, from.Y);
        if (move.Promote != null)
        {
            //change this to switch case
            reference.GetComponent<Piece>().name = move.Promote.Value.PromotedTo;
            
        }

        //Set the Chesspiece's original location to be empty
        SetPositionEmpty(reference.GetComponent<Piece>().GetXBoard(),
            reference.GetComponent<Piece>().GetYBoard());

        //increase reference's move count
        reference.GetComponent<Piece>().IncreaseMoveCount(1);
        //Move reference chess piece to this position
        reference.GetComponent<Piece>().SetXBoard(to.X);
        reference.GetComponent<Piece>().SetYBoard(to.Y);
        reference.GetComponent<Piece>().SetCoords();
        reference.GetComponent<Piece>().SetLastMoveNumber(twoFoldMoveNumber);

        //Update the matrix
        SetPosition(reference);

        MoveUpdateGenerator(true, move);
    }

    public void UnmakeMoveOnBoard(Move move)
    {
        if (move == null) { return; }

        GameObject movingPiece = GetPosition(move.To.X, move.To.Y);

        UnmakeMove(move);

        if (move.Attack != null)
        {
            GameObject previouslyCapturedPiece = allPieces[move.Attack.Value.CapturedPieceID];
            previouslyCapturedPiece.SetActive(true);
        }

        if (move.Promote != null)
        {
            movingPiece.GetComponent<Piece>().Activate();
        }
        NextTurn(true);
    }

    public void UnmakeMove(Move m)
    {
        MoveUpdate(false, m);

        Point from = m.From;
        Point to = m.To;

        GameObject unMovingPiece = GetPosition(to.X, to.Y);
        SetPositionEmpty(to.X, to.Y);
        unMovingPiece.GetComponent<Piece>().SetXBoard(from.X);
        unMovingPiece.GetComponent<Piece>().SetYBoard(from.Y);
        unMovingPiece.GetComponent<Piece>().SetCoords();
        unMovingPiece.GetComponent<Piece>().IncreaseMoveCount(-1);
        SetPosition(unMovingPiece);

        if (m.Promote != null)
        {
            unMovingPiece.GetComponent<Piece>().name = (unMovingPiece.GetComponent<Piece>().GetPlayer() == Piece.PieceColor.White ? "white" : "black") + "_pawn";
        }

        if (m.Attack != null)
        {
            GameObject previouslyCapturedPiece = allPieces[m.Attack.Value.CapturedPieceID];
            previouslyCapturedPiece.GetComponent<Piece>().SetXBoard(m.Attack.Value.CapturedPieceCoords.X);
            previouslyCapturedPiece.GetComponent<Piece>().SetYBoard(m.Attack.Value.CapturedPieceCoords.Y);
            previouslyCapturedPiece.GetComponent<Piece>().SetCoords();
            SetPosition(previouslyCapturedPiece);
            //GameObject reference2 = previouslyCapturedPieces.Pop();
            //SetPosition(reference2);
        }
        else if (m.Castle != null)
        {
            GameObject rook = GetPosition(m.Castle.Value.RookTo.X, m.Castle.Value.RookTo.Y);
            SetPositionEmpty(m.Castle.Value.RookTo.X, m.Castle.Value.RookTo.Y);
            rook.GetComponent<Piece>().SetXBoard(m.Castle.Value.RookFrom.X);
            rook.GetComponent<Piece>().SetYBoard(m.Castle.Value.RookFrom.Y);
            rook.GetComponent<Piece>().SetCoords();
            rook.GetComponent<Piece>().IncreaseMoveCount(-1);
            SetPosition(rook);
        }

        MoveUpdateGenerator(false, m);
    }

    public bool InsufficientMaterial()
    {
        Dictionary<string, int> pieceCounts = new Dictionary<string, int>();
        bool isWhiteBishopOnWhite = false;
        bool isBlackBishopOnWhite = false;
        foreach (GameObject pieceObject in allPieces)
        {
            Piece piece = pieceObject.GetComponent<Piece>();
            if (piece.GetComponent<Piece>().GetXBoard() == -1 && piece.GetComponent<Piece>().GetYBoard() == -1)
                continue;
            else
            {
                if (pieceCounts.ContainsKey(piece.name))
                    pieceCounts[piece.name]++;
                else
                    pieceCounts.Add(piece.name, 1);

                if(piece.name == "white_bishop" && (piece.GetXBoard() + piece.GetYBoard()) % 2 == 1)
                    isWhiteBishopOnWhite = true;
                else if(piece.name == "white_bishop")
                    isWhiteBishopOnWhite = false;

                if (piece.name == "black_bishop" && (piece.GetXBoard() + piece.GetYBoard()) % 2 == 1)
                    isBlackBishopOnWhite = true;
                else if (piece.name == "black_bishop")
                    isBlackBishopOnWhite = false;
            }
        }

        if (pieceCounts.Count == 2)
            return true;
        else if (pieceCounts.Count == 3 && ((pieceCounts.ContainsKey("white_knight") && pieceCounts["white_knight"] == 1) || (pieceCounts.ContainsKey("black_knight") && pieceCounts["black_knight"] == 1)))
            return true;
        else if (pieceCounts.Count == 3 && ((pieceCounts.ContainsKey("white_bishop") && pieceCounts["white_bishop"] == 1) || (pieceCounts.ContainsKey("black_bishop") && pieceCounts["black_bishop"] == 1)))
            return true;
        else if (pieceCounts.Count == 4 && pieceCounts.ContainsKey("white_bishop") && pieceCounts.ContainsKey("black_bishop") && pieceCounts["white_bishop"] == 1 && pieceCounts["black_bishop"] == 1 && isWhiteBishopOnWhite == isBlackBishopOnWhite)
            return true;
        else if (pieceCounts.Count == 3 && ((pieceCounts.ContainsKey("white_knight") && pieceCounts["white_knight"] == 2) || (pieceCounts.ContainsKey("black_knight") && pieceCounts["black_knight"] == 2)))
            return true;

        return false;
    }

    private void MoveUpdateGenerator(bool forwards, Move move)
    {
        if (forwards)
        {
            moveGenerator.ForwardMoveUpdate(move);
            check = moveGenerator.GetCheck();
        }
        else
        {
            moveGenerator.BackwardMoveUpdate(move, currentPlayer, moveStack.Count == 0);
            check = moveGenerator.GetCheck();
        }
        
        ChangePlayer();
        EmptyMoves();

        moveGenerator.GenerateMoves(currentPlayer);
        possibleMoves = moveGenerator.GetMoves(currentPlayer);
        check = moveGenerator.GetCheck();
        //int moveCount = 0;
        //foreach(uint id in possibleMoves.Keys)
        //{
        //    moveCount += possibleMoves[id].Count;
        //}
        ////Debug.Log("moveCount: " + moveCount);

        string lastBoardLog = UpdateBoardLogs(!forwards);
        AssignCheckValues(lastBoardLog);
    }

    private void MoveUpdate(bool forwards, Move move)
    {
        if (forwards)
        {
            moveStack.Push(move);
            moveNumber = (int)Math.Ceiling(1.0 * moveStack.Count / 2);
            twoFoldMoveNumber = moveStack.Count;
            AssignCastleRightValues(move);
            UpdateLastBreakMoveNumber(move);
            lastMove = move;
            lastMoveLog = moveGenerator.GetMoveLogFirstPart(move);
        }
        else
        {
            moveStack.Pop();
            moveNumber = (int)Math.Ceiling(1.0 * moveStack.Count / 2);
            twoFoldMoveNumber = moveStack.Count;
            AssignCastleRightValues(move, false);
            UpdateLastBreakMoveNumber(move, false);
            lastMove = move;
            lastMoveLog = "";
        }
    }

    private void UpdateLastBreakMoveNumber(Move move, bool forwards = true)
    {
        if (forwards)
        {
            if (move.Attack != null)
                lastBreakMoveNumber.Push(twoFoldMoveNumber);
            else if (GetPosition(move.From.X, move.From.Y).GetComponent<Piece>().name.Split("_")[1] == "pawn")
                lastBreakMoveNumber.Push(twoFoldMoveNumber);
        }
        else
        {
            if (lastBreakMoveNumber.Peek() > twoFoldMoveNumber)
                lastBreakMoveNumber.Pop();
        }
    }

    private string UpdateBoardLogs(bool movedBackwards)
    {
        string boardLog = GetMetaData() + "//" + GetBoardLog();
        if (movedBackwards)
        {
            if (boardLogCounts.ContainsKey(boardLog))
            {
                boardLogCounts[boardLog]--;
            }
        }
        else
        {
            if (boardLogCounts.ContainsKey(boardLog))
            {
                boardLogCounts[boardLog]++;
            }
            else
            {
                boardLogCounts.Add(boardLog, 1);
            }
        }
        return boardLog;
    }

    public string GetBoardLog()
    {
        Dictionary<string, string> pieceShortNames = new Dictionary<string, string>() {
            {"white_pawn", "WP" },  {"black_pawn" , "BP" }, {"white_rook", "WR" },  {"black_rook" , "BR" }, {"white_bishop", "WB" },  {"black_bishop" , "BB" },
            {"white_queen", "WQ" },  {"black_queen" , "BQ" }, {"white_knight", "WN" },  {"black_knight" , "BN" }, {"white_king", "WK" },  {"black_king" , "BK" }};
        string log = "";
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                GameObject go = GetPosition(i, j);
                if(go != null)
                {
                    log += pieceShortNames[go.GetComponent<Piece>().name];
                }
                else
                {
                    log += "-";
                }
            }
        }
        return log;
    }

    private string GetMetaData()
    {
        List<uint> enPassants = moveGenerator.GetEnPassants();
        string enPassantLog = "";
        foreach(uint enPassant in enPassants)
            enPassantLog += enPassant.ToString();
        return (enPassantLog) + (whiteShortCastleRight ? "WSC" : "") + (whiteLongCastleRight ? "WLC" : "") + (blackShortCastleRight ? "BSC" : "") + (blackLongCastleRight ? "BLC" : "");
    }

    private void AssignCastleRightValues(Move move, bool forwards = true)
    {
        if (forwards)
        {
            uint id = GetPosition(move.From.X, move.From.Y).GetComponent<Piece>().id;
            switch (id)
            {
                case 0:
                    whiteLongCastleRight = false;
                    break;
                case 4:
                    whiteLongCastleRight = false;
                    whiteShortCastleRight = false;
                    break;
                case 7:
                    whiteShortCastleRight = false;
                    break;
                case 16:
                    blackLongCastleRight = false;
                    break;
                case 20:
                    blackLongCastleRight = false;
                    blackShortCastleRight = false;
                    break;
                case 23:
                    blackShortCastleRight = false;
                    break;
            }
        }
        else
        {
            uint id = GetPosition(move.To.X, move.To.Y).GetComponent<Piece>().id;
            switch (id)
            {
                case 0:
                    if (allPieces[0].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[4].GetComponent<Piece>().GetMoveCount() == 0)
                        whiteLongCastleRight = true;
                    break;
                case 4:
                    if (allPieces[0].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[4].GetComponent<Piece>().GetMoveCount() == 0)
                        whiteLongCastleRight = true;
                    if (allPieces[7].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[4].GetComponent<Piece>().GetMoveCount() == 0)
                        whiteShortCastleRight = true;
                    break;
                case 7:
                    if (allPieces[7].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[4].GetComponent<Piece>().GetMoveCount() == 0)
                        whiteShortCastleRight = true;
                    break;
                case 16:
                    if (allPieces[16].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[20].GetComponent<Piece>().GetMoveCount() == 0)
                        blackLongCastleRight = true;
                    break;
                case 20:
                    if (allPieces[16].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[20].GetComponent<Piece>().GetMoveCount() == 0)
                        blackLongCastleRight = true;
                    if (allPieces[23].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[20].GetComponent<Piece>().GetMoveCount() == 0)
                        blackShortCastleRight = true;
                    break;
                case 23:
                    if (allPieces[23].GetComponent<Piece>().GetMoveCount() == 0 && allPieces[20].GetComponent<Piece>().GetMoveCount() == 0)
                        blackShortCastleRight = true;
                    break;
            }
        }
    }
}
