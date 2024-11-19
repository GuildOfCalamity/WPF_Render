using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WPFRender;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.rectanglegeometry?view=windowsdesktop-8.0
/// </summary>
public class RectangleObject : RenderObject
{
    public RectangleGeometry? Rectangle { get; set; }
}

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.linegeometry?view=windowsdesktop-8.0
/// </summary>
public class LineObject : RenderObject
{
    public LineGeometry? Line { get; set; }
}

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/drawing-objects-overview?view=netframeworkdesktop-4.8
/// </summary>
public class GeometryObject : RenderObject
{
    public GeometryDrawing? Drawing { get; set; }
}

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.image?view=windowsdesktop-8.0
/// </summary>
public class ImageObject : RenderObject
{
    public Image? Image { get; set; }
}

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imagebrush?view=windowsdesktop-8.0
/// </summary>
public class ImageBrushObject : RenderObject
{
    public Rectangle? Rectangle { get; set; } // Fill prop will be an ImageBrush
}

/// <summary>
/// Similar to the <see cref="ImageObject"/>, but this class wraps a <see cref="DrawingGroup"/> inside a <see cref="TranslateTransform"/>.
/// </summary>
public class TransformObject : RenderObject
{
    public Image? WrappedImage { get; set; }
}

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/path-markup-syntax?view=netframeworkdesktop-4.8
/// </summary>
public class PathObject : RenderObject
{
    public Path? PathData { get; set; } // generic

    // Example Usage:
    //PathGeometry pg = new PathGeometry();
    //Geometry? g = PathGeometry.Parse("M8.625,0.5 C1.6875,10.5 2.6875,12.040001 1.5625,9.3525009 C0.4375,5.6650009 1.175,7.8525 0.5,8.6275");
    //pg.AddGeometry(g);
    //Path path = new Path { Data = pg };
}

/// <summary>
/// Superclass for inheritance
/// </summary>
public class RenderObject
{
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double SpeedX { get; set; }
    public double SpeedY { get; set; }
    public double Size { get; set; }
}
