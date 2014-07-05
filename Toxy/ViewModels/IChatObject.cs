namespace Toxy.ViewModels
{
    public interface IChatObject
    {
        int ChatNumber { get; set; }
        bool Selected { get; set; }
        bool HasNewMessage { get; set; }
    }
}