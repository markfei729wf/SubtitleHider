using System.Windows;
using System.Windows.Media;

namespace SubtitleHider
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var screen = SystemParameters.WorkArea;
            var hider = new Hider
            {
                Topmost = true,
                Background = Brushes.Black
            };
            hider.Show();
        }
    }
}
