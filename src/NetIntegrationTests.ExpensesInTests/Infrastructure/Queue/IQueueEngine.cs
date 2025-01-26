namespace NetIntegrationTests.ExpensesInTests.Infrastructure.Queue;

public interface IQueueEngine: IDisposable
{
    Task PurgeQueues(params string[] queues);
    Task PurgeQueue(string queue);
    Task DeleteQueue(string queueName);
    Task CreateQueueIfDoesNotExist(params string[] queues);
    Task<string> ReadRawMessageFromQueue(string queue);
    Task BindQueueToExchange(string queue, string exchangeName);
    public Task<bool> QueueIsEmpty(string queue);
    Task BindExchangeToExchange(string fromExchange, string toExchange);
}