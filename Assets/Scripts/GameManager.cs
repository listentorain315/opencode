using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int aiDepth = 6;
    public float aiMoveDelay = 0.5f;
    public bool useAdvancedAI = true;
    public bool useGPU = true;
    public int threadCount = 8;

    [Header("UI References")]
    public Text turnText;
    public Text statusText;
    public Text playerColorText;
    public Text moveCountText;
    public Text aiLevelText;
    public Button restartButton;
    public Button undoButton;
    public Button settingsButton;
    public GameObject gameOverPanel;
    public Text gameOverText;
    public Text gameOverSubText;
    public GameObject settingsPanel;

    private ChessPiece[,] board = new ChessPiece[10, 9];
    private PieceColor playerColor = PieceColor.Red;
    private PieceColor currentTurn = PieceColor.Red;
    private bool isGameOver = false;
    private bool isAIThinking = false;

    private Position? selectedPosition = null;
    private List<Move> currentValidMoves = new List<Move>();

    private GPUChessAI gpuAI;
    private AdvancedChessAI advancedAI;
    private ChessAI simpleAI;
    private List<ChessPiece[,]> moveHistory = new List<ChessPiece[,]>();
    private int moveCount = 0;

    private Camera mainCamera;
    private BoardRenderer boardRenderer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        boardRenderer = BoardRenderer.Instance;

        if (boardRenderer == null)
        {
            Debug.LogError("BoardRenderer not found!");
            return;
        }

        InitializeGame();
        SetupUI();
    }

    void InitializeGame()
    {
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                board[r, c] = new ChessPiece();
            }
        }
        moveHistory.Clear();
        moveCount = 0;

        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                PieceType type = ChessConstants.INITIAL_BOARD[r, c];
                if (type != PieceType.None)
                {
                    PieceColor color = r < 5 ? PieceColor.Black : PieceColor.Red;
                    board[r, c] = new ChessPiece(type, color, new Position(r, c));
                }
            }
        }

        boardRenderer.DrawBoard();
        CreateAllPieces();

        playerColor = Random.value < 0.5f ? PieceColor.Red : PieceColor.Black;
        currentTurn = PieceColor.Red;

        PieceColor aiColor = playerColor == PieceColor.Red ? PieceColor.Black : PieceColor.Red;

        if (useGPU && SystemInfo.supportsComputeShaders)
        {
            gpuAI = new GPUChessAI(aiColor, aiDepth, threadCount);
            Debug.Log("Using GPU-accelerated AI");
        }
        else if (useAdvancedAI)
        {
            advancedAI = new AdvancedChessAI(aiColor, aiDepth);
            Debug.Log("Using Advanced AI (CPU multi-threaded)");
        }
        else
        {
            simpleAI = new ChessAI(aiColor, aiDepth);
            Debug.Log("Using Simple AI");
        }

        UpdateUI();

        if (playerColor == PieceColor.Black)
        {
            StartCoroutine(AIMoveCoroutine());
        }
    }

    void SetupUI()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (undoButton != null)
            undoButton.onClick.AddListener(UndoMove);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettings);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void CreateAllPieces()
    {
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (!board[r, c].IsEmpty())
                {
                    boardRenderer.CreatePiece(board[r, c], r, c);
                }
            }
        }
    }

    void Update()
    {
        if (isGameOver || isAIThinking) return;
        if (currentTurn != playerColor) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Position boardPos = boardRenderer.GetBoardPosition(hit.point);

                if (boardPos.IsValid())
                {
                    HandlePlayerClick(boardPos);
                }
            }
        }
    }

    void HandlePlayerClick(Position pos)
    {
        ChessPiece clickedPiece = board[pos.row, pos.col];

        if (selectedPosition.HasValue)
        {
            bool isValidTarget = false;
            foreach (Move move in currentValidMoves)
            {
                if (move.to == pos)
                {
                    isValidTarget = true;
                    ExecutePlayerMove(move);
                    break;
                }
            }

            if (!isValidTarget)
            {
                if (clickedPiece.color == playerColor)
                {
                    SelectPiece(pos);
                }
                else
                {
                    DeselectPiece();
                }
            }
        }
        else
        {
            if (clickedPiece.color == playerColor)
            {
                SelectPiece(pos);
            }
        }
    }

    void SelectPiece(Position pos)
    {
        selectedPosition = pos;
        boardRenderer.ClearHighlights();

        boardRenderer.CreateHighlight(pos.row, pos.col, boardRenderer.selectedColor);

        currentValidMoves = ChessRules.GetValidMoves(board, pos, playerColor);
        foreach (Move move in currentValidMoves)
        {
            boardRenderer.CreateHighlight(move.to.row, move.to.col, boardRenderer.highlightColor);
        }
    }

    void DeselectPiece()
    {
        selectedPosition = null;
        currentValidMoves.Clear();
        boardRenderer.ClearHighlights();
    }

    void ExecutePlayerMove(Move move)
    {
        SaveHistory();
        ExecuteMove(move);
        DeselectPiece();

        if (!isGameOver)
        {
            currentTurn = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            UpdateUI();
            StartCoroutine(AIMoveCoroutine());
        }
    }

    IEnumerator AIMoveCoroutine()
    {
        isAIThinking = true;
        UpdateUI();

        yield return new WaitForSeconds(aiMoveDelay);

        PieceColor aiColor = playerColor == PieceColor.Red ? PieceColor.Black : PieceColor.Red;

        Move bestMove = new Move();
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        if (gpuAI != null)
        {
            bestMove = gpuAI.GetBestMove(board, aiColor);
        }
        else if (advancedAI != null)
        {
            bestMove = advancedAI.GetBestMove(board, aiColor);
        }
        else if (simpleAI != null)
        {
            bestMove = simpleAI.GetBestMove(board);
        }

        sw.Stop();
        Debug.Log($"AI thought for {sw.ElapsedMilliseconds}ms");

        if (bestMove.from.row >= 0)
        {
            SaveHistory();
            ExecuteMove(bestMove);
            currentTurn = playerColor;
        }

        isAIThinking = false;
        UpdateUI();
    }

    void ExecuteMove(Move move)
    {
        ChessPiece piece = board[move.from.row, move.from.col];
        ChessPiece captured = board[move.to.row, move.to.col];

        move.capturedPiece = captured;

        board[move.to.row, move.to.col] = new ChessPiece(piece.type, piece.color, move.to);
        board[move.from.row, move.from.col] = new ChessPiece();

        boardRenderer.RemovePiece(move.from.row, move.from.col);
        boardRenderer.RemovePiece(move.to.row, move.to.col);
        boardRenderer.CreatePiece(board[move.to.row, move.to.col], move.to.row, move.to.col);

        moveCount++;

        if (captured.type == PieceType.King)
        {
            GameOver(piece.color);
        }
        else
        {
            PieceColor nextTurn = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            if (ChessRules.IsCheckmate(board, nextTurn))
            {
                GameOver(currentTurn);
            }
            else if (ChessRules.IsInCheck(board, nextTurn))
            {
                ShowCheck();
            }
            else
            {
                ClearStatus();
            }
        }
    }

    void SaveHistory()
    {
        moveHistory.Add((ChessPiece[,])board.Clone());
    }

    public void UndoMove()
    {
        if (moveHistory.Count < 2) return;

        board = moveHistory[moveHistory.Count - 2];
        moveHistory.RemoveRange(moveHistory.Count - 2, 2);
        moveCount -= 2;

        boardRenderer.ClearBoard();
        boardRenderer.DrawBoard();
        CreateAllPieces();

        currentTurn = playerColor;
        isGameOver = false;
        DeselectPiece();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateUI();
    }

    void GameOver(PieceColor winner)
    {
        isGameOver = true;
        bool isPlayerWin = winner == playerColor;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverText != null)
            {
                gameOverText.text = isPlayerWin ? "恭喜获胜！" : "AI获胜！";
                gameOverText.color = isPlayerWin ? Color.green : Color.red;
            }

            if (gameOverSubText != null)
            {
                string winnerColor = winner == PieceColor.Red ? "红方" : "黑方";
                gameOverSubText.text = $"{winnerColor}获胜 - 共{moveCount}步";
            }
        }

        UpdateUI();
    }

    void ShowCheck()
    {
        if (statusText != null)
        {
            statusText.text = "将军！";
            statusText.color = Color.red;
        }
    }

    void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        isAIThinking = false;
        isGameOver = false;
        selectedPosition = null;
        currentValidMoves.Clear();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        InitializeGame();
    }

    void ToggleSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    void UpdateUI()
    {
        if (turnText != null)
        {
            string colorName = currentTurn == PieceColor.Red ? "红方" : "黑方";
            string playerName = currentTurn == playerColor ? "(你)" : "(AI)";
            turnText.text = $"{colorName}走棋 {playerName}";
            turnText.color = currentTurn == PieceColor.Red ? new Color(0.8f, 0.1f, 0.1f) : Color.black;
        }

        if (playerColorText != null)
        {
            playerColorText.text = $"你执: {(playerColor == PieceColor.Red ? "红方" : "黑方")}";
            playerColorText.color = playerColor == PieceColor.Red ? new Color(0.8f, 0.1f, 0.1f) : Color.black;
        }

        if (moveCountText != null)
        {
            moveCountText.text = $"回合: {moveCount / 2 + 1}";
        }

        if (aiLevelText != null)
        {
            string aiType = "";
            string level = "";
            string elo = "";

            if (gpuAI != null)
            {
                aiType = "GPU加速AI";
                level = "业余5-6级";
                elo = "ELO 1600-1800";
            }
            else if (advancedAI != null)
            {
                aiType = "高级CPU AI";
                level = "业余3-4级";
                elo = "ELO 1200-1400";
            }
            else
            {
                aiType = "基础AI";
                level = "业余1-2级";
                elo = "ELO 800-1000";
            }

            aiLevelText.text = $"对手: {aiType}\n棋力: {level}\n{elo}";
        }

        if (statusText != null && isAIThinking)
        {
            statusText.text = "AI思考中...";
            statusText.color = Color.gray;
        }
    }

    public void SetAIDepth(int depth)
    {
        aiDepth = Mathf.Clamp(depth, 2, 10);
        RestartGame();
    }

    public void SetUseGPU(bool use)
    {
        useGPU = use;
        RestartGame();
    }

    public void SetThreadCount(int count)
    {
        threadCount = Mathf.Clamp(count, 1, 16);
        RestartGame();
    }
}
