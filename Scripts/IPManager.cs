using System.Net;
using System.Net.Sockets;

namespace ZenTools.Postman
{
    public class IPManager
    {
        public static string GetIP(ADDRESSFAM Addfam)
        {
            //Return null if ADDRESSFAM is Ipv6 but Os does not support it
            if (Addfam == ADDRESSFAM.IPv6 && !Socket.OSSupportsIPv6)
            {
                return null;
            }

            string output = "";

            IPAddress[] localIPAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress localIPAddress in localIPAddresses)
            {
                if (localIPAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    output = localIPAddress.ToString();
                    break;
                }
            }
            return output;
        }
    }

    public enum ADDRESSFAM
    {
        IPv4, IPv6
    }
}