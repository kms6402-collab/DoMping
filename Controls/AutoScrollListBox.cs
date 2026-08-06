using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DoMping.Controls;

public class AutoScrollListBox : ListBox
{
	private bool IsAutoScrollEnabled = true;

	private AdornerLayer _adornerLayer;

	private AutoScrollAdorner _autoScrollAdorner;

	public AutoScrollListBox()
	{
		base.Loaded += ListBox_Loaded;
	}

	private void ListBox_Loaded(object sender, RoutedEventArgs e)
	{
		if (base.Items.Count > 1)
		{
			ScrollIntoView(base.Items[base.Items.Count - 1]);
		}
		if (VisualTreeHelper.GetChildrenCount(this) > 0 && VisualTreeHelper.GetChild(this, 0) is Decorator decorator)
		{
			ScrollViewer obj = decorator.Child as ScrollViewer;
			obj.LostMouseCapture += Scroll_LostMouseCapture;
			obj.ScrollChanged += Scroll_ScrollChanged;
		}
		_adornerLayer = AdornerLayer.GetAdornerLayer(this);
		_autoScrollAdorner = new AutoScrollAdorner(this);
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		if (_adornerLayer != null && _autoScrollAdorner != null && VisualTreeHelper.GetChildrenCount(this) > 0 && VisualTreeHelper.GetChild(this, 0) is Decorator decorator)
		{
			ScrollViewer scrollViewer = decorator.Child as ScrollViewer;
			if (scrollViewer.ComputedVerticalScrollBarVisibility != Visibility.Visible)
			{
				IsAutoScrollEnabled = true;
				_adornerLayer.Remove(_autoScrollAdorner);
			}
			if (IsAutoScrollEnabled && !scrollViewer.IsMouseCaptureWithin)
			{
				scrollViewer?.ScrollToEnd();
			}
		}
		base.OnRender(drawingContext);
	}

	private void Scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight || scrollViewer.ActualHeight < 11.0)
		{
			_adornerLayer.Remove(_autoScrollAdorner);
		}
		else if (_adornerLayer.GetAdorners(this) == null)
		{
			_adornerLayer.Add(_autoScrollAdorner);
		}
	}

	private void Scroll_LostMouseCapture(object sender, MouseEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		IsAutoScrollEnabled = scrollViewer.VerticalOffset >= ((scrollViewer.ScrollableHeight > 100.0) ? (scrollViewer.ScrollableHeight - 2.0) : (scrollViewer.ScrollableHeight - 1.0));
	}

	protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
	{
		base.OnItemsChanged(e);
		if (e.Action == NotifyCollectionChangedAction.Add && IsAutoScrollEnabled && VisualTreeHelper.GetChildrenCount(this) > 0 && VisualTreeHelper.GetChild(this, 0) is Decorator decorator)
		{
			ScrollViewer scrollViewer = decorator.Child as ScrollViewer;
			if (!scrollViewer.IsMouseCaptureWithin)
			{
				scrollViewer?.ScrollToEnd();
			}
		}
	}
}
