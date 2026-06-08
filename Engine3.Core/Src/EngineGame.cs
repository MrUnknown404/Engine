using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Core.Utility.Exceptions;
using Engine3.Core.Utility.Versions;
using NLog;

namespace Engine3.Core;

public abstract class EngineGame {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public string Name { get; }
	public IPackableVersion Version { get; }
	public Engine3 Engine { get; }

	[field: MaybeNull]
	public Assembly Assembly { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(EngineGame)} Assembly too early. Must call {nameof(Engine3)}#{nameof(Engine3.Start)} first"); internal set; } // set via engine

	public bool ShouldShutdown { get; private set; }

	public event OnSetupFinishedDelegate? OnSetupFinishedEvent;

	protected EngineGame(Engine3 engine, string name, IPackableVersion version) {
		Engine = engine;
		Name = name;
		Version = version;
	}

	protected internal abstract void Update();

	/// <summary> Requests shutdown. Program will shut down on the next update </summary>
	public void RequestShutdown() {
		Logger.Debug("Requested shutdown");
		ShouldShutdown = true;
	}

	protected internal abstract void Cleanup();

	internal void InvokeOnSetupFinished() => OnSetupFinishedEvent?.Invoke();

	public delegate void OnSetupFinishedDelegate();
}