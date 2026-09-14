using System.Diagnostics;

namespace Engine4.Utility;

// TODO monitor timing anomalies/drift
// TODO calc avg/min/max. ring buffer for profiling?
public class PerformanceMonitor {
	/// <summary> In seconds </summary>
	public ushort ProfileTime { get; init; }

	public uint Ups { get; private set; }
	public uint Fps { get; private set; }

	private ulong secondTimer;
	private uint updateCounter;
	private uint frameCounter;

	internal void TimeUpdate(Action update) {
		long startTime = Stopwatch.GetTimestamp();
		update();
		long endTime = Stopwatch.GetTimestamp();
		ulong time = (ulong)(endTime - startTime); // TODO store this
	}

	internal void TimeRender(Action<float> render, float delta) {
		long startTime = Stopwatch.GetTimestamp();
		render(delta);
		long endTime = Stopwatch.GetTimestamp();
		ulong time = (ulong)(endTime - startTime); // TODO store this
	}

	internal void AddTime(ulong time) {
		secondTimer += time;

		if (secondTimer > (ulong)Stopwatch.Frequency) {
			secondTimer -= (ulong)Stopwatch.Frequency;

			Ups = updateCounter;
			Fps = frameCounter;
			updateCounter = 0;
			frameCounter = 0;
		}
	}

	public void IncrementUpdateCounter() => updateCounter++;
	public void IncrementFrameCounter() => frameCounter++;
}