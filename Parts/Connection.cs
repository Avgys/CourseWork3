using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


namespace CourseWork.Parts
{

    //public class Connection
    //{
    //    public virtual void Connect(string input = "localhost", int port = 8888)
    //    {
    //    }

    //    public virtual void Listen(int count)
    //    {
    //    }
    //}

    public class UdpConnection
    {
        public UdpClient client;

        public UdpConnection()
        {
            client = new UdpClient(0);            
        }

        public IPEndPoint IPEndPoint
        {
            get
            {
                return client.Client.RemoteEndPoint as IPEndPoint;
            }
        }

        public void Connect(string input_ = "127.0.0.1", string port_ = "8888")
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(input_);
                int port = int.Parse(port_);
                IPEndPoint endPoint = new IPEndPoint(localAddr, port);
                client.Connect(endPoint);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void Connect(IPEndPoint endPoint)
        {
            try
            {
                client.Connect(endPoint);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public void Send(byte[] input)
        {
            client.Send(input, input.Length);
        }


        public void Send(byte[] input, IPEndPoint address)
        {
            client.Send(input, input.Length, address);
        }

        public byte[] Receive(ref IPEndPoint iPEndPoint)
        {
            byte[] buff;
            try
            {
                buff = client.Receive(ref iPEndPoint);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return buff;
        }

        public void Close()
        {
            client.Close();
            client.Dispose();
        }

        //public void Listen(IPEndPoint iPEndPoint)
        //{
        //    try
        //    {
        //        //IPAddress localAddr = IPAddress.Parse(input);
        //        //IPEndPoint iPEndPoint = new IPEndPoint(localAddr, port);
        //        client.Receive(ref iPEndPoint);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }
        //}
    }

    public class TcpConnection
    {
        TcpClient _Client;



        public IPEndPoint IPEndPoint
        {
            get
            {
                return _Client.Client.RemoteEndPoint as IPEndPoint;
            }
        }
        public bool isClientConnected
        {
            get
            {
                return _Client.Connected;
            }
        }
        TcpListener _Server;
        public TcpConnection()
        {
            _Client = new TcpClient();

        }

        public TcpConnection(TcpClient e)
        {
            _Client = e;
        }

        ~TcpConnection()
        {
            Close();
        }

        public NetworkStream Connect(string input = "localhost", int port = 8888)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse("input");
                _Client.Connect(localAddr, port);
            }
            catch (Exception ex)
            {
                //ex.Message;
                return null;
            }
            return _Client.GetStream();

        }

        public NetworkStream Connect(IPEndPoint endPoint)
        {
            try
            {
                _Client.Connect(endPoint);
            }
            catch (Exception ex)
            {
                //(ex.Message);
                return null;
            }
            return _Client.GetStream();
        }

        public NetworkStream ConnectAsync(IPEndPoint endPoint)
        {
            try
            {
                var result = _Client.BeginConnect(endPoint.Address, endPoint.Port, null,null);

                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(5));

                if (!success)
                {
                    throw new Exception("Failed to connect.");
                }

                // we have connected
                _Client.EndConnect(result);
                return _Client.GetStream();
            }
            catch (Exception ex)
            {
                //(ex.Message);
                return null;

            }
            return null;
        }

        public bool Pending()
        {
            return _Server.Pending();
        }

        public TcpClient AcceptClient()
        {
            if (_Server != null)
            {
                TcpClient Client;
                try
                {

                    Client = _Server.AcceptTcpClient();

                }
                catch (Exception ex)
                {
                    return null;
                }
                return Client;
            }
            else
            {
                return null;
            }
        }

        public bool Listen(string input = "127.0.0.1", int port = 8888)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(input);
                _Server = new TcpListener(localAddr, port);
                _Server.Start();
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message);
                return false;
            }
            return true;
        }

        public bool Listen(IPEndPoint endPoint)
        {
            try
            {
                _Server = new TcpListener(endPoint);
                _Server.Start();
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message);
                return false;
            }
            return true;
        }

        public void Close()
        {
            if (_Server != null)
            {
                _Server.Stop();
                //_Server.Server.Shutdown(SocketShutdown.Both);
                _Server.Server.Close();
            }
            if (_Client != null)
            {
                _Client.Close();
                _Client.Dispose();
            }

        }
    }
}
