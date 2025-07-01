using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Communicator
{
    public class Receiver
    {
        private TcpListener listener;
        private bool isRunning = false;

        public event Action<byte> DataReceived;

        public Receiver(string ip, int port)
        {
            IPAddress address = IPAddress.Parse(ip);
            listener = new TcpListener(address, port);
        }

        public void Start()
        {
            isRunning = true;
            listener.Start();

            Thread thread = new Thread(ListenForClients);
            thread.IsBackground = true;
            thread.Start();
        }

        private void ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    int receivedByte = stream.ReadByte();
                    if (receivedByte >= 0)
                    {
                        DataReceived?.Invoke((byte)receivedByte);
                    }

                    stream.Close();
                    client.Close();
                }
                catch
                {
                    // Ignorăm erorile pentru început
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            listener.Stop();
        }
    }
}
