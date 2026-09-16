namespace SaviaUp.Backend.Domain.Results;

public sealed record PageData<T>(IReadOnlyCollection<T> Items, int TotalCount);
