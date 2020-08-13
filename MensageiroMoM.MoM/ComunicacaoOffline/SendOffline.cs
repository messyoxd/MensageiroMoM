using System;
using System.Text;
using System.Text.Json;
using MensageiroMoM.Classes;
using RabbitMQ.Client;

namespace MensageiroMoM.MoM
{
    public class SendOffline
    {
        private readonly ConnectionFactory _factory;
        public SendOffline(string ip, string porta)
        {
            try
            {
                _factory =  new ConnectionFactory()
                {
                    HostName = ip,
                    Port = 5672,
                };
                var connection = _factory.CreateConnection();
                connection.Close();
            }
            catch (System.Exception e)
            {
                
                throw e;
            }
        }
        public void Send(string exchangeName, string routingKey, Mensagem message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // certificar que a exchange existe
                channel.ExchangeDeclare(exchange: exchangeName, type: "topic");
                
                var json = JsonSerializer.Serialize(message);

                var body = Encoding.UTF8.GetBytes(json);

                var props = channel.CreateBasicProperties();
                props.Persistent = true;
                Console.WriteLine(routingKey);
                channel.BasicPublish(exchange: exchangeName,
                                        routingKey: routingKey, //"fulano.cicrano"
                                        basicProperties: props,
                                        body: body);

                Console.WriteLine(" [x] Sent '{0}':'{1}'", routingKey, message._Mensagem);
            }
        }
    }
}