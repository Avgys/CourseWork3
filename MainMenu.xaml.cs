﻿using System;
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
using CourseWork.Parts;

namespace CourseWork
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        Settings _SettingWindow;
        WindowMask _WindowMask;
        Manipulator control;
        
        public MainMenu()
        {
            InitializeComponent();
            
            control = new Manipulator();

            control.changeScreen += ChangeWindowToMask;

            _SettingWindow = new Settings();
            //_SettingWindow.Owner = this;
            //_SettingWindow.Show();
            _WindowMask = new WindowMask();
            //_WindowMask.Owner = this;
            //_WindowMask.Show();

            //mouse.Clip();
            ////Thread.Sleep(10000);
            //mouse.Unclip();
            
        }

        private void ChangeWindowToMask(ScreenEdges flag)
        {
            Dispatcher.Invoke(() =>
            {
                this.Hide();
                _WindowMask.Show();
            });
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}