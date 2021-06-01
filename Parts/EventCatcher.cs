using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

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

    public class EventCatcher
    {

        public MouseControl _mouseControl;
        public KeyboardControl _keyboardControl;

       public EventCatcher()
        {
            _mouseControl = new MouseControl();
            _keyboardControl = new KeyboardControl();

        }

    }

    public class KeyboardControl
    {

    }

    public class MouseControl
    {

        public MouseControl()
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
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
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
        //    mouse_event(MouseFlags.Absolute | MouseFlags.Move, x, y, 0, UIntPtr.Zero);
        
    }
}