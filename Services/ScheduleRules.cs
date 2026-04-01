using System.Globalization;
using blzr.Models;

namespace blzr.Services;

public static class ScheduleRules
{
    private const string TimeFormat = "HH:mm";

    public static List<ScheduleActivity> CreateDefaultActivities() =>
    [
        new()
        {
            Id = "activity-1",
            Title = "Morning Briefing",
            Start = "08:30",
            End = "09:00",
            Personnel = "Alex Tan, Priya Nair",
            Location = "Operations Room"
        },
        new()
        {
            Id = "activity-2",
            Title = "Client Walkthrough",
            Start = "09:15",
            End = "10:30",
            Personnel = "Jamie Lee, Chen Wei, Maria Santos",
            Location = "Conference Hall A"
        },
        new()
        {
            Id = "activity-3",
            Title = "Systems Check",
            Start = "11:00",
            End = "12:00",
            Personnel = "Facilities Team",
            Location = "Control Center"
        },
        new()
        {
            Id = "activity-4",
            Title = "Afternoon Review",
            Start = "14:00",
            End = "15:00",
            Personnel = "Leadership Group",
            Location = "Boardroom"
        }
    ];

    public static (bool Ok, string? Error, List<ScheduleActivity> Normalized) ValidateActivities(IEnumerable<ScheduleActivity>? activities)
    {
        if (activities is null)
        {
            return (false, "JSON must be an array of activity objects.", []);
        }

        var normalized = new List<ScheduleActivity>();
        var index = 0;

        foreach (var source in activities)
        {
            var activity = NormalizeActivity(source);
            index += 1;

            if (string.IsNullOrWhiteSpace(activity.Title))
            {
                return (false, $"Activity {index} is missing a title.", []);
            }

            if (string.IsNullOrWhiteSpace(activity.Personnel))
            {
                return (false, $"Activity {index} is missing personnel.", []);
            }

            if (string.IsNullOrWhiteSpace(activity.Location))
            {
                return (false, $"Activity {index} is missing a location.", []);
            }

            if (!IsValidTimeText(activity.Start) || !IsValidTimeText(activity.End))
            {
                return (false, $"Activity {index} must use HH:mm for start and end times.", []);
            }

            if (!HasPositiveDuration(activity.Start, activity.End))
            {
                return (false, $"Activity {index} must end after it starts.", []);
            }

            normalized.Add(activity);
        }

        return (true, null, SortActivities(normalized));
    }

    public static bool IsValidTimeText(string? value) =>
        TryParseTime(value, out _);

    public static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(
            (value ?? string.Empty).Trim(),
            TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);

    public static bool HasPositiveDuration(string? start, string? end)
    {
        if (!TryParseTime(start, out var startTime) || !TryParseTime(end, out var endTime))
        {
            return false;
        }

        return endTime > startTime;
    }

    public static string GetSuggestedStartTime(IEnumerable<ScheduleActivity> activities)
    {
        var latest = SortActivities(activities).LastOrDefault();
        return latest?.End ?? "09:00";
    }

    public static string GetSuggestedEndTime(string startTime)
    {
        if (!TryParseTime(startTime, out var parsedStart))
        {
            return "10:00";
        }

        return parsedStart.Add(TimeSpan.FromHours(1)).ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

    public static List<ScheduleActivity> SortActivities(IEnumerable<ScheduleActivity> activities) =>
        activities
            .Select(NormalizeActivity)
            .OrderBy(activity => ParseSortableTime(activity.Start))
            .ThenBy(activity => ParseSortableTime(activity.End))
            .ThenBy(activity => activity.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static ScheduleActivity NormalizeActivity(ScheduleActivity? activity) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(activity?.Id) ? Guid.NewGuid().ToString("N") : activity.Id.Trim(),
            Title = (activity?.Title ?? string.Empty).Trim(),
            Start = (activity?.Start ?? "00:00").Trim(),
            End = (activity?.End ?? "00:00").Trim(),
            Personnel = (activity?.Personnel ?? string.Empty).Trim(),
            Location = (activity?.Location ?? string.Empty).Trim()
        };

    private static TimeOnly ParseSortableTime(string? value) =>
        TryParseTime(value, out var time) ? time : TimeOnly.MinValue;
}
