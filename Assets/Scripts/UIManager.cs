using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Main UI")]
    public Canvas mainCanvas;
    public Text titleText;
    public Text turnText;
    public Text statusText;
    public Text playerColorText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Text gameOverTitle;
    public Text gameOverMessage;
    public Button playAgainButton;
    public Button quitButton;

    [Header("Control Buttons")]
    public Button restartButton;
    public Button undoButton;
    public Button hintButton;

    [Header("Info Panel")]
    public GameObject infoPanel;
    public Text moveCountText;
    public Text redScoreText;
    public Text blackScoreText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupUI();
    }

    void SetupUI()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClick);

        if (undoButton != null)
            undoButton.onClick.AddListener(OnUndoClick);

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnRestartClick);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClick);
    }

    public void UpdateTurn(PieceColor currentTurn, bool isPlayerTurn)
    {
        if (turnText != null)
        {
            string colorName = currentTurn == PieceColor.Red ? "红方" : "黑方";
            string playerName = isPlayerTurn ? "(你)" : "(AI)";
            turnText.text = $"{colorName}走棋 {playerName}";
            turnText.color = currentTurn == PieceColor.Red ? Color.red : Color.black;
        }
    }

    public void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    public void UpdatePlayerColor(PieceColor color)
    {
        if (playerColorText != null)
        {
            playerColorText.text = $"你执: {(color == PieceColor.Red ? "红方" : "黑方")}";
            playerColorText.color = color == PieceColor.Red ? Color.red : Color.black;
        }
    }

    public void UpdateMoveCount(int count)
    {
        if (moveCountText != null)
        {
            moveCountText.text = $"回合: {count / 2 + 1}";
        }
    }

    public void UpdateScore(int redScore, int blackScore)
    {
        if (redScoreText != null)
            redScoreText.text = $"红方: {redScore}";

        if (blackScoreText != null)
            blackScoreText.text = $"黑方: {blackScore}";
    }

    public void ShowGameOver(PieceColor winner, bool isPlayerWin)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverTitle != null)
            {
                gameOverTitle.text = isPlayerWin ? "恭喜获胜！" : "再接再厉！";
                gameOverTitle.color = isPlayerWin ? Color.green : Color.red;
            }

            if (gameOverMessage != null)
            {
                string winnerColor = winner == PieceColor.Red ? "红方" : "黑方";
                gameOverMessage.text = $"{winnerColor}获胜";
            }
        }
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ShowCheck()
    {
        if (statusText != null)
        {
            statusText.text = "将军！";
            statusText.color = Color.red;
        }
    }

    public void ShowThinking()
    {
        if (statusText != null)
        {
            statusText.text = "AI思考中...";
            statusText.color = Color.gray;
        }
    }

    public void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    void OnRestartClick()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    void OnUndoClick()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.UndoMove();
        }
    }

    void OnQuitClick()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
