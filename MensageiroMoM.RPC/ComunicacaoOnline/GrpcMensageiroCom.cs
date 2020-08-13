using System;
using System.Threading;
using Grpc.Core;
using MensageiroMoM.Classes;

namespace MensageiroMoM.RPC
{
    public class GrpcMensageiroCom
    {
        private readonly string _myName;
        private readonly string _myIp;
        private readonly string _myPort;
        private readonly Action<Mensagem> _receiveMessage;
        private readonly Action<Mensagem, bool> _insertMessage;
        private GrpcMensageiroClientImpl _client;
        public Server _server;
        public Thread _servidor;
        public GrpcMensageiroCom(string MyName, string MyIp, string MyPort, Action<Mensagem> ReceiveMessage, Action<Mensagem, bool> InsertMessage)
        {
            _myName = MyName;
            _myIp = MyIp;
            _myPort = MyPort;
            _receiveMessage = ReceiveMessage;
            _insertMessage = InsertMessage;
        }
        public void EnviarMensagemPeloChat(Mensagem mensagem)
        {
            _client.SendMessage(mensagem);
            _insertMessage(
                mensagem,
                true
            );
            _receiveMessage(mensagem);
        }
        // public void iniciarConexao()
        // {
        //     _servidor = new Thread(() => IniciarServidor());
        //     _servidor.Start();
        // }
        public void IniciarServidor()
        {
            try
            {
                _server = new Server
                {

                    Services = { Mensageiro.BindService(new GrpcMensageiroServerImpl(
                        _myName,
                        _receiveMessage,
                        _insertMessage
                    )) },
                    Ports = { new ServerPort(_myIp, int.Parse(_myPort), ServerCredentials.Insecure) }
                };
                _server.Start();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public void ConectarComContato(Contato contato)
        {
            try
            {
                // criar canal de conexao com o outro jogador
                var channel = new Channel($"{contato.Ip}:{contato.Port}", ChannelCredentials.Insecure);
                // conectar com o outro jogador
                _client = new GrpcMensageiroClientImpl(new Mensageiro.MensageiroClient(channel));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}