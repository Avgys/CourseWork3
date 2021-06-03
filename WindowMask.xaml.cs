using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.Runtime.InteropServices;

using System.Drawing;
using System.Diagnostics;

namespace CourseWork
{
    /// <summary>
    /// Логика взаимодействия для WindowMask.xaml
    /// </summary>
    public partial class WindowMask : Window
    {
        public WindowMask()
        {
            InitializeComponent();
        }

        public void SetCurrent()
        {

        }

        public void UnsetCurrent()
        {

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {

            //if (e.Key == Key.F9 && Keyboard.Modifiers == ModifierKeys.Shift)
            //{
            //    MessageBox.Show("Wake up");
            //}

            //if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Alt)
            //{
            //    MessageBox.Show("Wake up");
            //}

        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
