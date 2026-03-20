using FindAncestor.ViewModels;

namespace FindAncestor.Roc
{
    public class FrameEngine
    {
        private double _lastTime;
        private double _scrollPos;

        public double ScrollSpeed { get; set; }
        public void SetPosition(double pos)
        {
            _scrollPos = pos;
        }
        public double Update(double currentTime)
        {
            double delta = currentTime - _lastTime;

            _scrollPos += ScrollSpeed * delta;

            _lastTime = currentTime;

            return _scrollPos;
        }
    }
}