using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace CourseWork.Parts
{
    using System.Windows.Forms;
    using System.Text.Json;
    using System.Net.Sockets;
    using System.Net;

    public class Manipulator
    {
        int _PCid;
        public Options _options;
        SoundTransfer _sound;
        //FileTransfer _fileTransfer;
        EventController _eventControler;

        TcpConnection _MainListener;

        public bool _isFileTranfering { set; get; }
        public bool _isSoundConnected { private set; get; }


        public List<IPEndPoint> ConnectedRemoteClientsAddress { get; private set; }
        private List<ClientInfo> _Clients;

        struct ClientInfo
        {
            public NetworkStream Stream;
            public TcpConnection tcpInfo;
        }

        static public Manipulator _currManipulator { get; private set; }

        Thread acceptingClients;
        Thread tryToConnect;
        Thread readMessages;
        Thread sendMessages;

        public Manipulator()
        {
            _PCid = 1;
            _currManipulator = this;

            ConnectedRemoteClientsAddress = new List<IPEndPoint>();
            _Clients = new List<ClientInfo>();
            LoadSettings();
            //Setting on Accepting external connections

            _MainListener = new TcpConnection();
            _MainListener.Listen("127.0.0.1", _options.defualtTcpPort);

            acceptingClients = new Thread(new ThreadStart(AcceptingClients));
            acceptingClients.Name = "Waiting TcpConnect";
            acceptingClients.Start();

            tryToConnect = new Thread(new ThreadStart(TryConnectToClients));
            tryToConnect.Name = "TryingToConnect";
            tryToConnect.Start();

            readMessages = new Thread(new ThreadStart(ReadMessagesMainStream));
            readMessages.Name = "readMessages";
            readMessages.Start();

            sendMessages = new Thread(new ThreadStart(SendingMessagesMainStream));
            sendMessages.Name = "sendMessages";
            sendMessages.Start();

            _sound = new SoundTransfer(this);
            //_fileTransfer = new FileTransfer();
            //_eventControler = new EventController();
            //_eventControler._changeScreen += ChangeScreen;
        }

        ~Manipulator()
        {

        }

        public void Close()
        {
            SaveSettings();
            _options._isAcceptable = false;
            _options._isTryingConnect = false;
            _isRecevingMessages = false;
            _MainListener.Close();
            if (_Clients != null)
            {
                foreach (var client in _Clients)
                {
                    client.Stream.Close();
                    client.tcpInfo.Close();
                }
            }
            _options.Close();
        }        

        private enum Commands
        {
            SET = 0x1,
            UNSET = 0x2,
            MainTCP = 0x4,
            SoundConnection = 0x8,
            EventConnection = 0x10,
            FileTranferConnection = 0x20
        }

        private void ProcessCommand(Commands e)
        {

        }

        private bool _isRecevingMessages = true;

        private void ReadMessagesMainStream()
        {
            while (_isRecevingMessages)
            {
                foreach (ClientInfo client in _Clients)
                {
                     
                    byte[] buff = new byte[2048];
                    client.Stream.Read(buff, 0, 2048);
                    string temp = Encoding.Default.GetString(buff);
                    if (temp != "")
                        MessageBox.Show(temp);
                }
                Thread.Sleep(100);
            }
        }

        private void SendingMessagesMainStream()
        {
            //while (true)
            //{
            //    //foreach (ClientInfo client in _Clients)
            //    //{
            //    //    string str = "message";
            //    //    byte[] buff = new byte[2048];
            //    //    //buff = Encoding.Default.GetBytes(str);
            //    //    buff = Encoding.Default.GetBytes(str);
            //    //    client.Stream.Write(buff, 0, buff.Length);
            //    //}
            //    Thread.Sleep(100);
            //}
        }



        private void AcceptingClients()
        {
            while (_options._isAcceptable)
            {
                if (!_MainListener.Pending())
                {
                    Thread.Sleep(300); // choose a number (in milliseconds) that makes sense
                    continue; // skip to next iteration of loop
                }
                else
                {
                    TcpClient temp = _MainListener.AcceptClient();
                    TcpConnection tcpConnection = new TcpConnection(temp);
                    _Clients.Add(new ClientInfo
                    {
                        Stream = temp.GetStream(),
                        tcpInfo = tcpConnection
                    });
                    ConnectedRemoteClientsAddress.Add(temp.Client.RemoteEndPoint as IPEndPoint);
                }
            }
        }

        public void TryConnectToClients()
        {
            while (_options._isTryingConnect)
            {
                foreach (var iPEnd in _options.remoteClientsAddress)
                {
                    if (!ConnectedRemoteClientsAddress.Contains(iPEnd))
                    {
                        TcpConnection temp = new TcpConnection();
                        NetworkStream buff = temp.Connect(iPEnd);
                        if (buff != null)
                        {
                            ConnectedRemoteClientsAddress.Add(iPEnd);
                            _Clients.Add(
                                new ClientInfo
                                {
                                    Stream = buff,
                                    tcpInfo = temp
                                }
                                );

                        }
                    }
                }
                Thread.Sleep(500); // choose a number (in milliseconds) that makes sense
            }
        }

        public void LoadSettings()
        {
            if (System.IO.File.Exists("./Settings.json"))
            {
                try
                {
                    string json = System.IO.File.ReadAllText("./Settings.json");
                    _options = JsonSerializer.Deserialize<Options>(json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                if (System.IO.File.Exists("./DefaultSettings.json"))
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText("./Settings.json");
                        _options = JsonSerializer.Deserialize<Options>(json);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            if (_options == null)
            {
                _options = new Options();
            }
            //_options = new Options();

        }

        public void SaveSettings()
        {
            string json;
            json = JsonSerializer.Serialize<Options>(_options);
            System.IO.File.WriteAllText("./Settings.json", json);
        }

        //public void CheckSoundClients(List<IPEndPoint> remoteClients)
        //{
        //    _sound.CheckSendConnections(remoteClients);
        //}

        public void ChangeScreen(ScreenEdges side)
        {

        }
    }
}