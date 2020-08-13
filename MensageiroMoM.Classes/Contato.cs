namespace MensageiroMoM.Classes
{
    public class Contato
    {
        public string Port { get; set; }
        public string Ip { get; set; }
        public string Name { get; set; }
        public Contato(string name, string port, string ip)
        {
            this.Name = name;
            this.Ip = ip;
            this.Port = port;
        }

        public string SendingRoute(string nome)
        {
            return nome + "." + this.Name;
        }
        public string ReceivingRoute(string nome)
        {
            return this.Name + "." + nome;
        }
    }
}