using System;
using System.Threading.Tasks;
using Grpc.Core;
using MensageiroMoM.Classes;

namespace MensageiroMoM.RPC
{
    public class GrpcMensageiroServerImpl : Mensageiro.MensageiroBase
    {
        private readonly string _myName;
        private readonly Action<Mensagem> _receiveMessage;
        private readonly Action<Mensagem, bool> _insertMessage;

        public GrpcMensageiroServerImpl(string MyName, Action<Mensagem> ReceiveMessage, Action<Mensagem, bool> InsertMessage)
        {
            _myName = MyName;
            _receiveMessage = ReceiveMessage;
            _insertMessage = InsertMessage;
        }

        public override Task<Message> SendMessage(Message request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult(TratarSendMensagem(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public Message TratarSendMensagem(Message m)
        {
            var mensagem = new Mensagem(
                    m.Texto,
                    DateTime.Now,
                    new Contato(m.EnviadoPor.Nome, m.EnviadoPor.Porta, m.EnviadoPor.Ip),
                    new Contato(m.EnviadoPara.Nome, m.EnviadoPara.Porta, m.EnviadoPara.Ip),
                    false
                );
            if(_myName == m.EnviadoPor.Nome)
                _insertMessage(
                    mensagem,
                    true
                );
            else
                _insertMessage(
                    mensagem,
                    false
                );
            _receiveMessage(mensagem);
            return new Message
            {
                EnviadoPor = m.EnviadoPor,
                EnviadoPara = m.EnviadoPara,
                Texto = m.Texto
            };
        }
    }
}