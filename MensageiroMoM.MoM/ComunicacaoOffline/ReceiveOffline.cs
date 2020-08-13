using System;
using System.Text;
using MensageiroMoM.Classes;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MensageiroMoM.MoM
{
    public class ReceiveOffline
    {
        private readonly ConnectionFactory _factory;
        Action<Mensagem> ReceiveMessage;
        public ReceiveOffline(string ip, string porta, Action<Mensagem> receiveMessage)
        {
            this.ReceiveMessage = receiveMessage;
            try
            {
                _factory = new ConnectionFactory()
                {
                    HostName = ip,
                    Port = 5672,
                };
                // tentar conectar-se para checar se há algum erro com o servidor
                var connection = _factory.CreateConnection();
                connection.Close();
            }
            catch (System.Exception e)
            {

                throw e;
            }
        }
        public void Receive(string routingKey)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "Chat", type: "topic");

                var queueName = channel.QueueDeclare(
                    queue: routingKey,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                ).QueueName;

                channel.QueueBind(queue: queueName, exchange: "Chat", routingKey: routingKey);

                // Console.WriteLine(" [*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    
                    // var readOnlySpan = new ReadOnlySpan<byte>(body);
                    Mensagem message = null;
                    try
                    {
                        message = JsonConvert.DeserializeObject<Mensagem>(Encoding.UTF8.GetString(body));
                        // message = JsonSerializer.Deserialize<Mensagem>(readOnlySpan);
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    Console.WriteLine(" [x] Received '{0}':'{1}'", routingKey, message._Mensagem);

                    // Se mensagem não for 'Check', prosseguir normalmente
                    this.ReceiveMessage(message);

                    // só confirma mensagem quando o consumidor terminar de pegá-la
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                // Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}