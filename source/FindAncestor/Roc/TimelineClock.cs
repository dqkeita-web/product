using System.Diagnostics;

namespace FindAncestor.Roc
{
    public class TimelineClock
    {
        private readonly Stopwatch _sw = new();

        public void Start() => _sw.Start();
        public void Stop() => _sw.Stop();
        public void Reset() => _sw.Reset();

        public double CurrentTime => _sw.Elapsed.TotalSeconds;
    }
}