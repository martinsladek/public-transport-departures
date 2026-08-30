namespace Odjezdy;

public sealed class GtfsCalendar
{
    private readonly Dictionary<string, CalendarRow> _regular = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<DateOnly, int>> _exceptions = new(StringComparer.Ordinal);

    public void AddRegular(
        string serviceId,
        bool[] weekdaysMondayFirst,
        DateOnly start,
        DateOnly end)
    {
        _regular[serviceId] = new CalendarRow(weekdaysMondayFirst, start, end);
    }

    /// <param name="exceptionType">1 = added, 2 = removed.</param>
    public void AddException(string serviceId, DateOnly date, int exceptionType)
    {
        if (!_exceptions.TryGetValue(serviceId, out Dictionary<DateOnly, int>? byDate))
        {
            byDate = [];
            _exceptions[serviceId] = byDate;
        }

        byDate[date] = exceptionType;
    }

    public bool IsActive(string serviceId, DateOnly date)
    {
        if (_exceptions.TryGetValue(serviceId, out Dictionary<DateOnly, int>? byDate)
            && byDate.TryGetValue(date, out int type))
        {
            return type == 1;
        }

        if (!_regular.TryGetValue(serviceId, out CalendarRow row))
            return false;

        if (date < row.Start || date > row.End)
            return false;

        int index = ((int)date.DayOfWeek + 6) % 7;
        return row.Weekdays[index];
    }

    private readonly record struct CalendarRow(bool[] Weekdays, DateOnly Start, DateOnly End);
}
