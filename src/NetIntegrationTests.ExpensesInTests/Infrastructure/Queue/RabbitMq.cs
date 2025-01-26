using System.Text.Json;
using Newtonsoft.Json;
using RabbitMQ.Client;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure.Queue;

	public static class RabbitMqConfig
	{
		public static string RabbitMqHostName = "Settings:RabbitMqHostName";
		public static string RabbitMqPort = "Settings:RabbitMqPort";
		public static string RabbitMqUsername = "Settings:RabbitMqUsername";
		public static string RabbitMqPassword = "Settings:RabbitMqPassword";
	}
	public class RabbitMq : IQueueEngine
	{
		private IConnection _connection;

		public static async Task<RabbitMq> CreateAsync(
			string hostUrl,
			string user,
			string password,
			int port)
		{
			var rabbitMq = new RabbitMq();
			await rabbitMq.InitializeConnection(hostUrl, user, password, port);
			return rabbitMq;
		}
		
		public static async Task<RabbitMq> CreateAsync(
			string hostUrl,
			string user,
			string password,
			string port)
		{
			if (int.TryParse(port, out var portInIntFormat))
			{
				var rabbitMq = new RabbitMq();
				await rabbitMq.InitializeConnection(hostUrl, user, password, portInIntFormat);
				return rabbitMq;
			}

			throw new InvalidCastException($"'{port}' is not a valid Int32 value for the 'port' connection string option for Rabbitmq.");

		}
		private RabbitMq() { }

		private async Task InitializeConnection(string hostUrl, string user, string password, int port)
		{
			var connectionFactory = new ConnectionFactory
			{
				HostName = hostUrl,
				UserName = user,
				Password = password,
				Port = port
			};

			_connection = await connectionFactory.CreateConnectionAsync();
		}

		public async Task PurgeQueues(params string[] queues)
		{
			foreach (var queue in queues)
			{
				await PurgeQueue(queue);
			}
		}

		public async Task PurgeQueue(string queue)
		{
			// RabbitMQ throws exception when queue does not exist. There is no possibility to check that queue exists.
			try
			{
				using (var channel = await _connection.CreateChannelAsync())
				{
					await channel.QueuePurgeAsync(queue);
				}
			}
			catch (Exception e)
			{ }
		}

		public async Task DeleteQueue(string queueName)
		{
			await using var channel = await _connection.CreateChannelAsync();
			await channel.ExchangeDeleteAsync(queueName);
			await channel.QueueDeleteAsync(queueName);
		}

		public async Task CreateQueueIfDoesNotExist(params string[] queues)
		{
			foreach (var queue in queues)
			{
				var queueName = queue.ToLower();

				await using var channel = await _connection.CreateChannelAsync();
				//QueueDeclare is an idempotent operation. Rabbit sdk does not expose Queue.Exists function.

				//Service queue
				await channel.QueueDeclareAsync(
					queueName,
					durable: true,
					exclusive: false,
					autoDelete: false);

				await channel.ExchangeDeclareAsync(
					queueName,
					type: "fanout",
					durable: true);

				await channel.QueueBindAsync(
					queueName, queueName, routingKey: $"");

				//Error queue
				var errorQueueName = $"{queueName}_error";

				await channel.QueueDeclareAsync(
					errorQueueName,
					durable: true,
					exclusive: false,
					autoDelete: false);

				await channel.ExchangeDeclareAsync(
					errorQueueName,
					type: "fanout",
					durable: true);

				await channel.QueueBindAsync(
					errorQueueName, errorQueueName, routingKey: "");
			}
		}

		public async Task BindQueueToExchange(string queue, string exchangeName)
		{
			var queueName = queue.ToLower();

			await using var channel = await _connection.CreateChannelAsync();
			await channel.ExchangeDeclareAsync(
				exchangeName,
				type: "fanout",
				durable: true);

			await channel.QueueBindAsync(
				queueName,
				exchangeName,
				$"");
		}
		
		public async Task BindExchangeToExchange(string fromExchange, string toExchange)
		{
			await using var channel = await _connection.CreateChannelAsync();
			await channel.ExchangeDeclareAsync(
				fromExchange,
				type: "fanout",
				durable: true);

			await channel.ExchangeBindAsync(
				toExchange,
				fromExchange,
				$"");
		}


		public async Task<string> ReadRawMessageFromQueue(
			string queue)
		{
			await using var channel = await _connection.CreateChannelAsync();
			var rabbitGetResult = await channel.BasicGetAsync(queue, true);

			// The queue does not contain messages
			if (rabbitGetResult == null)
				return null;
			

			using (var stream = new MemoryStream(rabbitGetResult.Body.ToArray()))
			{
				using (var streamReader = new StreamReader(stream))
				{
					var rawMessage =  await streamReader.ReadToEndAsync();
					
					var jsonDoc = JsonDocument.Parse(rawMessage);
					var root = jsonDoc.RootElement;

					//Implementation specific for MassTransit. You need to adjust impl for different library for RabbitMQ
					if (root.TryGetProperty("message", out var messageElement))
					{
						return messageElement.GetRawText();
					}

					return null;
				}
			}
		}

		public async Task<bool> QueueIsEmpty(string queue)
		{
			await using var channel = await _connection.CreateChannelAsync();
			var rabbitGetResult = await channel.BasicGetAsync(queue, true);
			return rabbitGetResult == null;
		}
		
		public void Dispose()
		{
			_connection?.Dispose();
		}
	}
