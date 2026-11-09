using System.Threading.Channels;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Reports;

public sealed class ReportJobQueue : IReportJobQueue
{
    private readonly Channel<Guid> queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid reportId, CancellationToken cancellationToken)
        => queue.Writer.WriteAsync(reportId, cancellationToken);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => queue.Reader.ReadAsync(cancellationToken);
}

