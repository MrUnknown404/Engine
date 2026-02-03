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

		private readonly List<float> updateTimesInSampleTime = new();
		private readonly List<float> frameTimesInSampleTime = new();

		private long updateStartTick;
		private long frameStartTick;
		private long updateCounterAccumulator;
		private long frameCounterAccumulator;
		private uint updateCounter;
		private uint frameCounter;

		public static long GetTimeDifference(ref long currentTime) {
			long cycleStart = Stopwatch.GetTimestamp();
			long time = cycleStart - currentTime;
			currentTime = cycleStart;
			return time;
		}

		public void AddUpdateCounterAccumulator(long time) => updateCounterAccumulator += time;
		public void AddFrameCounterAccumulator(long time) => frameCounterAccumulator += time;

		public void StartTimingUpdate() => updateStartTick = Stopwatch.GetTimestamp();
		public void StartTimingFrame() => frameStartTick = Stopwatch.GetTimestamp();

		public void StopTimingUpdate() {
			float updateTime = (float)(Stopwatch.GetTimestamp() - updateStartTick) / TicksPerMillisecond;

			if (CalculateMinMaxAverage) { updateTimesInSampleTime.Add(updateTime); }
			UpdateTime = updateTime;
		}

		public void StopTimingFrame() {
			float frameTime = (float)(Stopwatch.GetTimestamp() - frameStartTick) / TicksPerMillisecond;

			if (CalculateMinMaxAverage) { frameTimesInSampleTime.Add(frameTime); }
			FrameTime = frameTime;
		}

		public void AddUpdate() => updateCounter++;
		public void AddFrame() => frameCounter++;

		public void CheckUpdateTime() {
			if (updateCounterAccumulator >= TicksPerSecond) {
				if (CalculateMinMaxAverage) {
					MinUpdateTime = updateTimesInSampleTime.Min();
					AvgUpdateTime = updateTimesInSampleTime.Average();
					MaxUpdateTime = updateTimesInSampleTime.Max();

					updateTimesInSampleTime.Clear();
				}

				Ups = updateCounter;
				updateCounterAccumulator -= TicksPerSecond;
				updateCounter = 0;
			}
		}

		public void CheckFrameTime() {
			if (frameCounterAccumulator >= TicksPerSecond) {
				if (CalculateMinMaxAverage) {
					MinFrameTime = frameTimesInSampleTime.Min();
					AvgFrameTime = frameTimesInSampleTime.Average();
					MaxFrameTime = frameTimesInSampleTime.Max();

					frameTimesInSampleTime.Clear();
				}

				Fps = frameCounter;
				frameCounterAccumulator -= TicksPerSecond;
				frameCounter = 0;
			}
		}
	}
}