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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace CourseWork
{
    using CourseWork.Parts;
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

            //control.changeScreen += ChangeWindowToMask;

            _SettingWindow = new Settings();
            
            _WindowMask = new WindowMask();
           

            //mouse.Clip();
            ////Thread.Sleep(10000);
            //mouse.Unclip();
            //NotifyIcon icon = new NotifyIcon();

            //Dispatcher.Invoke(() =>
            //{
            //    this.Hide();
            //    _WindowMask.Show();
            //});

            this.Show();
            if (_SettingWindow.Owner == null)
                _SettingWindow.Owner = this;
            if (_WindowMask.Owner == null)
                _WindowMask.Owner = this;
            Button_Click(this, null);
        }



        private void ChangeWindowToMask(ScreenEdges flag)
        {
            //Dispatcher.Invoke(() =>
            //{
            //    this.Hide();
            //    _WindowMask.Show();
            //});
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
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            manipulator.Close();
            foreach (Window window in this.OwnedWindows)
            {
                //if (window/*)*/
                    window.Close();
            }
            //System.Windows.MessageBox.Show("Closed");
            //System.Windows.Application.Current.Shutdown();
        }
    }
}
