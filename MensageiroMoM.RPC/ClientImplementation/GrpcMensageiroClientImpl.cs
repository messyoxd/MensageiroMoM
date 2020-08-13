
using MensageiroMoM.Classes;

namespace MensageiroMoM.RPC
{
    public class GrpcMensageiroClientImpl
    {
        private readonly Mensageiro.MensageiroClient _client;
        public GrpcMensageiroClientImpl(Mensageiro.MensageiroClient client)
        {
            _client = client;
        }
        public void SendMessage(Mensagem message)
        {
            var mensagem = new Message
            {
                EnviadoPor = new Contact { Ip = message._EnviadoPor.Ip, Porta = message._EnviadoPor.Port, Nome = message._EnviadoPor.Name },
                EnviadoPara = new Contact { Ip = message._RecebidaPor.Ip, Porta = message._RecebidaPor.Port, Nome = message._RecebidaPor.Name },
                Texto = message._Mensagem
            };
            _client.SendMessage(mensagem);
        }
    }
}