using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace DoMping.Controls;

internal class AutoScrollAdorner : Adorner
{
	public AutoScrollAdorner(UIElement adornedElement)
		: base(adornedElement)
	{
		base.IsHitTestVisible = false;
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		Rect rectangle = new Rect(base.AdornedElement.RenderSize.Width - 36.0, base.AdornedElement.RenderSize.Height - 14.0, 13.0, 11.0);
		drawingContext.DrawImage((DrawingImage)Application.Current.Resources["icon.down-arrow-scroll-indicator"], rectangle);
		base.OnRender(drawingContext);
	}
}
