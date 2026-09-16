using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
