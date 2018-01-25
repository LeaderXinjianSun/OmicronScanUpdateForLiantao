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
using BingLibrary.hjb;

namespace OmicronScanUpdateForLiantao.View
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : UserControl
    {
        string ParameterIniPath = @"C:\Parameter.ini";
        public HomePage()
        {
            InitializeComponent();
        }
        private void MsgTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MsgTextBox.ScrollToEnd();
        }

        private void textBox1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            textBox1.IsReadOnly = false;
        }

        private void textBox1_LostFocus(object sender, RoutedEventArgs e)
        {
            textBox1.IsReadOnly = true;
            Inifile.INIWriteValue(ParameterIniPath,"Text", "JiTaiHao", textBox1.Text);
        }
    }
}
