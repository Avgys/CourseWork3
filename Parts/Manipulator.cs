using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.Json;

namespace CourseWork.Parts
{
    using SoundTranfer;
    public class Manipulator
    {
        int _PCid;

        Options _options;
        SoundTransfer _sound;
        FileTransfer _fileTransfer;
        EventController _eventControler;
        public bool _isFileTranfering { set; get; }
        public bool _isSoundConnected { set; get; }

        public Manipulator()
        {

            _sound = new SoundTransfer(this);
            //_fileTransfer = new FileTransfer();
            _eventControler = new EventController();
            _eventControler._changeScreen += ChangeScreen;
            LoadSettings();


        }

        public void LoadSettings()
        {

            string str = System.IO.File.ReadAllText("./Settings.json");
            _options = JsonSerializer.Deserialize(, Options);
                
        }

        public void SaveSettings()
        {
            string json;
            json = JsonSerializer.Serialize(_options);
            System.IO.File.WriteAllText("./Settings.json", str);
        }

        

        public void ChangeScreen(ScreenEdges side)
        {

        }
    }
}