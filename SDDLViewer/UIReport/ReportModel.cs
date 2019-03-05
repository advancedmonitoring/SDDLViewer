using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SDDLViewer.UIReport
{
    static class ReportModel
    {
        public static void ButtonSave(string content)
        {
            var dlg = new SaveFileDialog
            {
                FileName = "report",
                DefaultExt = ".txt",
                Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 0,
                OverwritePrompt = true
            };
            if (dlg.ShowDialog() ?? false)
                File.WriteAllText(dlg.FileName, content);
        }

        public static string MakeReport(string sddl, List<string> SIDs, List<string> rights, bool isIncludeAllow, bool isIncludeDeny, bool translateSID)
        {
            var lines = sddl.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var prev = "";
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var sd = new SecurityDescriptor(line.Trim());
                    if (sd.IsOk)
                    {
                        var r = GetLineForReport(sd, SIDs, rights, isIncludeAllow, isIncludeDeny, translateSID);
                        if (!string.IsNullOrWhiteSpace(r))
                        {
                            if (!string.IsNullOrWhiteSpace(prev))
                                sb.AppendLine(prev);
                            sb.AppendLine(r);
                            sb.AppendLine();
                        }
                        prev = "";
                        continue;
                    }
                    prev = line;
                }
            }

            return sb.ToString();
        }

        private static string GetLineForReport(SecurityDescriptor sd, List<string> SIDs, List<string> rights, bool isIncludeAllow, bool isIncludeDeny, bool translateSID)
        {
            var rv = "";
            var owner = sd.GetOwner();
            if (SIDs.Contains(owner))
                rv += "O:" + owner;
            var group = sd.GetGroup();
            if (SIDs.Contains(group))
                rv += "G:" + group;
            var sb = new StringBuilder();
            foreach (var ace in sd.GetACEs())
            {
                if (!SIDs.Contains(ace.GetSID()))
                    continue;
                var type = ace.GetRuleType();
                if (!isIncludeAllow && (type == ACE.AllowRule)) 
                    continue;
                if (!isIncludeDeny && (type == ACE.DenyRule))
                    continue;
                var aceRights = ace.GetAllRights();
                var matchRights = aceRights.Where(rights.Contains).ToList();
                if (matchRights.Count != 0)
                    sb.Append(ace.WithNewRights(matchRights));
            }
            var aces = sb.ToString();
            if (!string.IsNullOrWhiteSpace(aces))
                rv += "D:" + aces;
            return rv;
        }

        public static string GetHelperText(string line, int rightsType, out Tuple<string, string, TreeViewItem[]> details, bool translateSID)
        {
            var sd = new SecurityDescriptor(line);
            if (sd.IsOk)
            {
                var lSIDs = sd.GetAllSIDs();
                var lRights = sd.GetAllRights();
                var sb = new StringBuilder();
                if (lSIDs.Count != 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("SIDs:");
                    sb.AppendLine("-----");
                    foreach (var lSID in lSIDs)
                        sb.AppendLine(SecurityDescriptor.SIDToLong(lSID, translateSID));
                    sb.AppendLine("-----");
                }
                if (lRights.Count != 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Rights (" + ACE.RightType(rightsType)+"):");
                    sb.AppendLine("-----");
                    foreach (var lRight in lRights)
                        sb.AppendLine(ACE.RigthToLong(lRight, rightsType));
                    sb.AppendLine("-----");
                }
                var aces = sd.GetACEs();
                var treeElements = new TreeViewItem[aces.Count];
                for (var i = 0; i < aces.Count; i++)
                    treeElements[i] = ACEToTreeViewItem(aces[i], rightsType, translateSID);
                details = new Tuple<string, string, TreeViewItem[]>(sd.GetOwner(), sd.GetGroup(), treeElements);
                return sb.ToString();
            }
            details = new Tuple<string, string, TreeViewItem[]>("", "", new TreeViewItem[0]);
            return string.Empty;
        }

        private static TreeViewItem ACEToTreeViewItem(ACE ace, int rightsType, bool translateSID)
        {
            var rv = new TreeViewItem {Header = SecurityDescriptor.SIDToLong(ace.GetSID(), translateSID) };
            rv.Items.Clear();
            foreach (var right in ace.GetAllRights())
                rv.Items.Add(ACE.RigthToLong(right, rightsType));
            return rv;
        }
    }
}
