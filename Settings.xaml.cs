using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;

namespace CourseWork
{
    using CourseWork.Parts;
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        public Settings()
        {
            InitializeComponent();
        }

        private void HideAllGrids()
        {
            ClientsGrid.Visibility = Visibility.Hidden;
            SoundGrid.Visibility = Visibility.Hidden;
            NetworkGrid.Visibility = Visibility.Hidden;
        }

        public void Reset()
        {
            HideAllGrids();
            ResetClientGrid();
        }

        private void ResetClientGrid()
        {
            if (Manipulator._currManipulator._options != null)
            {
                ClientsAddresses.Items.Clear();
                List<ConnectionInfo> ConnectedClients = new ();
                foreach (var e in Manipulator._currManipulator.ConnectedRemoteClientsAddress)
                {
                    ConnectedClients.Add(new (e.RemoteClient));
                }
                for (int i = 0; i < Manipulator._currManipulator._options.serializableClients.Count; i++)
                {
                    bool flag = false;
                    if (ConnectedClients.Exists(x => x.RemoteClient.IPEndPoint == Manipulator._currManipulator._options.remoteClientsAddress[i]))
                        flag = true;
                    ClientsAddresses.Items.Add(Manipulator._currManipulator._options.serializableClients[i] + " " + flag.ToString());
                }
            }
        }

        private void MainMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            this.Owner.Top = this.Top;
            this.Owner.Left = this.Left;
            this.Owner.Width = this.Width;
            this.Owner.Height = this.Height;
            this.Owner.Show();
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            var options = Manipulator._currManipulator._options;
            if (!options.isContainsClient(IpAddress.Text, Port.Text))
                options.AddClient(IpAddress.Text, Port.Text);
            ResetClientGrid();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator.LoadSettings();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator.SaveSettings();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator.SaveSettings();
        }

        private void ClientsAddresses_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        //public void ReadSelectedItem()
        //{
        //    IPEndPoint temp;
        //    if (IPEndPoint.TryParse(ClientsAddresses.SelectedItem.ToString(), out temp))
        //    {
        //        IpAddress.Text = temp.Address.ToString();
        //        Port.Text = temp.Port.ToString();
        //    }
        //}

        private void ClientsAddresses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //IPEndPoint temp;
            //e.AddedItems;

            if (e.AddedItems.Count >= 1) {
                IpAddress.Text = Manipulator._currManipulator._options.remoteClientsAddress[ClientsAddresses.SelectedIndex].Address.ToString();
                Port.Text = Manipulator._currManipulator._options.remoteClientsAddress[ClientsAddresses.SelectedIndex].Port.ToString();
            }   
        }

        private void Clients_Click(object sender, RoutedEventArgs e)
        {
            HideAllGrids();
            ResetClientGrid();
            ClientsGrid.Visibility = Visibility.Visible;
        }

        private void ResetSoundGrid()
        {
            if (Manipulator._currManipulator._options != null)
            {

                var options = Manipulator._currManipulator._options;

                RecordDevice.Items.Clear();
                for (int i = 0; i < options._SoundDevices.Count; i++)
                {
                    RecordDevice.Items.Add(options._SoundDevices[i].FriendlyName);
                    if (options._SoundDevices[i].ID == options.defaultInputSound)
                    {
                        RecordDevice.SelectedItem = options._SoundDevices[i].FriendlyName;
                    }
                }

                PlayDevice.Items.Clear();
                for (int i = 0; i < options._SoundDevices.Count; i++)
                {
                    if (options._SoundDevices[i].DataFlow == NAudio.CoreAudioApi.DataFlow.Render)
                        PlayDevice.Items.Add(options._SoundDevices[i].FriendlyName);
                    if (options._SoundDevices[i].ID == options.defaultOutputSound)
                    {
                        PlayDevice.SelectedItem = options._SoundDevices[i].FriendlyName;
                    }
                }

                SendSound.IsChecked = Manipulator._currManipulator._options.isSendingSound;
                ReceiveSound.IsChecked = Manipulator._currManipulator._options.isReceivingSound;
                 
            }
        }

        private void Sound_Click(object sender, RoutedEventArgs e)
        {
            HideAllGrids();
            ResetSoundGrid();
            SoundGrid.Visibility = Visibility.Visible;
        }

        private void SetRecordDevice_Click(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.SetDefaultInputSound(RecordDevice.SelectedItem.ToString());

        }

        private void SetPlayDevice_Click(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.SetDefaultoutputSound(PlayDevice.SelectedItem.ToString());
        }


        private void Window_Closing(object sender, EventArgs e)
        {
            if (!this.Owner.IsActive)
                this.Owner.Close();
        }

        private void ResetNetworkGrid()
        {
            TCPport.Text = Manipulator._currManipulator._options.defualtTcpPort.ToString();
            UDPport.Text = Manipulator._currManipulator._options.defualtUdpPort.ToString();
        }

        private void DefalutPorts_Click(object sender, RoutedEventArgs e)
        {
            HideAllGrids();
            ResetNetworkGrid();
            NetworkGrid.Visibility = Visibility.Visible;
        }

        private void SetPorts_Click(object sender, RoutedEventArgs e)
        {
            int temp = -1;
            int.TryParse(TCPport.Text, out temp);
            if (temp != -1)
                Manipulator._currManipulator._options.defualtTcpPort = temp;
            temp = -1;
            int.TryParse(UDPport.Text, out temp);
            if (temp != -1)
                Manipulator._currManipulator._options.defualtUdpPort = temp;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var options = Manipulator._currManipulator._options;
            if (options.isContainsClient(IpAddress.Text, Port.Text))
               options.RemoveClient(IpAddress.Text, Port.Text);
            ResetClientGrid();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.isReceivingSound = true;
        }

        private void ReceiveSound_Unchecked(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.isReceivingSound = false;
        }

        private void SendSound_Unchecked(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.isSendingSound = false;
        }

        private void SendSound_Checked(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.isSendingSound = true;
        }
    }
}
