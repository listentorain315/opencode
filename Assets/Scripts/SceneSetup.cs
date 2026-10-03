using UnityEngine;
using UnityEngine.UI;

public class SceneSetup : MonoBehaviour
{
    [Header("Prefabs")]
    public Font defaultFont;

    void Awake()
    {
        SetupScene();
    }

    void SetupScene()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        mainCamera.transform.position = new Vector3(0, 0, -10);
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 6;
        mainCamera.backgroundColor = new Color(0.2f, 0.2f, 0.25f);

        GameObject boardRendererObj = new GameObject("BoardRenderer");
        BoardRenderer boardRenderer = boardRendererObj.AddComponent<BoardRenderer>();

        GameObject gameManagerObj = new GameObject("GameManager");
        GameManager gameManager = gameManagerObj.AddComponent<GameManager>();

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        CreateUI(canvasObj, gameManager);

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    void CreateUI(GameObject canvasObj, GameManager gameManager)
    {
        Font font = defaultFont != null ? defaultFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Top panel - Turn info
        GameObject topPanel = CreatePanel(canvasObj, "TopPanel", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(0, -50), new Vector2(Screen.width, 60));

        GameObject turnTextObj = CreateText(topPanel, "TurnText", "红方走棋 (你)",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 24, font);
        gameManager.turnText = turnTextObj.GetComponent<Text>();

        GameObject playerColorObj = CreateText(topPanel, "PlayerColorText", "你执: 红方",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(150, 30), 18, font);
        gameManager.playerColorText = playerColorObj.GetComponent<Text>();

        GameObject moveCountObj = CreateText(topPanel, "MoveCountText", "回合: 1",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(150, 30), 18, font);
        gameManager.moveCountText = moveCountObj.GetComponent<Text>();

        // Status text
        GameObject statusObj = CreateText(canvasObj, "StatusText", "",
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(300, 40), 28, font);
        gameManager.statusText = statusObj.GetComponent<Text>();
        statusObj.GetComponent<Text>().color = Color.red;

        // AI Level panel - Right side
        GameObject aiLevelPanel = CreatePanel(canvasObj, "AILevelPanel", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-90, 0), new Vector2(160, 120));
        aiLevelPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        GameObject aiLevelTitle = CreateText(aiLevelPanel, "AILevelTitle", "对手信息",
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(140, 25), 16, font);
        aiLevelTitle.GetComponent<Text>().color = new Color(0.8f, 0.8f, 0.2f);

        GameObject aiLevelTextObj = CreateText(aiLevelPanel, "AILevelText", "GPU加速AI\n棋力: 业余5-6级\nELO 1600-1800",
            new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(140, 70), 14, font);
        gameManager.aiLevelText = aiLevelTextObj.GetComponent<Text>();
        aiLevelTextObj.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.9f);

        // Bottom button panel
        GameObject bottomPanel = CreatePanel(canvasObj, "BottomPanel", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(0, 10), new Vector2(Screen.width, 50));

        GameObject restartBtn = CreateButton(bottomPanel, "RestartButton", "重新开始",
            new Vector2(0.3f, 0.5f), new Vector2(0.3f, 0.5f), new Vector2(0, 0), new Vector2(120, 40), font);
        gameManager.restartButton = restartBtn.GetComponent<Button>();

        GameObject undoBtn = CreateButton(bottomPanel, "UndoButton", "悔棋",
            new Vector2(0.7f, 0.5f), new Vector2(0.7f, 0.5f), new Vector2(0, 0), new Vector2(120, 40), font);
        gameManager.undoButton = undoBtn.GetComponent<Button>();

        // Settings button
        GameObject settingsBtn = CreateButton(topPanel, "SettingsButton", "设置",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(180, 0), new Vector2(80, 35), font);
        gameManager.settingsButton = settingsBtn.GetComponent<Button>();
        settingsBtn.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f);

        // Game Over panel
        GameObject gameOverPanel = CreatePanel(canvasObj, "GameOverPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400, 200));
        gameOverPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        gameManager.gameOverPanel = gameOverPanel;

        GameObject gameOverText = CreateText(gameOverPanel, "GameOverText", "游戏结束",
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(300, 50), 36, font);
        gameManager.gameOverText = gameOverText.GetComponent<Text>();

        GameObject gameOverSubText = CreateText(gameOverPanel, "GameOverSubText", "",
            new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(300, 30), 20, font);
        gameManager.gameOverSubText = gameOverSubText.GetComponent<Text>();

        GameObject playAgainBtn = CreateButton(gameOverPanel, "PlayAgainButton", "再来一局",
            new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), Vector2.zero, new Vector2(150, 40), font);
        playAgainBtn.GetComponent<Button>().onClick.AddListener(gameManager.RestartGame);

        // Settings panel
        CreateSettingsPanel(canvasObj, gameManager, font);
    }

    void CreateSettingsPanel(GameObject canvasObj, GameManager gameManager, Font font)
    {
        GameObject settingsPanel = CreatePanel(canvasObj, "SettingsPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(350, 300));
        settingsPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        gameManager.settingsPanel = settingsPanel;

        GameObject title = CreateText(settingsPanel, "SettingsTitle", "AI 设置",
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(200, 40), 24, font);
        title.GetComponent<Text>().color = new Color(0.8f, 0.8f, 0.2f);

        // AI Depth
        GameObject depthLabel = CreateText(settingsPanel, "DepthLabel", "搜索深度: 6",
            new Vector2(0.3f, 0.7f), new Vector2(0.3f, 0.7f), Vector2.zero, new Vector2(150, 30), 16, font);

        // Thread Count
        GameObject threadLabel = CreateText(settingsPanel, "ThreadLabel", "线程数: 8",
            new Vector2(0.3f, 0.5f), new Vector2(0.3f, 0.5f), Vector2.zero, new Vector2(150, 30), 16, font);

        // GPU toggle
        GameObject gpuLabel = CreateText(settingsPanel, "GPULabel", "使用GPU: 是",
            new Vector2(0.3f, 0.3f), new Vector2(0.3f, 0.3f), Vector2.zero, new Vector2(150, 30), 16, font);

        // Close button
        GameObject closeBtn = CreateButton(settingsPanel, "CloseSettings", "关闭",
            new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(100, 35), font);
        closeBtn.GetComponent<Button>().onClick.AddListener(() => settingsPanel.SetActive(false));
    }

    GameObject CreatePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        return panel;
    }

    GameObject CreateText(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, int fontSize, Font font)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = font;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;

        return textObj;
    }

    GameObject CreateButton(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Font font)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.35f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = font;
        textComponent.fontSize = 18;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;

        return buttonObj;
    }
}
