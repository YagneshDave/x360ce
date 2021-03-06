﻿using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;

namespace JocysCom.ClassLibrary.Security
{
    public class PermissionHelper
    {

        public void ExportToFile(string filename)
        {
            var permissionSet = new PermissionSet(PermissionState.None);
            var writePermission = new FileIOPermission(FileIOPermissionAccess.Write, filename);
            permissionSet.AddPermission(writePermission);
            if (permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
            {
                //using (FileStream fstream = new FileStream(filename, FileMode.Create))
                //using (TextWriter writer = new StreamWriter(fstream))
                //{
                //	// try catch block for write permissions 
                //	writer.WriteLine("sometext");
                //}
            }
            else
            {
                //perform some recovery action here

            }
        }

        #region Registry

        public static bool HasRights(RegistryKey key, WellKnownSidType wksid, RegistryRights rights)
        {
            var sid = new SecurityIdentifier(wksid, null);
            return HasRights(key, sid, rights);
        }

        public static bool HasRights(RegistryKey key, SecurityIdentifier sid, RegistryRights rights)
        {
            if (key == null)
                return false;
            var security = key.GetAccessControl();
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (RegistryAccessRule rule in rules)
            {
                if (rule.IdentityReference != sid)
                    continue;
                if (rule.AccessControlType != AccessControlType.Allow)
                    continue;
                if ((rule.RegistryRights & rights) == rights)
                    return true;
            }
            return false;
        }

        public static bool SetRights(RegistryKey baseKey, string registryName, SecurityIdentifier sid, RegistryRights rights)
        {
            var key = baseKey.OpenSubKey(registryName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            if (key == null)
                return false;
            var rs = new RegistrySecurity();
            var security = key.GetAccessControl();
            RegistryAccessRule sidRule = null;
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (RegistryAccessRule rule in rules)
            {
                if (rule.IdentityReference != sid)
                    continue;
                if (rule.AccessControlType == AccessControlType.Allow)
                {
                    sidRule = rule;
                    break;
                }
            }
            if (sidRule == null)
            {
                sidRule = new RegistryAccessRule(sid,
                    rights,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);
                security.AddAccessRule(sidRule);
            }
            else
            {
                var newRule = new RegistryAccessRule(sid,
                     sidRule.RegistryRights | rights,
                     sidRule.InheritanceFlags,
                    sidRule.PropagationFlags,
                    AccessControlType.Allow);
                security.SetAccessRule(sidRule);
            }
            key.SetAccessControl(security);
            return true;
        }
        #endregion
    }
}
