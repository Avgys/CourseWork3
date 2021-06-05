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
        public MouseControl _mouseControl;
        public MouseHook _mouseHook;

        bool isSendingKey;
        bool _isScreenChanged;
        public bool _isCheckingInput;

        Thread keyboardHook;
        Thread keyboardEmulate;
        Thread GetRemoteKeys;
        Thread SendRemoteKeys;

        public EventController()
        {


            _mouseControl = new MouseControl();

            KeyLocalSocket = new UdpConnection();

            keyboardHook = new Thread(new ThreadStart(KeyboardHook.Start));
            keyboardHook.Start();

            //keyboardEmulate = new Thread(new ThreadStart(CallKeyboardEvent));
            //keyboardEmulate.Start();

            GetRemoteKeys = new Thread(new ThreadStart(getRemoteKeys));
            GetRemoteKeys.Start();

            SendRemoteKeys = new Thread(new ThreadStart(SendKeyboardEvent));
            SendRemoteKeys.Start();

            //Thread mouseInput = new Thread(new ThreadStart(MouseHook.Start));
            //mouseInput.Start();

            _isCheckingInput = true;
            //Thread checkInputs = new Thread(new ThreadStart(CheckInputs));
            //checkInputs.Start();
        }

        public void Close()
        {
            //keyboardEmulate.Join();
            _isCheckingInput = false;
            KeyboardHook.Stop();
            GetRemoteKeys.Join();
            keyboardHook.Join();
            isSendingKey = false;
            SendRemoteKeys.Join();
        }

        private void getRemoteKeys()
        {

        }

        private void CallKeyboardEvent()
        {

        }


        private void SendKeyboardEvent()
        {
            while (isSendingKey)
            {
                var Events = KeyboardHook.getInputQueue();

                foreach (var key in Events)
                {
                    List<byte> intBytes = new();                    
                    intBytes.AddRange(BitConverter.GetBytes((int)key.key));
                    //if (BitConverter.IsLittleEndian)
                    //    Array.Reverse(intBytes);
                    intBytes.AddRange(BitConverter.GetBytes((int)key.state));
                    //if (BitConverter.IsLittleEndian)
                    //    Array.Reverse(intBytes);
                    byte[] result = intBytes.ToArray();
                    KeyLocalSocket.Send(result, currRemoteIP);
                }
            }
        }

        public static void KeyDown(Keys vKey)
        {
            //keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
        }

        public static void KeyUp(Keys vKey)
        {
            //keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        public void CheckInputs()
        {
            ScreenEdges flag;
            while (_isCheckingInput)
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
