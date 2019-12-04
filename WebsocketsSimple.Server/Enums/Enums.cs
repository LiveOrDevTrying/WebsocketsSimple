namespace WebsocketsSimple.Server.Enums
{
    public enum BLLConnectionEventType
    {
        ConnectedNotAuthorized,
        Disconnect,
        ServerStart,
        ServerStop,
        Connecting,
        MaxConnectionsReached,
        ConnectedAuthorized,
        ConnectError
    }
}
