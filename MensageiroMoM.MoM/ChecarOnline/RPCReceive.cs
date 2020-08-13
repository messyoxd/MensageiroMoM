using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MensageiroMoM.MoM
{
    public class RPCReceive
    {
        private readonly ConnectionFactory _factory;
        public IConnection _connection { get; set; }
        public IModel _channel { get; set; }
        public Func<bool> _getEstado { get; set; }
        public Func<string> _getContatoAtual { get; }
        public RPCReceive(string ip, int porta, Func<bool> getEstado, Func<string> getContatoAtual)
        {
            _getContatoAtual = getContatoAtual;
            _getEstado = getEstado;
            // this.ReceiveMessage = receiveMessage;
            try
            {
                _factory = new ConnectionFactory()
                {
                    HostName = ip,
                    Port = porta,
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

        public void Receive(string MyName, string exchange)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: exchange, type: "topic");
                var queueName = channel.QueueDeclare(queue: MyName, // b
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null
                                        ).QueueName;
                channel.QueueBind(queue: queueName, exchange: exchange, routingKey: "*." + MyName + "Check"); // *.b
                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                consumer.Received += (model, ea) =>
                {
                    //quando receber uma mensagem do cliente
                    string response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;
                    replyProps.Persistent = true;
                    try
                    {
                        var message = Encoding.UTF8.GetString(body);

                        // Console.WriteLine(" [.] {0}:{1}", message, MyName);
                        // response = fib(n).ToString();
                        response = _getEstado().ToString() + "."+_getContatoAtual();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" [.] " + e.Message);
                        response = "";
                    }
                    finally
                    {
                        // enviar confirmacao
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        //                                                      messyo.anonimo
                        channel.BasicPublish(exchange: exchange, routingKey: MyName + "." + props.ReplyTo + "Check", basicProperties: replyProps, body: responseBytes);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                };


                // Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}