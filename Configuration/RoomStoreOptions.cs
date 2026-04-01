namespace blzr.Configuration;

public sealed class RoomStoreOptions
{
    public const string SectionName = "RoomStore";

    public string Path { get; set; } = "Data/rooms.json";
}
