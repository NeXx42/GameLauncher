using CSharpSqliteORM;
using CSharpSqliteORM.Structure;

namespace GameLibrary.Logic.Helpers;

public class ConfigProvider<ENUMTYPE>
    where ENUMTYPE : struct, Enum
{
    private Dictionary<ENUMTYPE, string?> data;

    private Func<string, string, Task>? handleSave;
    private Func<string, Task>? handleDelete;

    public ConfigProvider(ConfigProvider<ENUMTYPE> copy)
    {
        data = new Dictionary<ENUMTYPE, string?>(copy.data);

        handleDelete = null;
        handleSave = null;
    }

    public ConfigProvider(IEnumerable<(string, string?)> input, Func<string, string, Task> handleSave, Func<string, Task> handleDelete)
    {
        data = new Dictionary<ENUMTYPE, string?>();

        foreach ((string key, string? value) in input)
        {
            if (!Enum.TryParse(key, out ENUMTYPE id))
                continue;

            data.Add(id, value);
        }

        this.handleSave = handleSave;
        this.handleDelete = handleDelete;
    }

    // save

    public async Task SaveGeneric<T>(ENUMTYPE key, T obj)
    {
        switch (typeof(T).Name)
        {
            case nameof(String): await SaveValue(key, Convert.ToString(obj)); break;
            case nameof(Boolean): await SaveBool(key, Convert.ToBoolean(obj)); break;

            case nameof(Enum):
            case nameof(Int32): await SaveInteger(key, Convert.ToInt32(obj)); break;
        }
    }

    public async Task SaveEnum<T>(ENUMTYPE key, T v) where T : Enum => await SaveInteger(key, Convert.ToInt32(v));
    public async Task SaveInteger(ENUMTYPE key, int v) => await SaveValue(key, v.ToString());
    public async Task SaveBool(ENUMTYPE key, bool b) => await SaveValue(key, b ? "1" : "0");

    public async Task SaveValue(ENUMTYPE key, string? val)
    {
        if (string.IsNullOrEmpty(val))
        {
            if (handleDelete != null)
                await handleDelete(key.ToString());

            data.Remove(key);
            return;
        }

        if (handleSave != null)
            await handleSave(key.ToString(), val);

        data[key] = val;
    }

    // get

    public async Task<T> GetGeneric<T>(ENUMTYPE key, T defaultVal)
    {
        if (!TryGetValue(key, out string res))
            return defaultVal;

        switch (typeof(T).Name)
        {
            case nameof(String): return (T)Enum.ToObject(typeof(T), int.Parse(res));
            case nameof(Boolean): return (T)(object)(res == "1");

            case nameof(Enum): return (T)(object)(res);
            case nameof(Int32): return (T)(object)int.Parse(res);
        }

        return defaultVal;
    }

    public T GetEnum<T>(ENUMTYPE key, T defaultVal) where T : Enum
    {
        if (TryGetValue(key, out string res))
            return (T)Enum.ToObject(typeof(T), int.Parse(res));

        return defaultVal;
    }

    public int GetInteger(ENUMTYPE key, int defaultVal)
    {
        if (TryGetValue(key, out string res))
            return int.Parse(res);

        return defaultVal;
    }

    public bool GetBoolean(ENUMTYPE key, bool defaultVal)
    {
        if (TryGetValue(key, out string res))
            return res == "1";

        return defaultVal;
    }

    public string? GetValue(ENUMTYPE key)
    {
        if (data.TryGetValue(key, out string? res))
            return res;

        return null;
    }

    public bool TryGetValue(ENUMTYPE key, out string val)
    {
        val = GetValue(key) ?? string.Empty;
        return !string.IsNullOrEmpty(val);
    }
}
