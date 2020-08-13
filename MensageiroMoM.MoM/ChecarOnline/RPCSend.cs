using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MensageiroMoM.MoM
{
    public class Aux{
        public bool estado = true;
        public string contatoAtual = "";

        public Aux(bool estado, string contatoAtual)
        {
            this.estado = estado;
            this.contatoAtual = contatoAtual;
        }
    }
    public class RPCClient
    {
        private string MyQueueName;
        private EventingBasicConsumer consumer;
        private ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper =
                    new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        public ConnectionFactory _factory { get; set; }
        public IConnection _connection { get; set; }
        public IModel _channel { get; set; }
        public bool Estado = true;
        public string ContatoAtual = "";
        public RPCClient(string ip, int porta, string MyName, string exchangeName)
        {
            try
            {
                _factory = new ConnectionFactory()
                {
                    HostName = ip,
                    Port = porta,
                };
                var connection = _factory.CreateConnection();
                connection.Close();
            }
            catch (System.Exception e)
            {

                throw e;
            }
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            ////////// Preparar o callback do servidor
            _channel.ExchangeDeclare(exchange: exchangeName, type: "topic");

            MyQueueName = _channel.QueueDeclare(
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null).QueueName;
            Console.WriteLine("O nome dessa queue: " + MyQueueName);
            _channel.QueueBind(queue: MyQueueName, exchange: exchangeName, routingKey: "*." + MyQueueName + "Check"); // *.messyoCheck
            consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                // Callback das respostas do servidor
                if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string> tcs))
                    return;
                var body = ea.Body;
                var response = Encoding.UTF8.GetString(body);
                Estado = Boolean.Parse(response.Split(".").ElementAt(0));
                ContatoAtual = response.Split(".").ElementAt(1);
                tcs.TrySetResult(response);
            };
        }
        // public void CallBack(string exchangeName)
        // {
        //     _connection = _factory.CreateConnection();
        //     _channel = _connection.CreateModel();
        //     ////////// Preparar o callback do servidor
        //     _channel.ExchangeDeclare(exchange: exchangeName, type: "topic");

        //     _channel.QueueDeclare(queue: MyQueueName,
        //                             durable: true,
        //                             exclusive: false,
        //                             autoDelete: false,
        //                             arguments: null);

        //     _channel.QueueBind(queue: MyQueueName, exchange: exchangeName, routingKey: "*." + MyQueueName); // *.messyo
        //     consumer = new EventingBasicConsumer(_channel);
        //     consumer.Received += (model, ea) =>
        //     {
        //         // Callback das respostas do servidor
        //         if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string> tcs))
        //             return;
        //         var body = ea.Body;
        //         var response = Encoding.UTF8.GetString(body);
        //         Console.WriteLine("Mensagem mandada devolta do servidor, confirmando sua entrega");
        //         tcs.TrySetResult(response);
        //     };
        // }
        public Aux Call(
            string MyName, string remetente,
            string exchangeName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // _connection = _factory.CreateConnection();
            // _channel = _connection.CreateModel();
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //////// Enviar mensagem ao servidor, A.K.A outro cliente
                var routingKey = MyName + "." + remetente; // a.bCheck
                var mensagem = MyName;

                IBasicProperties props = channel.CreateBasicProperties();
                var correlationId = Guid.NewGuid().ToString();
                props.CorrelationId = correlationId;
                props.ReplyTo = MyQueueName;
                props.Persistent = true;

                var messageBytes = Encoding.UTF8.GetBytes(mensagem);
                var tcs = new TaskCompletionSource<string>();
                callbackMapper.TryAdd(correlationId, tcs);

                // manda a mensagem
                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: MyName + "." + remetente + "Check", // a.b
                    basicProperties: props,
                    body: messageBytes);


                // começa a consumir mensagens da fila MyQueueName
                channel.BasicConsume(
                    consumer: consumer,
                    queue: MyQueueName, // a
                    autoAck: true);
                channel.BasicNacks += (s, ea) =>
                {
                    Console.WriteLine("erro!");
                };
                cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out var tmp));
                if (!WaitUntil(2000, () => tcs.Task.IsCompleted))
                    return null;
                else{

                    return new Aux(Estado, ContatoAtual);
                }
            }
        }
        public void Close()
        {
            _connection.Close();
        }
        private static bool WaitUntil(int numberOfMiliSeconds, Func<bool> condition)
        {
            int waited = 0;
            while (!condition() && waited < numberOfMiliSeconds)
            {
                Thread.Sleep(100);
                waited += 100;
            }

            return condition();
        }
    }
}