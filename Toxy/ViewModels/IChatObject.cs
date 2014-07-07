namespace Toxy.ViewModels
{
    public interface IChatObject
    {
        string Name { get; set; }
        int ChatNumber { get; set; }
        bool Selected { get; set; }
        bool HasNewMessage { get; set; }
        string StatusMessage { get; set; }
    }
}