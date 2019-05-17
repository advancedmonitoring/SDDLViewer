using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using SDDLViewer.UIFolderRegPicker;
using SDDLViewer.UIPerformWork;
using SDDLViewer.UIReport;

namespace SDDLViewer.Logic
{
    public static class Model
    {
        public static MainWindow MW { set; private get; }

        public static void SelectionChangedRightsCombobox()
        {
            var selected = ((ComboBoxItem)MW.CmbxRightsType.SelectedItem).Content as string;
            MW.RightsList.Clear();
            foreach (var r in new List<string> {"GA", "GX", "GW", "GR", "SD", "RC", "WD", "WO"})
                MW.RightsList.Add(new BoolStringClass(ACE.RigthToLong(r), r));
            switch (selected)
            {
                case "Service Access Rights":
                    foreach (var r in new List<string> { "CC", "DC", "LC", "DW", "RP", "WP", "DT", "LO", "CR" })
                        MW.RightsList.Add(new BoolStringClass(ACE.RigthToLong(r, 0), r));
                    break;

                case "Directory Access Rights":
                    foreach (var r in new List<string> { "CC", "DC", "LC", "SW", "RP", "WP", "DT", "LO", "CR" })
                        MW.RightsList.Add(new BoolStringClass(ACE.RigthToLong(r, 1), r));
                    break;

                case "File Access Rights":
                    foreach (var r in new List<string> { "CC", "DC", "LC", "SW", "RP", "WP", "LO", "CR", "FA", "FR", "FW", "FX" })
                        MW.RightsList.Add(new BoolStringClass(ACE.RigthToLong(r, 2), r));
                    break;

                case "File and Directory Access Rights":
                    foreach (var r in new List<string> { "CC", "DC", "LC", "SW", "RP", "WP", "DT", "LO", "CR", "FA", "FR", "FW", "FX" })
                        MW.RightsList.Add(new BoolStringClass(ACE.RigthToLong(r, 3), r));
                    break;

                case "Registry Key Access Rights":
                    foreach (var r in new List<string> { "CC", "DC", "LC", "SW", "RP", "WP", "KA", "KR", "KW", "KX" })
                        MW.RightsList.Add(new BoolStringClass(ACE.RigthToLong(r, 4), r));
                    break;
            }
            TextChangedContentEdit();
        }

        private static readonly ManualResetEvent TextChangedEvent = new ManualResetEvent(false);
        private static Thread _textChangedThread;
        private static Thread _textChangedSpinnerThread;

        public static void TextChangedContentEdit()
        {
            if (_textChangedThread != null && _textChangedThread.IsAlive)
            {
                TextChangedEvent.Set();
                _textChangedThread.Join();
            }
            TextChangedEvent.Reset();
            _textChangedThread = new Thread(TextChangedFunc);
            _textChangedThread.Start(MW.GetContent());
            _textChangedSpinnerThread = new Thread(TextChangedSpinnerFunc);
            _textChangedSpinnerThread.Start();
        }

        private static int _textChangedIndex;

