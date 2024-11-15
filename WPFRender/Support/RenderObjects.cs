using System.Windows.Controls;
using System.Windows.Media;

namespace WPFRender;

public class RectangleObject : RenderObject
{
    public RectangleGeometry? Rectangle { get; set; }
}

public class ImageObject : RenderObject
{
    public Image? Image { get; set; }
}

public class RenderObject
{
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double SpeedX { get; set; }
    public double SpeedY { get; set; }
    public double Size { get; set; }
}
