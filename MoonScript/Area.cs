using System.Collections.Generic;

namespace MoonScript
{
    public class Area
    {
        public List<Point> Points { get; set; }

        public Area()
        {
            this.Points = new List<Point>();
        }
    }

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}