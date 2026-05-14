using System.Diagnostics;

namespace Engine3.Utility;

public class PerformanceMonitor {
	private const long TicksPerSecond = 1000000000; // Stopwatch.Frequency;
	private const long TicksPerMillisecond = TicksPerSecond / 1000;

	/// <summary> The amount of updates per second </summary> <remarks> Will be zero before first sample </remarks>
	public uint Ups { get; private set; }
	/// <summary> The amount of frames per second </summary> <remarks> Will be zero before first sample </remarks>
	public uint Fps { get; private set; }

	/// <summary> The lowest update time in the sampled update times measured in milliseconds </summary>
	/// <remarks> Will be zero before first sample.<br/> Only calculated if <see cref="CalculateMinMaxAverage"/> is true </remarks>
	public float MinUpdateTime { get; private set; }
	/// <summary> The highest update time in the sampled update times measured in milliseconds </summary>
	/// <remarks> Will be zero before first sample.<br/> Only calculated if <see cref="CalculateMinMaxAverage"/> is true </remarks>
	public float MaxUpdateTime { get; private set; }
	/// <summary> The average update time in the sampled update times measured in milliseconds </summary>
	/// <remarks> Will be zero before first sample.<br/> Only calculated if <see cref="CalculateMinMaxAverage"/> is true </remarks>
	public float AvgUpdateTime { get; private set; }
	/// <summary> The amount of time the last update took measured in milliseconds </summary>
	public float UpdateTime { get; private set; }

	/// <summary> The lowest frame time in the sampled frame times measured in milliseconds </summary>
	/// <remarks> Will be zero before first sample.<br/> Only calculated if <see cref="CalculateMinMaxAverage"/> is true </remarks>
	public float MinFrameTime { get; private set; }
	/// <summary> The highest frame time in the sampled frame times measured in milliseconds </summary>
	/// <remarks> Will be zero before first sample.<br/> Only calculated if <see cref="CalculateMinMaxAverage"/> is true </remarks>
	public float MaxFrameTime { get; private set; }
	/// <summary> The average frame time in the sampled frame times measured in milliseconds </summary>
	/// <remarks> Will be zero before first sample.<br/> Only calculated if <see cref="CalculateMinMaxAverage"/> is true </remarks>
	public float AvgFrameTime { get; private set; }
	/// <summary> The amount of time the last frame took measured in milliseconds </summary>
	public float FrameTime { get; private set; }

	/// <summary> Whether or not to enable calculating the minimum/maximum/average update/frame times </summary>
	public bool CalculateMinMaxAverage { get; init; }
	/// <summary> How long the sample time should be in seconds </summary>
	public byte MinMaxAverageSampleTime { get; init; } = 3;

	/// <summary> Whether or not to store previous update/frame times </summary>
	public bool StoreTimesForGraph { get; init; }
	/// <summary> The amount of previous update time entries to store </summary>
	public ushort LastUpdateTimeSize { get; init; } = 100;
	/// <summary> The amount of previous frame time entries to store </summary>
	public ushort LastFrameTimeSize { get; init; } = 1000;

	/// <summary> An array copy of the values inside the update time sample list </summary>
	public float[] UpdateTimesInSampleTime => updateTimesInSampleTime.ToArray();
	/// <summary> An array copy of the values inside the frame time sample list </summary>
	public float[] FrameTimesInSampleTime => frameTimesInSampleTime.ToArray();
	/// <summary> An array copy of the last <see cref="LastUpdateTimeSize"/> previous update times </summary>
	public float[] LastUpdateTimes => lastUpdateTimes.ToArray();
	/// <summary> An array copy of the last <see cref="LastFrameTimeSize"/> previous frame times </summary>
	public float[] LastFrameTimes => lastFrameTimes.ToArray();

	private readonly List<float> updateTimesInSampleTime = new();
	private readonly List<float> frameTimesInSampleTime = new();
	private readonly List<float> lastUpdateTimes = new(); // TODO use better collection? array or circular buffer? or something else?
	private readonly List<float> lastFrameTimes = new();

	private long updateStartTick;
	private long frameStartTick;

	private long updateAccumulator;
	private long updateMinMaxAvgAccumulator;
	private long frameAccumulator;
	private long frameMinMaxAvgAccumulator;

	private uint updateCounter;
	private uint frameCounter;

	internal static long GetTimeDifference(ref long currentTime) {
		long cycleStart = Stopwatch.GetTimestamp();
		long time = cycleStart - currentTime;
		currentTime = cycleStart;
		return time;
	}

	internal void AddUpdateAccumulator(long time) {
		updateAccumulator += time;
		updateMinMaxAvgAccumulator += time;
	}

	internal void AddFrameAccumulator(long time) {
		frameAccumulator += time;
		frameMinMaxAvgAccumulator += time;
	}

	internal void StartTimingUpdate() => updateStartTick = Stopwatch.GetTimestamp();
	internal void StartTimingFrame() => frameStartTick = Stopwatch.GetTimestamp();

	internal void StopTimingUpdate() {
		UpdateTime = (float)(Stopwatch.GetTimestamp() - updateStartTick) / TicksPerMillisecond;

		if (CalculateMinMaxAverage) { updateTimesInSampleTime.Add(UpdateTime); }
		if (StoreTimesForGraph) {
			lastUpdateTimes.Add(UpdateTime);
			if (lastUpdateTimes.Count > LastUpdateTimeSize) { lastUpdateTimes.RemoveAt(0); }
		}
	}

	internal void StopTimingFrame() {
		FrameTime = (float)(Stopwatch.GetTimestamp() - frameStartTick) / TicksPerMillisecond;

		if (CalculateMinMaxAverage) { frameTimesInSampleTime.Add(FrameTime); }
		if (StoreTimesForGraph) {
			lastFrameTimes.Add(FrameTime);
			if (lastFrameTimes.Count > LastFrameTimeSize) { lastFrameTimes.RemoveAt(0); }
		}
	}

	internal void AddUpdate() => updateCounter++;
	internal void AddFrame() => frameCounter++;

	internal void CheckUpdateTime() {
		if (updateAccumulator >= TicksPerSecond) {
			Ups = updateCounter;
			updateAccumulator -= TicksPerSecond;
			updateCounter = 0;
		}

		if (CalculateMinMaxAverage && updateMinMaxAvgAccumulator >= MinMaxAverageSampleTime * TicksPerSecond) {
			MinUpdateTime = updateTimesInSampleTime.Min();
			AvgUpdateTime = updateTimesInSampleTime.Average();
			MaxUpdateTime = updateTimesInSampleTime.Max();

			updateTimesInSampleTime.Clear();

			updateMinMaxAvgAccumulator -= MinMaxAverageSampleTime * TicksPerSecond;
		}
	}

	internal void CheckFrameTime() {
		if (frameAccumulator >= TicksPerSecond) {
			Fps = frameCounter;
			frameAccumulator -= TicksPerSecond;
			frameCounter = 0;
		}

		if (CalculateMinMaxAverage && frameMinMaxAvgAccumulator >= MinMaxAverageSampleTime * TicksPerSecond) {
			MinFrameTime = frameTimesInSampleTime.Min();
			AvgFrameTime = frameTimesInSampleTime.Average();
			MaxFrameTime = frameTimesInSampleTime.Max();

			frameTimesInSampleTime.Clear();

			frameMinMaxAvgAccumulator -= MinMaxAverageSampleTime * TicksPerSecond;
		}
	}
}