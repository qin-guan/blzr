namespace blzr.Models;

public sealed class ScheduleActivity
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Start { get; set; } = "00:00";

    public string End { get; set; } = "00:00";

    public string Personnel { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public ScheduleActivity Clone() =>
        new()
        {
            Id = Id,
            Title = Title,
            Start = Start,
            End = End,
            Personnel = Personnel,
            Location = Location
        };
}
