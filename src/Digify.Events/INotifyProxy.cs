namespace Digify.Events
{
    public interface INotifyProxy
    {
        IEventBus EventBus { get; set; }
    }
}