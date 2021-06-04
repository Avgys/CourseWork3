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

    class SoundTransfer
    {
        //Подключены ли мы
        private bool _Connected;
        //сокет отправитель

        //Socket _client;
        List<UdpConnection> _ConnectionsToSend;

        UdpConnection _ConnectionToReceive;
        //поток для входящего звука для отправки
        //WasapiLoopbackCapture _SoundInput;
        WasapiCapture _SoundInput;
        //поток для полученного звука
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
            var enumerator = new MMDeviceEnumerator();
            if (mainControler._options.isReceivingSound)
            {
                var device = enumerator.GetDevice(MainControler._options.defaultOutputSound);
                _SoundOutput = new WasapiOut(device, AudioClientShareMode.Shared, false, 300);
                
                //создаем поток для буферного потока и определяем у него такой же формат как и потока с микрофона
                _BufferStream = new BufferedWaveProvider(new WaveFormat(48000, 32, 2));
                //привязываем поток входящего звука к буферному потоку
                _SoundOutput.Init(_BufferStream);
                _ConnectionToReceive = new UdpConnection();
                in_thread = new Thread(new ThreadStart(Listening));
                //запускаем его
                in_thread.Name = "Listening Sound";
                in_thread.Start();
            }
            if (mainControler._options.isSendingSound)
            {
                var device = enumerator.GetDevice(MainControler._options.defaultInputSound);
                _SoundInput = new WasapiCapture(device);

                //создаем поток для записи нашей речи
                //_SoundInput = new WaveIn();

                //определяем его формат - частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
                //_SoundInput.WaveFormat = new WaveFormat(48000, 24, 2);
                //добавляем код обработки нашего голоса, поступающего на микрофон
                _SoundInput.DataAvailable += Sound_Input;
                StartRecord();
            }
        }

        public void CheckSendConnections(List<IPEndPoint> remoteClients)
        {

            List<IPEndPoint> remoteEndPoints = new List<IPEndPoint>();
            if (_ConnectionsToSend != null)
            {
                foreach (var connection in _ConnectionsToSend)
                {
                    if (!remoteClients.Contains(connection.client.Client.RemoteEndPoint as IPEndPoint))
                    {
                        connection.Close();
                        _ConnectionsToSend.Remove(connection);
                    }
                    else
                    {
                        remoteEndPoints.Add(connection.client.Client.RemoteEndPoint as IPEndPoint);
                    }
                }

                for (int i = 0; i < remoteClients.Count; i++)
                {
                    if (remoteEndPoints.Contains(remoteClients[i]))
                    {
                        //(remoteClients.RemoveAt(i));
                        remoteEndPoints.Add(remoteClients[i]);
                    }
                    else
                    {
                        UdpConnection temp = new UdpConnection();
                        temp.Connect(remoteClients[i]);
                        _ConnectionsToSend.Add(temp);
                    }
                }
            }
            else
            {
                _ConnectionsToSend = new List<UdpConnection>();
                foreach (var endPoint in remoteClients)
                {
                        UdpConnection temp = new UdpConnection();
                        temp.Connect(endPoint);
                        _ConnectionsToSend.Add(temp);
                }
            }
        }

        //Обработка нашего голоса
        private void Sound_Input(object sender, WaveInEventArgs e)
        {
            try
            {
                //Рассылаем всем подключенным клиентам
                foreach (var connection in _ConnectionsToSend)
                {
                    connection.Send(e.Buffer);
                }

            }
            catch (Exception ex)
            {
                throw ex;
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
            while (MainControler._options.isReceivingSound)
            {
                _Connected = MainControler._isSoundConnected;
                //_Connected = true;
                if (_Connected)
                {
                    //_ConnectionToReceive.Connect("127.0.0.1", 8888);

                    while (_Connected)
                    {
                        try
                        {
                            //промежуточный буфер
                            //byte[] data = new byte[65535];
                            //получено данных
                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            byte[] buff = _ConnectionToReceive.Receive(ref iPEndPoint);
                            //добавляем данные в буфер, откуда output будет воспроизводить звук
                            _BufferStream.AddSamples(buff, 0, buff.Length);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
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