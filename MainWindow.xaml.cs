using System.Windows;

namespace MonitorConsumer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ConsumerViewModel();
        }
    }
}