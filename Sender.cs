using System;
using System.Net.Sockets;
using System.Text;

namespace Communicator
{
    public class Sender
    {
        private readonly string ip;
        private readonly int port;

        public Sender(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public void Send(byte data)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(ip, port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.WriteByte(data);
                        stream.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                // Pentru început, doar afișăm în consolă
                Console.WriteLine($"Eroare la trimitere: {ex.Message}");
            }
        }
    }
}
