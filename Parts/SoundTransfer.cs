using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Windows;

namespace CourseWork.Parts
{
    using NAudio.Wave;
    using NAudio.CoreAudioApi;



    public class SoundTransfer
    {
        //Подключены ли мы
        private bool _Connected;
        //сокет отправитель
        object SoundClienAddresses = new();
        //Socket _client;
        public List<IPEndPoint> _ClientsAddress;
        public UdpConnection _ConnectionToReceive;
        public UdpConnection _ConnectionToSend;
        bool isReceiveSound;
        WasapiCapture _SoundInput;
        WasapiOut _SoundOutput;
        //буфферный поток для передачи через сеть
        BufferedWaveProvider _BufferStream;
        //поток для прослушивания входящих сообщений
        Thread in_thread;
        //сокет для приема (протокол UDP)
        Manipulator MainControler;

        public static List<MMDevice> GetDeviceNames()
        {
            List<MMDevice> Devices = new List<MMDevice>();
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
            {
                if (wasapi.State == DeviceState.Active)
                {
                    Devices.Add(wasapi);
                }
            }
            return Devices;
        }

        ~SoundTransfer()
        {
            StopRecord();
            _SoundOutput.Stop();
        }

        public void Stop()
        {
            StopRecord();
            _SoundOutput.Stop();
        }

        public SoundTransfer(Manipulator mainControler)
        {
            _Connected = false;
            MainControler = mainControler;
            //создаем поток для прослушивания
            _ClientsAddress = new();
            _ConnectionToReceive = new UdpConnection();

            Activate(DataFlow.Render);
            Activate(DataFlow.Capture);
        }



        public void Activate(DataFlow flow)
        {
            if (MainControler._options.isReceivingSound)
            {
                if (flow == DataFlow.Render)
                {
                    isReceiveSound = MainControler._options.isReceivingSound;
                    var enumerator = new MMDeviceEnumerator();
                    MMDevice device;
                    try
                    {
                        device = enumerator.GetDevice(MainControler._options.defaultOutputSound);
                    }
                    catch
                    {
                        device = enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
                    }
                    if (device == null)
                    {
                        device = enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
                    }
                    _SoundOutput = new WasapiOut(device, AudioClientShareMode.Shared, true, 100);

                    _BufferStream = new BufferedWaveProvider(_SoundOutput.OutputWaveFormat);

                    _SoundOutput.Init(_BufferStream);


                    in_thread = new Thread(new ThreadStart(Listening));
                    //запускаем его
                    in_thread.Name = "Listening Sound";
                    in_thread.Start();
                }
            }

            if (MainControler._options.isSendingSound)
            {
                if (flow == DataFlow.Capture)
                {
                    _ConnectionToSend = new UdpConnection();
                    _SoundInput = new WasapiLoopbackCapture();

                    _SoundInput.DataAvailable += SoundSend;
                    StartRecord();
                }
            }
        }

        public void Deactivate(DataFlow flow)
        {
            var enumerator = new MMDeviceEnumerator();
            if (flow == DataFlow.Render)
            {
                if (in_thread.IsAlive)
                {
                    MainControler._options.isReceivingSound = false;
                    _SoundOutput.Stop();
                    isReceiveSound = false;
                    _ConnectionToReceive.Close();
                    if (in_thread.IsAlive)
                        in_thread.Join();
                }
            }

            if (flow == DataFlow.Capture)
            {
                if (_SoundInput != null)
                {
                    _SoundInput.StopRecording();
                    _SoundInput.Dispose();
                }
            }
        }

        public void CheckSendConnections(List<IPEndPoint> remoteClients)
        {
            lock (SoundClienAddresses)
                _ClientsAddress = remoteClients;
            if (_ClientsAddress.Count != 0)
                _Connected = true;
            else
                _Connected = false;
            //if (_ClientsAddress != null)
            //{

            //    int s =_ClientsAddress.RemoveAll(x => !remoteClients.Contains(x));

            //    //foreach (var connection in _ConnectionsToSend)
            //    //{
            //    //    if (!remoteClients.Contains(connection.client.Client.RemoteEndPoint as IPEndPoint))
            //    //    {
            //    //        connection.Close();
            //    //        _ConnectionsToSend.Remove(connection);
            //    //    }
            //    //    else
            //    //    {
            //    //        remoteEndPoints.Add(connection.client.Client.RemoteEndPoint as IPEndPoint);
            //    //    }
            //    //}

            //    //for (int i = 0; i < remoteClients.Count; i++)
            //    //{
            //    //    if (remoteEndPoints.Contains(remoteClients[i]))
            //    //    {
            //    //        //(remoteClients.RemoveAt(i));
            //    //        remoteEndPoints.Add(remoteClients[i]);
            //    //    }
            //    //    else
            //    //    {
            //    //        UdpConnection temp = new UdpConnection();
            //    //        temp.Connect(remoteClients[i]);
            //    //        _ConnectionsToSend.Add(temp);
            //    //    }
            //    //}
            //}
            //else
            //{

            //    _ClientsAddress = remoteClients;
            //}
        }

        //Обработка нашего голоса
        private void SoundSend(object sender, WaveInEventArgs e)
        {
            try
            {
                //_ConnectionToSend.Send(e.Buffer, address);

                //_BufferStream.AddSamples(e.Buffer, 0, e.BytesRecorded);
                //Рассылаем всем подключенным клиентам
                var list = _ClientsAddress.ToList();

                if (list.Count > 0)
                {
                    byte[] buff = new byte[e.BytesRecorded];
                    Array.Copy(e.Buffer, buff, e.BytesRecorded);

                    foreach (var address in list)
                    {
                        _ConnectionToSend.Send(buff, address);
                        //_BufferStream.AddSamples(e.Buffer, 0, e.Buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                //_BufferStream.ClearBuffer();
                //throw ex;
            }
        }
        //Прослушивание входящих подключений
        private void Listening()
        {
            //Прослушиваем по адресу
            //начинаем воспроизводить входящий звук
            _SoundOutput.Play();
            //адрес, с которого пришли данные
            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            //бесконечный цикл
            _Connected = MainControler._isSoundConnected;
            while (MainControler._options.isReceivingSound && isReceiveSound)
            {
                //CheckSendConnections(MainControler.ConnectedRemoteClientsAddress);
                //_Connected = true;                
                while (_Connected && isReceiveSound)
                {
                    try
                    {
                        //промежуточный буфер
                        //byte[] data = new byte[65535];
                        //получено данных
                        IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] buff = _ConnectionToReceive.ReceiveDirect(ref iPEndPoint);
                        //добавляем данные в буфер, откуда output будет воспроизводить звук
                        if (buff != null && buff.Length > 0)
                            _BufferStream.AddSamples(buff, 0, buff.Length);
                    }
                    catch (Exception ex)
                    {
                        _BufferStream.ClearBuffer();
                        //throw ex;
                    }
                }
                Thread.Sleep(100);
            }
            _SoundOutput.Stop();
        }

        public void StartRecord()
        {
            _SoundInput.StartRecording();
        }

        public void StopRecord()
        {
            _SoundInput.StopRecording();
        }
    }

}