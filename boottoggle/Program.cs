using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

public class BcdStoreAccessor
{
    public static void Main()
    {
        ConnectionOptions connectionOptions = new ConnectionOptions();
        connectionOptions.Impersonation = ImpersonationLevel.Impersonate;
        connectionOptions.EnablePrivileges = true;

        // The ManagementScope is used to access the WMI info as Administrator
        ManagementScope managementScope = new ManagementScope(@"root\WMI", connectionOptions);

        // {9dea862c-5cdd-4e70-acc1-f32b344d4795} is the GUID of the System BcdStore
        ManagementObject BcdStore = 
            new ManagementObject(managementScope, 
                new ManagementPath("root\\WMI:BcdObject.Id=\"{9dea862c-5cdd-4e70-acc1-f32b344d4795}\",StoreFilePath=\"\""), null);

        ManagementBaseObject inParams = null;
        inParams = BcdStore.GetMethodParameters("GetElement");

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

            Console.WriteLine("" + currentManObj.GetPropertyValue("Id") + " |" + currentManObj.GetPropertyValue("Type") + "|");
        }

        inParams["Type"] = ((UInt32)0x23000003); // BcdBootMgrObject_DefaultObject 
        outParams = BcdStore.InvokeMethod("GetElement", inParams, null);
        mboOut = ((ManagementBaseObject)(outParams.Properties["Element"].Value));
        Console.WriteLine("current default boot " + mboOut.Properties["Id"].Value);
    }
}
