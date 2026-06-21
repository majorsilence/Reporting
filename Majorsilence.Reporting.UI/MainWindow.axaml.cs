using System.Threading.Tasks;
using Avalonia.Controls;

namespace Majorsilence.Reporting.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Task OpenFileAsync(string path)
        {
            return ReportViewer.SetSourceFileAsync(new System.Uri(path));
        }
    }
}