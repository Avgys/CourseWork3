using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace CourseWork.Parts
{
    [Flags]
    public enum ScreenEdges
    {
        NONE = 0x1,
        LEFT = 0x2,
        UP = 0x4,
        DOWN = 0x8,
        RIGHT = 0x16
    }

    //public class EventCatcher
    //{

    //    public MouseControl _mouseControl;
    //    public MouseHook _mouseHook;

    //    public EventCatcher()
    //    {





    //        //Thread.Sleep(5000);

    //        //keyboardInput.Interrupt();
    //        //keyboardInput.Interrupt();
    //        //keyboardInput.Resume();

    //        //Thread mouseInput = new Thread(new ThreadStart(MouseHook.Start));
    //        //mouseInput.Start();
    //    }
    //}

    public class KeyboardControl
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const int KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYUP = 2;

        public static void KeyDown(Keys vKey)
        {
            keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
        }

        public static void KeyUp(Keys vKey)
        {
            keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }
    }


    public static class KeyboardHook
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool BlockInput([In, MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        //public static Mutex _KeyHookMutex = new Mutex();

        static object InputQueuelocker = new object();

        static public bool _isCheckingInput { get; set; }

        static public void Start()
        {

            _hookID = SetHook(_proc);
            _InputKeyQueue = new List<QueueKey>();
            Application.Run();
        }

        static public void Close()
        {
            _isCheckingInput = false;
        }

        static public void Stop()
        {
            UnhookWindowsHookEx(_hookID);
            Application.Exit();
        }

        static public List<QueueKey> getInputQueue()
        {
            bool acquiredLock = false;
            List<QueueKey> buff = null;
            try
            {
                Monitor.Enter(InputQueuelocker, ref acquiredLock);
                buff = _InputKeyQueue;
                _InputKeyQueue = new List<QueueKey>();
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(InputQueuelocker);
            }
            return buff;
        }


        private const int WH_KEYBOARD_LL = 13;
        //private const int WH_KEYBOARD = 2;

        [Flags]
        public enum KeyStates
        {
            WM_KEYDOWN = 0x100,
            WM_KEYUP = 0x101,
            WM_SYSKEYDOWN = 0x104,
            WM_SYSKEYUP = 0x105
        }
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static List<QueueKey> _InputKeyQueue;

        static public List<Keys> HookedKeys = new List<Keys>();

        public struct QueueKey
        {
            public Keys key;
            public KeyStates state;
        }

        private struct KBHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, int wParam, KBHookStruct lParam);

        private static IntPtr HookCallback(int nCode, int wParam, KBHookStruct lParam)
        {
                        
            bool blnEat = false;

            if (nCode >= 0)
            {
                //Keys vkCode = (Keys)lParam.vkCode;
                QueueKey key = new QueueKey()
                {
                    key = (Keys)lParam.vkCode,
                    state = (KeyStates)wParam
                };
                _InputKeyQueue.Add(key);
            }

            switch (wParam)
            {
                case 256:
                case 257:
                case 260:
                case 261:
                    //Alt+Tab, Alt+Esc, Ctrl+Esc, Windows Key,
                    blnEat =
                            //((lParam.vkCode == 9) && (lParam.flags == 32))
                            /*|*/ ((lParam.vkCode == 27) && (lParam.flags == 32))
                            | ((lParam.vkCode == 27) && (lParam.flags == 0))
                            | ((lParam.vkCode == 91) && (lParam.flags == 1))
                            | ((lParam.vkCode == 92) && (lParam.flags == 1))
                            | ((lParam.vkCode == 73) && (lParam.flags == 0));
                    break;
            }

            if (blnEat == true)
            {
                return (IntPtr)1;
            }
            else
            {
                return CallNextHookEx(_hookID, nCode, wParam, ref lParam);
            }
        }

        /// <summary>
        /// Sets the windows hook, do the desired event, one of hInstance or threadId must be non-null
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, int wParam, ref KBHookStruct lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public class MouseHook
    {
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static List<MSLLHOOKSTRUCT> _InputMouseQueue;
        private const int WH_MOUSE_LL = 14;

        public MouseHook()
        {
            _InputMouseQueue = new List<MSLLHOOKSTRUCT>();
        }


        static public void Start()
        {
            _hookID = SetHook(_proc);
            Application.Run();
        }

        static public void Stop()
        {
            UnhookWindowsHookEx(_hookID);
            Application.Exit();
        }

        [Flags]
        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT PT;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, int wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                //MessageBox.Show(hookStruct.PT.X + ", " + hookStruct.PT.Y);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public class MouseControl
    {

        public MouseControl()
        {
        }

        public void getInput()
        {

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

        public void Hide()
        {
            ShowCursor(false);
        }

        public void Show()
        {
            ShowCursor(true);
        }

        void pressLeftMouse()
        {

            //    mouse_event(MouseFlags.Absolute | MouseFlags.Move, x, y, 0, UIntPtr.Zero);
            mouse_event(MouseEventFlags.LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventFlags.LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        [DllImport("User32.dll")]
        static extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);

        [DllImport("User32.dll")]
        static extern int ShowCursor(bool bShow);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            MOVE = 0x0001,
            ABSOLUTE = 0x8000,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010
        }

        public void Clip()
        {
            var RectClipCoords = new Point()
            {
                X = -1920,
                Y = -1080
            };
            var rect = Cursor.Clip;
            Cursor.Position = new Point(Cursor.Position.X - 50, Cursor.Position.Y - 50);
            rect = new Rectangle(RectClipCoords, Screen.PrimaryScreen.Bounds.Size);
            Cursor.Clip = rect;
            Cursor.Hide();

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

    }
}
