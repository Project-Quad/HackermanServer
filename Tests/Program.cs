using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

public static class SocketClient
{
    public static int Main(String[] args)
    {
        StartClient();
        return 0;
    }

    public static void StartClient()
    {
        byte[] bytes = new byte[1024];

        try
        {
            Console.WriteLine("Please enter a server IP to connect to:");
            var serverIP = Console.ReadLine();
            Console.WriteLine("Please enter the server port:");
            int serverPort = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Please enter a message to send to the server");
            var serverMessage = Console.ReadLine();

            IPAddress ipAddress = IPAddress.Parse(serverIP);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

            Socket sender = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sender.Connect(remoteEP);
                Console.WriteLine($"Socket connected to {sender.RemoteEndPoint}");

                var conn = ProcessServer(sender);

                // Create header
                var header = new byte[20];
                conn.Aes.IV.CopyTo(header, 0);

                // Create encrypted message
                byte[] encryptedMsg;
                using (MemoryStream mem = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(mem, conn.Aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(serverMessage);
                        encryptedMsg = mem.ToArray();
                    }
                }
                // Finish header
                BitConverter.GetBytes(encryptedMsg.Length).CopyTo(header, 16);

                conn.Sock.Send(header);
                conn.Sock.Send(encryptedMsg);

                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
                Console.ReadLine();

            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane);
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static ServerConnection ProcessServer(Socket s)
    {
        byte[] test = new byte[23];
        s.Receive(test);
        Console.WriteLine(Encoding.ASCII.GetString(test));
        ServerConnection conn = new ServerConnection();
        conn.Sock = s;
        conn.Aes = new AesCryptoServiceProvider();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using (RSACng rsa = new RSACng(3072))
            {
                var bytes = new byte[3072];
                conn.Sock.Receive(bytes);
                rsa.ImportRSAPublicKey(bytes, out _);
                conn.Sock.Send(rsa.Encrypt(conn.Aes.Key, RSAEncryptionPadding.Pkcs1));
            }
        }
        else
        {
            using (RSAOpenSsl rsa = new RSAOpenSsl(3072))
            {
                var bytes = new byte[3072];
                conn.Sock.Receive(bytes);
                rsa.ImportRSAPublicKey(bytes, out _);
                conn.Sock.Send(rsa.Encrypt(conn.Aes.Key, RSAEncryptionPadding.Pkcs1));
            }
        }
        var headerBytes = new byte[20];
        conn.Sock.Receive(headerBytes);
        conn.Aes.IV = headerBytes.Take(16).ToArray();
        int msgLength = BitConverter.ToInt32(headerBytes.Skip(16).Take(4).ToArray());
        var encryptedBytes = new byte[msgLength];
        conn.Sock.Receive(encryptedBytes);
        string msg;
        using (MemoryStream ms = new MemoryStream(encryptedBytes))
        {
            using (CryptoStream cs = new CryptoStream(ms, conn.Aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                using (StreamReader sr = new StreamReader(cs))
                {
                    msg = sr.ReadToEnd();
                }
            }
        }
        if (msg != "OK!") throw new Exception("Server didn't return \"OK!\"");
        return conn;
    }
    
    public struct ServerConnection
    {
        public AesCryptoServiceProvider Aes;
        public Socket Sock;
    }
}
