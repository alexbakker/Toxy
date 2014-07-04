namespace Toxy.ViewModels
{
    public interface IChatObject
    {
        bool Selected { get; set; }
        bool HasNewMessage { get; set; }
    }
}