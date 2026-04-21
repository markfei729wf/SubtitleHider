using System;
using System.Windows;

namespace SubtitleHider
{
    public partial class MainWindow : Window
    {
        private readonly Hider _hider;

        public MainWindow(Hider hider)
        {
            _hider = hider;
            InitializeComponent();
        }

        private void ButtonSetOpacity_Click(object sender, RoutedEventArgs e)
        {
            var numberString = TextBoxOpacityValue.Text;
            var opacity = Convert.ToDouble(numberString);
            _hider.SetOpacity(opacity);
        }

        private void ButtonHideHider_Click(object sender, RoutedEventArgs e)
        {
            _hider.SetOpacity(0);
        }

        private void ButtonShowHider_Click(object sender, RoutedEventArgs e)
        {
            _hider.SetOpacity(1);
        }
    }
}
