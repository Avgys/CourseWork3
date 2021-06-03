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

namespace CourseWork
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
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

        }
    }
}
