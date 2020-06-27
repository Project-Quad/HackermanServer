using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;

// Socket Listener acts as a server and listens to the incoming   
// messages on the specified port and protocol.  
namespace Hackerman_Server
{

    public class Networking
    {
        private readonly Configuration _config;
        private System.Threading.ManualResetEvent allDone = new System.Threading.ManualResetEvent(false);
        private List<UserConnection> allCurrentUsers = new List<UserConnection>();

        public Networking(Configuration config)
        {
            _config = config;
        }
        
        public void StartServer()
        {
            // Get Host IP Address that is used to establish a connection  
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
            // If a host has multiple addresses, you will get a list of addresses  
            IPAddress ipAddress = IPAddress.Parse(_config.bindAddr);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, _config.bindPort);

            // Create timer to check if clients are alive
            var checkClients = new System.Timers.Timer(10000);
            checkClients.Elapsed += (Sender, Args) =>
            {
                foreach (var client in allCurrentUsers)
                {
                    if (!client.Sock.IsConnected())
                    {
                        client.Sock.Close();
                        allCurrentUsers.Remove(client);
                    }
                }
            };
            checkClients.AutoReset = true;
            checkClients.Enabled = true;

            // Create a Socket that will use Tcp protocol      
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            try
            {
                // A Socket must be associated with an endpoint using the Bind method  
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.  
                // We will listen 10 requests at a time  
                listener.Listen(10);

                while (true)
                {
                    // Reset receiving state
                    allDone.Reset();
                    
                    Console.WriteLine("Waiting for a connection...");
                    // Listen for connections
                    listener.BeginAccept(ar =>
                    {
                        // Allow server to receive again
                        allDone.Set();
                        
                        // Get the listener socket
                        var listener = (Socket)ar.AsyncState;
                        // Create new handler socket
                        var handler = listener.EndAccept(ar);
                        

                        // Create and store connect object
                        var newConnection = ProcessClient(handler);
                        allCurrentUsers.Add(newConnection);
                        
                        // Begin recieving on the new socket
                        newConnection.Buffer = new byte[20];
                        newConnection.Sock.BeginReceive(newConnection.Buffer, 0, newConnection.Buffer.Length, 0, ReadHeaderCallback,
                            newConnection);
                    }, listener);
                    
                    // Wait until connection finishes
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }
        
        #region CALLBACKS
        
        #region RECEIVE CALLBACKS

        private void ReadHeaderCallback(IAsyncResult ar)
        {
            // Retrieve the connection object
            UserConnection conn = (UserConnection)ar.AsyncState;
            Socket handler = conn.Sock;

            if (handler.EndReceive(ar) == conn.Buffer.Length)
            {
                conn.Aes.IV = conn.Buffer.Take(16).ToArray();
                int newLength = BitConverter.ToInt32(conn.Buffer.Skip(16).Take(4).ToArray());

                conn.Buffer = new byte[newLength];

                handler.BeginReceive(conn.Buffer, 0, conn.Buffer.Length, 0, ReadCallback,
                    conn);
            } else throw new Exception("Socket didn't receive required bytes");
        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the connection object
            UserConnection conn = (UserConnection)ar.AsyncState;
            Socket handler = conn.Sock;

            string msg;
            
            if (handler.EndReceive(ar) == conn.Buffer.Length)
            {
                using (MemoryStream ms = new MemoryStream(conn.Buffer))
                {
                    using (CryptoStream cs = new CryptoStream(ms, conn.Aes.CreateDecryptor(conn.Aes.Key, conn.Aes.IV),
                        CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            msg = sr.ReadToEnd();
                        }
                    }
                }
                ProcessRequest(msg);
                Console.WriteLine($"Client sent:\n{msg}");
                conn.Buffer = new byte[20];
                conn.Sock.BeginReceive(conn.Buffer, 0, conn.Buffer.Length, 0, ReadHeaderCallback,
                    conn);
            } else throw new Exception("Socket didn't receive required bytes");
        }
        
        #endregion
        
        #region SEND CALLBACKS

        private void Send(UserConnection conn, string data)
        {
            byte[] header = new byte[20];
            conn.Aes.IV.CopyTo(header, 0);
            byte[] dataBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, conn.Aes.CreateEncryptor(conn.Aes.Key, conn.Aes.IV),
                    CryptoStreamMode.Write))
                {
                    using (StreamWriter sr = new StreamWriter(cs))
                        sr.Write(data);
                    dataBytes = ms.ToArray();
                }
            }
            BitConverter.GetBytes(dataBytes.Length).CopyTo(header, 16);
            byte[] msgToSend = new byte[dataBytes.Length + 20];
            header.CopyTo(msgToSend, 0);
            dataBytes.CopyTo(msgToSend, 20);

            conn.Sock.BeginSend(msgToSend, 0, msgToSend.Length, 0, SendCallBack, conn);
        }

        private void SendCallBack(IAsyncResult ar)
        {
            try
            {
                UserConnection conn = (UserConnection) ar.AsyncState;
                conn.Sock.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                UserConnection conn = (UserConnection)ar.AsyncState;
                conn.Sock.Close();
                allCurrentUsers.Remove(conn);
            }
        }
        
        #endregion
        
        #endregion

        private void ProcessRequest(string msg)
        {
            
        }
        
        private UserConnection ProcessClient(Socket s)
        {
            var ss = Encoding.ASCII.GetBytes("aaaa can you see this??");
            s.Send(ss);
            UserConnection conn = new UserConnection();
            conn.Sock = s;
            conn.Aes = new AesCryptoServiceProvider();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (RSACng rsa = new RSACng(3072))
                {
                    conn.Sock.Send(rsa.ExportRSAPublicKey());
                    var aesKey = new byte[384];
                    conn.Sock.Receive(aesKey);
                    conn.Aes.Key = rsa.Decrypt(aesKey, RSAEncryptionPadding.Pkcs1);
                }
            }
            else
            {
                using (RSAOpenSsl rsa = new RSAOpenSsl(3072))
                {
                    conn.Sock.Send(rsa.ExportRSAPublicKey());
                    var aesKey = new byte[384];
                    conn.Sock.Receive(aesKey);
                    conn.Aes.Key = rsa.Decrypt(aesKey, RSAEncryptionPadding.Pkcs1);
                }
            }
            byte[] encryptedMsg;
            var header = new byte[20];
            conn.Aes.IV.CopyTo(header, 0);
            using (MemoryStream mem = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(mem, conn.Aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                        sw.Write("OK!");
                    encryptedMsg = mem.ToArray();
                }
            }
            BitConverter.GetBytes(encryptedMsg.Length).CopyTo(header, 16);
            conn.Sock.Send(header);
            conn.Sock.Send(encryptedMsg);
            return conn;
        }

        public class UserConnection
        {
            public AesCryptoServiceProvider Aes;
            public Socket Sock;
            public byte[] Buffer;
            public int TotalBytesRec = 0;
            public bool Receiving = false;
        }
    }
    static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
    }
}