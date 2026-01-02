using CSharpSqliteORM;
using GameLibrary.DB;

namespace GameLibrary.Logic;

public static class DatabaseManager
{
    public const string APPLICATION_NAME = "MyLibraryApplication";
    public const string DB_POINTER_FILE = "dblink";

    public static string GetUserStorageFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME);
    public static string? cachedDBLocation { get; private set; }

    public static async Task Init()
    {
        string root = GetUserStorageFolder();

        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);

        string dbPointerFile = Path.Combine(root, DB_POINTER_FILE);

        if (File.Exists(dbPointerFile))
        {
            string pointer = File.ReadAllText(dbPointerFile);

            if (File.Exists(pointer))
                cachedDBLocation = pointer;
        }
    }

    public static async Task LoadDatabase()
    {
        if (string.IsNullOrEmpty(cachedDBLocation))
        {
            throw new Exception("Invalid pointer file");
        }

        await Database_Manager.Init(cachedDBLocation, HandleException);
    }

    public static async Task CreateDBPointerFile(string path)
    {
        string dbPointerFile = Path.Combine(GetUserStorageFolder(), DB_POINTER_FILE);

        if (File.Exists(dbPointerFile))
            File.Delete(dbPointerFile);

        await File.WriteAllTextAsync(dbPointerFile, path);
        cachedDBLocation = path;
    }

    private static async void HandleException(Exception e)
    {
        await DependencyManager.uiLinker!.OpenYesNoModal("SQL exception", e.Message);
    }
}
