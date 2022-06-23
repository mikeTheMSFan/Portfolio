//Stack Exchange - Nick Kovalsky
//https://stackoverflow.com/questions/69961449/net6-and-datetime-problem-cannot-write-datetime-with-kind-utc-to-postgresql-ty

namespace Portfolio.Extensions;

public static class DateTimeExtensions
{
    public static DateTime? SetKindUtcNullable(this DateTime? dateTime)
    {
        if (dateTime.HasValue)
            return dateTime.Value.SetKindUtc();
        return null;
    }

    public static DateTime SetKindUtc(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }
}