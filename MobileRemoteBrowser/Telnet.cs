using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MobileRemoteBrowser {
    public class Telnet: IDisposable {
        private Socket socket;
        private readonly Encoding encoding = Encoding.UTF8;

        public void Connect(string ip, int port) {
            var ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipe);
        }

        public void Send(string s) {
            socket.Send(encoding.GetBytes(s + "\n"));
        }

        public string Receive() {
            var buffer = new byte[100];
            var size = socket.Receive(buffer);
            Array.Resize(ref buffer, size);
            return encoding.GetString(buffer);
        }

        public void Dispose() {
            if (socket != null)
                (socket as IDisposable).Dispose();
        }
    }
}