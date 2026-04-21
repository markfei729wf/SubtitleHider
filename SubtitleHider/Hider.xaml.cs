using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace SubtitleHider
{
    public partial class Hider : Window
    {
        private MainWindow? _settingsWindow;
        private bool _isClosing;
        private const int EdgeSize = 6;
        private DispatcherTimer? _topmostTimer;

        // Win32 constants for window resizing
        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_SIZEHTLEFT = 0xF001;
        private const int SC_SIZEHTRIGHT = 0xF002;
        private const int SC_SIZEHTTOP = 0xF003;
        private const int SC_SIZEHTTOPLEFT = 0xF004;
        private const int SC_SIZEHTTOPRIGHT = 0xF005;
        private const int SC_SIZEHTBOTTOM = 0xF006;
        private const int SC_SIZEHTBOTTOMLEFT = 0xF007;
        private const int SC_SIZEHTBOTTOMRIGHT = 0xF008;

        // Win32 constants for SetWindowPos
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public Hider()
        {
            InitializeComponent();
            LoadSettings();
            SetupTopmostTimer();
        }

        private void SetupTopmostTimer()
        {
            _topmostTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _topmostTimer.Tick += (s, e) =>
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
            };
            _topmostTimer.Start();
        }

        private void LoadSettings()
        {
            var settings = WindowSettings.Load();
            this.Left = settings.Left;
            this.Top = settings.Top;
            this.Width = settings.Width;
            this.Height = settings.Height;
            this.Opacity = settings.Opacity;
            this.Topmost = true;
        }

        public void SetOpacity(double opacityValue)
        {
            this.Opacity = opacityValue;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) return;

            var pos = e.GetPosition(this);
            var cursor = GetCursorForPosition(pos);
            this.Cursor = cursor;
        }

        private Cursor GetCursorForPosition(Point pos)
        {
            var width = this.ActualWidth;
            var height = this.ActualHeight;

            bool onLeft = pos.X <= EdgeSize;
            bool onRight = pos.X >= width - EdgeSize;
            bool onTop = pos.Y <= EdgeSize;
            bool onBottom = pos.Y >= height - EdgeSize;

            if ((onLeft && onTop) || (onRight && onBottom))
                return Cursors.SizeNWSE;
            if ((onRight && onTop) || (onLeft && onBottom))
                return Cursors.SizeNESW;
            if (onLeft || onRight)
                return Cursors.SizeWE;
            if (onTop || onBottom)
                return Cursors.SizeNS;

            return Cursors.Arrow;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            var width = this.ActualWidth;
            var height = this.ActualHeight;

            bool onLeft = pos.X <= EdgeSize;
            bool onRight = pos.X >= width - EdgeSize;
            bool onTop = pos.Y <= EdgeSize;
            bool onBottom = pos.Y >= height - EdgeSize;

            int direction = 0;
            if (onRight && onBottom) direction = SC_SIZEHTBOTTOMRIGHT;
            else if (onLeft && onTop) direction = SC_SIZEHTTOPLEFT;
            else if (onRight && onTop) direction = SC_SIZEHTTOPRIGHT;
            else if (onLeft && onBottom) direction = SC_SIZEHTBOTTOMLEFT;
            else if (onRight) direction = SC_SIZEHTRIGHT;
            else if (onLeft) direction = SC_SIZEHTLEFT;
            else if (onBottom) direction = SC_SIZEHTBOTTOM;
            else if (onTop) direction = SC_SIZEHTTOP;

            if (direction != 0)
            {
                var helper = new WindowInteropHelper(this);
                ReleaseCapture();
                SendMessage(helper.Handle, WM_SYSCOMMAND, direction, 0);
                e.Handled = true;
            }
            else
            {
                this.DragMove();
            }
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                ShowContextMenuWindow();
            }
        }

        private void ShowContextMenuWindow()
        {
            var menuWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = new SolidColorBrush(Color.FromArgb(0xF0, 0x30, 0x30, 0x30)),
                Topmost = true,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Width = 120,
                Height = 90,
                Owner = this
            };

            // Position below Hider window (as user suggested)
            menuWindow.Left = this.Left + this.Width / 2 - menuWindow.Width / 2;
            menuWindow.Top = this.Top + this.Height;

            // If goes beyond screen bottom, show above Hider instead
            var screen = SystemParameters.WorkArea;
            if (menuWindow.Top + menuWindow.Height > screen.Height)
            {
                menuWindow.Top = this.Top - menuWindow.Height;
            }
            if (menuWindow.Left < screen.Left)
            {
                menuWindow.Left = screen.Left;
            }
            else if (menuWindow.Left + menuWindow.Width > screen.Width)
            {
                menuWindow.Left = screen.Width - menuWindow.Width;
            }

            var panel = new StackPanel { Margin = new Thickness(8) };
            bool menuClosedByButton = false;

            var setOpacityBtn = new Button
            {
                Content = "设置透明度",
                Margin = new Thickness(0, 0, 0, 8),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                Padding = new Thickness(5)
            };
            setOpacityBtn.Click += (s, args) =>
            {
                menuClosedByButton = true;
                menuWindow.Close();
                ShowSettingsWindow();
            };

            var closeBtn = new Button
            {
                Content = "关闭",
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                Padding = new Thickness(5)
            };
            closeBtn.Click += (s, args) =>
            {
                menuClosedByButton = true;
                _isClosing = true;
                menuWindow.Close();
                this.Close();
            };

            panel.Children.Add(setOpacityBtn);
            panel.Children.Add(closeBtn);

            menuWindow.Content = new Border
            {
                Child = panel,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };

            menuWindow.Deactivated += (s, args) =>
            {
                if (!menuClosedByButton)
                    menuWindow.Close();
            };
            menuWindow.Show();
        }

        private void ShowSettingsWindow()
        {
            if (_settingsWindow == null || !_settingsWindow.IsVisible)
            {
                _settingsWindow = new MainWindow(this);
                PositionSettingsWindow(_settingsWindow);
                _settingsWindow.Show();
                // Force settings window to topmost using Win32 API
                var helper = new WindowInteropHelper(_settingsWindow);
                if (helper.Handle != IntPtr.Zero)
                {
                    SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
            }
            else
            {
                _settingsWindow.Activate();
            }
        }

        private void PositionSettingsWindow(Window settingsWindow)
        {
            var screen = SystemParameters.WorkArea;
            var hiderLeft = this.Left;
            var hiderTop = this.Top;
            var hiderWidth = this.Width;
            var hiderHeight = this.Height;

            double targetTop = hiderTop - settingsWindow.Height;
            double targetLeft = hiderLeft + (hiderWidth - settingsWindow.Width) / 2;

            if (targetTop < screen.Top)
            {
                targetTop = hiderTop + hiderHeight;
            }

            if (targetLeft < screen.Left)
            {
                targetLeft = screen.Left + 10;
            }
            else if (targetLeft + settingsWindow.Width > screen.Width)
            {
                targetLeft = screen.Width - settingsWindow.Width - 10;
            }

            settingsWindow.Left = targetLeft;
            settingsWindow.Top = targetTop;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _topmostTimer?.Stop();
            if (_isClosing)
            {
                WindowSettings.Save(this.Left, this.Top, this.Width, this.Height, this.Opacity);
            }
        }

        private void Window_LocationChanged(object sender, System.EventArgs e)
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                PositionSettingsWindow(_settingsWindow);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                PositionSettingsWindow(_settingsWindow);
            }
        }

        private void Window_MouseEnter(object sender, System.EventArgs e)
        {
            WindowBorder.BorderBrush = Brushes.Yellow;
        }

        private void Window_MouseLeave(object sender, System.EventArgs e)
        {
            WindowBorder.BorderBrush = Brushes.Transparent;
        }
    }
}