        private static void TextChangedSpinnerFunc()
        {
            while (true)
            {
                Thread.Sleep(300);
                var head = "User/Group/SID";
                if (_textChangedThread != null && _textChangedThread.IsAlive)
                {
                    _textChangedIndex = (_textChangedIndex + 1) % 4;
                    head = "[" + "|/-\\"[_textChangedIndex] + "] " + head;
                }
                MW.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action<string>((x) => MW.grpUGS.Header = x), head);
                if (TextChangedEvent.WaitOne(0))
                {
                    MW.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new Action(() => MW.grpUGS.Header = "User/Group/SID"));
                    Thread.CurrentThread.Abort();
                    return;
                }
            }
        }
        
        private static void TextChangedFunc(object o)
        {
            var data = (string)o;
            var lines = data.Split('\r', '\n');
            var allSIDs = new List<string>();
            var allRights = new List<string>();
            var prevSids = 0;
            var prevRights = 0;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var sd = new SecurityDescriptor(line.Trim());
                    if (!sd.IsOk)
                        continue;

                    var lSIDs = sd.GetAllSIDs();
                    foreach (var sid in lSIDs)
                        if (!allSIDs.Contains(sid))
                            allSIDs.Add(sid);

                    var lRights = sd.GetAllRights();
                    foreach (var right in lRights)
                        if (!allRights.Contains(right))
                            allRights.Add(right);

                    if (allSIDs.Count != prevSids)
                    {
                        prevSids = allSIDs.Count;
                        var sortedSIDs = allSIDs.OrderBy(q => q[1] == '-' ? "ZZ" + q : q).ToList();
                        MW.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<List<string>>((x) =>
                        {
                            MW.SIDList.Clear();
                            foreach (var sid in x)
                                MW.SIDList.Add(new BoolStringClass(SecurityDescriptor.SIDToLong(sid, MW.IsTranslateSID), sid));
                        }), sortedSIDs);
                    }

                    if (allRights.Count != prevRights)
                    {
                        prevRights = allRights.Count;
                        MW.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            var newRightsList = new ObservableCollection<BoolStringClass>();
                            foreach (var element in MW.RightsList)
                            {
                                if (allRights.Contains(element.Tag))
                                    element.TextBrush = new SolidColorBrush(Color.FromRgb(50, 150, 255));
                                newRightsList.Add(element);
                            }
                            MW.RightsList.Clear();
                            foreach (var element in newRightsList)
                                MW.RightsList.Add(element);
                        }));
                    }
                }
                if (TextChangedEvent.WaitOne(0))
                {
                    if (_textChangedSpinnerThread != null && _textChangedSpinnerThread.IsAlive)
                        _textChangedSpinnerThread.Join();
                    Thread.CurrentThread.Abort();
                    return;
                }
            }
            
            TextChangedEvent.Set();
            if (_textChangedSpinnerThread != null && _textChangedSpinnerThread.IsAlive)
                _textChangedSpinnerThread.Join();
            Thread.CurrentThread.Abort();
        }

        private static string _fillData;

        public static void ButtonFillServicesClicked()
        {
            var work = new Thread(ServiceFill);
            var abortEvent = new ManualResetEvent(false);
            var dlg = new UIPerformWorkWindow(MW, abortEvent, "Count services...");
            work.Start(dlg);
            if (dlg.ShowDialog() ?? false)
            {
                MW.SetContent();
                if (MW.CmbxRightsType.SelectedIndex != 0)
                    MW.CmbxRightsType.SelectedIndex = 0;
                MW.SetContent(_fillData);
            }
            work.Join();
        }

        private static void ServiceFill(object o)
        {
            var ww = (UIPerformWorkWindow) o;
            var sb = new StringBuilder();
            var services = ServiceController.GetServices();
            var managerHandle = Win32Native.OpenSCManager(null, null, 0x20004);
            for (var i = 0; i < services.Length; i++)
            {
                var service = services[i];
                try
                {
                    var localSb = new StringBuilder();
                    localSb.AppendLine($"{service.DisplayName} ({service.ServiceName})");
                    var serviceHandle = Win32Native.OpenService(managerHandle, service.ServiceName, 0x20000);
                    uint sdSize;
                    var result = Win32Native.QueryServiceObjectSecurity(serviceHandle, 7, null, 0, out sdSize);
                    var gle = Marshal.GetLastWin32Error();
                    if (result || (gle != 122))
                    {
                        Win32Native.CloseServiceHandle(serviceHandle);
                        throw new System.ComponentModel.Win32Exception(gle);
                    }
                    var binarySd = new byte[sdSize];
                    result = Win32Native.QueryServiceObjectSecurity(serviceHandle, 7, binarySd, binarySd.Length, out sdSize);
                    gle = Marshal.GetLastWin32Error();
                    if (!result)
                    {
                        Win32Native.CloseServiceHandle(serviceHandle);
                        throw new System.ComponentModel.Win32Exception(gle);
                    }
                    var cd = new CommonSecurityDescriptor(false, false, binarySd, 0);
                    localSb.AppendLine(cd.GetSddlForm(AccessControlSections.All));
                    sb.AppendLine(localSb.ToString());
                    Win32Native.CloseServiceHandle(serviceHandle);
                    ww.Percentage = (int) ((i + 0.0) * 100.0 / (services.Length + 0.0));
                    if (ww.AbortEvent.WaitOne(0))
                        break;
                }
                catch
                {
                    // continue
                }
            }
            Win32Native.CloseServiceHandle(managerHandle);
            _fillData = sb.ToString();
            ww.AbortEvent.Set();
        }
        
        public static void ButtonFillDirectoryClicked()
        {
            var dlg = new UIFolderRegPickerWindow(MW, true);
            if (dlg.ShowDialog() ?? false)
            {
                var work = new Thread(DirectoryFill);
                var abortEvent = new ManualResetEvent(false);
                var w = new UIPerformWorkWindow(MW, abortEvent, "Count directories...");
                work.Start(new Tuple<UIFolderRegPickerWindow, UIPerformWorkWindow>(dlg, w));
                if (w.ShowDialog() ?? false)
                {
                    int target = 1;
                    if (dlg.IsFilesInclude)
                        target = 3;
                    MW.SetContent();
                    if (MW.CmbxRightsType.SelectedIndex != target)
                        MW.CmbxRightsType.SelectedIndex = target;
                    MW.SetContent(_fillData);
                }
                work.Join();
            }
        }

        private static int _maxFiles;
        private static int _currentFiles;
        private static bool _isFileInclude;
        private static bool _isRecursing;
        private static UIPerformWorkWindow _ww;
        private static StringBuilder _sb;

        private static void DirectoryFill(object o)
        {
            var t = (Tuple<UIFolderRegPickerWindow, UIPerformWorkWindow>) o;
            _sb = new StringBuilder();
            _ww = t.Item2;
            _currentFiles = 0;
            _isFileInclude = t.Item1.IsFilesInclude;
            _isRecursing = t.Item1.IsRecursing;
            _maxFiles = CalcDirCount(t.Item1.PathName) + 1;
            UpdateDirectory(t.Item1.PathName);
            _fillData = _sb.ToString();
            t.Item2.AbortEvent.Set();
        }

        private static int CalcDirCount(string path)
        {
            if (_ww.AbortEvent.WaitOne(0))
                return 0;
            List<string> dirs;
            try
            {
                dirs = DirectorySearcher.MyGetDirectories(path);
            }
            catch
            {
                return 1;
            }
            var c = 1;
            if (_isRecursing)
                foreach (var dir in dirs)
                    c += CalcDirCount(dir);
            if (_isFileInclude)
                c += DirectorySearcher.MyGetFiles(path).Count;
            return c;
        }

        private static void UpdateDirectory(string path)
        {
            var localSb = new StringBuilder();
            localSb.AppendLine(path);
            _currentFiles++;
            var sddlString = "Unable obtain SDDL";
            try
            {
                sddlString = Directory.GetAccessControl(path).GetSecurityDescriptorSddlForm(AccessControlSections.All);
            }
            catch
            {
                // ignore
            }
            localSb.AppendLine(sddlString);
            _sb.AppendLine(localSb.ToString());
            foreach (var dir in DirectorySearcher.MyGetDirectories(path))
            {
                try
                {
                    if (_isRecursing)
                        UpdateDirectory(dir);
                    else
                    {
                        localSb.Clear();
                        localSb.AppendLine(dir);
                        _currentFiles++;
                        localSb.AppendLine(Directory.GetAccessControl(dir).GetSecurityDescriptorSddlForm(AccessControlSections.All));
                        _sb.AppendLine(localSb.ToString());
                    }
                }
                catch
                {
                    // ignore
                }
                _ww.Percentage = (int)((_currentFiles + 0.0) * 100.0 / (_maxFiles + 0.0));
                if (_ww.AbortEvent.WaitOne(0))
                    return;
            }
            if (_isFileInclude)
                foreach (var file in DirectorySearcher.MyGetFiles(path))
                {
                    try
                    {
                        localSb.Clear();
                        localSb.AppendLine(file);
                        _currentFiles++;
                        localSb.AppendLine(File.GetAccessControl(file).GetSecurityDescriptorSddlForm(AccessControlSections.All));
                        _sb.AppendLine(localSb.ToString());
                    }
                    catch
                    {
                        // ignore
                    }
                    _ww.Percentage = (int)((_currentFiles + 0.0) * 100.0 / (_maxFiles + 0.0));
                    if (_ww.AbortEvent.WaitOne(0))
                        return;
                }
        }

        public static void ButtonFillFilesClicked()
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() ?? false)
            {
                MW.SetContent();
                if (MW.CmbxRightsType.SelectedIndex != 2)
                    MW.CmbxRightsType.SelectedIndex = 2;
                var sb = new StringBuilder();
                foreach (var file in dlg.FileNames)
                {
                    try
                    {
                        if ((string.IsNullOrWhiteSpace(file)) || (!File.Exists(file)))
                            continue;
                        var localSb = new StringBuilder();
                        localSb.AppendLine($"{file}");
                        var sddlString = "Unable obtain SDDL";
                        try
                        {
                            sddlString = File.GetAccessControl(file).GetSecurityDescriptorSddlForm(AccessControlSections.All);
                        }
                        catch
                        {
                            // ignore
                        }
                        localSb.AppendLine(sddlString);
                        sb.AppendLine(localSb.ToString());
                    }
                    catch
                    {
                        // continue
                    }
                }
                MW.SetContent(sb.ToString());
            }
        }
        
        public static void ButtonFillRegistryClicked()
        {
            var dlg = new UIFolderRegPickerWindow(MW, false);
            if (dlg.ShowDialog() ?? false)
            {
                var work = new Thread(RegistryFill);
                var abortEvent = new ManualResetEvent(false);
                var w = new UIPerformWorkWindow(MW, abortEvent, "Count reg-keys...");
                work.Start(new Tuple<UIFolderRegPickerWindow, UIPerformWorkWindow>(dlg, w));
                if (w.ShowDialog() ?? false)
                {
                    MW.SetContent();
                    if (MW.CmbxRightsType.SelectedIndex != 4)
                        MW.CmbxRightsType.SelectedIndex = 4;
                    MW.SetContent(_fillData);
                }
                work.Join();
            }
        }

        private static void RegistryFill(object o)
        {
            var t = (Tuple<UIFolderRegPickerWindow, UIPerformWorkWindow>)o;
            _sb = new StringBuilder();
            _ww = t.Item2;
            _currentFiles = 0;
            _isFileInclude = t.Item1.IsFilesInclude;
            _isRecursing = t.Item1.IsRecursing;
            var key = UIFolderRegPickerWindow.GetKeyFromString(t.Item1.PathName);
            _maxFiles = CalcKeyCount(key) + 1;
            UpdateRegKey(key);
            _fillData = _sb.ToString();
            t.Item2.AbortEvent.Set();
        }

        private static int CalcKeyCount(RegistryKey key)
        {
            if (_ww.AbortEvent.WaitOne(0))
                return 0;
            string[] keys;
            try
            {
                keys = key.GetSubKeyNames();
            }
            catch
            {
                return 1;
            }
            var c = 1;
            if (_isRecursing)
                foreach (var k in keys)
                    try
                    {
                        c += CalcKeyCount(key.OpenSubKey(k));
                    }
                    catch
                    {
                        c++;
                    }
            return c;
        }

        private static void UpdateRegKey(RegistryKey key)
        {
            var localSb = new StringBuilder();
            localSb.AppendLine(key.Name);
            _currentFiles++;
            var sddlString = "Unable obtain SDDL";
            try
            {
                sddlString = key.GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.All);
            }
            catch
            {
                // ignore
            }
            localSb.AppendLine(sddlString);
            _sb.AppendLine(localSb.ToString());
            if (key.SubKeyCount != 0)
                foreach (var sub in key.GetSubKeyNames())
                {
                    try
                    {
                        var oKey = key.OpenSubKey(sub);
                        if (oKey != null)
                            if (_isRecursing)
                                UpdateRegKey(oKey);
                            else
                            {
                                localSb.Clear();
                                localSb.AppendLine(oKey.Name);
                                _currentFiles++;
                                localSb.AppendLine(
                                    oKey.GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.All));
                                _sb.AppendLine(localSb.ToString());
                            }
                    }
                    catch
                    {
                        // ignored
                    }
                    _ww.Percentage = (int) ((_currentFiles + 0.0) * 100.0 / (_maxFiles + 0.0));
                    if (_ww.AbortEvent.WaitOne(0))
                        return;
                }
        }

        public static void ButtonSaveClicked()
        {
            var dlg = new SaveFileDialog
            {
                FileName = "raw_sddl",
                DefaultExt = ".txt",
                Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 0,
                OverwritePrompt = true
            };
            if (dlg.ShowDialog() ?? false)
                File.WriteAllText(dlg.FileName, MW.GetContent());
        }

        public static void ButtonAll(int listboxNumber, bool state)
        {
            ListBox lb = null;
            switch (listboxNumber)
            {
                case 0:
                    lb = MW.LstChkSIDs;
                    break;

                case 1:
                    lb = MW.LstChkRights;
                    break;
            }
            if (lb != null)
                foreach (var lbItem in lb.ItemsSource)
                    if (lbItem is BoolStringClass)
                        (lbItem as BoolStringClass).IsSelected = state;
            List<BoolStringClass> tmp;
            switch (listboxNumber)
            {
                case 0:
                    tmp = new List<BoolStringClass>(MW.SIDList);
                    MW.SIDList.Clear();
                    foreach (var element in tmp)
                        MW.SIDList.Add(element);
                    break;

                case 1:
                    tmp = new List<BoolStringClass>(MW.RightsList);
                    MW.RightsList.Clear();
                    foreach (var element in tmp)
                        MW.RightsList.Add(element);
                    break;
            }
        }
        
        public static void ButtonMakeReport()
        {
            var listInterestSIDs = (from sid in MW.SIDList where sid.IsSelected select sid.Tag).ToList();
            var listInterestRights = (from right in MW.RightsList where right.IsSelected select right.Tag).ToList();

            new UIReportWindow(MW, MW.GetContent(), listInterestSIDs, listInterestRights, MW.CmbxRightsType.SelectedIndex).ShowDialog();
        }

        public static void OnClose()
        {
            if (!TextChangedEvent.WaitOne(0))
                TextChangedEvent.Set();
        }

        public static void ButtonOpenClicked()
        {
            var dlg = new OpenFileDialog
            {
                FileName = "raw_sddl",
                DefaultExt = ".txt",
                Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 0,
                CheckFileExists = true,
                CheckPathExists = true
            };
            if (dlg.ShowDialog() ?? false)
            {
                MW.SetContent();
                MW.SetContent(File.ReadAllText(dlg.FileName));
            }
        }

        public static void ButtonTranslateClicked()
        {
            TextChangedContentEdit();
        }
    }
}
