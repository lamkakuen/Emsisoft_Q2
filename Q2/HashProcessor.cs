using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Data.SqlClient;

namespace Q2
{
    public class HashProcessor
    {
        private const string RabbitMQHost = "localhost"; // Replace with your RabbitMQ host
        private const string RabbitMQQueue = "hashes"; // Replace with your RabbitMQ queue name
        private const string ServerName = "GEOFF\\SQLEXPRESS"; // Replace with your SQL Server instance name
        private const string DatabaseName = "master"; // Replace with your database name
        private const string Username = "GEOFF\\lamka"; // Replace with your SQL Server username
        private const string Password = ""; // Replace with your SQL Server password

        private const string ConnectionString = $"Server={ServerName};Database={DatabaseName};User Id={Username};Password={Password};Encrypt = false;TrustServerCertificate = true;";
        private const int MaxConcurrentThreads = 4;

        private readonly object lockObject = new object();
        private readonly AutoResetEvent workerEvent = new AutoResetEvent(false);
        private int runningThreads = 0;

        public void ProcessHashes()
        {
            var factory = new ConnectionFactory { HostName = RabbitMQHost };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: RabbitMQQueue, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, args) =>
            {
                var body = args.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                SaveHashToDatabase(message);

                lock (lockObject)
                {
                    runningThreads--;
                    workerEvent.Set();
                }
            };

            channel.BasicConsume(queue: RabbitMQQueue, autoAck: true, consumer: consumer);

            while (true)
            {
                lock (lockObject)
                {
                    if (runningThreads >= MaxConcurrentThreads)
                    {
                        workerEvent.WaitOne();
                    }

                    runningThreads++;
                }
            }
        }

        private void SaveHashToDatabase(string hash)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            using var command = new SqlCommand("INSERT INTO hashes (date, sha1) VALUES (@Date, @Sha1)", connection);
            command.Parameters.AddWithValue("@Date", DateTime.Now);
            command.Parameters.AddWithValue("@Sha1", hash);

            command.ExecuteNonQuery();
        }
    }
}
