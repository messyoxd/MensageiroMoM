using System;

namespace MensageiroMoM.Classes
{
    public class Mensagem
    {
        public string _Mensagem { get; set; }
        public DateTime _EnviadoEm { get; set; }
        public Contato _EnviadoPor { get; set; }
        public Contato _RecebidaPor { get; set; }
        public bool _Lida { get; set; }
        public Mensagem(string mensagem, DateTime enviadoEm, Contato enviadoPor, Contato recebidaPor, bool Lida = false)
        {
            _EnviadoPor = enviadoPor;
            _EnviadoEm = enviadoEm;
            _Mensagem = mensagem;
            _RecebidaPor = recebidaPor;
            _Lida = Lida;
        }
    }
}