namespace FindAncestor.Models
{
    public class DisplaySize
    {
        public string Name { get; set; } = "";
        public double Width { get; set; }       // 横幅
        public double AspectRatio { get; set; } // 横幅 / 縦幅
        public double Height { get; set; } // 自動計算 => Width / AspectRatio; 
        public override string ToString() => Name;
    }
}