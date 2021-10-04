using System.Net.NetworkInformation;

namespace CallCentre
{
    public static class Helpers
    {
        /// <summary>
        /// Finds the MAC address of the first operation NIC found.
        /// </summary>
        /// <returns>The MAC address.</returns>
        public static string GetMacAddress()
        {
            var macAddresses = string.Empty;

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                macAddresses += nic.GetPhysicalAddress().ToString();
                break;
            }

            return macAddresses;
        }
    }
}