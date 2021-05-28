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
    namespace SoundTranfer
    {

        class SoundTransfer
        {
            //Подключены ли мы
            private bool _Connected;
            //сокет отправитель
            //Socket _client;
            UdpConnection _ConnectionToSend;

            UdpConnection _ConnectionToReceive;
            //поток для нашей речи
            WaveIn _SoundInput;
            //поток для речи собеседника
            WaveOut _SoundOutput;
            //буфферный поток для передачи через сеть
            BufferedWaveProvider _BufferStream;
            //поток для прослушивания входящих сообщений
            Thread in_thread;
            //сокет для приема (протокол UDP)
            //Socket listeningSocket;

            public SoundTransfer()
            {
                //создаем поток для записи нашей речи
                _SoundInput = new WaveIn();
                MessageBox.Show(_SoundInput.DeviceNumber.ToString());
                //определяем его формат - частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
                _SoundInput.WaveFormat = new WaveFormat(48000, 24, 1);
                //добавляем код обработки нашего голоса, поступающего на микрофон
                _SoundInput.DataAvailable += Sound_Input;
                //создаем поток для прослушивания входящего звука
                _SoundOutput = new WaveOut();
                //создаем поток для буферного потока и определяем у него такой же формат как и потока с микрофона
                _BufferStream = new BufferedWaveProvider(new WaveFormat(48000, 24, 1));
                //привязываем поток входящего звука к буферному потоку
                _SoundOutput.Init(_BufferStream);
                //сокет для отправки звука
                _ConnectionToSend = new UdpConnection();

                //client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _Connected = true;
                _ConnectionToReceive = new UdpConnection();
                //listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //создаем поток для прослушивания
                in_thread = new Thread(new ThreadStart(Listening));
                //запускаем его
                in_thread.Start();
            }

            //Обработка нашего голоса
            private void Sound_Input(object sender, WaveInEventArgs e)
            {
                try
                {
                    //Подключаемся к удаленному адресу
                    IPEndPoint remote_point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555);
                    //посылаем байты, полученные с микрофона на удаленный адрес
                    _ConnectionToSend.Send(e.Buffer);
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
                while (_Connected == true)
                {
                    try
                    {
                        //промежуточный буфер
                        //byte[] data = new byte[65535];
                        //получено данных
                        IPEndPoint iPEndPoint = null;
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
}