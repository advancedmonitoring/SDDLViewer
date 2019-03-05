using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SDDLViewer.UIPerformWork
{
    /// <summary>
    /// Interaction logic for UIPerformWorkWindow.xaml
    /// </summary>
    public partial class UIPerformWorkWindow
    {
        private readonly Line[] _lines = new Line[8];
        private int _lIndex;

        private readonly Brush _brush1 = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private readonly Brush _brush2 = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        private readonly Brush _brush3 = new SolidColorBrush(Color.FromRgb(160, 160, 160));

        public readonly ManualResetEvent AbortEvent;

        public UIPerformWorkWindow(Window parent, ManualResetEvent ev, string textPlaceholder, string percentFormat = "Complete: {0,3}%")
        {
            Owner = parent;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _percentFormat = percentFormat;
            AbortEvent = ev;
            Percentage = 0;
            InitializeComponent();
            _lines[0] = L1;
            _lines[1] = L2;
            _lines[2] = L3;
            _lines[3] = L4;
            _lines[4] = L5;
            _lines[5] = L6;
            _lines[6] = L7;
            _lines[7] = L8;
            lblPercentage.Text = textPlaceholder;
            foreach (var line in _lines)
                line.Stroke = _brush3;
            var timer = new DispatcherTimer();
            timer.Tick += timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Start();
        }

        private int _percentage;
        public int Percentage { private get; set; }

        private readonly string _percentFormat;

        private void timer_Tick(object sender, EventArgs e)
        {
            _lIndex = (_lIndex + 1) % 8;
            _lines[_lIndex].Stroke = _brush1;
            _lines[(_lIndex + 7) % 8].Stroke = _brush2;
            _lines[(_lIndex + 6) % 8].Stroke = _brush3;
            if (_percentage != Percentage)
            {
                lblPercentage.Text = string.Format(_percentFormat, Percentage);
                _percentage = Percentage;
            }
            if (AbortEvent.WaitOne(0))
                Close();
        }

        private void Abort_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_OnClose(object sender, CancelEventArgs e)
        {
            DialogResult = true;
            AbortEvent.Set();
        }
    }
}
