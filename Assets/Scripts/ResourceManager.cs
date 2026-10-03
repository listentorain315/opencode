using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Resource Paths")]
    public string openingBookPath = "Books/BOOK.DAT";
    public string endgameTablePath = "Books/endgame/";
    public string pgnDatabasePath = "Games/";

    [Header("Settings")]
    public bool loadOnStart = true;
    public bool useCache = true;
    public int maxCacheSize = 10000;

    private XQWizardBookLoader bookLoader;
    private OpeningBookLoader openingBook;
    private PGNParser pgnParser;
    private Dictionary<string, string> moveCache;
    private bool isLoaded = false;

    public event Action OnResourcesLoaded;
    public event Action<string> OnLoadProgress;

    void Awake()
    {
        Instance = this;
        bookLoader = new XQWizardBookLoader();
        openingBook = new OpeningBookLoader();
        pgnParser = new PGNParser();
        moveCache = new Dictionary<string, string>();
    }

    void Start()
    {
        if (loadOnStart)
        {
            LoadAllResources();
        }
    }

    public async void LoadAllResources()
    {
        Debug.Log("Loading chess resources...");
        OnLoadProgress?.Invoke("Loading opening book...");

        await LoadOpeningBookAsync();

        OnLoadProgress?.Invoke("Loading PGN database...");
        await LoadPGNDatabaseAsync();

        OnLoadProgress?.Invoke("Resources loaded!");
        isLoaded = true;
        OnResourcesLoaded?.Invoke();

        Debug.Log("All chess resources loaded successfully");
    }

    private async Task LoadOpeningBookAsync()
    {
        // 尝试加载BOOK.DAT
        string bookPath = GetFullPath("Books/BOOK.DAT");
        if (File.Exists(bookPath))
        {
            await Task.Run(() =>
            {
                bool success = bookLoader.LoadBook(bookPath);
                if (success)
                {
                    Debug.Log($"XQWizard book loaded: {bookLoader.GetPositionCount()} positions, {bookLoader.GetMoveCount()} moves");
                }
            });
        }
        else
        {
            Debug.LogWarning("BOOK.DAT not found, using built-in opening book");
        }
    }

    private async Task LoadPGNDatabaseAsync()
    {
        string gamesPath = GetFullPath("Games/");
        
        if (!Directory.Exists(gamesPath))
        {
            Debug.LogWarning("Games directory not found");
            return;
        }

        await Task.Run(() =>
        {
            string[] pgnFiles = Directory.GetFiles(gamesPath, "*.PGN");
            pgnFiles = pgnFiles.Length > 0 ? pgnFiles : Directory.GetFiles(gamesPath, "*.pgn");
            
            int totalGames = 0;
            foreach (string pgnFile in pgnFiles)
            {
                var games = pgnParser.ParseFile(pgnFile);
                totalGames += games.Count;
                Debug.Log($"Loaded {games.Count} games from {Path.GetFileName(pgnFile)}");
            }

            Debug.Log($"Total PGN games loaded: {totalGames}");
        });
    }

    // 查询开局库
    public string QueryOpeningBook(ChessPiece[,] board)
    {
        // 首先查询XQWizard开局库
        string bookMove = bookLoader.QueryPosition(board);
        if (!string.IsNullOrEmpty(bookMove))
        {
            Debug.Log($"Using XQWizard book move: {bookMove}");
            return bookMove;
        }

        // 然后查询内置开局库
        string builtInMove = BuiltInOpeningBook.GetOpeningMove(GetPositionKey(board));
        if (!string.IsNullOrEmpty(builtInMove))
        {
            Debug.Log($"Using built-in opening move: {builtInMove}");
            return builtInMove;
        }

        return null;
    }

    // 查询开局库（通过局面键）
    public string QueryOpeningBook(string positionKey)
    {
        // 检查缓存
        if (useCache && moveCache.ContainsKey(positionKey))
        {
            return moveCache[positionKey];
        }

        // 查询内置开局库
        string builtInMove = BuiltInOpeningBook.GetOpeningMove(positionKey);
        if (!string.IsNullOrEmpty(builtInMove))
        {
            if (useCache)
                moveCache[positionKey] = builtInMove;
            return builtInMove;
        }

        return null;
    }

    private string GetPositionKey(ChessPiece[,] board)
    {
        // 简化的位置键生成
        var sb = new System.Text.StringBuilder();
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                ChessPiece piece = board[r, c];
                if (piece.IsEmpty())
                    sb.Append('.');
                else
                {
                    char color = piece.color == PieceColor.Red ? 'r' : 'b';
                    char type = ' ';
                    switch (piece.type)
                    {
                        case PieceType.King: type = 'k'; break;
                        case PieceType.Advisor: type = 'a'; break;
                        case PieceType.Elephant: type = 'e'; break;
                        case PieceType.Horse: type = 'h'; break;
                        case PieceType.Rook: type = 'r'; break;
                        case PieceType.Cannon: type = 'c'; break;
                        case PieceType.Pawn: type = 'p'; break;
                    }
                    sb.Append(color);
                    sb.Append(type);
                }
            }
        }
        return sb.ToString();
    }

    private string GetFullPath(string relativePath)
    {
        string[] searchPaths = {
            Path.Combine(Application.dataPath, relativePath),
            Path.Combine(Application.persistentDataPath, relativePath),
            relativePath
        };

        foreach (string path in searchPaths)
        {
            if (File.Exists(path) || Directory.Exists(path))
                return path;
        }

        return searchPaths[0];
    }

    public bool CheckResources()
    {
        string bookPath = GetFullPath("Books/BOOK.DAT");
        bool hasBook = File.Exists(bookPath);
        
        Debug.Log($"Resources check - BOOK.DAT: {hasBook}");
        return hasBook;
    }

    public Dictionary<string, string> GetResourceInfo()
    {
        var info = new Dictionary<string, string>();
        
        string bookPath = GetFullPath("Books/BOOK.DAT");
        info["BOOK.DAT"] = File.Exists(bookPath) ? $"Found ({new FileInfo(bookPath).Length / 1024}KB)" : "Not found";
        info["Positions"] = bookLoader.GetPositionCount().ToString();
        info["Moves"] = bookLoader.GetMoveCount().ToString();
        info["Cache Size"] = $"{moveCache.Count} entries";
        info["Is Loaded"] = isLoaded.ToString();

        return info;
    }

    public void ClearCache()
    {
        moveCache.Clear();
        Debug.Log("Resource cache cleared");
    }
}
