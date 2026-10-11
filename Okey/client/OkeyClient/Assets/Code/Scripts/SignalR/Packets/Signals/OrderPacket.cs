namespace Code.Scripts.SignalR.Packets
{
    using System.Collections.Generic;

    public class OrderPacket
    {
        public List<string>? playerConnectionIds { get; set; }
        public List<string>? playerUsernames { get; set; }
    }
}
