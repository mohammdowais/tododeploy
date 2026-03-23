using Microsoft.UI.Xaml;
using Velopack;

namespace tododeploy
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            VelopackApp.Build().Run();
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
