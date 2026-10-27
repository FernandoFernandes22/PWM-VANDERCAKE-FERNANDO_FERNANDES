using SQLite;
using System.Text.Json;
using VanderCake.Contracts;
namespace VanderCake.Mobile.Services;

public class LocalDraft
{
    [PrimaryKey] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Indexed] public long UserId { get; set; }
    public long ClientId { get; set; }
    public string ClientName { get; set; } = "";
    public string Payload { get; set; } = "";
    public string State { get; set; } = "EDITANDO";
    public string AttemptKey { get; set; } = "";
    public string? ServerOrderId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
public class CacheEntry
{
    [PrimaryKey] public string Key { get; set; } = "";
    public long UserId { get; set; }
    public string Json { get; set; } = "";
}
public class DraftStore
{
    private SQLiteAsyncConnection? connection;
    private readonly SemaphoreSlim gate = new(1, 1);
    private async Task<SQLiteAsyncConnection> Db()
    {
        await gate.WaitAsync();
        try
        {
            if (connection is not null) return connection;
            var db = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "vandercake.db3"));
            await db.CreateTableAsync<LocalDraft>(); await db.CreateTableAsync<CacheEntry>(); connection = db; return db;
        }
        finally { gate.Release(); }
    }
    public async Task Save(LocalDraft draft) { draft.UpdatedAtUtc = DateTime.UtcNow; await (await Db()).InsertOrReplaceAsync(draft); }
    public async Task<List<LocalDraft>> List(long user) => await (await Db()).Table<LocalDraft>().Where(x => x.UserId == user && x.State != "ENVIADO").OrderByDescending(x => x.UpdatedAtUtc).ToListAsync();
    public async Task Delete(LocalDraft draft)
    {
        if (draft.State != "EDITANDO") throw new InvalidOperationException("Confirme o resultado do envio antes de excluir.");
        await (await Db()).DeleteAsync(draft);
    }
    public async Task Cache<T>(long user, string name, T value) => await (await Db()).InsertOrReplaceAsync(new CacheEntry { Key = user + ":" + name, UserId = user, Json = JsonSerializer.Serialize(value, ApiJson.Options) });
    public async Task<T?> Cached<T>(long user, string name)
    {
        var row = await (await Db()).FindAsync<CacheEntry>(user + ":" + name); return row is null ? default : JsonSerializer.Deserialize<T>(row.Json, ApiJson.Options);
    }
    public async Task ClearOtherCaches(long user) => await (await Db()).ExecuteAsync("DELETE FROM CacheEntry WHERE UserId <> ?", user);
}
