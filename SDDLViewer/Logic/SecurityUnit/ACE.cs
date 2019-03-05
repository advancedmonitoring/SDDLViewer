using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SDDLViewer
{
    public class ACE
    {
        public const int AllowRule = 0;
        public const int DenyRule = 1;
        public const int OtherRule = 2;

        private readonly string _ace;
        public readonly bool IsOk;

        private readonly string _type;
        private readonly string _righs;
        private readonly string _sid;

        private static readonly List<Tuple<UInt32, string>> RightsValues = new List<Tuple<uint, string>>()
        {
            new Tuple<uint, string>(0x001200A0, "FX"),
            new Tuple<uint, string>(0x00120116, "FW"),
            new Tuple<uint, string>(0x00120089, "FR"),
            new Tuple<uint, string>(0x001F01FF, "FA"),
            new Tuple<uint, string>(0x00120116, "FW"),

            new Tuple<uint, string>(0x00000100, "CR"),
            new Tuple<uint, string>(0x00000080, "LO"),
            new Tuple<uint, string>(0x00000040, "DT"),
            new Tuple<uint, string>(0x00000020, "WP"),
            new Tuple<uint, string>(0x00000010, "RP"),
            new Tuple<uint, string>(0x00000008, "SW"),
            new Tuple<uint, string>(0x00000004, "LC"),
            new Tuple<uint, string>(0x00000002, "DC"),
            new Tuple<uint, string>(0x00000001, "CC"),

            new Tuple<uint, string>(0x00010000, "SD"),
            new Tuple<uint, string>(0x00020000, "RC"),
            new Tuple<uint, string>(0x00040000, "Wd"),
            new Tuple<uint, string>(0x00080000, "WO"),
        };

        public ACE(string ace)
        {
            _ace = ace;
            try
            {
                var lst = _ace.Substring(1, _ace.Length - 2).Split(';');
                if (lst.Length != 6)
                    throw new Exception();
                _type = lst[0];
                _righs = lst[2];
                if (_righs.StartsWith("0x"))
                {
                    var r = "";
                    UInt32 v = UInt32.Parse(_righs.Substring(2), NumberStyles.AllowHexSpecifier);
                    UInt32 v2 = 0;
                    foreach (var value in RightsValues)
                    {
                        if ((v & value.Item1) == value.Item1)
                        {
                            r += value.Item2;
                            v2 |= value.Item1;
                        }
                    }
                    if (v2 != v)
                        r += "??";
                    _righs = r;
                }
                _sid = lst[5];
                IsOk = true;
            }
            catch
            {
                IsOk = false;
            }
        }

        public string GetSID()
        {
            return _sid;
        }

        public List<string> GetAllRights()
        {
            var rv = new List<string>();
            for (var i = 0; i < _righs.Length / 2; i++)
                rv.Add(_righs.Substring(2 * i, 2));
            return rv;
        }

        public string WithNewRights(List<string> newRights)
        {
            var sb = new StringBuilder();
            sb.Append('(');
            var lst = _ace.Substring(1, _ace.Length - 2).Split(';');
            for (int i = 0; i < lst.Length; i++)
            {
                if (i != 0)
                    sb.Append(';');
                if (i == 2)
                    foreach (var r in newRights)
                        sb.Append(r);
                else
                    sb.Append(lst[i]);
            }
            sb.Append(')');
            return sb.ToString();
        }

        public static string RigthToLong(string right, int rightsType = -1)
        {
            switch (right)
            {
                case "GA": return "[GA] Generic All";
                case "GX": return "[GX] Generic Execute/Traverse";
                case "GW": return "[GW] Generic Write";
                case "GR": return "[GR] Generic Read";
                case "SD": return "[SD] Standard Delete";
                case "RC": return "[RC] Read Control";
                case "WD": return "[WD] Write Discretionary Access Control";
                case "WO": return "[WO] Write Owner";
            }
            switch (rightsType)
            {
                case 0:
                    switch (right)
                    {
                        case "CC": return "[CC] Query Configuration";
                        case "DC": return "[DC] Change Configuration";
                        case "LC": return "[LC] Query Status";
                        case "DW": return "[SW] Enumerate Dependencies";
                        case "RP": return "[RP] Start";
                        case "WP": return "[WP] Stop";
                        case "DT": return "[DT] Pause";
                        case "LO": return "[LO] Interrogate";
                        case "CR": return "[CR] User Defined";
                    }
                    break;

                case 1:
                    switch (right)
                    {
                        case "CC": return "[CC] List Directory";
                        case "DC": return "[DC] Create File";
                        case "LC": return "[LC] Create Subdirectory";
                        case "SW": return "[SW] Read Extended Attributes";
                        case "RP": return "[RP] Write Extended Attributes";
                        case "WP": return "[WP] Traverse Directory";
                        case "DT": return "[DT] Delete Tree";
                        case "LO": return "[LO] Read Attributes";
                        case "CR": return "[CR] Write Attributes";
                    }
                    break;

                case 2:
                    switch (right)
                    {
                        case "CC": return "[CC] Read Data";
                        case "DC": return "[DC] Write Data";
                        case "LC": return "[LC] Append Data";
                        case "SW": return "[SW] Read Extended Attributes";
                        case "RP": return "[RP] Write Extended Attributes";
                        case "WP": return "[WP] Execute File";
                        case "LO": return "[LO] Read Attributes";
                        case "CR": return "[CR] Write Attributes";
                        case "FA": return "[FA] File All";
                        case "FR": return "[FR] File Read";
                        case "FW": return "[FW] File Write";
                        case "FX": return "[FX] File Execute";
                    }
                    break;

                case 3:
                    switch (right)
                    {
                        case "CC": return "[CC] Read Data / List Directory";
                        case "DC": return "[DC] Write Data / Create File";
                        case "LC": return "[LC] Append Data / Create Subdirectory";
                        case "SW": return "[SW] Read Extended Attributes";
                        case "RP": return "[RP] Write Extended Attributes";
                        case "WP": return "[WP] Execute File / Traverse Directory";
                        case "DT": return "[DT] Delete Tree";
                        case "LO": return "[LO] Read Attributes";
                        case "CR": return "[CR] Write Attributes";
                        case "FA": return "[FA] File All";
                        case "FR": return "[FR] File Read";
                        case "FW": return "[FW] File Write";
                        case "FX": return "[FX] File Execute";
                    }
                    break;

                case 4:
                    switch (right)
                    {
                        case "CC": return "[CC] Query Value";
                        case "DC": return "[DC] Set Value";
                        case "LC": return "[LC] Create Subkey";
                        case "SW": return "[SW] Enumerate Subkeys";
                        case "RP": return "[RP] Notify";
                        case "WP": return "[WP] Create Link";
                        case "KA": return "[KA] Key All";
                        case "KR": return "[KR] Key Read";
                        case "KW": return "[KW] Key Write";
                        case "KX": return "[KX] Key Execute";
                    }
                    break;
            }
            return $"[{right}] ??";
        }

        public static string RightType(int rightsType)
        {
            switch (rightsType)
            {
                case 0:
                    return "Service Access Rights";
                case 1:
                    return "Directory Access Rights";
                case 2:
                    return "File Access Rights";
                case 3:
                    return "File and Directory Access Rights";
                case 4:
                    return "Registry Key Access Rights";
                default:
                    return "??";
            }
        }

        public int GetRuleType()
        {
            switch (_type)
            {
                case "A":
                    return ACE.AllowRule;
                case "D":
                    return ACE.DenyRule;
                default:
                    return ACE.OtherRule;
            }
        }
    }
}