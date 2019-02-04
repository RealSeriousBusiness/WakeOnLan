using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace Wake_On_Lan
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 6225);
            StreamReader reader = null;
            StreamWriter writer = null;
            TcpClient client = null;
            string ipendpoint = null;
            listener.Start();
            Console.WriteLine("{0}: Server has been started.", DateTime.Now);
            while (true)
            {
                if (listener.Pending() && (client == null || !client.Connected))
                {
                    client = listener.AcceptTcpClient();
                    reader = new StreamReader(client.GetStream());
                    writer = new StreamWriter(client.GetStream());
                    ipendpoint = client.Client.RemoteEndPoint.ToString();
                    Console.WriteLine("{0}: Client has been connected. {1}", DateTime.Now, ipendpoint);
                }

                if (client != null && client.Connected)
                {
                    try
                    {
                        string input = reader.ReadLine();
                        string pw = reader.ReadLine();
                        string ip = reader.ReadLine();
                        if (!pw.Equals("rs6LF5PrsDA1QnJg3vjjSu7WJaxmmb")) //What's SSL? LUL
                        {
                            Thread.Sleep(1000);
                            Console.WriteLine("{0}: Unknown Client detected. Client has been removed. {1}", DateTime.Now, ipendpoint);
                            throw new InvalidOperationException();
                        }

                        bool isIp = false;

                        if (Regex.Match(ip, "^([0-9]{1,3}.){3}[0-9]{1,3}$").Success)
                        {
                            isIp = true;
                        }
                        else if (!Regex.Match(ip, "^([0-9a-f]{2}[-:]){5}[0-9a-f]{2}$").Success)
                        {
                            writer.WriteLine("|Invalid input. Try again.");
                            writer.Flush();
                            continue;
                        }

                        switch (input[0])
                        {
                            case '1':
                                string mac;
                                if (isIp)
                                {
                                    mac = GetMacAddress(ip);
                                    if (String.IsNullOrEmpty(mac))
                                    {
                                        writer.WriteLine("|Cannot find the mac address that matches this ip address: " + ip);
                                        writer.Flush();
                                        continue;
                                    }
                                }
                                else
                                {
                                    mac = ip.Replace("-", "").Replace(":", "");
                                }

                                writer.WriteLine("Sending WOL packet to {0}....", mac);
                                writer.Flush();
                                if (!SendWOLPacket(mac)) writer.WriteLine("Invalid Mac Address");
                                writer.WriteLine("|Done.");
                                writer.Flush();

                                break;
                            case '2':
                                writer.WriteLine("Checking availability...");
                                writer.Flush();
                                Ping p = new Ping();
                                PingReply result = null;
                                int i = 0;
                                do
                                {
                                    writer.WriteLine("Pinging " + ip);
                                    writer.Flush();
                                    result = p.Send(ip);
                                    i++;
                                }
                                while (result.Status != IPStatus.Success && i < 5);
                                writer.WriteLine(result.Status == IPStatus.Success ? "|The Device is now reachable!" : "|Cannot reach device");
                                writer.Flush();
                                break;
                        }

                    }
                    catch
                    {
                        Console.WriteLine("{0}: Client has been disconnected. {1}", DateTime.Now, ipendpoint);
                        ipendpoint = null;
                        client.Close();
                        client = null;
                        continue;
                    }
                }
                Thread.Sleep(100);
            }
        }

        public static string GetMacAddress(string ipAddress)
        {
            bool windows = Environment.OSVersion.ToString().ToLower().Contains("windows");
            Process p = new Process();
            p.StartInfo.FileName = windows ? "arp" : "sudo";
            p.StartInfo.Arguments = (windows ? "-a " : "arp -a ") + ipAddress;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            Match m = Regex.Match(output, "([0-9a-f]{2}[-:]){5}[0-9a-f]{2}"); //match mac address
            return m.Value.Replace("-", "").Replace(":", "");
        }

        public static bool SendWOLPacket(string macAddress)
        {
            if (macAddress.Length % 2 != 0 || macAddress.Length != 12)
                return false;

            byte[] mac = new byte[6];
            for (var i = 0; i < 6; i++)
            {
                var t = macAddress.Substring((i * 2), 2);
                mac[i] = Convert.ToByte(t, 16);
            }


            using (UdpClient client = new UdpClient())
            {
                client.Connect(IPAddress.Broadcast, 40000);
                byte[] packet = new byte[17 * 6];

                for (int i = 0; i < 6; i++)
                    packet[i] = 0xFF;

                for (int i = 1; i <= 16; i++)
                    for (int j = 0; j < 6; j++)
                        packet[i * 6 + j] = mac[j];

                client.Send(packet, packet.Length);
            }
            return true;
        }
    }
}
