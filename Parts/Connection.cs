using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


namespace CourseWork.Parts
{

    public class Connection
    {
        public virtual void Connect(string input = "localhost", int port = 8888)
        {
        }

        public virtual void Listen(int count)
        {
        }
    }

    public class UdpConnection : Connection
    {
        UdpClient client;

        public UdpConnection()
        {
            client = new UdpClient();
        }

        public override void Connect(string input = "127.0.0.1", int port = 8888)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(input);
                client.Connect(localAddr, port);
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

        public byte[] Receive(ref IPEndPoint iPEndPoint)
        {
            byte[] buff;
            try
            {
                iPEndPoint = null;
                buff = client.Receive(ref iPEndPoint);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return buff;
        }

        public void Listen(string input = "127.0.0.1", int port = 8888)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(input);
                IPEndPoint iPEndPoint = new IPEndPoint(localAddr, port);
                client.Receive(ref iPEndPoint);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

    class TcpConnection : Connection
    {
        TcpConnection()
        {
        }
        public new NetworkStream Connect(string input = "localhost", int port = 8888)
        {
            TcpClient client = new TcpClient(); ;
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                client.Connect(localAddr, port);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return client.GetStream();
        }

        public NetworkStream Listen(string input = "127.0.0.1", int port = 8888)
        {
            TcpClient client;
            try
            {
                IPAddress localAddr = IPAddress.Parse(input);
                TcpListener server = new TcpListener(localAddr, port);
                server.Start();
                client = server.AcceptTcpClient();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return client.GetStream();
        }
    }
}
