using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SDDLViewer.UIReport
{
    public partial class UIReportWindow
    {
        private readonly int _rightsType;
        private readonly bool _translateSID;

        private readonly string[] _lines;

        public UIReportWindow(MainWindow parent, string sddl, List<string> SIDs, List<string> rights, int rightsType)
        {
            Owner = parent;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _rightsType = rightsType;
            InitializeComponent();
            _translateSID = parent.IsTranslateSID;
            EdtContent.Text = ReportModel.MakeReport(sddl, SIDs, rights, parent.IsIncludeAllow, parent.IsIncludeDeny, parent.IsTranslateSID);
            _lines = EdtContent.Text.Split(new [] {Environment.NewLine}, StringSplitOptions.None);
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            ReportModel.ButtonSave(EdtContent.Text);
        }

        private void EdtContent_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                var lNumber = EdtContent.GetLineIndexFromCharacterIndex(EdtContent.CaretIndex);
                var line = _lines[lNumber];
                var obj = line;
                Tuple<string, string, TreeViewItem[]> detail;
                var r = ReportModel.GetHelperText(line, _rightsType, out detail, _translateSID);
                if (string.IsNullOrWhiteSpace(r))
                {
                    line = _lines[lNumber + 1];
                    r = ReportModel.GetHelperText(line, _rightsType, out detail, _translateSID);
                }
                else
                {
                    obj = (lNumber == 0) ? string.Empty : _lines[lNumber - 1];
                }
                EdtHelper.Text = r;
                lblObject.Text = "Object:\n " + obj;
                lblObject.Visibility = string.IsNullOrWhiteSpace(obj) ? Visibility.Collapsed : Visibility.Visible;
                lblOwner.Text = "Owner:\n " + SecurityDescriptor.SIDToLong(detail.Item1, _translateSID);
                lblOwner.Visibility = string.IsNullOrWhiteSpace(detail.Item1) ? Visibility.Collapsed : Visibility.Visible;
                lblGroup.Text = "Group:\n " + SecurityDescriptor.SIDToLong(detail.Item2, _translateSID);
                lblGroup.Visibility = string.IsNullOrWhiteSpace(detail.Item2) ? Visibility.Collapsed : Visibility.Visible;
                treeACE.Items.Clear();
                foreach (var treeViewItem in detail.Item3)
                    treeACE.Items.Add(treeViewItem);
                treeACE.Visibility = (detail.Item3.Length == 0) ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                EdtHelper.Text = string.Empty;
                lblOwner.Visibility = Visibility.Collapsed;
                lblGroup.Visibility = Visibility.Collapsed;
                treeACE.Visibility = Visibility.Collapsed;
            }
        }
    }
}
