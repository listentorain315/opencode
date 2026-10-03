using UnityEngine;
using System.Collections.Generic;

public class BoardRenderer : MonoBehaviour
{
    public static BoardRenderer Instance { get; private set; }

    [Header("Colors")]
    public Color boardColor = new Color(0.96f, 0.87f, 0.70f);
    public Color lineColor = new Color(0.2f, 0.1f, 0.0f);
    public Color redPieceColor = new Color(0.8f, 0.15f, 0.1f);
    public Color blackPieceColor = new Color(0.1f, 0.1f, 0.1f);
    public Color highlightColor = new Color(0.2f, 0.8f, 0.2f, 0.6f);
    public Color selectedColor = new Color(1f, 0.8f, 0f, 0.7f);

    [Header("Sizes")]
    public float cellSize = 1.0f;
    public float pieceRadius = 0.4f;
    public float lineWidth = 0.03f;

    private Dictionary<string, GameObject> createdObjects = new Dictionary<string, GameObject>();
    private Material lineMaterial;
    private Material pieceMaterial;

    void Awake()
    {
        Instance = this;
        CreateMaterials();
    }

    void CreateMaterials()
    {
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = lineColor;

        pieceMaterial = new Material(Shader.Find("Standard"));
    }

    public void DrawBoard()
    {
        ClearBoard();

        // 绘制背景
        GameObject bg = CreateQuad("BoardBg", Vector3.zero, new Vector3(9, 10, 1) * cellSize, boardColor, 0.1f);

        // 绘制网格线
        for (int r = 0; r <= 9; r++)
        {
            Vector3 start = new Vector3(-4, -4.5f + r, 0) * cellSize;
            Vector3 end = new Vector3(4, -4.5f + r, 0) * cellSize;
            CreateLine($"HLine_{r}", start, end);
        }

        for (int c = 0; c <= 8; c++)
        {
            // 左边界
            if (c == 0 || c == 8)
            {
                Vector3 start = new Vector3(-4 + c, -4.5f, 0) * cellSize;
                Vector3 end = new Vector3(-4 + c, 4.5f, 0) * cellSize;
                CreateLine($"VLine_{c}", start, end);
            }
            else
            {
                // 上半部分
                Vector3 start1 = new Vector3(-4 + c, -4.5f, 0) * cellSize;
                Vector3 end1 = new Vector3(-4 + c, 0.5f, 0) * cellSize;
                CreateLine($"VLine_{c}_1", start1, end1);

                // 下半部分
                Vector3 start2 = new Vector3(-4 + c, 0.5f, 0) * cellSize;
                Vector3 end2 = new Vector3(-4 + c, 4.5f, 0) * cellSize;
                CreateLine($"VLine_{c}_2", start2, end2);
            }
        }

        // 绘制九宫格对角线
        // 红方（下方）
        CreateLine("Diag_R1", new Vector3(-1, 2.5f, 0) * cellSize, new Vector3(1, 4.5f, 0) * cellSize);
        CreateLine("Diag_R2", new Vector3(1, 2.5f, 0) * cellSize, new Vector3(-1, 4.5f, 0) * cellSize);

        // 黑方（上方）
        CreateLine("Diag_B1", new Vector3(-1, -4.5f, 0) * cellSize, new Vector3(1, -2.5f, 0) * cellSize);
        CreateLine("Diag_B2", new Vector3(1, -4.5f, 0) * cellSize, new Vector3(-1, -2.5f, 0) * cellSize);

        // 河界文字
        CreateText("RiverText_R", "楚 河", new Vector3(-1.5f, 0, -0.1f) * cellSize, 0.35f);
        CreateText("RiverText_B", "汉 界", new Vector3(1.5f, 0, -0.1f) * cellSize, 0.35f);

        // 绘制交叉点标记
        DrawCrossMarkers();
    }

    void DrawCrossMarkers()
    {
        // 兵/卒位置标记
        int[,] pawnPositions = {
            {3, 0}, {3, 2}, {3, 4}, {3, 6}, {3, 8},
            {6, 0}, {6, 2}, {6, 4}, {6, 6}, {6, 8}
        };

        // 炮位置标记
        int[,] cannonPositions = {
            {2, 1}, {2, 7}, {7, 1}, {7, 7}
        };

        // 这些位置需要绘制小十字标记
        for (int i = 0; i < pawnPositions.GetLength(0); i++)
        {
            int r = pawnPositions[i, 0];
            int c = pawnPositions[i, 1];
            DrawPositionMarker(r, c);
        }

        for (int i = 0; i < cannonPositions.GetLength(0); i++)
        {
            int r = cannonPositions[i, 0];
            int c = cannonPositions[i, 1];
            DrawPositionMarker(r, c);
        }
    }

