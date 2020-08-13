using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using System.Threading;
using MensageiroMoM.Classes;
using MensageiroMoM.MoM;
using MensageiroMoM.RPC;

namespace MensageiroMoM.GUI
{
    public class TelaPrincipal
    {
        public string MyName { get; set; }
        public string MyPort { get; set; }
        public string MyIP { get; set; }
        public RPCClient checkOn { get; set; }
        public RPCReceive beCheckOn { get; set; }
        public ReceiveOffline receiveOffline { get; set; }
        public SendOffline sendOffline { get; set; }
        public HorizontalSplitPane horizontalSplitPane { get; set; }
        public VerticalStackPanel verticalStackPanel { get; set; }
        public List<Contato> contatos { get; set; }
        public Dictionary<string, List<Mensagem>> batePapo { get; set; }
        public ScrollViewer CurrentScrollViewer { get; set; }
        public TextBox CurrentTextBox { get; set; }
        public Action<string> ShowErrors { get; set; }
        public bool ChatEnabled { get; set; }
        public bool Estado = true;
        public string contatoAtual = "";
        public GrpcMensageiroCom _com { get; set; }
        public Thread comThread { get; set; }
        public TelaPrincipal(string myName, string myIP, string myPort, Action<string> showErrors)
        {
            try
            {
                // objeto para confirmar se os outros estão on
                checkOn = new RPCClient(myIP, 5672, myName, "confirmarOnline");
                beCheckOn = new RPCReceive(myIP, 5672, getEstado, getContatoAtual);
                // Thread checkOnCallBackThread = new Thread(() => checkOn.CallBack("confirmarOnline"));
                var condicoes = checkOn.Call(myName, myName, "confirmarOnline");
                if (condicoes != null && condicoes.estado == true)
                    throw new Exception("Nome de usuario ja escolhido!");
                // checkOnCallBackThread.Start();
                Thread beCheckOnThread = new Thread(() => beCheckOn.Receive(myName, "confirmarOnline"));
                beCheckOnThread.Start();
                // objeto que manda mensagens quando o destinatário está offline
                sendOffline = new SendOffline(myIP, myPort);
                // objeto que recebe mensagens enviadas enquanto offline
                receiveOffline = new ReceiveOffline(myIP, myPort, ReceiveMessage);
                // inicia thread que irá receber todas as mensagens do tópico *.Nome
                Thread receiveOfflineThread = new Thread(() => receiveOffline.Receive("*." + myName));
                receiveOfflineThread.Start();

                //iniciar servidor grpc
                _com = new GrpcMensageiroCom(
                    myName, myIP, myPort, ReceiveMessage, InsertMessage
                );
                comThread = new Thread(() => _com.IniciarServidor());
                comThread.Start();

            }
            catch (System.Exception e)
            {
                showErrors(e.Message.ToString());
                // showErrors("IP ou Porta inválidos!");
                throw;
            }
            MyIP = myIP;
            MyPort = myPort;
            MyName = myName;
            ShowErrors = showErrors;
            contatos = new List<Contato>();
            batePapo = new Dictionary<string, List<Mensagem>>();

        }
        public VerticalStackPanel ReturnTelaPrincipal()
        {
            verticalStackPanel = new VerticalStackPanel
            {
                Spacing = 1
            };

            verticalStackPanel.Proportions.Add(new Proportion { Type = ProportionType.Auto });
            verticalStackPanel.Proportions.Add(new Proportion { Type = ProportionType.Fill });

            var horizontalMenu = new HorizontalMenu
            {
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var menuItem = new MenuItem
            {
                Text = "Adicionar contato"
            };
            menuItem.Selected += (s, ea) =>
            {
                // chamar a tela de adicionar
                horizontalSplitPane.Widgets.RemoveAt(1);
                horizontalSplitPane.Widgets.Add(carregarAdicionarContatoWidget());
            };
            var menuItem2 = new MenuItem
            {
                Text = "Ficar Offline"
            };
            menuItem2.Selected += (s, ea) =>
            {
                // mudar de estado ao clicar
                var menu = (HorizontalMenu)verticalStackPanel.Widgets.ElementAt(0);
                menu.Items.RemoveAt(1);
                menu.Items.Add(mudarDeEstado());
            };
            horizontalMenu.Items.Add(menuItem);
            horizontalMenu.Items.Add(menuItem2);

            horizontalSplitPane = new HorizontalSplitPane
            {
                Left = -75,
                IsDraggable = false,

            };

            var scrollContatos = new ScrollViewer
            {
                Left = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 380,
                Height = 500,
            };

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Id = "Contatos"
            };
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var label = new Label
            {
                Text = "Bem vindo",
                Margin = new Thickness { Left = 50 },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Id = "labelBemVindo"
            };

            scrollContatos.Content = grid;
            horizontalSplitPane.Widgets.Add(scrollContatos);
            horizontalSplitPane.Widgets.Add(label);

            verticalStackPanel.Widgets.Add(horizontalMenu);
            verticalStackPanel.Widgets.Add(horizontalSplitPane);
            return verticalStackPanel;
        }
        public Grid carregarChatWidget(Contato contato)
        {

            var gridChat = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Id = "chatBox"
            };
            gridChat.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            gridChat.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            gridChat.RowsProportions.Add(new Proportion(ProportionType.Auto));
            gridChat.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var gridmensagens = new Grid
            {
                RowSpacing = 10,
                ColumnSpacing = 0,
                GridRowSpan = 10,
                Id = "chatMensagens"
            };
            gridChat.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            gridChat.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            gridChat.RowsProportions.Add(new Proportion(ProportionType.Fill));
            gridChat.RowsProportions.Add(new Proportion(ProportionType.Fill));

