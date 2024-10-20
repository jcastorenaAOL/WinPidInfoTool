﻿using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Xml;
using System.Management;
using System.Diagnostics;

[DllImport("pidgenx.dll", EntryPoint = "PidGenX", CharSet = CharSet.Auto)]
static extern int PidGenX(string ProductKey, string PkeyPath, string MSPID, int UnknownUsage, IntPtr ProductID, IntPtr DigitalProductID, IntPtr DigitalProductID4);

static string CheckProductKey(string ProductKey)
{
    string result = "";
    int RetID;
    byte[] gpid = new byte[0x32];
    byte[] opid = new byte[0xA4];
    byte[] npid = new byte[0x04F8];

    IntPtr PID = Marshal.AllocHGlobal(0x32);
    IntPtr DPID = Marshal.AllocHGlobal(0xA4);
    IntPtr DPID4 = Marshal.AllocHGlobal(0x04F8);


    // Local xrm-ms from system32
    string PKeyPath = Environment.SystemDirectory + @"\spp\tokens\pkeyconfig\pkeyconfig.xrm-ms";

    //string PKeyPath = Environment.CurrentDirectory(;
    string MSPID = "00000";
    Console.WriteLine(PKeyPath);

    gpid[0] = 0x32;
    opid[0] = 0xA4;
    npid[0] = 0xF8;
    npid[1] = 0x04;

    Marshal.Copy(gpid, 0, PID, 0x32);
    Marshal.Copy(opid, 0, DPID, 0xA4);
    Marshal.Copy(npid, 0, DPID4, 0x04F8);

    RetID = PidGenX(ProductKey, PKeyPath, MSPID, 0, PID, DPID, DPID4);

    if (RetID == 0)
    {
        Marshal.Copy(PID, gpid, 0, gpid.Length);
        Marshal.Copy(DPID4, npid, 0, npid.Length);
        string pid = GetString(gpid, 0x0000);
        string eid = GetString(npid, 0x0008);
        string aid = GetString(npid, 0x0088);
        string edi = GetString(npid, 0x0118);
        string sub = GetString(npid, 0x0378);
        string lit = GetString(npid, 0x03F8);
        string lic = GetString(npid, 0x0478);
        string cid = Convert.ToInt32(eid.Substring(6, 5)).ToString(CultureInfo.InvariantCulture);
        string prd = GetProductDescription(PKeyPath, "{" + aid + "}", edi);

        result += "Valid" + Environment.NewLine;
        result += pid + Environment.NewLine;
        result += eid + Environment.NewLine;
        result += aid + Environment.NewLine;
        result += prd + Environment.NewLine;
        result += edi + Environment.NewLine;
        result += sub + Environment.NewLine;
        result += lit + Environment.NewLine;
        result += lic + Environment.NewLine;
        result += cid;
    }
    else if (RetID == -2147024809)
    {
        result = "Invalid Arguments";
    }
    else if (RetID == -1979645695)
    {
        result = "Invalid Key";
    }
    else if (RetID == -2147024894)
    {
        result = "pkeyconfig.xrm.ms file is not found";
    }
    else
        result = "Invalid input!!!";
    Marshal.FreeHGlobal(PID);
    Marshal.FreeHGlobal(DPID);
    Marshal.FreeHGlobal(DPID4);
    /*
    FreeLibrary(dllHandle);
    */
    return result;
}

static string GetProductDescription(string pkey, string aid, string edi)
{
    XmlDocument doc = new XmlDocument();
    doc.Load(pkey);
    using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(doc.GetElementsByTagName("tm:infoBin")[0].InnerText)))
    {
        doc.Load(stream);
        XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("pkc", "http://www.microsoft.com/DRM/PKEY/Configuration/2.0");
        try
        {
            XmlNode node = doc.SelectSingleNode("/pkc:ProductKeyConfiguration/pkc:Configurations/pkc:Configuration[pkc:ActConfigId='" + aid + "']", ns);
            if (node == null)
            {
                node = doc.SelectSingleNode("/pkc:ProductKeyConfiguration/pkc:Configurations/pkc:Configuration[pkc:ActConfigId='" + aid.ToUpper() + "']", ns);
            }
            if (node != null && node.HasChildNodes)
            {

                if (node.ChildNodes[2].InnerText.Contains(edi))
                {
                    return node.ChildNodes[3].InnerText;
                }
                return "Not Found";
            }
            return "Not Found";
        }
        catch (Exception)
        {
            return "Not Found";
        }
    }
}

static string GetString(byte[] bytes, int index)
{
    int n = index;
    while (!(bytes[n] == 0 && bytes[n + 1] == 0)) n++;
    return Encoding.ASCII.GetString(bytes, index, n - index).Replace("\0", "");
}


//Get product key OEM from motherboard
// ManagementObjectSearcher searcher =
//     new ManagementObjectSearcher("SELECT Product, SerialNumber FROM Win32_BaseBoard");

// ManagementObjectCollection information = searcher.Get();

// foreach (ManagementObject obj in information)
// {
//     foreach (PropertyData data in obj.Properties)
//         Console.WriteLine("{0} = {1}", data.Name, data.Value);
//     Console.WriteLine();
// }

string WIndowsPID = string.Empty;

ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * FROM SoftwareLicensingService");
ManagementObjectCollection information = searcher.Get();

foreach (ManagementObject obj in information)
{
    foreach (PropertyData data in obj.Properties)
        if(data.Name == "OA3xOriginalProductKey"){
            Console.WriteLine("Windows Key Found! : {0}", data.Value);
            WIndowsPID = data.Value.ToString();
        }

}

Console.WriteLine(
    WIndowsPID == string.Empty?
    "Windows Product Key Not Found!" : 
    CheckProductKey(WIndowsPID)
);

Console.WriteLine("Press any Key to exit...");
Console.ReadLine();
