using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SDDLViewer.UIFolderRegPicker
{
    /// <summary>
    /// Interaction logic for FolderRegPicker.xaml
    /// </summary>
    public partial class UIFolderRegPickerWindow
    {
        private readonly bool _isFolderPicker;

        public UIFolderRegPickerWindow(Window parent, bool isFolderPicker)
        {
            Owner = parent;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            IsRecursing = true;
            DataContext = this;
            InitializeComponent();
            _isFolderPicker = isFolderPicker;
            if (_isFolderPicker)
            {
                ChkIncludeFiles.Visibility = Visibility.Visible;
                var allDrives = DriveInfo.GetDrives();
                foreach (var d in allDrives)
                {
                    var item = new TreeViewItem
                    {
                        Header = d.Name,
                        Tag = d.Name,
                    };
                    try
                    {
                        if (Directory.GetDirectories(d.Name).Length != 0)
                            item.Items.Add(new TreeViewItem {Header = ""});
                    }
                    catch
                    {
                        // ignored
                    }
                    item.Expanded += folder_OnExpanded;
                    item.Selected += element_OnSelected;
                    TreViewer.Items.Add(item);
                }
            }
            else
            {
                var allKeys = new List<string> { "HKEY_CLASSES_ROOT", "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE", "HKEY_USERS", "HKEY_CURRENT_CONFIG" };
                foreach (var k in allKeys)
                {
                    var item = new TreeViewItem
                    {
                        Header = k,
                        Tag = k,
                    };
                    item.Items.Add(new TreeViewItem { Header = "" });
                    item.Expanded += keyreg_OnExpanded;
                    item.Selected += element_OnSelected;
                    TreViewer.Items.Add(item);
                }
            }
        }

        private void element_OnSelected(object sender, RoutedEventArgs e)
        {
            EdtPath.Text = ((TreeViewItem)sender).Tag.ToString().Trim();
            e.Handled = true;
        }

        private void folder_OnExpanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem) sender;
            try
            {
                if (item.Items.Count == 1 && (item.Items[0] is TreeViewItem) && ( string.IsNullOrWhiteSpace((string)((TreeViewItem)item.Items[0]).Header)))
                {
                    EdtPath.Text = item.Tag.ToString().Trim();
                    item.Items.Clear();
                    try
                    {
                        foreach (var s in Directory.GetDirectories(item.Tag.ToString()))
                        {
                            var subitem = new TreeViewItem
                            {
                                Header = s.Substring(s.LastIndexOf("\\") + 1),
                                Tag = s
                            };
                            try
                            {
                                if (Directory.GetDirectories(s).Length != 0)
                                    subitem.Items.Add(new TreeViewItem {Header = ""});
                            }
                            catch { /* ignore */ }
                            subitem.Expanded += folder_OnExpanded;
                            subitem.Selected += element_OnSelected;
                            item.Items.Add(subitem);
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }

        private void keyreg_OnExpanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            try
            {
                if (item.Items.Count == 1 && (item.Items[0] is TreeViewItem) && (string.IsNullOrWhiteSpace((string)((TreeViewItem)item.Items[0]).Header)))
                {
                    EdtPath.Text = item.Tag.ToString().Trim();
                    item.Items.Clear();
                    try
                    {
                        RegistryKey rk = GetKeyFromString(item.Tag.ToString());
                        foreach (string s in rk.GetSubKeyNames())
                        {
                            TreeViewItem subitem = new TreeViewItem
                            {
                                Header = s,
                                Tag = item.Tag + "\\" + s
                            };
                            try
                            {
                                var oSubKey = rk.OpenSubKey(s);
                                if (oSubKey != null && oSubKey.SubKeyCount != 0)
                                    subitem.Items.Add(new TreeViewItem { Header = "" });
                            }
                            catch { /* ignore */ }
                            subitem.Expanded += keyreg_OnExpanded;
                            subitem.Selected += element_OnSelected;
                            item.Items.Add(subitem);
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }

        public static RegistryKey GetKeyFromString(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            path = path.Trim();
            if (path[path.Length - 1] == '\\')
                path = path.Substring(0, path.Length - 1);
            var index = path.IndexOf('\\');
            var start = (index == -1) ? path : path.Substring(0, index);
            RegistryKey rv;
            switch (start.ToUpperInvariant())
            {
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    rv = Registry.ClassesRoot;
                    break;

                case "HKEY_CURRENT_USER":
                case "HKCU":
                    rv = Registry.CurrentUser;
                    break;

                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    rv = Registry.LocalMachine;
                    break;

                case "HKEY_USERS":
                case "HKU":
                    rv = Registry.Users;
                    break;

                case "HKEY_CURRENT_CONFIG":
                    rv = Registry.CurrentConfig;
                    break;

                default:
                    return null;
            }
            if (index == -1)
            {
                return rv;
            }
            try
            {
                path = path.Substring(index + 1);
                return rv.OpenSubKey(path);
            }
            catch
            {
                return null;
            }
        }

        public string PathName { get; set; }
        public bool IsRecursing { get; set; }
        public bool IsFilesInclude { get; set; }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            var p = EdtPath.Text.Trim();
            if (_isFolderPicker)
            {
                if (Directory.Exists(p))
                {
                    PathName = p;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Unable to open directory (not found or access denied).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (GetKeyFromString(p) != null)
                {
                    PathName = p;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Unable to open registry key (not found or access denied).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
