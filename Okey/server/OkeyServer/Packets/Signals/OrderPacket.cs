namespace OkeyServer.Packets;

public class OrderPacket
{
    public List<string>? playerConnectionIds { get; set; }
    public List<string>? playerUsernames { get; set; }
}
