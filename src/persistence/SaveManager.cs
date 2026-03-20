namespace ElevenLegends.Persistence;

/// <summary>
/// Manages save slots and autosave. Each save is a separate .db file.
/// </summary>
public sealed class SaveManager
{
    private readonly string _savesDirectory;

    public SaveManager(string savesDirectory)
    {
        _savesDirectory = savesDirectory;
        Directory.CreateDirectory(_savesDirectory);
    }

    /// <summary>
    /// Saves game to a named slot.
    /// </summary>
    public void SaveGame(GameState gameState, string slotName)
    {
        string filePath = GetSlotPath(slotName);
        GameSaver.Save(gameState, filePath);
    }

    /// <summary>
    /// Loads game from a named slot.
    /// </summary>
    public GameState LoadGame(string slotName)
    {
        string filePath = GetSlotPath(slotName);
        return GameLoader.Load(filePath);
    }

    /// <summary>
    /// Autosaves to the "autosave" slot.
    /// </summary>
    public void AutoSave(GameState gameState)
    {
        SaveGame(gameState, "autosave");
    }

    /// <summary>
    /// Returns true if an autosave exists.
    /// </summary>
    public bool HasAutoSave()
    {
        return File.Exists(GetSlotPath("autosave"));
    }

    /// <summary>
    /// Lists all available save slots.
    /// </summary>
    public List<SaveSlotInfo> ListSaves()
    {
        var saves = new List<SaveSlotInfo>();

        if (!Directory.Exists(_savesDirectory))
            return saves;

        foreach (var file in Directory.GetFiles(_savesDirectory, "*.db"))
        {
            var info = new FileInfo(file);
            saves.Add(new SaveSlotInfo
            {
                SlotName = Path.GetFileNameWithoutExtension(file),
                FilePath = file,
                LastModified = info.LastWriteTimeUtc,
                SizeBytes = info.Length
            });
        }

        return saves.OrderByDescending(s => s.LastModified).ToList();
    }

    /// <summary>
    /// Deletes a save slot.
    /// </summary>
    public bool DeleteSave(string slotName)
    {
        string filePath = GetSlotPath(slotName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if a slot exists.
    /// </summary>
    public bool SlotExists(string slotName)
    {
        return File.Exists(GetSlotPath(slotName));
    }

    private string GetSlotPath(string slotName)
    {
        return Path.Combine(_savesDirectory, $"{slotName}.db");
    }
}

/// <summary>
/// Information about a save slot.
/// </summary>
public sealed class SaveSlotInfo
{
    public required string SlotName { get; init; }
    public required string FilePath { get; init; }
    public required DateTime LastModified { get; init; }
    public required long SizeBytes { get; init; }
}
