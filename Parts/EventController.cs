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
    public class EventController
    {
        public delegate void СursorState(ScreenEdges side);
        public event СursorState _changeScreen;
        public MouseControl _mouseControl;
        public MouseHook _mouseHook;

        bool _isScreenChanged;
        public bool _isCheckingInput;

        public EventController()
        {
            _mouseControl = new MouseControl();

            Thread keyboardHook = new Thread(new ThreadStart(KeyboardHook.Start));
            keyboardHook.Start();
            //Thread mouseInput = new Thread(new ThreadStart(MouseHook.Start));
            //mouseInput.Start();

            _isCheckingInput = true;
            //Thread checkInputs = new Thread(new ThreadStart(CheckInputs));
            //checkInputs.Start();
        }

        public void Start()
        {

        }

        public void Close()
        {
            _isCheckingInput = false;            
            KeyboardHook.Stop();
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
