using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sulakore.Communication;
using Sulakore.Extensions;
using WebSocketSharp;
using Sulakore.Protocol;

namespace Bookie
{
    public partial class frmMain : ExtensionForm
    {
        //WebSocketWrapper socket;
        WebSocket socket;
        Dictionary<int, int> replacements = new Dictionary<int, int>();
        int roomId = 0;

        public frmMain()
        {
            InitializeComponent();
        }

        private void InitializeSocket()
        {
            socket = new WebSocket("ws://198.50.128.206:3000/");
            socket.OnOpen += Socket_OnOpen;
            socket.OnClose += Socket_OnClose;
            socket.OnMessage += Socket_OnMessage;
            socket.Connect();
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            ssMain.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = "Status: Disconnected";
            });
        }

        private void Socket_OnOpen(object sender, EventArgs e)
        {
            ssMain.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = "Status: Connected";
            });
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            var parameters = e.Data.Split(':');
            if (parameters[0] == "update")
            {
                int id = int.Parse(parameters[1]);
                int result = int.Parse(parameters[2]);
                Connection.SendToClientAsync(934, id, result);
            }
            if (parameters[0] == "clearfurni")
                replacements.Clear();
            if (parameters[0] == "furni")
            {
                HMessage msg = new HMessage(StringToByteArray(parameters[1]));
                msg.Header = 2726;
                Connection.SendToClientAsync(msg.ToBytes());
            }

        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Triggers.DetectOutgoing = true;
            Triggers.DetectIncoming = true;
            Triggers.OutAttach(1912, DiceRolled);
            Triggers.OutAttach(2960, DiceClosed);
            Triggers.OutAttach(3718, EnterRoom);
            Triggers.InAttach(2726, FurniLoaded);

            InitializeSocket();
        }

        private void EnterRoom(InterceptedEventArgs e)
        {
            roomId = e.Packet.ReadInteger();
            socket.Send($"loadedroom:{roomId}");
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private void DiceClosed(InterceptedEventArgs e)
        {
            socket.Send($"close:{e.Packet.ReadInteger()}");
            e.IsBlocked = true;
        }

        private void FurniLoaded(InterceptedEventArgs e)
        {
            e.IsBlocked = true;
            socket.Send($"loadedroom:{roomId}");
        }

        private void DiceRolled(InterceptedEventArgs e)
        {
            socket.Send($"roll:{e.Packet.ReadInteger()}");
            e.IsBlocked = true;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            socket.Close();
            socket.Connect();
        }
    }
}
