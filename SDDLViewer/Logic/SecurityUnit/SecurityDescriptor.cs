using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace SDDLViewer
{
    public class SecurityDescriptor
    {
        private readonly string _sddl;

        private readonly string _owner;
        private readonly string _group;
        private readonly List<ACE> _dacl;
        private readonly List<ACE> _sacl;

        public readonly bool IsOk;
        
        public SecurityDescriptor(string sddl)
        {
            _sddl = sddl.Trim();
            _owner = "";
            _group = "";
            _dacl = new List<ACE>();
            _sacl = new List<ACE>();
            var state = 0; // 1 - dacl, 2-sacl

            try
            {
                var index = 0;
                while (index < _sddl.Length)
                {
                    if (_sddl[index] == 'O')
                    {
                        if (_sddl[index+1] != ':')
                        {
                            IsOk = false;
                            return;
                        }
                        index += 2;
                        _owner = ParseSID(_sddl, ref index);
                        continue;
                    }
                    if (_sddl[index] == 'G')
                    {
                        if (_sddl[index + 1] != ':')
                        {
                            IsOk = false;
                            return;
                        }
                        index += 2;
                        _group = ParseSID(_sddl, ref index);
                        continue;
                    }
                    if (_sddl[index] == 'D')
                    {
                        if (_sddl[index + 1] != ':')
                        {
                            IsOk = false;
                            return;
                        }
                        index += 2;
                        ParseFlags(_sddl, ref index);
                        state = 1;
                        continue;
                    }
                    if (_sddl[index] == 'S')
                    {
                        if (_sddl[index + 1] != ':')
                        {
                            IsOk = false;
                            return;
                        }
                        index += 2;
                        ParseFlags(_sddl, ref index);
                        state = 2;
                        continue;
                    }
                    if (_sddl[index] == '(')
                    {
                        var ace = ParseACE(_sddl, ref index);
                        if (!ace.IsOk)
                        {
                            IsOk = false;
                            return;
                        }
                        switch (state)
                        {
                            case 1:
                                _dacl.Add(ace);
                                break;
                            case 2:
                                _sacl.Add(ace);
                                break;
                            default:
                                IsOk = false;
                                return;
                        }
                        continue;
                    }
                    IsOk = false;
                    return;
                }
                IsOk = true;
            }
            catch
            {
                IsOk = false;
            }
        }

        private static readonly List<string> KnownSIDsList = new List<string>
        {
            "AA", "AC", "AN", "AO", "AU", "BA", "BG", "BO", "BU", "CA",
            "CD", "CG", "CN", "CO", "CY", "DA", "DC", "DD", "DG", "DU",
            "EA", "ED", "ER", "ES", "HA", "HI", "IS", "IU", "LA", "LG",
            "LS", "LU", "LW", "ME", "MP", "MS", "MU", "NO", "NS", "NU",
            "OW", "PA", "PO", "PS", "PU", "RA", "RC", "RD", "RE", "RM",
            "RO", "RS", "RU", "SA", "SI", "SO", "SU", "SY", "UD", "WD",
            "WR"
        };

        private string ParseSID(string s, ref int index)
        {
            var tmp = s.Substring(index, 2);
            if (KnownSIDsList.Contains(tmp))
            {
                index += 2;
                return tmp;
            }
            if (tmp != "S-")
                throw new Exception();
            var start = index;
            index += 2;
            while (index < s.Length)
            {
                if ("0123456789-".IndexOf(s[index]) == -1)
                    break;
                index++;
            }
            var sid = s.Substring(start, index - start);
            if ((sid.IndexOf("--") != -1) || (sid[sid.Length - 1] == '-'))
                throw new Exception();
            return sid;
        }

        private void ParseFlags(string s, ref int index)
        {
            while (index < s.Length)
            {
                if (s[index] == 'P')
                {
                    index++;
                    continue;
                }
                var tmp = s.Substring(index, 2);
                if ((tmp == "AI") || (tmp == "AR"))
                {
                    index += 2;
                    continue;
                }
                return;
            }
        }

        private ACE ParseACE(string s, ref int index)
        {
            var start = index;
            while (s[index] != ')')
                index++;
            index++;
            var rv = new ACE(s.Substring(start, index - start));
            return rv;
        }

        public List<string> GetAllSIDs()
        {
            var rv = new List<string>();
            if (!string.IsNullOrWhiteSpace(_owner))
                if (!rv.Contains(_owner))
                    rv.Add(_owner);
            if (!string.IsNullOrWhiteSpace(_group))
                if (!rv.Contains(_group))
                    rv.Add(_group);
            foreach (var ace in _dacl)
                if (!string.IsNullOrWhiteSpace(ace.GetSID()))
                    if (!rv.Contains(ace.GetSID()))
                        rv.Add(ace.GetSID());
            foreach (var ace in _sacl)
                if (!string.IsNullOrWhiteSpace(ace.GetSID()))
                    if (!rv.Contains(ace.GetSID()))
                        rv.Add(ace.GetSID());
            return rv;
        }

        public static string SIDToLong(string sid, bool translateSID)
        {
            if (sid.Length < 2)
                return sid;
            if (sid[1] == '-')
            {
                if (!translateSID)
                    return "\t" + sid;
                try
                {
                    var t = new SecurityIdentifier(sid).Translate(typeof(NTAccount)).ToString();
                    return "[??]\t" + sid + " \t" + (string.IsNullOrWhiteSpace(t) ? "??" : t);
                }
                catch
                {
                    return "[??]\t" + sid;
                }
            }
            switch (sid)
            {
                case "AA": return "[AA]\tS-1-5-32-579 \tAccess Control Assistance Operators";
                case "AC": return "[AC]\tS-1-15-2-1 \tAll App Packages";
                case "AN": return "[AN]\tS-1-5-7 \tAnonymous Logged-on Users";
                case "AO": return "[AO]\tS-1-5-32-548 \tAccount Operators";
                case "AU": return "[AU]\tS-1-5-11 \tAuthenticated Users";
                case "BA": return "[BA]\tS-1-5-32-544 \tBuilt-in (Local) Administrators";
                case "BG": return "[BG]\tS-1-5-32-546 \tBuilt-in (Local) Guests";
                case "BO": return "[BO]\tS-1-5-32-551 \tBackup Operators";
                case "BU": return "[BU]\tS-1-5-32-545 \tBuilt-in (Local) Users";
                case "CA": return "[CA]\tS-1-5-32-517 \tCertificate Server Administrators";
                case "CD": return "[CD]\tS-1-5-32-574 \tUsers who can connect to certification authorities using Distributed Component Object Model (DCOM)";
                case "CG": return "[CG]\tS-1-3-1 \tCreator Group";
                case "CN": return "[CN]\tS-1-5-32-522 \tCloneable Controllers";
                case "CO": return "[CO]\tS-1-3-0 \tCreator Owner";
                case "CY": return "[CY]\tS-1-5-32-569 \tCrypto Operators";
                case "DA": return "[DA]\tS-1-5-32-512 \tDomain Administrators";
                case "DC": return "[DC]\tS-1-5-32-515 \tDomain Computers";
                case "DD": return "[DD]\tS-1-5-32-516 \tDomain Controllers";
                case "DG": return "[DG]\tS-1-5-32-514 \tDomain Guests";
                case "DU": return "[DU]\tS-1-5-32-513 \tDomain Users";
                case "EA": return "[EA]\tS-1-5-32-519 \tEnterprise Administrators";
                case "ED": return "[ED]\tS-1-5-9 \tEnterprise Domain Controllers";
                case "ER": return "[ER]\tS-1-5-32-573 \tEvent Log Readers";
                case "ES": return "[ES]\tS-1-5-32-576 \tRDS Endpoint Servers";
                case "HA": return "[HA]\tS-1-5-32-578 \tHyper-V Administrators";
                case "HI": return "[HI]\tS-1-16-12288 \tHigh Integrity Level";
                case "IS": return "[IS]\tS-1-5-32-568 \tAnonymous Internet Users";
                case "IU": return "[IU]\tS-1-5-4 \tInteractive Logged-on Users";
                case "LA": return "[LS]\tS-1-5-21-*-*-*-500 \tLocal Administrator Account";
                case "LG": return "[LG]\tS-1-5-21-*-*-*-501 \tLocal Guest Account";
                case "LS": return "[LS]\tS-1-5-19 \tLocal Service Account";
                case "LU": return "[LU]\tS-1-5-32-559 \tPerformance Log Users";
                case "LW": return "[LW]\tS-1-16-4096 \tLow Security Level";
                case "ME": return "[ME]\tS-1-16-8192 \tMedium Security Level";
                case "MP": return "[MP]\tS-1-16-8448 \tMedium Plus Security Level";
                case "MS": return "[MS]\tS-1-5-32-577 \tRDS Management Servers";
                case "MU": return "[MU]\tS-1-5-32-558 \tPerformance Monitor Users";
                case "NO": return "[NO]\tS-1-5-32-556 \tNetwork Configuration Operators";
                case "NS": return "[NS]\tS-1-5-20 \tNetwork Service Account";
                case "NU": return "[NU]\tS-1-5-2 \tNetwork Logged-on Users";
                case "OW": return "[OW]\tS-1-3-4 \tOwner Rights";
                case "PA": return "[PA]\tS-1-5-32-520 \tGroup Policy Administrators";
                case "PO": return "[PO]\tS-1-5-32-550 \tPrinter Operators";
                case "PS": return "[PS]\tS-1-5-10 \tPrincipal Self/Personal Self";
                case "PU": return "[PU]\tS-1-5-32-547 \tPower Users";
                case "RA": return "[RA]\tS-1-5-32-575 \tRDS Remote Access Servers";
                case "RC": return "[RC]\tS-1-5-12 \tRestricted Code";
                case "RD": return "[RD]\tS-1-5-32-555 \tTerminal Server Users (Remote Desktop)";
                case "RE": return "[RE]\tS-1-5-32-552 \tReplicator";
                case "RM": return "[RM]\tS-1-5-32-580 \tRemote Management Users";
                case "RO": return "[RO]\tS-1-5-32-498 \tEnterprise Read-Only Domain Controllers";
                case "RS": return "[RS]\tS-1-5-32-553 \tRemote Access Servers";
                case "RU": return "[RU]\tS-1-5-32-554 \tAlias to grant permissions to accounts using applications compatible with Windows NT 4.0 operating systems";
                case "SA": return "[SA]\tS-1-5-32-518 \tSchema Administrators";
                case "SI": return "[SI]\tS-1-16-16384 \tSystem Integrity Level";
                case "SO": return "[SO]\tS-1-5-32-549 \tServer Operators";
                case "SU": return "[SU]\tS-1-5-6 \tService Logged-on Users";
                case "SY": return "[SY]\tS-1-5-18 \tLocal System Account";
                case "UD": return "[UD]\tS-1-5-84-0-0-0-0-0 \tUser-Mode Drivers";
                case "WD": return "[WD]\tS-1-1-0 \tWorld (Everyone)";
                case "WR": return "[WR]\tS-1-5-33 \tWrite Restricted Code";
                default:
                    return sid;
            }
        }

        public List<string> GetAllRights()
        {
            var rv = new List<string>();
            foreach (var ace in _dacl)
            {
                var l = ace.GetAllRights();
                foreach (var right in l)
                {
                    if (!rv.Contains(right))
                        rv.Add(right);
                }
            }
            foreach (var ace in _sacl)
            {
                var l = ace.GetAllRights();
                foreach (var right in l)
                {
                    if (!rv.Contains(right))
                        rv.Add(right);
                }
            }
            return rv;
        }

        public string GetOwner()
        {
            return _owner;
        }

        public string GetGroup()
        {
            return _group;
        }

        public List<ACE> GetACEs()
        {
            return _dacl;
        }
    }
}