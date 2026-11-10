using BuildingBlocks.Messaging;

namespace UnitTests.Fakes;

public sealed class RecordingEventPublisher : IEventPublisher
{
    public List<(string TopicId, string EventType, object Data)> Published { get; } = [];

    public Task PublishAsync<TData>(
        string topicId,
        string eventType,
        TData data,
        CancellationToken cancellationToken = default) =>
        PublishAsync(topicId, eventType, data, 1, cancellationToken);

    public Task PublishAsync<TData>(
        string topicId,
        string eventType,
        TData data,
        int version,
        CancellationToken cancellationToken = default)
    {
        Published.Add((topicId, eventType, data!));
        return Task.CompletedTask;
    }
}
