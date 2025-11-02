using System.Diagnostics;
using System.Windows;

namespace MiningOps
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // DataContext is already set in App.xaml.cs via DI
            Debug.WriteLine("🏗️ MainWindow initialized with DataContext from DI");
        }
    }
}