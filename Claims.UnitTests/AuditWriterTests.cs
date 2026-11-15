using System.Threading.Channels;
using Claims.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Claims.UnitTests;

public class AuditWriterTests
{
    private static readonly DateTime Created = new(2027, 1, 10, 12, 30, 0, DateTimeKind.Utc);

    private readonly Channel<AuditMessage> _channel = Channel.CreateUnbounded<AuditMessage>();

    [Fact]
    public async Task Queued_messages_are_saved_in_batches_of_the_batch_size()
    {
        var saves = new SaveInterceptor();
        Enqueue(Enumerable.Range(1, 250).Select(i => $"cover-{i}"));

        var rows = await RunWriterAsync(saves, batchSize: 100);

        Assert.Equal(250, rows.Count);
        Assert.Equal([100, 100, 50], saves.RowsPerSave);
    }

    [Fact]
    public async Task When_a_batch_fails_its_messages_are_saved_one_by_one_so_only_the_bad_one_is_lost()
    {
        var saves = new SaveInterceptor(failFor: "cover-3");
        Enqueue(["cover-1", "cover-2", "cover-3", "cover-4", "cover-5"]);

        var rows = await RunWriterAsync(saves, batchSize: 100);

        Assert.Equal(["cover-1", "cover-2", "cover-4", "cover-5"], rows.Order());
        Assert.Equal([5, 1, 1, 1, 1, 1], saves.RowsPerSave);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_batch_size_below_one_is_rejected(int batchSize) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateWriter(new ServiceCollection().BuildServiceProvider(), batchSize));

    private void Enqueue(IEnumerable<string> coverIds)
    {
        foreach (var coverId in coverIds)
        {
            Assert.True(_channel.Writer.TryWrite(new AuditMessage(AuditedEntity.Cover, coverId, "POST", Created)));
        }

        _channel.Writer.Complete();
    }

    // The queue is filled and closed before the writer starts, so the batches don't depend on timing.
    private async Task<List<string>> RunWriterAsync(SaveInterceptor saves, int batchSize)
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection()
            .AddDbContext<AuditContext>(options => options.UseInMemoryDatabase(databaseName).AddInterceptors(saves))
            .BuildServiceProvider();

        var writer = CreateWriter(services, batchSize);
        await writer.StartAsync(TestContext.Current.CancellationToken);
        await writer.ExecuteTask!;

        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AuditContext>().CoverAudits
            .Select(audit => audit.CoverId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private AuditWriter CreateWriter(IServiceProvider services, int batchSize) =>
        new(_channel, services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AuditWriter>.Instance, batchSize);

    /// <summary>Records how many rows each <c>SaveChanges</c> tried to save, and fails any save that includes <c>failFor</c>.</summary>
    private sealed class SaveInterceptor(string? failFor = null) : SaveChangesInterceptor
    {
        public List<int> RowsPerSave { get; } = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var coverIds = eventData.Context!.ChangeTracker.Entries<CoverAudit>().Select(entry => entry.Entity.CoverId).ToList();
            RowsPerSave.Add(coverIds.Count);

            if (failFor is not null && coverIds.Contains(failFor))
            {
                throw new InvalidOperationException($"Simulated failure for {failFor}.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
