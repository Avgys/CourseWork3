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

    [Flags]
    public enum KeyStates
    {
        WM_KEYDOWN = 0x100,
        WM_KEYUP = 0x101,
        WM_SYSKEYDOWN = 0x104,
        WM_SYSKEYUP = 0x105
    }

    public struct QueueKey
    {
        public Keys key;
        public KeyStates state;
    }

    public static class KeyboardControl
    {

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const int KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYUP = 2;        

        static object InputQueuelocker = new object();

        static public bool _isCheckingInput { get; set; }

        static public EventController _Owner;

        static public void Start()
        {
           
            _hookID = SetHook(_proc);
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

        private const int WH_KEYBOARD_LL = 13;
        //private const int WH_KEYBOARD = 2;
        //
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        public static QueueKey _InputKeyQueue;     

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public struct KBHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, int wParam, KBHookStruct lParam);

        private static IntPtr HookCallback(int nCode, int wParam, KBHookStruct lParam)
        {
                        
            bool blnEat = false;

             if (nCode >= 0)
            {
                _Owner.SendKeyboardEvent((Keys)lParam.vkCode, (KeyStates)wParam);                
            }

            switch (wParam)
            {
                case 256:
                case 257:
                case 260:
                case 261:
                    //Alt+Tab, Alt+Esc, Ctrl+Esc, Windows Key,
                    blnEat =
                            ((lParam.vkCode == 9) && (lParam.flags == 32))
                            | ((lParam.vkCode == 27) && (lParam.flags == 32))
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

    [Flags]
    public enum MouseEventFlags
    {
        LEFTDOWN = 0x0002,
        LEFTUP = 0x0004,
        MIDDLEDOWN = 0x0020,
        MIDDLEUP = 0x0040,
        WHEEl = 0x0800,
        MOVE = 0x0001,
        ABSOLUTE = 0x8000,
        RIGHTDOWN = 0x0008,
        RIGHTUP = 0x0010
    }

    [Flags]
    public enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MOUSEMOVE = 0x0200,
        WM_MOUSEWHEEL = 0x020A,
        WM_MOUSEWHEELUp = 520,
        WM_MOUSEWHEELDOWN = 519,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public int X;
        public int Y;
        public int mouseData;
        public uint flags;
        public uint time;
        public uint dwExtraInfo;
    }


    public static class MouseControl
    {
        public static EventController _Owner;

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static MSLLHOOKSTRUCT _InputMouse;
        private const int WH_MOUSE_LL = 14;

        [DllImport("User32.dll")]
        static public extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, int dwData, uint dwExtraInfo);


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
            if (nCode >= 0)
            {
                _InputMouse = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                _Owner.SendMouseEvent((MouseMessages)wParam, _InputMouse);
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
}
