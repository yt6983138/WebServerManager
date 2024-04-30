namespace WebServerManager.Components.Generic;

public class Event<TEventHandler> where TEventHandler : Delegate
{
	public List<TEventHandler>? EventHandlers { get; private set; } = new();

	public void Add(TEventHandler handler)
	{
		this.EventHandlers ??= new();
		this.EventHandlers.Add(handler);
	}
	public void Remove(TEventHandler handler)
	{
		this.EventHandlers ??= new();
		this.EventHandlers.Remove(handler);
	}
	public void Invoke(params object?[] args)
	{
		this.EventHandlers ??= new();
		foreach (TEventHandler handler in this.EventHandlers)
		{
			handler.DynamicInvoke(args);
		}
	}
	public static Event<TEventHandler> operator +(Event<TEventHandler> @event, TEventHandler handler)
	{
		@event.Add(handler);
		return @event;
	}
	public static Event<TEventHandler> operator -(Event<TEventHandler> @event, TEventHandler handler)
	{
		@event.Remove(handler);
		return @event;
	}
}
