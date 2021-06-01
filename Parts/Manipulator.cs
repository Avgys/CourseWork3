using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CourseWork.Parts
{
    using SoundTranfer;
    public class Manipulator
    {
        public Manipulator()
        {
            _sound = new SoundTransfer(this);
            _fileTransfer = new FileTransfer();
            _eventCatcher = new EventCatcher(this);
            _eventTransfer = new EventTransfer();
            Thread checkInputs = new Thread(new ThreadStart(CheckInputs)); 
            checkInputs.Start();
            changeScreen += ChangeScreen;
            
        }

        public delegate void СursorState(ScreenEdges side);
        public event СursorState changeScreen;

        bool _isScreenChanged;

        SoundTransfer _sound;
        FileTransfer _fileTransfer;
        EventCatcher _eventCatcher;
        EventTransfer _eventTransfer;
        public bool _isSoundConnected { set; get; }

        public void CheckInputs()
        {
            ScreenEdges flag;
            while (true)
            {
                Thread.Sleep(10);
                flag = _eventCatcher._mouseControl.isMouseTouchScreenEdge();
                if (flag != ScreenEdges.NONE)
                {
                    _isScreenChanged = true;
                    changeScreen?.Invoke(flag);
                }

                if (_isScreenChanged)
                {
                    _eventCatcher._mouseControl.getInput();
                    _eventCatcher._keyboardControl.getInput();
                }

            }
        }

        public void ChangeScreen(ScreenEdges side)
        {

        }
    }
}