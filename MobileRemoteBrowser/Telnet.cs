using System;
using System.Threading;
using De.Mud.Telnet;
using Net.Graphite.Telnet;

namespace MobileRemoteBrowser {
    public class Telnet : IDisposable {
        private readonly ManualResetEvent mre = new ManualResetEvent(false);
        private readonly TelnetWrapper telnet;
        private string receivedData;

        public Telnet() {
            telnet = new TelnetWrapper();
            telnet.Disconnected += (s, e) => { }; // dummy handler, otherwise NRE
            telnet.DataAvailable += telnet_DataAvailable;
        }

        public void Dispose() {
            if (telnet.Connected)
                telnet.Dispose();
        }

        private void telnet_DataAvailable(object sender, DataAvailableEventArgs e) {
            receivedData = e.Data;
            mre.Set();
        }

        public void Connect(string host, int port) {
            telnet.Connect(host, port);
            telnet.Receive();
            mre.WaitOne();
            mre.Reset();
        }

        public string Send(string cmd) {
            telnet.Send(cmd + telnet.CRLF);
            telnet.Receive();
            mre.WaitOne();
            mre.Reset();
            return receivedData;
        }
    }
}