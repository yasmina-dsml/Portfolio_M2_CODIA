namespace Code.Scripts.SignalR.Packets.Rooms
{
    using System.Collections.Generic;
    using JetBrains.Annotations;

    public class RoomState
    {
        [CanBeNull]
        public List<string?> playerDatas { get; set; }
        public string? roomName { get; set; }
    }
}
