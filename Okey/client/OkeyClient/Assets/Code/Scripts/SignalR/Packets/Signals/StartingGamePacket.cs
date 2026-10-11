namespace Code.Scripts.SignalR.Packets
{
    using System.Collections.Generic;

    public class StartingGamePacket
    {
        public List<string>? playersList { get; set; }
        public List<string>? playersUsernames { get; set; }
        public string? playerId { get; set; }
    }
}
