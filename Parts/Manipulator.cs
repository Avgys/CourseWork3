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
    using NAudio.Wave;
    using NAudio.CoreAudioApi;

    public class ConnectionInfo
    {
        public IPEndPoint Sound;
        public IPEndPoint Event;
        public IPEndPoint File;
        public TcpConnection RemoteClient;

        public ConnectionInfo(TcpConnection client = null, IPEndPoint sound = null, IPEndPoint eventValue = null, IPEndPoint file = null)
        {
            Sound = sound;
            Event = eventValue;
            File = file;
            RemoteClient = client;
        }
    }

    public class Manipulator
    {
        public static object remoteClientsAddressInUseLock = new object();
        int _PCid;
        public Options _options;
        public SoundTransfer _sound;
        //FileTransfer _fileTransfer;
        public EventController _eventControler;

        TcpConnection _MainListener;

        public bool _isFileTranfering { set; get; }
        public bool _isSoundConnected { private set; get; }

        public static object ConnectedRemoteLock = new object();

        public List<ConnectionInfo> ConnectedRemoteClientsAddress { get; private set; }

        //private List<TcpClientInfo> _Clients;

        //struct TcpClientInfo
        //{
        //    public NetworkStream Stream;

        //}

        static public Manipulator _currManipulator { get; private set; }



        Thread acceptingClients;
        Thread tryToConnect;
        Thread readMessages;
        Thread sendMessages;

        IPAddress SubnetMask;
        public IPAddress localIP;

        public Manipulator()
        {

            _currManipulator = this;
            SubnetMask = GetSubnetMask();
            _PCid = localIP.GetAddressBytes()[3];
            ConnectedRemoteClientsAddress = new List<ConnectionInfo>();
            
            LoadSettings();
            //Setting on Accepting external connections

            _sound = new SoundTransfer(this);

            _MainListener = new TcpConnection();
            _MainListener.Listen(new IPEndPoint(localIP, _options.defualtTcpPort));

            tryToConnect = new Thread(new ThreadStart(CheckConnectToServers));
            tryToConnect.Name = "CheckConnectToServers";
            tryToConnect.Start();

            acceptingClients = new Thread(new ThreadStart(AcceptingClients));
            acceptingClients.Name = "Waiting TcpConnect";
            acceptingClients.Start();            

            readMessages = new Thread(new ThreadStart(ReadMessagesMainStream));
            readMessages.Name = "readMessages";
            readMessages.Start();

            sendMessages = new Thread(new ThreadStart(SendingMessagesMainStream));
            sendMessages.Name = "sendMessages";
            sendMessages.Start();

            _eventControler = new EventController();
            _eventControler._changeScreen += ChangeScreen;
        }

        public void RemoveRemoteTcp(IPEndPoint e)
        {
            lock (ConnectedRemoteLock)
            {
                var list = ConnectedRemoteClientsAddress.ToList();
                ConnectedRemoteClientsAddress.RemoveAll(x => e == x.RemoteClient.IPEndPoint);           
            }
            CheckSoundClients();
        }        


        public void Close()
        {
            SaveSettings();
            _eventControler.Close();
            _options._isAcceptable = false;
            _options._isTryingConnect = false;
            _isRecevingMessages = false;
            _isSendingMessages = false;

            _MainListener.Close();
            if (ConnectedRemoteClientsAddress != null)
            {
                lock (ConnectedRemoteLock)
                    foreach (var client in ConnectedRemoteClientsAddress)
                    {
                        client.RemoteClient.Close();
                    }
            }
            if (acceptingClients.IsAlive)
            {
                acceptingClients.Join();
            }
            if (tryToConnect.IsAlive)
            {
                tryToConnect.Join();
            }
            if (readMessages.IsAlive)
            {
                readMessages.Join();
            }
            if (sendMessages.IsAlive)
            {
                sendMessages.Join();
            }
            _sound.Deactivate(DataFlow.Render);
            _sound.Deactivate(DataFlow.Capture);

            _options.Close();
        }

        private enum Commands
        {
            SEND = 0x1,
            UNSET = 0x2,
            MainTCP = 0x4,
            SoundConnection = 0x8,
            EventConnection = 0x10,
            GET = 0x40
        }

        private bool SendCommand(Commands command, ConnectionInfo client)
        {
            string endPoint = "";
            Commands neutral = (Commands)188;
            if ((command & Commands.SEND) == Commands.SEND)
                switch (command & neutral)
                {
                    case Commands.MainTCP:
                        {
                            break;
                        }
                    case Commands.SoundConnection:
                        {
                            endPoint = _sound._ConnectionToReceive.IPEndPoint.ToString();
                            break;
                        }
                    case Commands.EventConnection:
                        {
                            endPoint = _eventControler.ConnectionToReceive.IPEndPoint.ToString();
                            break;
                        }                    
                }


            List<byte> buff = new();

            int bytesLength = 0;
            bytesLength += 2;

            if ((command & Commands.SEND) == Commands.SEND)
                bytesLength += Encoding.UTF8.GetBytes(endPoint).Length;

            buff.Add((byte)bytesLength);

            buff.Add((byte)command);

            if ((command & Commands.SEND) == Commands.SEND)
                buff.AddRange(Encoding.UTF8.GetBytes(endPoint));

            try
            {
                var arr = buff.ToArray();
                client.RemoteClient._Client.GetStream().Write(arr, 0, bytesLength);
                return true;
            }
            catch
            {
                RemoveRemoteTcp(client.RemoteClient.IPEndPoint);
                return false;
            }
        }

        private void ProcessCommand(byte[] buff, ConnectionInfo client, int bytesRead)
        {
            Commands command = (Commands)buff[1];
            Commands neutral = (Commands)188;
            switch (command & neutral)
            {
                case Commands.MainTCP:
                    {
                        break;
                    }
                case Commands.SoundConnection:
                    {
                        if ((command & Commands.SEND) == Commands.SEND)
                        {
                            int headerLength = buff[0];
                            byte[] IpBuff = new byte[headerLength - 2];
                            Array.Copy(buff, 2, IpBuff, 0, headerLength - 2);                            
                            IPEndPoint iPEnd;
                            if (IPEndPoint.TryParse(Encoding.UTF8.GetString(IpBuff), out iPEnd))
                            {
                                client.Sound = iPEnd;
                                CheckSoundClients();
                            }
                        }
                        else if (((command & Commands.GET) == Commands.GET))
                        {
                            Commands newCommand = Commands.SEND | Commands.SoundConnection;

                            if (!SendCommand(newCommand, client))
                            {
                                client.RemoteClient.Close();
                            }
                        }
                        break;
                    }
                case Commands.EventConnection:
                    {
                        if ((command & Commands.SEND) == Commands.SEND)
                        {
                            int headerLength = buff[0];
                            byte[] IpBuff = new byte[headerLength - 2];
                            Array.Copy(buff, 2, IpBuff, 0, headerLength - 2);
                            IPEndPoint iPEnd;
                            if (IPEndPoint.TryParse(Encoding.UTF8.GetString(IpBuff), out iPEnd))
                            {
                                client.Event = iPEnd;
                            }
                        }
                        else if (((command & Commands.GET) == Commands.GET))
                        {
                            Commands newCommand = Commands.SEND | Commands.EventConnection;

                            if (!SendCommand(newCommand, client))
                            {
                                client.RemoteClient.Close();
                            }
                        }
                        break;
                    }
            }
        }

        private bool _isRecevingMessages = true;

        private async void ReadMessagesMainStream()
        {
            while (_isRecevingMessages)
            {
                var list = ConnectedRemoteClientsAddress.ToList();
                foreach (var client in list)
                {

                    byte[] buff = new byte[2048];
                    try
                    {
                        if (client.RemoteClient._Client.GetStream().DataAvailable)
                        {
                            int bytesRead = client.RemoteClient._Client.GetStream().Read(buff, 0, 2048);
                            if (bytesRead > 0)
                                await Task.Run(() => ProcessCommand(buff, client, bytesRead));
                        }
                    }
                    catch
                    {
                        client.RemoteClient.Close();
                    }
                }
                Thread.Sleep(100);
            }
        }

        private bool _isSendingMessages = true;

        private void SendingMessagesMainStream()
        {
            while (_isSendingMessages)
            {
                var list = ConnectedRemoteClientsAddress.ToList();
                foreach (var client in list)
                {
                    if (client.RemoteClient.isClientConnected)
                    {
                        if (client.Sound == null)
                        {
                            Commands command = Commands.GET | Commands.SoundConnection;

                            if (!SendCommand(command, client))
                            {
                                client.RemoteClient.Close();
                            }
                        }

                        if (client.Event == null)
                        {
                            Commands command = Commands.GET | Commands.EventConnection;

                            if (!SendCommand(command, client))
                            {
                                client.RemoteClient.Close();
                            }
                        }
                    }
                }
                Thread.Sleep(200);
            }
        }

        public IPAddress GetSubnetMask()
        {
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
                    //_MainListener.Connect()k

                    TcpClient temp = _MainListener.AcceptClient();
                    if (!ConnectedRemoteClientsAddress.Exists(x => x.RemoteClient.IPEndPoint?.Address.Equals((temp.Client.RemoteEndPoint as IPEndPoint).Address) ?? false))
                    {
                        TcpConnection tcpConnection = new TcpConnection(temp);

                        lock (ConnectedRemoteLock)
                        {
                            _options.AddClient(temp.Client.RemoteEndPoint as IPEndPoint);
                            ConnectedRemoteClientsAddress.Add(new(tcpConnection));
                        }
                    }
                    else
                    {
                        temp.Close();
                    }
                }
                CheckIsConnectionActive();
            }
        }

        private void CheckIsConnectionActive()
        {
            lock (ConnectedRemoteLock)
                ConnectedRemoteClientsAddress.RemoveAll(x => !x.RemoteClient.isClientConnected || x.RemoteClient.IPEndPoint == null);
            CheckSoundClients();
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
                CheckIsConnectionActive();
                var list = _options.remoteClientsAddress.ToList();
                lock (remoteClientsAddressInUseLock)
                    foreach (var iPEnd in list)
                    {
                        if (iPEnd.Address.Equals(localIP))
                            continue;
                        PingReply reply = pingSender.Send(iPEnd.Address, timeout, buffer, options);
                        if (reply.Status == IPStatus.Success)
                        {
                            if (!ConnectedRemoteClientsAddress.Exists(x =>
                            {
                                if (x.RemoteClient.IPEndPoint == null)
                                    return true;
                                return x.RemoteClient.IPEndPoint.Address.Equals(iPEnd.Address);
                            }))
                            {
                                TcpConnection temp = new TcpConnection();
                                NetworkStream buff = null;
                                //lock (ConnectedRemoteLock)
                                {
                                    buff = temp.ConnectAsync(iPEnd);
                                    if (buff != null)
                                    {
                                        ConnectedRemoteClientsAddress.Add(new(temp));
                                        _options.AddClient(temp.IPEndPoint);
                                    }
                                }
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

        }

        public void SaveSettings()
        {
            string json;
            json = JsonSerializer.Serialize<Options>(_options);
            System.IO.File.WriteAllText("./Settings.json", json);
        }

        public void CheckSoundClients()
        {
            List<IPEndPoint> remoteClients = new();
            foreach (var e in ConnectedRemoteClientsAddress)
            {
                if (e?.Sound != null)
                {
                    remoteClients.Add(e.Sound);
                }
            }
            _sound.CheckSendConnections(remoteClients);
        }

        public void ChangeScreen(ScreenEdges side)
        {
            switch (side)
            {
                case ScreenEdges.LEFT:
                    {
                        if (ConnectedRemoteClientsAddress.Count > 0)
                            _eventControler.currRemoteIP = ConnectedRemoteClientsAddress[0]?.Event ?? null;
                        break;
                    }
                case ScreenEdges.UP:
                    {
                        if (ConnectedRemoteClientsAddress.Count > 1)
                            _eventControler.currRemoteIP = ConnectedRemoteClientsAddress[1]?.Event ?? null;
                        break;
                    }
                case ScreenEdges.RIGHT:
                    {
                        if (ConnectedRemoteClientsAddress.Count > 2)
                            _eventControler.currRemoteIP = ConnectedRemoteClientsAddress[2]?.Event ?? null;
                        break;
                    }
                case ScreenEdges.DOWN:
                    {
                        if (ConnectedRemoteClientsAddress.Count > 3)
                            _eventControler.currRemoteIP = ConnectedRemoteClientsAddress[3]?.Event ?? null;
                        break;
                    }
            }
        }
    }
}