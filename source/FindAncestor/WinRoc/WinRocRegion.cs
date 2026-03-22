namespace FindAncestor.WinRoc
{
    public struct WinRocRegion
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public bool IsValid => Width > 0 && Height > 0;
    }
}