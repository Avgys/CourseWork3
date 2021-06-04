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
    using System.Net.NetworkInformation;
    using System.Threading.Tasks;

    public class Manipulator
    {
        public static object remoteClientsAddressInUseLock = new object();
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

        IPAddress SubnetMask;
        IPAddress localIP;

        public Manipulator()
        {
            _PCid = 1;
            _currManipulator = this;
            SubnetMask = GetSubnetMask();
            ConnectedRemoteClientsAddress = new List<IPEndPoint>();
            _Clients = new List<ClientInfo>();
            //_Clients[0].tcpInfo.
            LoadSettings();
            //Setting on Accepting external connections

            _MainListener = new TcpConnection();
            _MainListener.Listen(new IPEndPoint(localIP, _options.defualtTcpPort));

            acceptingClients = new Thread(new ThreadStart(AcceptingClients));
            acceptingClients.Name = "Waiting TcpConnect";
            acceptingClients.Start();

            tryToConnect = new Thread(new ThreadStart(CheckConnectToServers));
            tryToConnect.Name = "CheckConnectToServers";
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

            Thread firstCheck = new Thread(new ThreadStart(CheckSubNet));
            firstCheck.Name = "sendMessages";
            firstCheck.Start();
        }

        private async void CheckSubNet()
        {

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            options.DontFragment = true;

            byte[] maskBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                maskBytes[i] = (byte)(localIP.GetAddressBytes()[i] & SubnetMask.GetAddressBytes()[i]);
            }
            maskBytes[3]++;

            byte[] buffer = new byte[1024];
            int timeout = 1;

            for (int i = 0; i < 255; i++)
            {
                maskBytes[3]++;
                IPAddress ip = new IPAddress(maskBytes);

                PingReply reply = pingSender.Send(ip, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    if (ip.ToString() != localIP.ToString())
                    {
                        IPEndPoint iPEnd = new IPEndPoint(ip, _options.defualtTcpPort);
                        if (!ConnectedRemoteClientsAddress.Contains(iPEnd))
                        {
                            TcpConnection temp = new TcpConnection();
                            NetworkStream buff = null;
                            await Task.Run(() => buff = temp.Connect(iPEnd));
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
                }

            }

            //MessageBox.Show("Ended");
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
            FileTranferConnection = 0x20,
            ACCEPT = 0x40
        }

        private async void SendCommand(Commands command, ClientInfo client)
        {
            byte[] buff = new byte[2048];
            Array.Clear(buff, 0, buff.Length);
            int bytesLength = 0;

            buff.Append<byte>((byte)command);
            bytesLength++;
            string endPoint = "";
            switch (command !& Commands.SET !& Commands.UNSET !& Commands.ACCEPT)
            {
                case Commands.MainTCP:
                    {
                        break;
                    }
                case Commands.SoundConnection:
                    {
                        if ((command & Commands.SET) != 0)
                        {
                            endPoint = _sound._ConnectionToReceive.IPEndPoint.ToString();
                        }
                        break;
                    }
                case Commands.EventConnection:
                    {
                        if ((command & Commands.SET) != 0)
                        {
                            endPoint = _sound._ConnectionToReceive.IPEndPoint.ToString();
                        }
                        break;
                    }
                case Commands.FileTranferConnection:
                    {
                        if ((command & Commands.SET) != 0)
                        {
                            //string endPoint = FileTranferConnection._ConnectionToReceive.IPEndPoint.ToString();
                        }
                        break;
                    }
            }
            
                if((command & Commands.SET) != 0)
                bytesLength += Encoding.UTF8.GetBytes(endPoint).Length;
                buff.SetValue(Encoding.UTF8.GetBytes(endPoint), 1);

                client.Stream.Write(buff, 0, bytesLength);
                Array.Clear(buff, 0, buff.Length);
            //client.Stream.Read(buff, 0, buff.Length);

        }

        private async void ProcessCommand(byte[] buff, ClientInfo client, int bytesRead)
            {
                Commands command = (Commands)buff[0];

                switch (command)
                {
                    case Commands.MainTCP:
                        {
                            break;
                        }
                    case Commands.SoundConnection:
                        {
                            break;
                        }
                    case Commands.EventConnection:
                        {
                            break;
                        }
                    case Commands.FileTranferConnection:
                        {
                            break;
                        }
                }
            }

        private bool _isRecevingMessages = true;

        private async void ReadMessagesMainStream()
        {
            while (_isRecevingMessages)
            {
                foreach (ClientInfo client in _Clients)
                {

                    byte[] buff = new byte[2048];
                    int bytesRead = client.Stream.Read(buff, 0, 2048);
                    if (bytesRead > 0)
                        await Task.Run(() => ProcessCommand(buff, client, bytesRead));
                    //string temp = Encoding.Default.GetString(buff);
                    //if (temp != "")
                    //    MessageBox.Show(temp);
                }
                Thread.Sleep(100);
            }
        }

        private void SendingMessagesMainStream()
        {
            while (true)
            {
                foreach (ClientInfo client in _Clients)
                {
                    //string str = "message";
                    Commands command = Commands.SET | Commands.SoundConnection;
                    //byte[] buff = new byte[2048];
                    //Array.Clear(buff, 0, buff.Length);                    

                    //SendCommand(command, client);

                    //buff.Append<byte>((byte)command);
                    //bytesLength++;
                    //string endPoint = _sound._ConnectionToReceive.IPEndPoint.ToString();
                    //bytesLength += Encoding.UTF8.GetBytes(endPoint).Length;
                    //buff.SetValue(Encoding.UTF8.GetBytes(endPoint), 1);

                    //foreach (var byteBuff in address.Address.GetAddressBytes())
                    //{
                    //    buff.Append<byte>(byteBuff);
                    //    bytesLength++;
                    //}

                    //buff.Append<byte>(address.Port);

                    //client.Stream.Write(buff, 0, bytesLength);
                    //Array.Clear(buff, 0, buff.Length);
                    //client.Stream.Read(buff, 0, buff.Length);
                }
                Thread.Sleep(100);
            }
        }

        public IPAddress GetSubnetMask()
        {
            //var adapters = NetworkInterface.GetAllNetworkInterfaces();
            //foreach (NetworkInterface adapter in adapters)
            //{
            //    Console.WriteLine(adapter.Description + " :");
            //    var properties = adapter.GetIPProperties();
            //    foreach (var dns in properties.GetIPv4Properties    )
            //        Console.WriteLine(dns.ToString());
            //}

            IPAddress[] addresses;
            addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(ha => ha.AddressFamily == AddressFamily.InterNetwork).ToArray();
            localIP = addresses[0];
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (localIP.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", localIP));
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

        public void CheckConnectToServers()
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            options.DontFragment = true;

            byte[] buffer = new byte[1024];
            int timeout = 1;
            do
            {
                lock (remoteClientsAddressInUseLock)
                    foreach (var iPEnd in _options.remoteClientsAddress)
                    {
                        PingReply reply = pingSender.Send(iPEnd.Address, timeout, buffer, options);
                        if (reply.Status == IPStatus.Success)
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
                    }

                var list = _Clients.FindAll(x => !x.tcpInfo.isClientConnected);
                foreach (var elem in list)
                {
                    _Clients.Remove(elem);
                    ConnectedRemoteClientsAddress.Remove(elem.tcpInfo.IPEndPoint);
                }
                Thread.Sleep(500); // choose a number (in milliseconds) that makes sense                
            }
            while (_options._isTryingConnect);
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
            _options.CheckSettings();
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