namespace Toxy.ViewModels
{
    public interface IMessage
    {
        int FriendNumber { get; set; }
        int MessageId { get; set; }
        MessageType MessageType { get; }
        bool WasReceived { get; set; }
    }

    public enum MessageType
    {
        Message,
        FileTransfer
    }
}
