using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader reader = null;
            StreamWriter writer = null;
            string pw = "rs6LF5PrsDA1QnJg3vjjSu7WJaxmmb";
            while (true)
            {
                Console.Write("Connect to:");
                string ip = Console.ReadLine();

                TcpClient client = new TcpClient();
                IPAddress res = null;
                try
                {
                    res = IPAddress.Parse(ip);
                }
                catch
                {
                    try
                    {
                        res = Dns.GetHostEntry(ip).AddressList[0];
                    }
                    catch
                    {
                        Console.WriteLine("Invalid input or host is unreachable.");
                        continue;
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        client.Connect(res, 6225);
                        Console.WriteLine("Successfully connected to " + ip);
                        reader = new StreamReader(client.GetStream());
                        writer = new StreamWriter(client.GetStream());
                        break;
                    }
                    catch
                    {
                        Console.WriteLine("Cannot connect to {0}. {1} tries left", ip, 3 - i);
                        Thread.Sleep(500);
                    }
                }
                if (!client.Connected)
                {
                    Console.WriteLine("Cannot connect. Reset Program.");
                    continue;
                }

                while(client.Connected)
                {
                    Console.WriteLine("1:\tWake up Device\n2:\tCheck Device\nAny:\tExit");
                    char input = Console.ReadKey().KeyChar;
                    if (input != '1' && input != '2') Environment.Exit(0);
                    Console.WriteLine();
                    Console.Write("Enter IP or mac:");

                    string localIp = Console.ReadLine();

                    writer.WriteLine(input.ToString());
                    writer.WriteLine(pw);
                    writer.WriteLine(localIp);
                    writer.Flush();

                    string callback = reader.ReadLine();
                    while (!callback.StartsWith("|"))
                    {
                        Console.WriteLine(callback);
                        callback = reader.ReadLine();
                    }
                    Console.WriteLine(callback.Replace("|", ""));
                }

            }

        }
    }
}