            ///////////// carregar mensagens
            // pegar as conversas
            List<Mensagem> mensagens = null;
            if (batePapo.TryGetValue(contato.Name, out mensagens))
            {
                // adiciona-las no grid
                foreach (var item in mensagens)
                {
                    if (!item._Lida)
                    {
                        item._Lida = true;
                    }
                    gridmensagens.RowsProportions.Add(new Proportion(ProportionType.Auto));
                    gridmensagens.Widgets.Add(
                        createMessage(
                            item,
                            gridmensagens.Widgets.Count,
                            item._EnviadoPor.Name == this.MyName
                        )
                    );
                }
                // limpar label
                setLabelPendentesText(contato, "");
            }

            CurrentScrollViewer = new ScrollViewer
            {
                Left = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 350,
                Height = 400,

            };
            CurrentScrollViewer.Content = gridmensagens;

            CurrentTextBox = new TextBox
            {
                Left = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                GridRow = 1,
                Width = 400,
                Height = 24,
            };
            CurrentTextBox.TextChanged += (a, s) =>
            {
                if (CurrentTextBox.Text.Length + (MyName.Length + 2) > 33)
                {
                    CurrentTextBox.Text = CurrentTextBox.Text.Substring(0, 33 - (MyName.Length + 2));
                    CurrentTextBox.CursorPosition = 33;
                }
            };
            CurrentTextBox.KeyDown += (a, s) =>
            {
                if (s.Data.ToString() == "Enter" && CurrentTextBox.Text != "")
                {
                    var mensagem = new Mensagem
                    (
                        CurrentTextBox.Text,
                        DateTime.Now,
                        new Contato(this.MyName, this.MyPort, this.MyIP),
                        contato
                    );
                    //checar se contato esta online:
                    if (Estado)
                    {
                        var condicoes = checkOn.Call(MyName, contato.Name, "confirmarOnline");
                        if (condicoes != null && condicoes.estado == true && condicoes.contatoAtual == MyName)
                        {
                            //usar grpc para enviar mensagens
                            Console.WriteLine("os dois online e no chat");
                            _com.ConectarComContato(contato);
                            _com.EnviarMensagemPeloChat(mensagem);
                        }
                        else
                        {
                            //usar RabbitMQ
                            sendOffline.Send("Chat", MyName + "." + contato.Name, mensagem);
                            InsertMessage(
                                mensagem,
                                true);
                            ReceiveMessage(mensagem);
                            Console.WriteLine("So um online");
                        }
                    }
                    else
                    {
                        Console.WriteLine("enviando mensagem offline");
                    }

                    // var scrollParent = (ScrollViewer)grid.Parent;
                    // scrollParent.ScrollPosition = scrollParent.ScrollPosition;
                }
                // checar se o cliente esta online
            };
            gridChat.Widgets.Add(CurrentTextBox);
            gridChat.Widgets.Add(CurrentScrollViewer);
            return gridChat;
        }
        public MenuItem mudarDeEstado()
        {
            var menuItemEstado = new MenuItem { };
            if (Estado)
            {
                menuItemEstado.Text = "Ficar Online";
            }
            else
            {
                comThread = new Thread(() => _com.IniciarServidor());
                comThread.Start();
                menuItemEstado.Text = "Ficar Offline";
            }
            menuItemEstado.Selected += (s, ea) =>
            {
                var menu = (HorizontalMenu)verticalStackPanel.Widgets.ElementAt(0);
                menu.Items.RemoveAt(1);
                menu.Items.Add(mudarDeEstado());
            };
            Estado = !Estado;
            return menuItemEstado;
        }
        public string getContatoAtual(){
            return contatoAtual;
        }
        public bool getEstado()
        {
            return Estado;
        }
        public Grid carregarAdicionarContatoWidget()
        {
            var gridAdicionarContato = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Id = "AdicionarContato"
            };
            gridAdicionarContato.ColumnsProportions.Add(new Proportion());
            gridAdicionarContato.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            gridAdicionarContato.ColumnsProportions.Add(new Proportion());
            // espaço
            gridAdicionarContato.RowsProportions.Add(new Proportion
            {
                Type = ProportionType.Pixels,
                Value = 120
            });
            //Nome do contato
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));
            //IP do contato
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));
            //Porta do contato
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));
            // erros
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));
            // Botao OK
            gridAdicionarContato.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var label = new Label
            {
                Text = "Digite o nome do contato",
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 1,
                GridColumn = 1,
            };

            var textBox = new TextBox
            {
                Width = 400,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 2,
                GridColumn = 1
            };
            textBox.TextChanged += (b, ea) =>
            {
                if (textBox.Text.Length > 33)
                {
                    textBox.Text = textBox.Text.Substring(0, 33);
                    textBox.CursorPosition = 33;
                }
            };
            var labelIP = new Label
            {
                Text = "Digite o IP do contato",
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 3,
                GridColumn = 1,
            };

            var textBoxIP = new TextBox
            {
                Width = 400,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 4,
                GridColumn = 1
            };
            textBoxIP.TextChanged += (b, ea) =>
            {
                if (textBox.Text.Length > 33)
                {
                    textBox.Text = textBox.Text.Substring(0, 33);
                    textBox.CursorPosition = 33;
                }
            };
            var labelPorta = new Label
            {
                Text = "Digite a Porta do contato",
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 5,
                GridColumn = 1,
            };

            var textBoxPorta = new TextBox
            {
                Width = 400,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 6,
                GridColumn = 1
            };
            textBoxPorta.TextChanged += (b, ea) =>
            {
                if (textBox.Text.Length > 33)
                {
                    textBox.Text = textBox.Text.Substring(0, 33);
                    textBox.CursorPosition = 33;
                }
            };

            var labelErro = new Label
            {
                Text = "",
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 7,
                GridColumn = 1,
                TextColor = Microsoft.Xna.Framework.Color.Crimson
            };

            var button = new TextButton
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100,
                Text = "Ok",
                GridRow = 8,
                GridColumn = 1,
            };

            button.Click += (b, ea) =>
            {
                labelErro.Text = "";
                var NomeContato = "";
                var IPContato = "";
                var PortaContato = "";
                if (string.IsNullOrEmpty(textBox.Text))
                    labelErro.Text += "Escreva um nome!\n";
                else
                    NomeContato = textBox.Text;
                if (string.IsNullOrEmpty(textBoxIP.Text))
                    labelErro.Text += "Escreva um IP!\n";
                else
                    IPContato = textBoxIP.Text;

                if (string.IsNullOrEmpty(textBoxPorta.Text))
                    labelErro.Text += "Escreva uma porta!\n";
                else
                    PortaContato = textBoxPorta.Text;

                if (string.IsNullOrEmpty(labelErro.Text))
                {
                    // pegar grid com os contatos
                    var scrollPai = (ScrollViewer)horizontalSplitPane.Widgets.ElementAt(0);
                    var gridPai = (Grid)scrollPai.GetChild(0);

                    // Checar se existe botão com o nome do contato
                    var contato = new Contato(NomeContato, PortaContato, IPContato);
                    var gridContato = findContatoTextButton(contato);
                    if (MyName == NomeContato)
                        labelErro.Text += "Nao tente se adicionar!";
                    else if (gridContato == null)
                    {
                        // adicionar contato na GUI
                        gridPai.RowsProportions.Add(new Proportion(ProportionType.Auto));
                        gridPai.Widgets.Add(createContatoButton(contato, gridPai.ChildrenCount));
                        contatos.Add(contato);
                    }
                    else
                        labelErro.Text += "Contato já adicionado!";
                }

            };

            gridAdicionarContato.Widgets.Add(label);
            gridAdicionarContato.Widgets.Add(textBox);
            gridAdicionarContato.Widgets.Add(labelIP);
            gridAdicionarContato.Widgets.Add(textBoxIP);
            gridAdicionarContato.Widgets.Add(labelPorta);
            gridAdicionarContato.Widgets.Add(textBoxPorta);
            gridAdicionarContato.Widgets.Add(labelErro);
            gridAdicionarContato.Widgets.Add(button);

            return gridAdicionarContato;

        }
        public Grid createContatoButton(Contato contato, int gridRow)
        {
            var grid = new Grid
            {
                RowSpacing = 1,
                ColumnSpacing = 2,
                Id = "contatos",
                GridRow = gridRow
            };
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            var contatoButton = new TextButton
            {
                Margin = new Thickness { Left = 50, Bottom = -15 },
                Text = contato.Name,
                Height = 50,
                Width = 350,
                GridColumn = 0
            };

            contatoButton.Click += (s, a) =>
            {
                contatoAtual = contato.Name;
                var condicoes = checkOn.Call(MyName, contato.Name, "confirmarOnline");
                if (condicoes != null && condicoes.estado == true && condicoes.contatoAtual == MyName)
                    _com.ConectarComContato(contato);
                horizontalSplitPane.Widgets.RemoveAt(1);
                horizontalSplitPane.Widgets.Add(carregarChatWidget(contato));
                var grid = (Grid)horizontalSplitPane.Widgets.ElementAt(1);
                var textbox = grid.Widgets.ElementAt(0);
                textbox.SetKeyboardFocus();
            };
            var labelPendencias = new Label
            {
                Text = "",
                GridColumn = 1,
                TextColor = Microsoft.Xna.Framework.Color.White
            };

            grid.Widgets.Add(contatoButton);
            grid.Widgets.Add(labelPendencias);
            return grid;
        }
        private void ReceiveMessage(Mensagem mensagem)
        {
            // checar quantos contatos estão adicionados
            if (contatos.Count > 0)
            {
                // caso haja contatos, verificar se o contato já está adicionado
                if (contatos.Where(x => x.Name == mensagem._EnviadoPor.Name).Count() == 0)
                {
                    // se não há, entao adicioná-lo
                    contatos.Add(mensagem._EnviadoPor);
                }
                List<Mensagem> value = null;
                // se está adicionado, verificar se há mensagens já trocadas
                if (batePapo.TryGetValue(mensagem._EnviadoPor.Name, out value))
                {
                    // se já tem, adicionar a nova mensagem
                    value.Add(mensagem);
                }
                else
                {
                    // se não, entao criar nova chave no dicionario
                    var lista = new List<Mensagem>();
                    lista.Add(mensagem);
                    batePapo.Add(mensagem._EnviadoPor.Name, lista);
                }
            }
            else
            {
                // caso não haja contatos, mas há mensagens, então adicionar o remetente na lista de contatos
                contatos.Add(
                    mensagem._EnviadoPor
                );

                // adicionar mensagem no dicionario de mensagens com aquele contato
                var lista = new List<Mensagem>();
                lista.Add(mensagem);
                batePapo.Add(mensagem._EnviadoPor.Name, lista);
            }
            /////////// fazer mudanças na GUI ///////////////

            //// sinalizar que tal contato tem mensagens pendentes

            // Checar se há um TextButton do contato pendente
            var scroll = (ScrollViewer)horizontalSplitPane.Widgets.ElementAt(0);
            var grid = (Grid)scroll.GetChild(0);
            // se houver widgets em grid é porque há contatos lá
            if (mensagem._EnviadoPor.Name != MyName)
            {

                if (grid.Widgets.Count > 0)
                {
                    // Verificar se há um TextButton com o mesmo nome que o contato
                    Grid aux = findContatoTextButton(mensagem._EnviadoPor);
                    if (aux != null)
                    {
                        // se já existe, então atualizar o numero de mensagens pendentes
                        var label = (Label)aux.Widgets.ElementAt(1);
                        if (label.Text == "")
                            label.Text = "1";
                        else
                        {
                            try
                            {
                                label.Text = (int.Parse(label.Text) + 1).ToString();
                            }
                            catch (System.Exception)
                            {
                                label.Text = "1";
                            }
                        }
                    }
                    else
                    {
                        // se não existe, entao adicionar o botao do contato
                        var contato = new Contato(
                            mensagem._EnviadoPor.Name,
                            mensagem._EnviadoPor.Port,
                            mensagem._EnviadoPor.Ip
                        );
                        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
                        grid.Widgets.Add(createContatoButton(contato, grid.ChildrenCount));
                        aux = (Grid)grid.Widgets.ElementAt(0);
                        var label = (Label)aux.Widgets.ElementAt(1);
                        label.Text = "1";
                    }
                }
                else
                {
                    // se não há contatos, então basta adicionar um TextButton com uma label com numero ao lado
                    var contato = new Contato(
                            mensagem._EnviadoPor.Name,
                            mensagem._EnviadoPor.Port,
                            mensagem._EnviadoPor.Ip
                        );
                    grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
                    grid.Widgets.Add(createContatoButton(contato, grid.ChildrenCount));
                    var aux = (Grid)grid.Widgets.ElementAt(0);
                    var label = (Label)aux.Widgets.ElementAt(1);
                    label.Text = "1";
                }
            }


        }
        public void InsertMessage(Mensagem mensagem, bool IsMyMessage)
        {
            var grid = (Grid)CurrentScrollViewer.GetChild(0);
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.Widgets.Add(createMessage(mensagem, grid.Widgets.Count, IsMyMessage));
            CurrentTextBox.Text = "";
        }
        public Grid findContatoTextButton(Contato contato)
        {
            var scroll = (ScrollViewer)horizontalSplitPane.Widgets.ElementAt(0);
            var grid = (Grid)scroll.GetChild(0);
            Grid aux = null;
            TextButton aux2 = null;
            bool contador = false;
            foreach (var item in grid.Widgets)
            {
                aux = (Grid)item;
                aux2 = (TextButton)aux.Widgets.ElementAt(0);
                if (aux2.Text == contato.Name)
                {
                    contador = true;
                    break;
                }
            }
            if (contador)
                return aux;
            else
                return null;
        }
        public void setLabelPendentesText(Contato contato, string text)
        {
            Grid contatoGrid = findContatoTextButton(contato);
            if (contatoGrid != null)
            {
                var label = (Label)contatoGrid.Widgets.ElementAt(1);
                label.Text = text;
            }
        }
        public TextButton createMessage(Mensagem message, int row, bool IsMinhaMensagem)
        {
            var width = (message._Mensagem.Length + MyName.Length + 2) * 10 < 300 ? (message._Mensagem.Length + MyName.Length + 2) * 10 : 300;
            var height = 20;

            if (message._Mensagem.Length + (message._EnviadoPor.Name.Length) > 33)
            {
                message._Mensagem = message._Mensagem.Substring(0, 33 - message._EnviadoPor.Name.Length);
            }
            if (IsMinhaMensagem)
            {
                return new TextButton
                {
                    Padding = new Thickness { Right = 25 },
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Text = MyName + ": " + message._Mensagem,
                    Height = height,
                    Width = width,
                    GridRow = row,
                    Enabled = false,
                    ContentHorizontalAlignment = HorizontalAlignment.Left
                };
            }
            else
            {
                return new TextButton
                {
                    Padding = new Thickness { Right = 25 },
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Text = message._EnviadoPor.Name + ": " + message._Mensagem,
                    Height = height,
                    Width = width,
                    GridRow = row,
                    Enabled = false,
                    ContentHorizontalAlignment = HorizontalAlignment.Left
                };
            }
        }
    }
}