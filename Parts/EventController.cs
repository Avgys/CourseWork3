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

        //public MouseControl _mouseControl;

        bool isSendingKey = true;
        bool _isScreenChanged = false;
        bool _isLocalCheckingInput = true;
        public bool _isRemoteCheckingInput = true;

        Thread keyboardHook;
        Thread mouseHook;
        Thread GetRemoteKeys;

        private const int KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYUP = 2;

        public EventController()
        {
            _isRemoteCheckingInput = true;

            //_mouseControl = new MouseControl();

            KeyLocalSocket = new UdpConnection();
            ConnectionToReceive = new UdpConnection();
            KeyboardControl._Owner = this;

            MouseControl._Owner = this;

            keyboardHook = new Thread(new ThreadStart(KeyboardControl.Start));
            keyboardHook.Name = "keyboardHook";
            keyboardHook.Start();

            mouseHook = new Thread(new ThreadStart(MouseControl.Start));
            mouseHook.Name = "mouseHook";
            mouseHook.Start();

            GetRemoteKeys = new Thread(new ThreadStart(getRemoteInput));
            GetRemoteKeys.Name = "GetRemoteKeys";
            GetRemoteKeys.Start();

            Thread checkInputs = new Thread(new ThreadStart(CheckInputs));
            checkInputs.Start();
            //Clip();
        }

        public void Close()
        {
            //keyboardEmulate.Join();
            ConnectionToReceive.Close();
            KeyLocalSocket.Close();
            _isRemoteCheckingInput = false;
            _isLocalCheckingInput = false;
            KeyboardControl.Stop();
            GetRemoteKeys.Join();
            keyboardHook.Join();
            isSendingKey = false;
        }

        [Flags]
        private enum InputDevice
        {
            Mouse = 1,
            Keyboard = 0
        }

        private void getRemoteInput()
        {
            InputDevice device;
            while (_isRemoteCheckingInput)
            {
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buff = ConnectionToReceive.Receive(ref iPEndPoint);
                if (buff != null && buff.Length > 0)
                {
                    List<byte> n = new List<byte>(buff);
                    device = (InputDevice)BitConverter.ToUInt16(n.GetRange(0, 2).ToArray());
                    if (device == InputDevice.Keyboard)
                    {
                        Keys key = (Keys)BitConverter.ToInt32(n.GetRange(2, 4).ToArray());
                        KeyStates state = (KeyStates)BitConverter.ToInt32(n.GetRange(6, 4).ToArray());
                        CallKeyboardEvent(key, state);
                    }
                    else if (device == InputDevice.Mouse)
                    {
                        MSLLHOOKSTRUCT msStruct = new();
                        MouseEventFlags msg = (MouseEventFlags)BitConverter.ToInt32(n.GetRange(2, 4).ToArray());
                        msStruct.X = BitConverter.ToInt32(n.GetRange(6, 4).ToArray());
                        msStruct.Y = BitConverter.ToInt32(n.GetRange(10, 4).ToArray());
                        msStruct.mouseData = BitConverter.ToInt32(n.GetRange(14, 4).ToArray());
                        msStruct.flags = BitConverter.ToUInt32(n.GetRange(18, 4).ToArray());
                        msStruct.dwExtraInfo = BitConverter.ToUInt32(n.GetRange(22, 4).ToArray());

                        CallMouseEvent(msg, msStruct);
                    }
                }
            }
        }

        private void CallKeyboardEvent(Keys key, KeyStates state)
        {
            if (state == KeyStates.WM_KEYDOWN)
                KeyboardControl.keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY, 0);
            else if (state == KeyStates.WM_KEYUP)
                KeyboardControl.keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        QueueKey previousKey = new QueueKey
        {
            key = Keys.None,
            state = 0
        };

        public void SendKeyboardEvent(Keys Key, KeyStates State)
        {

            if (isSendingKey)
            {
                if (Key != Keys.None)
                {
                    if (Key == Keys.Escape && State == KeyStates.WM_KEYDOWN && previousKey.key == Keys.Escape)
                    {
                        _changeScreen.Invoke(ScreenEdges.NONE);
                        _isScreenChanged = false;
                        ShowCursor(true);
                        Unclip();
                    }
                    if (currRemoteIP != null)
                    {
                        List<byte> intBytes = new();
                        intBytes.AddRange(BitConverter.GetBytes((UInt16)InputDevice.Keyboard));
                        intBytes.AddRange(BitConverter.GetBytes((int)Key));
                        intBytes.AddRange(BitConverter.GetBytes((int)State));
                        byte[] result = intBytes.ToArray();
                        KeyLocalSocket.Send(result, currRemoteIP);

                    }
                    if (State == KeyStates.WM_KEYDOWN)
                        previousKey = new QueueKey
                        {
                            key = Key,
                            state = State
                        };

                }
            }
        }

        private void CallMouseEvent(MouseEventFlags msg, MSLLHOOKSTRUCT msStruct)
        {
            Cursor.Position = new Point(msStruct.X, msStruct.Y);
            MouseControl.mouse_event(msg, 0, 0, msStruct.mouseData/50000, msStruct.dwExtraInfo);
        }



        public void SendMouseEvent(MouseMessages msg, MSLLHOOKSTRUCT msStruct)
        {
            if (isSendingKey)
            {
                MouseEventFlags send = 0;
                switch (msg)
                {
                    case MouseMessages.WM_LBUTTONDOWN: { send = MouseEventFlags.LEFTDOWN; break; }
                    case MouseMessages.WM_LBUTTONUP: { send = MouseEventFlags.LEFTUP; break; }
                    case MouseMessages.WM_MOUSEMOVE: { send = MouseEventFlags.MOVE; break; }
                    case MouseMessages.WM_MOUSEWHEEL: { send = MouseEventFlags.WHEEl; break; }
                    case MouseMessages.WM_MOUSEWHEELUp: { send = MouseEventFlags.MIDDLEUP; break; }
                    case MouseMessages.WM_MOUSEWHEELDOWN: { send = MouseEventFlags.MIDDLEDOWN; break; }
                    case MouseMessages.WM_RBUTTONDOWN: { send = MouseEventFlags.RIGHTDOWN; break; }
                    case MouseMessages.WM_RBUTTONUP: { send = MouseEventFlags.RIGHTUP; break; }
                }
                if (currRemoteIP != null && send != 0)
                {
                    List<byte> intbytes = new();
                    intbytes.AddRange(BitConverter.GetBytes((UInt16)InputDevice.Mouse));
                    intbytes.AddRange(BitConverter.GetBytes((int)send));
                    intbytes.AddRange(BitConverter.GetBytes(msStruct.X));
                    intbytes.AddRange(BitConverter.GetBytes(msStruct.Y));
                    intbytes.AddRange(BitConverter.GetBytes(msStruct.mouseData));
                    intbytes.AddRange(BitConverter.GetBytes(msStruct.flags));
                    intbytes.AddRange(BitConverter.GetBytes(msStruct.dwExtraInfo));
                    byte[] result = intbytes.ToArray();
                    KeyLocalSocket.Send(result, currRemoteIP);
                }
            }
        }




        public ScreenEdges isMouseTouchScreenEdge()
        {
            int width = Screen.PrimaryScreen.Bounds.Size.Width;
            int height = Screen.PrimaryScreen.Bounds.Size.Height;

            if (Cursor.Position.X == 0)
                return ScreenEdges.LEFT;
            if (Cursor.Position.X == width)
                return ScreenEdges.RIGHT;
            if (Cursor.Position.Y == 0)
                return ScreenEdges.UP;
            if (Cursor.Position.Y == height)
                return ScreenEdges.DOWN;
            return ScreenEdges.NONE;
        }

        //public void Hide()
        //{
        //    ShowCursor(false);
        //}

        //public void Show()
        //{
        //    ShowCursor(true);
        //}

        //void pressLeftMouse()
        //{

        //    //    mouse_event(MouseFlags.Absolute | MouseFlags.Move, x, y, 0, UIntPtr.Zero);
        //    mouse_event(MouseEventFlags.LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        //    mouse_event(MouseEventFlags.LEFTUP, 0, 0, 0, UIntPtr.Zero);
        //}



        [DllImport("User32.dll")]
        static extern int ShowCursor(bool bShow);

        public void Clip()
        {

            var rect = Cursor.Clip;

            //Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y - 50);
            rect = new Rectangle(Cursor.Position, Screen.PrimaryScreen.Bounds.Size);
            //Cursor.Clip = rect;
            //Cursor.Hide();

            //int a;
            //re _clip;
            //HWND _window;

            ////Get the window's handle
            //_window = FindWindow(NULL, title);

            ////Create a RECT out of the window
            //GetWindowRect(_window, &_clip);

            ////Modify the rect slightly, so the frame doesn't get clipped with
            //_clip.left += 5;
            //_clip.top += 30;
            //_clip.right -= 5;
            //_clip.bottom -= 5;

            ////Clip the RECT
            //ClipCursor(&_clip);
            //ShowCursor?(false);

        }

        /* Makes the whole screen accessable again by using ClipCursor
         * on the complete screensize
         */
        public void Unclip()
        {
            Cursor.Clip = Rectangle.Empty;
            //Cursor.Clip.X -= 1920;
            //Cursor.Clip.Y -= 1080;
            //int a;

            //RECT _screen;

            ////Build a RECT with the size of the complete window (Note: GetSystemMetrics only covers the main monitor, this won't work in a multi-monitor setup)
            //_screen.left = 0;
            //_screen.top = 0;
            //_screen.right = GetSystemMetrics(SM_CXSCREEN);
            //_screen.bottom = GetSystemMetrics(SM_CYSCREEN);

            ////Unclip everything by using ClipCursor on the complete screen
            //ClipCursor(&_screen);
            //ShowCursor ? (TRUE);

        }

        public void CheckInputs()
        {
            ScreenEdges flag;
            while (_isLocalCheckingInput)
            {
                Thread.Sleep(100);
                flag = isMouseTouchScreenEdge();
                if (flag != ScreenEdges.NONE)
                {
                    _isScreenChanged = true;
                    ShowCursor(false);
                    //Clip();
                    _changeScreen?.Invoke(flag);
                }
            }
        }
    }
}
