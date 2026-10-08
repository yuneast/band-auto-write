namespace BandProgram.Tests;

[Collection("Serial")]
public class WorkControlTests
{
    // 작업 스레드가 확인 지점을 계속 지나가며 카운터를 올린다.
    private static Thread StartCounter(WorkControl control, Counter counter)
    {
        return control.Start(() =>
        {
            while (true)
            {
                counter.Increment();
                WorkControl.Checkpoint();
                Thread.Sleep(5);
            }
        });
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 3000)
    {
        DateTime end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > end) throw new TimeoutException("condition not met");
            Thread.Sleep(10);
        }
    }

    private static bool IsFrozen(Counter counter)
    {
        Thread.Sleep(100); // 확인 지점에 도달할 시간
        int before = counter.Value;
        Thread.Sleep(300);
        return counter.Value == before;
    }

    [Fact]
    public void Pause_blocks_and_Resume_continues()
    {
        var control = new WorkControl();
        var counter = new Counter();
        StartCounter(control, counter);
        WaitUntil(() => counter.Value > 3);

        control.Pause();
        Assert.True(control.IsPaused);
        Assert.True(IsFrozen(counter));

        control.Resume();
        int afterResume = counter.Value;
        WaitUntil(() => counter.Value > afterResume + 3);
        control.Reset();
    }

    [Fact]
    public void Reset_parks_old_work_and_new_work_runs()
    {
        var control = new WorkControl();
        var oldCounter = new Counter();
        StartCounter(control, oldCounter);
        WaitUntil(() => oldCounter.Value > 3);

        control.Reset();
        Assert.True(IsFrozen(oldCounter));

        var newCounter = new Counter();
        StartCounter(control, newCounter);
        WaitUntil(() => newCounter.Value > 3);
        Assert.True(IsFrozen(oldCounter));
        control.Reset();
    }

    [Fact]
    public void Reset_while_paused_parks_old_work()
    {
        var control = new WorkControl();
        var counter = new Counter();
        StartCounter(control, counter);
        WaitUntil(() => counter.Value > 3);

        control.Pause();
        control.Reset();
        Assert.False(control.IsPaused);
        Assert.True(IsFrozen(counter));

        control.Resume();
        Assert.True(IsFrozen(counter));
    }

    [Fact]
    public void Instances_are_independent()
    {
        // 포스팅을 일시정지한 채 댓글을 시작해도, 재개하면 포스팅이 이어져야 한다(기존 동작).
        var posting = new WorkControl();
        var comment = new WorkControl();
        var postingCounter = new Counter();
        var commentCounter = new Counter();
        StartCounter(posting, postingCounter);
        WaitUntil(() => postingCounter.Value > 3);

        posting.Pause();
        StartCounter(comment, commentCounter);
        WaitUntil(() => commentCounter.Value > 3);
        Assert.True(IsFrozen(postingCounter));

        posting.Resume();
        int afterResume = postingCounter.Value;
        WaitUntil(() => postingCounter.Value > afterResume + 3);
        posting.Reset();
        comment.Reset();
    }

    [Fact]
    public void Unregistered_thread_is_not_affected()
    {
        var control = new WorkControl();
        control.Pause();
        var task = Task.Run(() =>
        {
            WorkControl.Checkpoint();
            Util.getInstance().delay(50);
        });
        Assert.True(task.Wait(2000));
        control.Reset();
    }

    [Fact]
    public void Pause_inside_long_delay_stops_promptly()
    {
        var control = new WorkControl();
        int finished = 0;
        control.Start(() =>
        {
            Util.getInstance().delay(600);
            Volatile.Write(ref finished, 1);
        });
        Thread.Sleep(50);
        control.Pause();
        Thread.Sleep(1000); // delay(600)이 끝났을 시간
        Assert.Equal(0, Volatile.Read(ref finished));

        control.Resume();
        WaitUntil(() => Volatile.Read(ref finished) == 1);
        control.Reset();
    }

    [Fact]
    public void Delay_still_waits_the_requested_time()
    {
        var control = new WorkControl();
        long elapsed = 0;
        Thread t = control.Start(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Util.getInstance().delay(450);
            elapsed = sw.ElapsedMilliseconds;
        });
        Assert.True(t.Join(3000));
        Assert.InRange(elapsed, 440, 1500);
    }

    [Fact]
    public void Started_threads_are_background()
    {
        var control = new WorkControl();
        Thread t = control.Start(() => { });
        Assert.True(t.IsBackground);
        t.Join();
    }

    private sealed class Counter
    {
        private int value;
        public int Value => Volatile.Read(ref value);
        public void Increment() => Interlocked.Increment(ref value);
    }
}
