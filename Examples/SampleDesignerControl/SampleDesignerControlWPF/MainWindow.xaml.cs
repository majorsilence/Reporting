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
using Majorsilence.Forms.WinForms;

namespace SampleDesignerControlWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Majorsilence.Reporting.RdlDesign.RdlUserControl reportDesigner = new()
        {
            Dock = Majorsilence.Forms.DockStyle.Fill,
        };

        public MainWindow()
        {
            InitializeComponent();
            windowsFormsHost1.Child = reportDesigner.ToWinFormsControl();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            reportDesigner.OpenFile(@"C:\Users\Peter\Projects\My-FyiReporting\Examples\Examples\FileDirectoryTest.rdl");
        }
    }
}
