using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace CourseWork
{
    using CourseWork.Parts;
    using Hardcodet.Wpf.TaskbarNotification;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        Settings _SettingWindow;
        WindowMask _WindowMask;
        public Manipulator manipulator;

        public MainMenu()
        {
            InitializeComponent();

            manipulator = new Manipulator();
            manipulator._eventControler._changeScreen += ChangeWindowToMask;

            _SettingWindow = new Settings();

            _WindowMask = new WindowMask();

            TaskbarIcon tbi = new TaskbarIcon();

            Icon myIcon = new Icon("./cursor.ico");
            tbi.Icon = myIcon;
            tbi.ToolTipText = "RMouse";

            tbi.TrayMouseDoubleClick += iconTrayClick;

            this.Activated += HideOtherWindows;            
            
            this.Show();
            if (_SettingWindow.Owner == null)
                _SettingWindow.Owner = this;
            if (_WindowMask.Owner == null)
                _WindowMask.Owner = this;
            this.ShowInTaskbar = false;
            foreach (Window window in this.OwnedWindows)
                window.ShowInTaskbar = false;         
        }
        void HideOtherWindows(object sender, EventArgs e)
        {
            foreach (Window window in this.OwnedWindows)
                window.Hide();
        }

        void iconTrayClick(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Show();
        }

        private void ResetClients()
        {
            LeftClient.Content = "ClientLeft:" + (manipulator.ConnectedRemoteClientsAddress.Count > 0 ? manipulator.ConnectedRemoteClientsAddress[0].RemoteClient.IPEndPoint : " null");
            UpClient.Content = "ClientUp:" +  (manipulator.ConnectedRemoteClientsAddress.Count > 1 ? manipulator.ConnectedRemoteClientsAddress[1].RemoteClient.IPEndPoint : " null");
            RightClient.Content = "ClientRight:" + (manipulator.ConnectedRemoteClientsAddress.Count > 2 ? manipulator.ConnectedRemoteClientsAddress[2].RemoteClient.IPEndPoint : " null");
            DownClient.Content = "ClientDown:" + (manipulator.ConnectedRemoteClientsAddress.Count > 3 ? manipulator.ConnectedRemoteClientsAddress[3].RemoteClient.IPEndPoint : " null");
        }

        private void ChangeWindowToMask(ScreenEdges flag)
        {
            if (flag != ScreenEdges.NONE)
            {
                Dispatcher.Invoke(() =>
                {
                    HideOtherWindows(null, null);
                    _WindowMask.Show();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    _WindowMask.Hide();
                    HideOtherWindows(null, null);                    
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            _SettingWindow.Top = this.Top;
            _SettingWindow.Left = this.Left;
            _SettingWindow.Width = this.Width;
            _SettingWindow.Height = this.Height;
            _SettingWindow.Reset();
            _SettingWindow.Show();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void Window_Activated(object sender, EventArgs e)
        {
            HideOtherWindows(null,null);
            this.Show();
            ResetClients();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            manipulator.Close();
            foreach (Window window in this.OwnedWindows)
            {
                if (window.IsEnabled)
                    window.Close();               
            }
            System.Windows.Application.Current.Shutdown();
        }
    }
}
