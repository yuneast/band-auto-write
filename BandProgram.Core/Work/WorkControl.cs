using System;
using System.Threading;

namespace BandProgram
{
	// Thread.Suspend/Resume/Abort 대체. 작업 스레드는 Util.delay()의 확인 지점에서 멈춘다.
	// 작업 종류(포스팅/댓글/채팅/가입)마다 인스턴스를 하나씩 둔다.
	public sealed class WorkControl
	{
		internal const int SliceMs = 200;

		[ThreadStatic]
		private static WorkControl current;

		[ThreadStatic]
		private static int currentGeneration;

		private readonly object sync = new object();
		private readonly ManualResetEventSlim gate = new ManualResetEventSlim(true);
		private int generation;

		public bool IsPaused
		{
			get { return !this.gate.IsSet; }
		}

		public Thread Start(Action work)
		{
			int gen;
			lock (this.sync)
			{
				this.generation++;
				gen = this.generation;
				this.gate.Set();
			}
			Thread thread = new Thread(() =>
			{
				current = this;
				currentGeneration = gen;
				work();
			});
			thread.IsBackground = true;
			thread.Start();
			return thread;
		}

		public void Pause()
		{
			this.gate.Reset();
		}

		public void Resume()
		{
			this.gate.Set();
		}

		// 기존 Abort 자리. 진행 중인 작업은 다음 확인 지점에서 영구히 멈춘다.
		// 예외를 던지지 않는 이유: 흐름 코드의 catch {}가 삼키고 다음 단계를 실행할 수 있다.
		public void Reset()
		{
			lock (this.sync)
			{
				this.generation++;
				this.gate.Set();
			}
		}

		public static void Checkpoint()
		{
			WorkControl control = current;
			if (control == null)
			{
				return;
			}
			control.Wait(currentGeneration);
		}

		private void Wait(int gen)
		{
			while (true)
			{
				if (gen != Volatile.Read(ref this.generation))
				{
					Thread.Sleep(Timeout.Infinite);
				}
				this.gate.Wait();
				if (gen == Volatile.Read(ref this.generation))
				{
					return;
				}
			}
		}
	}
}
