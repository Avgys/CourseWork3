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
            LoadSettings();
            //Setting on Accepting external connections

            _MainListener = new TcpConnection();
            _MainListener.Listen(new IPEndPoint(localIP, _options.defualtTcpPort));

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

            CheckSubNet();
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

        public void TryConnectToClients()
        {
            do
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