using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Text.Json;
using System.Net.Sockets;
using System.Net;
using NAudio.Wave;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Text.Json.Serialization;

namespace CourseWork.Parts
{
    using NAudio.Wave;
    using NAudio.CoreAudioApi;

    public class Options
    {
        public string defaultInputSound { get; set; }
        public string defaultOutputSound { get; set; }

        public int defaultSoundPort { get; set; }
        public int defaultEventPort { get; set; }
        public int defaultFilePort { get; set; }

        public int defualtTcpPort { get; set; }
        public int defualtUdpPort { get; set; }

        public bool _isAcceptable { set; get; }
        public bool _isTryingConnect { set; get; }

        string defaultSysInputSound;
        string defaultSysOutputSound;

        [JsonIgnore]
        public List<MMDevice> _SoundDevices;
        [JsonIgnore]
        public List<IPEndPoint> remoteClientsAddress { get; set; }

        public List<string> serializableClients { get; set; }

        [JsonInclude]
        public bool ISReceivingSound;
        [JsonIgnore]
        public bool isReceivingSound
        {
            get
            {
                return ISReceivingSound;
            }
            set
            {
                ISReceivingSound = value;
            }
        }
        [JsonInclude]
        public bool ISSendingSound;
        [JsonIgnore]
        public bool isSendingSound
        {
            get
            {
                return ISSendingSound;
            }
            set
            {
                ISSendingSound = value;
            }
        }

        public Options()
        {
            isReceivingSound = false;
            isSendingSound = false;
            _isAcceptable = true;
            _isTryingConnect = true;
            serializableClients = new List<string>();
            remoteClientsAddress = new List<IPEndPoint>();
            SetSoundParams();
        }

        ~Options()
        {

        }

        public void Close()
        {
            isReceivingSound = false;
            isSendingSound = false;
            _isAcceptable = false;
        }

        private void SetSoundParams()
        {
            _SoundDevices = SoundTransfer.GetDeviceNames();
            var enumerator = new MMDeviceEnumerator();
            defaultSysOutputSound = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
            defaultSysInputSound = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
            if (defaultInputSound == null) defaultInputSound = defaultSysInputSound;
            if (defaultOutputSound == null) defaultOutputSound = defaultSysOutputSound;
        }

        public void SetDefaultInputSound(string devicename)
        {
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
            {
                if (wasapi.FriendlyName == devicename)
                {
                    defaultInputSound = wasapi.ID;
                    break;
                }
            }
        }
        public void SetDefaultoutputSound(string devicename)
        {
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (wasapi.FriendlyName == devicename)
                {
                    defaultOutputSound = wasapi.ID;
                    break;
                }
            }
        }

        public bool isContainsClient(string ip_, string port_)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(ip_);
                IPEndPoint iPEndPoint = new IPEndPoint(localAddr, Int32.Parse(port_));

                return remoteClientsAddress.Contains(iPEndPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            finally
            {

            }
        }

        public bool AddClient(string ip_, string port_)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(ip_);

                IPEndPoint iPEndPoint = new IPEndPoint(localAddr, Int32.Parse(port_));
                lock (Manipulator.remoteClientsAddressInUseLock)
                    remoteClientsAddress.Add(iPEndPoint);
                serializableClients.Add(iPEndPoint.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            finally
            {

            }
            return true;
        }

        public bool AddClient(IPEndPoint e)
        {
            try
            {
                if (!remoteClientsAddress.Contains(e))
                {
                    lock (Manipulator.remoteClientsAddressInUseLock)
                        remoteClientsAddress.Add(e);
                    serializableClients.Add(e.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            finally
            {

            }
            return true;
        }

        public void RemoveClient(string ip_, string port_)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(ip_);

                IPEndPoint iPEndPoint = new IPEndPoint(localAddr, Int32.Parse(port_));
                if (remoteClientsAddress.Contains(iPEndPoint))
                {                    
                    {
                        Manipulator._currManipulator.RemoveRemoteTcp(iPEndPoint);                        
                        remoteClientsAddress.Remove(iPEndPoint);
                        serializableClients.Remove(iPEndPoint.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {

            }
        }

        public void CheckSettings()
        {
            SetSoundParams();
            foreach(var client in serializableClients)
            {
                IPEndPoint ip;
                if (IPEndPoint.TryParse(client,out ip))
                {
                    lock (Manipulator.remoteClientsAddressInUseLock)
                        remoteClientsAddress.Add(ip);
                }
            }
            
        }
    }
}
