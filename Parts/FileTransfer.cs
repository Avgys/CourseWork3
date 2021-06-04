//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CourseWork.Parts
//{
//    class FileTransfer
//    {
//        /*void SendFile(std::string Filename, char* addrToConnect) {
//	char temp[1024] = "\0";
//	//std::string temp;
//	TCPSocket my_socket;
//	my_socket.CreateTCPSocket();

//	sockaddr_in dest_addr = my_socket.ConvertToAddr(addrToConnect);
//	FILE* inFile;
//	if (my_socket.Connect(dest_addr) == 1) {
//		errno_t err = fopen_s(&inFile, Filename.c_str(), "rb");
//		std::cout << Filename;
//		if (my_socket.Send(Filename.c_str(), strlen(Filename.c_str()))) {

//			fseek(inFile, 0, SEEK_END);
//			unsigned long fileSize = ftell(inFile);
//			rewind(inFile);

//			char sizeInChar[10] = { 0 };
//			err = _itoa_s(fileSize, sizeInChar, 10);
//			unsigned long alreadySent = 0;

//			my_socket.Send(sizeInChar, sizeof(sizeInChar));

//			while (alreadySent < fileSize) {
//				int realBytes = fread(temp, 1, sizeof(temp), inFile);
//				alreadySent += realBytes;
//				my_socket.Send(temp, realBytes);
//			}
//		}
//		fclose(inFile);
//	}
//	my_socket.CloseTCPSocket(my_socket.my_socket);
//}

//int AcceptFile() {
//	TCPSocket my_socket;
//	my_socket.CreateTCPSocket();

//	my_socket.Bind();
//	my_socket.Listen(32);
//	char temp[1024] = "\0";
//	char filename[40] = "\0";
//	SOCKET* sock = my_socket.Accept(my_socket.client_addr);
//	FILE* outFile;
//	if (WSAGetLastError() == 0) {
//		printf("Connect accepted");
//		if (my_socket.Receive(filename, sizeof(filename))) {
//			unsigned long fileSize = 0;
//			int bytesReceived;
//			char sizeInChar[10] = { 0 };

//			if (my_socket.Receive(sizeInChar, sizeof(sizeInChar))) {
//				fileSize = _atoi64(sizeInChar);
//				errno_t err = fopen_s(&outFile, filename, "wb");
//				unsigned long bytesCount = 0;
//				do {
//				bytesReceived = my_socket.Receive(temp, sizeof(temp));
//				fwrite(temp, 1, bytesReceived, outFile);
//				bytesCount += bytesReceived;
//				} while (bytesCount < fileSize);
//				fclose(outFile);
//				my_socket.CloseTCPSocket(my_socket.my_socket);
//				return 0;
//			}
//		}
//	}
//	return -1;
//}*/



//        public void SendData()
//        {
//            // Состав отсылаемого универсального сообщения
//            // 1. Заголовок о следующим объектом класса подробной информации дальнейших байтов
//            // 2. Объект класса подробной информации о следующих байтах
//            // 3. Байты непосредственно готовых к записи в файл или для чего-то иного.

//            SendInfo si = new SendInfo();
//            si.message = "текст сообщения";
//            ...

//    FileInfo fi = new FileInfo(SendFileName);
//            si.filesize = (int)fi.Length;
//            si.filename = fi.Name;

//            BinaryFormatter bf = new BinaryFormatter();
//            MemoryStream ms = new MemoryStream();
//            bf.Serialize(ms, si);
//            ms.Position = 0;
//            byte[] infobuffer = new byte[ms.Length];
//            int r = ms.Read(infobuffer, 0, infobuffer.Length);
//            ms.Close();

//            // байты главного заголовка
//            byte[] header = GetHeader(infobuffer.Length);

//            // Общий массив байтов
//            byte[] total = new byte[header.Length + infobuffer.Length + si.filesize];

//            Buffer.BlockCopy(header, 0, total, 0, header.Length);
//            Buffer.BlockCopy(infobuffer, 0, total, header.Length, infobuffer.Length);

//            // Добавим содержимое файла в общий массив сетевых данных
//            FileStream fs = new FileStream(SendFileName, FileMode.Open, FileAccess.Read);
//            fs.Read(total, header.Length + infobuffer.Length, si.filesize);
//            fs.Close();

//            // Отправим данные подключенным клиентам
//            NetworkStream ns = _tcpClient.tcpClient.GetStream();
//            // Так как данный метод вызывается в отдельном потоке 
//            // рациональней использовать синхронный метод отправки
//            ns.Write(total, 0, total.Length);

//            ...

//        // Подтверждение успешной отправки
//        Parent.ShowReceiveMessage("Данные успешно отправлены!");
//        }

//        public void ReadCallback(IAsyncResult ar)
//        {

//            TcpClientData myTcpClient = (TcpClientData)ar.AsyncState;

//            try
//            {
//                NetworkStream ns = myTcpClient.tcpClient.GetStream();

//                int r = ns.EndRead(ar);

//                if (r > 0)
//                {
//                    // Из главного заголовка получим размер массива байтов информационного объекта
//                    string header = Encoding.Default.GetString(myTcpClient.buffer);
//                    int leninfo = int.Parse(header);

//                    // Получим и десериализуем объект с подробной информацией
//                    // о содержании получаемого сетевого пакета
//                    MemoryStream ms = new MemoryStream(leninfo);
//                    byte[] temp = new byte[leninfo];
//                    r = ns.Read(temp, 0, temp.Length);
//                    ms.Write(temp, 0, r);
//                    BinaryFormatter bf = new BinaryFormatter();
//                    ms.Position = 0;
//                    SendInfo sc = (SendInfo)bf.Deserialize(ms);
//                    ms.Close();

//                    ...

//            // Создадим файл на основе полученной информации и
//            // массива байтов следующих за объектом информации
//            FileStream fs = new FileStream(sc.filename, FileMode.Create,
//                    FileAccess.ReadWrite, FileShare.ReadWrite, sc.filesize);
//                    do
//                    {
//                        temp = new byte[global.MAXBUFFER];
//                        r = ns.Read(temp, 0, temp.Length);

//                        // Записываем строго столько байтов сколько прочтено методом Read()
//                        fs.Write(temp, 0, r);

//                        // Как только получены все байты файла, останавливаем цикл,
//                        // иначе он заблокируется в ожидании новых сетевых данных
//                        if (fs.Length == sc.filesize)
//                        {
//                            fs.Close();
//                            break;
//                        }
//                    }
//                    while (r > 0);

//                    ...

//            if (Receive != null)
//                        Receive(this, new ReceiveEventArgs(sc));

//                    ...

//        }
//                else
//                {
//                    DeleteClient(myTcpClient);

//                    // Событие клиент отключился
//                    if (Disconnected != null)
//                        Disconnected.BeginInvoke(this, "Клиент отключился!", null, null);
//                }
//            }
//            catch (Exception e)
//            {
//                DeleteClient(myTcpClient);

//                // Событие клиент отключился
//                if (Disconnected != null)
//                    Disconnected.BeginInvoke(this, "Клиент отключился аварийно!", null, null);

//                SoundError();
//            }

//        }
//    }
//    }
//}
