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
                ConnectedAddress.Items.Clear();
                List<ConnectionInfo> ConnectedClients = Manipulator._currManipulator.ConnectedRemoteClientsAddress.ToList();
                for (int i = 0; i < Manipulator._currManipulator._options.remoteClientsAddress.Count; i++)
                {
                    ClientsAddresses.Items.Add(Manipulator._currManipulator._options.remoteClientsAddress[i].ToString());                       
                }
                foreach (var e in ConnectedClients)
                {
                    ConnectedAddress.Items.Add((e.RemoteClient.IPEndPoint != null ? e.RemoteClient.IPEndPoint.ToString() : "null") + " " 
                        + (e.Sound != null ? e.Sound.Port.ToString() : "null") 
                        +" " + (e.Event != null ? e.Event.Port.ToString() : "null"));
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

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator.SaveSettings();
        }

        private void ClientsAddresses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count >= 1)
            {
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

        private void SetPlayDevice_Click(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.SetDefaultoutputSound(PlayDevice.SelectedItem.ToString());
            Manipulator._currManipulator._sound.Deactivate(NAudio.CoreAudioApi.DataFlow.Render);
            Manipulator._currManipulator._sound.Activate(NAudio.CoreAudioApi.DataFlow.Render);
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
            Manipulator._currManipulator._sound.Activate(NAudio.CoreAudioApi.DataFlow.Render);

        }

        private void ReceiveSound_Unchecked(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.isReceivingSound = false;
            Manipulator._currManipulator._sound.Deactivate(NAudio.CoreAudioApi.DataFlow.Render);
        }

        private void SendSound_Unchecked(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.isSendingSound = false;
            Manipulator._currManipulator._sound.Deactivate(NAudio.CoreAudioApi.DataFlow.Capture);
        }

        private void SendSound_Checked(object sender, RoutedEventArgs e)
        {
            Manipulator._currManipulator._options.isSendingSound = true;
            Manipulator._currManipulator._sound.Activate(NAudio.CoreAudioApi.DataFlow.Capture);
        }
    }
}
