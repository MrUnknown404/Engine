using System.Diagnostics;

namespace Engine3.Utility {
	public class PerformanceMonitor {
		private const long TicksPerSecond = 1000000000; // Stopwatch.Frequency;
		private const long TicksPerMillisecond = TicksPerSecond / 1000;

		public uint Ups { get; private set; }
		public uint Fps { get; private set; }

		public float MinUpdateTime { get; private set; } // ms
		public float MaxUpdateTime { get; private set; } // ms
		public float AvgUpdateTime { get; private set; } // ms
		public float UpdateTime { get; private set; } // ms

		public float MinFrameTime { get; private set; } // ms
		public float MaxFrameTime { get; private set; } // ms
		public float AvgFrameTime { get; private set; } // ms
		public float FrameTime { get; private set; } // ms

		public bool CalculateMinMaxAverage { get; init; }
		public byte MinMaxAverageSampleTime { get; init; } = 3; // seconds

		public bool StoreLastTimeValues { get; init; }
		public ushort AmountOfUpdateTimeToStore { get; init; } = 100;
		public ushort AmountOfFrameTimeToStore { get; init; } = 1000;

		public float[] UpdateTimesInSampleTime => updateTimesInSampleTime.ToArray();
		public float[] FrameTimesInSampleTime => frameTimesInSampleTime.ToArray();
		public float[] LastUpdateTimes => lastUpdateTimes.ToArray();
		public float[] LastFrameTimes => lastFrameTimes.ToArray();

		private readonly List<float> updateTimesInSampleTime = new();
		private readonly List<float> frameTimesInSampleTime = new();
		private readonly List<float> lastUpdateTimes = new(); // TODO use better collection?
		private readonly List<float> lastFrameTimes = new();

		private long updateStartTick;
		private long frameStartTick;
		private long updateAccumulator;
		private long updateMinMaxAvgAccumulator;
		private long frameAccumulator;
		private long frameMinMaxAvgAccumulator;
		private uint updateCounter;
		private uint frameCounter;

		public static long GetTimeDifference(ref long currentTime) {
			long cycleStart = Stopwatch.GetTimestamp();
			long time = cycleStart - currentTime;
			currentTime = cycleStart;
			return time;
		}

		public void AddUpdateAccumulator(long time) {
			updateAccumulator += time;
			updateMinMaxAvgAccumulator += time;
		}

		public void AddFrameAccumulator(long time) {
			frameAccumulator += time;
			frameMinMaxAvgAccumulator += time;
		}

		public void StartTimingUpdate() => updateStartTick = Stopwatch.GetTimestamp();
		public void StartTimingFrame() => frameStartTick = Stopwatch.GetTimestamp();

		public void StopTimingUpdate() {
			UpdateTime = (float)(Stopwatch.GetTimestamp() - updateStartTick) / TicksPerMillisecond;

			if (CalculateMinMaxAverage) { updateTimesInSampleTime.Add(UpdateTime); }
			if (StoreLastTimeValues) {
				lastUpdateTimes.Add(UpdateTime);
				if (lastUpdateTimes.Count > AmountOfUpdateTimeToStore) { lastUpdateTimes.RemoveAt(0); }
			}
		}

		public void StopTimingFrame() {
			FrameTime = (float)(Stopwatch.GetTimestamp() - frameStartTick) / TicksPerMillisecond;

			if (CalculateMinMaxAverage) { frameTimesInSampleTime.Add(FrameTime); }
			if (StoreLastTimeValues) {
				lastFrameTimes.Add(FrameTime);
				if (lastFrameTimes.Count > AmountOfFrameTimeToStore) { lastFrameTimes.RemoveAt(0); }
			}
		}

		public void AddUpdate() => updateCounter++;
		public void AddFrame() => frameCounter++;

		public void CheckUpdateTime() {
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

		public void CheckFrameTime() {
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
}