    void DrawPositionMarker(int row, int col)
    {
        Vector3 center = GetWorldPosition(row, col);
        float markSize = 0.1f * cellSize;
        float gap = 0.05f * cellSize;

        // 根据位置决定绘制哪些方向的标记
        bool drawLeft = col > 0;
        bool drawRight = col < 8;

        if (drawLeft)
        {
            CreateLine($"Mark_{row}_{col}_L1", center + new Vector3(-gap, 0, 0), center + new Vector3(-gap - markSize, 0, 0));
            CreateLine($"Mark_{row}_{col}_L2", center + new Vector3(-gap, markSize, 0), center + new Vector3(-gap, 0, 0));
            CreateLine($"Mark_{row}_{col}_L3", center + new Vector3(-gap, -markSize, 0), center + new Vector3(-gap, 0, 0));
        }

        if (drawRight)
        {
            CreateLine($"Mark_{row}_{col}_R1", center + new Vector3(gap, 0, 0), center + new Vector3(gap + markSize, 0, 0));
            CreateLine($"Mark_{row}_{col}_R2", center + new Vector3(gap, markSize, 0), center + new Vector3(gap, 0, 0));
            CreateLine($"Mark_{row}_{col}_R3", center + new Vector3(gap, -markSize, 0), center + new Vector3(gap, 0, 0));
        }
    }

    public GameObject CreatePiece(ChessPiece piece, int row, int col)
    {
        string key = $"Piece_{row}_{col}";
        if (createdObjects.ContainsKey(key))
            Destroy(createdObjects[key]);

        Vector3 pos = GetWorldPosition(row, col);

        // 创建棋子底座
        GameObject pieceObj = CreateCylinder(key, pos, pieceRadius, 0.15f * cellSize,
            piece.color == PieceColor.Red ? redPieceColor : blackPieceColor);

        // 创建内圈
        GameObject innerCircle = CreateCylinder(key + "_inner", pos + new Vector3(0, 0.001f, 0),
            pieceRadius * 0.75f, 0.16f * cellSize,
            piece.color == PieceColor.Red ? new Color(0.9f, 0.3f, 0.2f) : new Color(0.3f, 0.3f, 0.3f));

        // 创建文字
        GameObject textObj = new GameObject(key + "_text");
        textObj.transform.position = pos + new Vector3(0, 0, -0.01f);
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = piece.GetDisplayName();
        tm.characterSize = 0.25f * cellSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = piece.color == PieceColor.Red ? Color.white : Color.yellow;
        tm.fontStyle = FontStyle.Bold;

        createdObjects[key] = pieceObj;
        createdObjects[key + "_inner"] = innerCircle;
        createdObjects[key + "_text"] = textObj;

        return pieceObj;
    }

    public void RemovePiece(int row, int col)
    {
        string key = $"Piece_{row}_{col}";
        if (createdObjects.ContainsKey(key))
        {
            Destroy(createdObjects[key]);
            createdObjects.Remove(key);
        }
        if (createdObjects.ContainsKey(key + "_inner"))
        {
            Destroy(createdObjects[key + "_inner"]);
            createdObjects.Remove(key + "_inner");
        }
        if (createdObjects.ContainsKey(key + "_text"))
        {
            Destroy(createdObjects[key + "_text"]);
            createdObjects.Remove(key + "_text");
        }
    }

    public GameObject CreateHighlight(int row, int col, Color color)
    {
        string key = $"Highlight_{row}_{col}";
        if (createdObjects.ContainsKey(key))
            Destroy(createdObjects[key]);

        Vector3 pos = GetWorldPosition(row, col);
        GameObject highlight = CreateCylinder(key, pos + new Vector3(0, 0, -0.02f),
            pieceRadius * 0.4f, 0.02f * cellSize, color);

        createdObjects[key] = highlight;
        return highlight;
    }

    public void RemoveHighlight(int row, int col)
    {
        string key = $"Highlight_{row}_{col}";
        if (createdObjects.ContainsKey(key))
        {
            Destroy(createdObjects[key]);
            createdObjects.Remove(key);
        }
    }

    public void ClearHighlights()
    {
        List<string> toRemove = new List<string>();
        foreach (var kvp in createdObjects)
        {
            if (kvp.Key.StartsWith("Highlight_"))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (string key in toRemove)
        {
            createdObjects.Remove(key);
        }
    }

    public void ClearBoard()
    {
        foreach (var kvp in createdObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        createdObjects.Clear();
    }

    public Vector3 GetWorldPosition(int row, int col)
    {
        return new Vector3(-4 + col, 4.5f - row, 0) * cellSize;
    }

    public Position GetBoardPosition(Vector3 worldPos)
    {
        int col = Mathf.RoundToInt(worldPos.x / cellSize + 4);
        int row = Mathf.RoundToInt(4.5f - worldPos.y / cellSize);
        return new Position(row, col);
    }

    GameObject CreateQuad(string name, Vector3 position, Vector3 scale, Color color, float zOrder = 0)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = name;
        obj.transform.position = position + new Vector3(0, 0, zOrder);
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material.color = color;
        return obj;
    }

    GameObject CreateCylinder(string name, Vector3 position, float radius, float height, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(radius * 2, height / 2, radius * 2);
        obj.GetComponent<Renderer>().material = pieceMaterial;
        obj.GetComponent<Renderer>().material.color = color;
        return obj;
    }

    void CreateLine(string name, Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject(name);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = lineMaterial;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        lr.sortingOrder = 1;
    }

    void CreateText(string name, string text, Vector3 position, float size)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.position = position;
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = size;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = lineColor;
        tm.fontStyle = FontStyle.Bold;
        createdObjects[name] = textObj;
    }
}
