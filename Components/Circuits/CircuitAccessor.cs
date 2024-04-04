using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WebServerManager.Components.Circuits;

public interface ICircuitAccessor
{
	Circuit? CurrentCircuit { get; set; }
}

public class CircuitAccessor : ICircuitAccessor
{
	public Circuit? CurrentCircuit { get; set; }
}