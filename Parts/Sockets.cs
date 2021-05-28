using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace MySockets
{
     class Net {
        static int CLOSE_BOTH = 3;

        public Socket _Sock;

        public Socket CreateSocket(string Ip = "localhost",  SocketType socketType = SocketType.Stream, ProtocolType protocolType = ProtocolType.Tcp)
        {            
            IPAddress ipAddr = Dns.GetHostEntry(Ip).AddressList[0];
            return new Socket(ipAddr.AddressFamily, socketType, protocolType);
        }

        public int Send(byte[] msg)
        {
            return _Sock.Send(msg);
        }

        public int Receive(byte[] bytes)
        {
            return _Sock.Receive(bytes);
        }

        public void Listen(int count)
        {
            _Sock.Listen(count);
        }

        public void Bind(string input = "localhost", int port = 513)
        {
            IPEndPoint ipEndPoint = new((Dns.GetHostEntry("localhost")).AddressList[0], port);
            _Sock.Bind(ipEndPoint);
            Listen(4);
        }

        public void Connect(string input = "localhost", int port = 513)
        {
            IPEndPoint ipEndPoint = new((Dns.GetHostEntry("localhost")).AddressList[0], port);
            _Sock.Connect(ipEndPoint);
        }
        
        public Socket Accept()
        {
            return _Sock.Accept();
        }

        public void ShutDown(int way = 3)
        {
            if (way == CLOSE_BOTH)
            _Sock.Shutdown(SocketShutdown.Both);
        }

        public void Close()
        {
            _Sock.Close();
        }
    }
}