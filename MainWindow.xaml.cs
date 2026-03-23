using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace tododeploy
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Todo Studio";
            AppWindow.Resize(new SizeInt32(1460, 940));
        }
    }
}
