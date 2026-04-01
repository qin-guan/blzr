using System.Text.Json;
using blzr.Configuration;
using blzr.Models;
using Microsoft.Extensions.Options;

namespace blzr.Services;

public sealed class RoomStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<RoomStore> _logger;
    private readonly string _storagePath;
    private List<ScheduleRoom> _rooms;

    public RoomStore(
        IWebHostEnvironment environment,
        IOptions<RoomStoreOptions> options,
        ILogger<RoomStore> logger)
    {
        _logger = logger;

        var configuredPath = options.Value.Path;
        _storagePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        _rooms = LoadRooms();
    }

    public event EventHandler<RoomChangedEventArgs>? Changed;

    public async Task<IReadOnlyList<ScheduleRoom>> GetRoomsAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _rooms
                .OrderBy(room => room.Name, StringComparer.OrdinalIgnoreCase)
                .Select(room => room.Clone())
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ScheduleRoom?> GetRoomAsync(string roomId)
    {
        await _gate.WaitAsync();
        try
        {
            return _rooms.FirstOrDefault(room => room.Id == roomId)?.Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ScheduleRoom> CreateRoomAsync(string name)
    {
        RoomChangedEventArgs? change = null;
        ScheduleRoom createdRoom;

        await _gate.WaitAsync();
        try
        {
            createdRoom = new ScheduleRoom
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name.Trim(),
                Activities = [],
                UpdatedUtc = DateTimeOffset.UtcNow
            };

            _rooms.Add(createdRoom);
            await PersistAsync();
            change = new RoomChangedEventArgs(createdRoom.Id, RoomChangeType.Created);
        }
        finally
        {
            _gate.Release();
        }

        NotifyChanged(change);
        return createdRoom.Clone();
    }

    public async Task<ScheduleRoom?> UpdateRoomNameAsync(string roomId, string name)
    {
        RoomChangedEventArgs? change = null;
        ScheduleRoom? updated = null;

        await _gate.WaitAsync();
        try
        {
            var room = _rooms.FirstOrDefault(item => item.Id == roomId);
            if (room is null)
            {
                return null;
            }

            room.Name = name.Trim();
            room.UpdatedUtc = DateTimeOffset.UtcNow;
            await PersistAsync();

            updated = room.Clone();
            change = new RoomChangedEventArgs(room.Id, RoomChangeType.Updated);
        }
        finally
        {
            _gate.Release();
        }

        NotifyChanged(change);
        return updated;
    }

    public async Task<ScheduleRoom?> SaveScheduleAsync(string roomId, IEnumerable<ScheduleActivity> activities)
    {
        var validation = ScheduleRules.ValidateActivities(activities);
        if (!validation.Ok)
        {
            throw new InvalidOperationException(validation.Error);
        }

        RoomChangedEventArgs? change = null;
        ScheduleRoom? updated = null;

        await _gate.WaitAsync();
        try
        {
            var room = _rooms.FirstOrDefault(item => item.Id == roomId);
            if (room is null)
            {
                return null;
            }

            room.Activities = validation.Normalized.Select(activity => activity.Clone()).ToList();
            room.UpdatedUtc = DateTimeOffset.UtcNow;
            await PersistAsync();

            updated = room.Clone();
            change = new RoomChangedEventArgs(room.Id, RoomChangeType.Updated);
        }
        finally
        {
            _gate.Release();
        }

        NotifyChanged(change);
        return updated;
    }

    public async Task<bool> DeleteRoomAsync(string roomId)
    {
        RoomChangedEventArgs? change = null;

        await _gate.WaitAsync();
        try
        {
            var removed = _rooms.RemoveAll(room => room.Id == roomId) > 0;
            if (!removed)
            {
                return false;
            }

            await PersistAsync();
            change = new RoomChangedEventArgs(roomId, RoomChangeType.Deleted);
        }
        finally
        {
            _gate.Release();
        }

        NotifyChanged(change);
        return true;
    }

    private void NotifyChanged(RoomChangedEventArgs? change)
    {
        if (change is null)
        {
            return;
        }

        Changed?.Invoke(this, change);
    }

    private List<ScheduleRoom> LoadRooms()
    {
        try
        {
            EnsureStorageDirectory();

            if (!File.Exists(_storagePath))
            {
                var seededRooms = CreateSeedRooms();
                PersistSync(seededRooms);
                return seededRooms;
            }

            var json = File.ReadAllText(_storagePath);
            var parsed = JsonSerializer.Deserialize<List<ScheduleRoom>>(json, JsonOptions);
            var normalized = NormalizeRooms(parsed);

            if (normalized.Count == 0)
            {
                normalized = CreateSeedRooms();
                PersistSync(normalized);
            }

            return normalized;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to load room storage from {StoragePath}. Falling back to seeded data.", _storagePath);
            var seededRooms = CreateSeedRooms();
            PersistSync(seededRooms);
            return seededRooms;
        }
    }

    private async Task PersistAsync()
    {
        EnsureStorageDirectory();
        var json = JsonSerializer.Serialize(_rooms, JsonOptions);
        await File.WriteAllTextAsync(_storagePath, json);
    }

    private void PersistSync(List<ScheduleRoom> rooms)
    {
        EnsureStorageDirectory();
        var json = JsonSerializer.Serialize(rooms, JsonOptions);
        File.WriteAllText(_storagePath, json);
    }

    private void EnsureStorageDirectory()
    {
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static List<ScheduleRoom> NormalizeRooms(IEnumerable<ScheduleRoom>? rooms)
    {
        if (rooms is null)
        {
            return [];
        }

        return rooms
            .Select(room => new ScheduleRoom
            {
                Id = string.IsNullOrWhiteSpace(room.Id) ? Guid.NewGuid().ToString("N") : room.Id.Trim(),
                Name = string.IsNullOrWhiteSpace(room.Name) ? "Untitled Room" : room.Name.Trim(),
                UpdatedUtc = room.UpdatedUtc == default ? DateTimeOffset.UtcNow : room.UpdatedUtc,
                Activities = ScheduleRules.SortActivities(room.Activities ?? [])
            })
            .ToList();
    }

    private static List<ScheduleRoom> CreateSeedRooms() =>
    [
        new()
        {
            Id = "operations-room",
            Name = "Operations Room",
            UpdatedUtc = DateTimeOffset.UtcNow,
            Activities = ScheduleRules.CreateDefaultActivities()
        }
    ];
}

public sealed class RoomChangedEventArgs(string roomId, RoomChangeType changeType) : EventArgs
{
    public string RoomId { get; } = roomId;

    public RoomChangeType ChangeType { get; } = changeType;
}

public enum RoomChangeType
{
    Created,
    Updated,
    Deleted
}
