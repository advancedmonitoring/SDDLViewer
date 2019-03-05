using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SDDLViewer.Logic;
using SDDLViewer.UIPerformWork;

namespace SDDLViewer
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<BoolStringClass> RightsList { get; set; }
        public ObservableCollection<BoolStringClass> SIDList { get; set; }

        public bool IsTranslateSID { get; set; }
        public bool IsIncludeAllow { get; set; }
        public bool IsIncludeDeny { get; set; }
        
        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Model.MW = this;
            SIDList = new ObservableCollection<BoolStringClass>();
            RightsList = new ObservableCollection<BoolStringClass>();
            IsTranslateSID = false;
            IsIncludeAllow = true;
            IsIncludeDeny = true;
            DataContext = this;
            InitializeComponent();
        }

        private void CmbxRightsType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Model.SelectionChangedRightsCombobox();
        }

        private void EdtContent_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Model.TextChangedContentEdit();
        }

        private void btnFillServices_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonFillServicesClicked();
        }

        private void btnFillDirectory_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonFillDirectoryClicked();
        }

        private void btnFillFiles_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonFillFilesClicked();
        }
        
        private void btnFillRegistry_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonFillRegistryClicked();
        }

        private void btnSave_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonSaveClicked();
        }

        private void btnCASIDS_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonAll(0, true);
        }

        private void btnUCASIDS_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonAll(0, false);
        }

        private void btnCARights_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonAll(1, true);
        }

        private void btnUCARights_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonAll(1, false);
        }

        private void btnMakeReport_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonMakeReport();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Model.OnClose();
        }

        private void btnOpen_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonOpenClicked();
        }

        private void chkTranslateSIDs_OnClick(object sender, RoutedEventArgs e)
        {
            Model.ButtonTranslateClicked();
        }

        public const int CONTENT_CHUNK_SIZE = 50000;

        public void SetContent(string content = null)
        {
            var setContent = content ?? string.Empty;
            if (setContent.Length < CONTENT_CHUNK_SIZE)
            {
                EdtContent.Text = setContent;
                return;
            }
            EdtContent.TextChanged -= EdtContent_OnTextChanged;
            var abortEvent = new ManualResetEvent(false);
            var dlg = new UIPerformWorkWindow(this, abortEvent, "Displaying content", "Displaying content: {0,3}%");
            Thread work = new Thread(ContentUpdate);
            work.Start(new Tuple<string, UIPerformWorkWindow>(setContent, dlg));
            dlg.ShowDialog();
            work.Join();
        }

        private void ContentUpdate(object obj)
        {
            var t = (Tuple<string, UIPerformWorkWindow>) obj;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => { EdtContent.Text = ""; }));
            var content = t.Item1.Substring(0, t.Item1.Length - 1);
            var last = t.Item1.Substring(content.Length);
            var all = content.Length + 1;
            var setted = 0;
            while (content.Length > CONTENT_CHUNK_SIZE)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<string>((x) => { EdtContent.AppendText(x); }), content.Substring(0, CONTENT_CHUNK_SIZE));
                content = content.Substring(CONTENT_CHUNK_SIZE);
                setted += CONTENT_CHUNK_SIZE;
                t.Item2.Percentage = (int) ((setted + 0.0) * 100 / (all + 0.0));
                if (t.Item2.AbortEvent.WaitOne(0))
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => { EdtContent.TextChanged += EdtContent_OnTextChanged; }));
                    Thread.CurrentThread.Abort();
                    return;
                }
            }
            if (content.Length > 0)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<string>((x) => { EdtContent.AppendText(x); }), content);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<string>((x) => { EdtContent.TextChanged += EdtContent_OnTextChanged; EdtContent.AppendText(x); }), last);

            var wait = new ManualResetEvent(false);
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action<ManualResetEvent>((x) => { x.Set(); }), wait);
            wait.WaitOne();
            t.Item2.AbortEvent.Set();
            Thread.CurrentThread.Abort();
        }

        public string GetContent()
        {
            return EdtContent.Text;
        }
    }
}
