using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Management;

public class BcdStoreAccessor
{
    static ManagementScope GetManagementScope()
    {
        ConnectionOptions connectionOptions = new ConnectionOptions();
        connectionOptions.Impersonation = ImpersonationLevel.Impersonate;
        connectionOptions.EnablePrivileges = true;

        // The ManagementScope is used to access the WMI info as Administrator
        return new ManagementScope(@"root\WMI", connectionOptions);

    }

    static ManagementObject GetBCDStore(ManagementScope managementScope)
    {

        // {9dea862c-5cdd-4e70-acc1-f32b344d4795} is the GUID of the System BcdStore
        return new ManagementObject(managementScope,
                new ManagementPath("root\\WMI:BcdObject.Id=\"{9dea862c-5cdd-4e70-acc1-f32b344d4795}\",StoreFilePath=\"\""), null);
    }

    static List<string> GetBootEntries(ManagementObject BcdStore, ManagementScope managementScope)
    {
        var bootEntries = new List<string>();

        var inParams = BcdStore.GetMethodParameters("GetElement");

        // 0x24000001 is a BCD constant: BcdBootMgrObjectList_DisplayOrder
        inParams["Type"] = ((UInt32)0x24000001);
        ManagementBaseObject outParams = BcdStore.InvokeMethod("GetElement", inParams, null);
        ManagementBaseObject mboOut = ((ManagementBaseObject)(outParams.Properties["Element"].Value));

        string[] osIdList = (string[])mboOut.GetPropertyValue("Ids");

        // Each osGuid is the GUID of one Boot Manager in the BcdStore
        foreach (string osGuid in osIdList)
        {
            ManagementObject currentManObj = new ManagementObject(managementScope,
                new ManagementPath("root\\WMI:BcdObject.Id=\"" + osGuid + "\",StoreFilePath=\"\""), null);

            var id = currentManObj.GetPropertyValue("Id");
            bootEntries.Add((string)currentManObj.GetPropertyValue("Id"));
        }

        return bootEntries;
    }

    static string GetDefaultBootEntry(ManagementObject BcdStore, ManagementScope managementScope)
    {
        var inParams = BcdStore.GetMethodParameters("GetElement");

        inParams["Type"] = ((UInt32)0x23000003); // BcdBootMgrObject_DefaultObject 
        var outParams = BcdStore.InvokeMethod("GetElement", inParams, null);
        var mboOut = ((ManagementBaseObject)(outParams.Properties["Element"].Value));

        return (string) mboOut.Properties["Id"].Value;
    }

    static void SetDefaultBootEntry(string newBootEntry, ManagementObject BcdStore, ManagementScope managementScope)
    {
        var inParams = BcdStore.GetMethodParameters("SetObjectElement");

        inParams["Type"] = ((UInt32)0x23000003); // BcdBootMgrObject_DefaultObject 
        inParams["Id"] = newBootEntry;
        var outParams = BcdStore.InvokeMethod("SetObjectElement", inParams, null);
//        var mboOut = ((ManagementBaseObject)(outParams.Properties["Element"].Value));
    }

    public static void Main()
    {
        var managementScope = GetManagementScope();
        var BcdStore = GetBCDStore(managementScope);

        var currentDefaultBoot = GetDefaultBootEntry(BcdStore, managementScope);
        string newDefaultBoot = null;

        foreach (var entry  in GetBootEntries(BcdStore, managementScope))
        {
            if (entry.CompareTo(currentDefaultBoot) != 0)
            {
                newDefaultBoot = entry;
                break;
            }

            Console.WriteLine("boot entry " + entry);
        }
        Debug.Assert(newDefaultBoot != null, "could not figure out new boot entry");


        Console.WriteLine("current " + currentDefaultBoot + " new " + newDefaultBoot);
        SetDefaultBootEntry(newDefaultBoot, BcdStore, managementScope);


    }
}
