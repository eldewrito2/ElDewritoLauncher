using System;
using System.Runtime.InteropServices;

namespace InstallerLib.Utility
{
    class Firewall
    {
        public static void AddAuthorizedApplication(string name, string description, string programPath)
        {
            Type fwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", true)!;
            dynamic? fwMgr = Activator.CreateInstance(fwMgrType);
            if (fwMgr == null)
            {
                throw new InvalidComObjectException("Failed to activate INetFwMgr");
            }

            Type? authAppType = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication", true);
            dynamic? authApp = Activator.CreateInstance(authAppType!);
            if (authApp == null)
            {
                throw new InvalidComObjectException("Failed to activate INetFwAuthorizedApplication ");
            }

            authApp.Name = name;
            authApp.Description = description;
            authApp.ProcessImageFileName = programPath;
            fwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(authApp);
        }
    }
}
