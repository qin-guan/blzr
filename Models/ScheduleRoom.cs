namespace blzr.Models;

public sealed class ScheduleRoom
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<ScheduleActivity> Activities { get; set; } = [];

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public ScheduleRoom Clone() =>
        new()
        {
            Id = Id,
            Name = Name,
            UpdatedUtc = UpdatedUtc,
            Activities = Activities.Select(activity => activity.Clone()).ToList()
        };
}
