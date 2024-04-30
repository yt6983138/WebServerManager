using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WebServerManager.Components.Circuits;

public class TrackingCircuitHandler : CircuitHandler
{
	private ICircuitAccessor _circuitAccessor;

	public TrackingCircuitHandler(ICircuitAccessor circuitAccessor)
	{
		this._circuitAccessor = circuitAccessor;
	}

	public static event EventHandler<ICircuitAccessor>? CircuitConnected;
	public static event EventHandler<ICircuitAccessor>? CircuitDisconnected;
	public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		this._circuitAccessor.CurrentCircuit = circuit;
		CircuitConnected?.Invoke(this, this._circuitAccessor);
		return base.OnConnectionUpAsync(circuit, cancellationToken);
	}

	public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		// this._circuitAccessor.CurrentCircuit = null;
		CircuitDisconnected?.Invoke(this, this._circuitAccessor);
		return base.OnConnectionDownAsync(circuit, cancellationToken);
	}
}
