using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace CourseWork.Parts
{

    using System.Net.Sockets;
    using System.Net;

    public class EventController
    {
        public delegate void СursorState(ScreenEdges side);
        public event СursorState _changeScreen;

        public IPEndPoint currRemoteIP;

        public UdpConnection KeyLocalSocket;
        public UdpConnection ConnectionToReceive;

        public MouseControl _mouseControl;
        public MouseHook _mouseHook;

        bool isSendingKey = true;
        bool _isScreenChanged = false;
        bool _isLocalCheckingInput = true;
        public bool _isRemoteCheckingInput = true;

        Thread keyboardHook;
        Thread keyboardEmulate;
        Thread GetRemoteKeys;
        Thread SendRemoteKeys;

        private const int KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYUP = 2;

        public EventController()
        {
            _isRemoteCheckingInput = true;

            _mouseControl = new MouseControl();

            KeyLocalSocket = new UdpConnection();
            ConnectionToReceive = new UdpConnection();
            keyboardHook = new Thread(new ThreadStart(KeyboardHook.Start));
            keyboardHook.Name = "keyboardHook";
            keyboardHook.Start();

            //keyboardEmulate = new Thread(new ThreadStart(CallKeyboardEvent));
            //keyboardEmulate.Start();

            GetRemoteKeys = new Thread(new ThreadStart(getRemoteKeys));
            GetRemoteKeys.Name = "GetRemoteKeys";
            GetRemoteKeys.Start();

            SendRemoteKeys = new Thread(new ThreadStart(SendKeyboardEvent));
            SendRemoteKeys.Name = "SendRemoteKeys";
            //SendRemoteKeys.Start();

            //Thread mouseInput = new Thread(new ThreadStart(MouseHook.Start));
            //mouseInput.Start();


            Thread checkInputs = new Thread(new ThreadStart(CheckInputs));
            checkInputs.Start();
        }

        public void Close()
        {
            //keyboardEmulate.Join();
            ConnectionToReceive.Close();
            KeyLocalSocket.Close();
            _isRemoteCheckingInput = false;
            _isLocalCheckingInput = false;
            KeyboardHook.Stop();
            GetRemoteKeys.Join();
            keyboardHook.Join();
            isSendingKey = false;
            SendRemoteKeys.Join();
        }

        private void getRemoteKeys()
        {
            while (_isRemoteCheckingInput)
            {
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buff = ConnectionToReceive.Receive(ref iPEndPoint);
                if (buff != null && buff.Length > 0)
                {
                    List<byte> n = new List<byte>(buff);
                    Keys key = (Keys)BitConverter.ToInt32(n.GetRange(0, 4).ToArray());
                    KeyStates state = (KeyStates)BitConverter.ToInt32(n.GetRange(4, 4).ToArray());
                    CallKeyboardEvent(key, state);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private void CallKeyboardEvent(Keys key, KeyStates state)
        {
            if (state == KeyStates.WM_KEYDOWN)
                keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY, 0);
            else if (state == KeyStates.WM_KEYUP)
                keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }


        private void SendKeyboardEvent()
        {
            while (isSendingKey)
            {
                QueueKey previousKey = new QueueKey()
                {
                    key = 0,
                    state = 0
                };
                var Events = KeyboardHook.getInputQueue();
                if (Events != null && currRemoteIP != null)
                    foreach (var key in Events)
                    {
                        if (key.Equals(previousKey) && previousKey.key == Keys.Escape)
                        {
                            _changeScreen.Invoke(ScreenEdges.NONE);
                            _isScreenChanged = false;
                            break;
                        }
                            
                        List<byte> intBytes = new();
                        intBytes.AddRange(BitConverter.GetBytes((int)key.key));
                        //if (BitConverter.IsLittleEndian)
                        //    Array.Reverse(intBytes);
                        intBytes.AddRange(BitConverter.GetBytes((int)key.state));
                        //if (BitConverter.IsLittleEndian)
                        //    Array.Reverse(intBytes);
                        byte[] result = intBytes.ToArray();
                        KeyLocalSocket.Send(result, currRemoteIP);
                        
                            previousKey = key;
                    }
            }
        }






        public void CheckInputs()
        {
            ScreenEdges flag;
            while (_isLocalCheckingInput)
            {
                Thread.Sleep(100);
                flag = _mouseControl.isMouseTouchScreenEdge();
                if (flag != ScreenEdges.NONE)
                {
                    _isScreenChanged = true;
                    _changeScreen?.Invoke(flag);
                }
            }
        }
    }
}
