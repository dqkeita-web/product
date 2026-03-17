namespace FindAncestor.Models;

public class DisplaySize
{
    public string Name { get; set; } = "";

    public int Width { get; set; }

    public int Height { get; set; }

    public double AspectRatio
    {
        get
        {
            return (double)Width / Height;
        }
    }
}