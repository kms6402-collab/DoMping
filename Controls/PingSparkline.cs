using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DoMping.Controls;

public class PingSparkline : FrameworkElement
{
	public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register("Values", typeof(System.Collections.ObjectModel.ObservableCollection<double>), typeof(PingSparkline), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnValuesChanged));

	private const double ScaleMs = 200.0;

	private const double TopPadding = 10.0;

	private const double BottomPadding = 4.0;

	private const double LeftAxisWidth = 34.0;

	private const double BottomAxisHeight = 14.0;

	private const double AxisFontSize = 9.0;

	private static readonly Pen LinePen;

	private static readonly Pen SlowPen;

	private static readonly Pen LossPen;

	private static readonly Brush LineBrush;

	private static readonly Brush SlowBrush;

	private static readonly Brush LossBrush;

	private static readonly Brush AreaFillBrush;

	private static readonly Pen GridPen;

	private static readonly Brush AxisTextBrush;

	private static readonly Typeface AxisTypeface;

	public System.Collections.ObjectModel.ObservableCollection<double> Values
	{
		get
		{
			return (System.Collections.ObjectModel.ObservableCollection<double>)GetValue(ValuesProperty);
		}
		set
		{
			SetValue(ValuesProperty, value);
		}
	}

	static PingSparkline()
	{
		LineBrush = FreezeBrush("#33ff99");
		SlowBrush = FreezeBrush("#ffcf5c");
		LossBrush = FreezeBrush("#ff5d5d");
		LinePen = FreezePen(LineBrush, 2.0);
		SlowPen = FreezePen(SlowBrush, 2.0);
		LossPen = FreezePen(LossBrush, 2.0);
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
		linearGradientBrush.StartPoint = new Point(0.0, 0.0);
		linearGradientBrush.EndPoint = new Point(0.0, 1.0);
		linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(70, 51, 255, 153), 0.0));
		linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 51, 255, 153), 1.0));
		linearGradientBrush.Freeze();
		AreaFillBrush = linearGradientBrush;
		GridPen = FreezePen(FreezeBrush("#223028"), 1.0);
		AxisTextBrush = FreezeBrush("#5f7d6c");
		AxisTypeface = new Typeface("Cascadia Mono, Consolas, Courier New");
	}

	private static Brush FreezeBrush(string hex)
	{
		Brush brush = (Brush)new BrushConverter().ConvertFromString(hex);
		brush.Freeze();
		return brush;
	}

	private static Pen FreezePen(Brush brush, double thickness)
	{
		Pen pen = new Pen(brush, thickness)
		{
			StartLineCap = PenLineCap.Round,
			EndLineCap = PenLineCap.Round,
			LineJoin = PenLineJoin.Round
		};
		pen.Freeze();
		return pen;
	}

	private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		PingSparkline pingSparkline = (PingSparkline)d;
		if (e.OldValue is INotifyCollectionChanged notifyCollectionChanged)
		{
			notifyCollectionChanged.CollectionChanged -= pingSparkline.OnCollectionChanged;
		}
		if (e.NewValue is INotifyCollectionChanged notifyCollectionChanged2)
		{
			notifyCollectionChanged2.CollectionChanged += pingSparkline.OnCollectionChanged;
		}
		pingSparkline.InvalidateVisual();
	}

	private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		InvalidateVisual();
	}

	private FormattedText MakeText(string text)
	{
		return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, AxisTypeface, AxisFontSize, AxisTextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
	}

	protected override void OnRender(DrawingContext dc)
	{
		double actualWidth = base.ActualWidth;
		double actualHeight = base.ActualHeight;
		if (actualWidth <= 0.0 || actualHeight <= 0.0)
		{
			return;
		}
		double plotLeft = LeftAxisWidth;
		double plotRight = actualWidth;
		double plotWidth = Math.Max(0.0, plotRight - plotLeft);
		double plotBottom = actualHeight - BottomAxisHeight;
		double plotTop = TopPadding;
		double usableHeight = Math.Max(1.0, plotBottom - plotTop - BottomPadding);
		dc.DrawLine(GridPen, new Point(plotLeft, plotBottom), new Point(plotRight, plotBottom));
		dc.DrawLine(GridPen, new Point(plotLeft, (plotTop + plotBottom) / 2.0), new Point(plotRight, (plotTop + plotBottom) / 2.0));
		System.Collections.ObjectModel.ObservableCollection<double> values = Values;
		if (values == null || values.Count == 0)
		{
			FormattedText emptyText = MakeText("no data");
			dc.DrawText(emptyText, new Point(plotLeft + (plotWidth - emptyText.Width) / 2.0, (plotTop + plotBottom - emptyText.Height) / 2.0));
			return;
		}
		int count = values.Count;
		double stepX = ((count > 1) ? (plotWidth / (double)(count - 1)) : 0.0);
		double minVal = double.MaxValue;
		double maxVal = double.MinValue;
		for (int m = 0; m < count; m++)
		{
			if (values[m] > 0.0)
			{
				if (values[m] < minVal)
				{
					minVal = values[m];
				}
				if (values[m] > maxVal)
				{
					maxVal = values[m];
				}
			}
		}
		if (minVal > maxVal)
		{
			minVal = 0.0;
			maxVal = 100.0;
		}
		const double minSpan = 8.0;
		if (maxVal - minVal < minSpan)
		{
			double mid = (maxVal + minVal) / 2.0;
			minVal = Math.Max(0.0, mid - minSpan / 2.0);
			maxVal = minVal + minSpan;
		}
		double range = maxVal - minVal;
		Point[] points = new Point[count];
		for (int i = 0; i < count; i++)
		{
			double val = values[i];
			double fraction = ((val <= 0.0) ? 0.0 : Math.Min(Math.Max((val - minVal) / range, 0.0), 1.0));
			double y = plotBottom - BottomPadding - fraction * usableHeight;
			double x = plotLeft + ((count > 1) ? ((double)i * stepX) : (plotWidth / 2.0));
			points[i] = new Point(x, y);
		}
		if (count > 1)
		{
			StreamGeometry streamGeometry = new StreamGeometry();
			using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
			{
				streamGeometryContext.BeginFigure(new Point(points[0].X, plotBottom), isFilled: true, isClosed: true);
				streamGeometryContext.LineTo(points[0], isStroked: false, isSmoothJoin: false);
				for (int j = 1; j < count; j++)
				{
					streamGeometryContext.LineTo(points[j], isStroked: false, isSmoothJoin: false);
				}
				streamGeometryContext.LineTo(new Point(points[count - 1].X, plotBottom), isStroked: false, isSmoothJoin: false);
			}
			streamGeometry.Freeze();
			dc.DrawGeometry(AreaFillBrush, null, streamGeometry);
			for (int k = 1; k < count; k++)
			{
				double prev = values[k - 1];
				double cur = values[k];
				Pen pen = ((cur <= 0.0 || prev <= 0.0) ? LossPen : ((cur >= 150.0) ? SlowPen : LinePen));
				dc.DrawLine(pen, points[k - 1], points[k]);
			}
		}
		for (int l = 0; l < count; l++)
		{
			if (values[l] <= 0.0)
			{
				dc.DrawEllipse(LossBrush, null, new Point(points[l].X, plotBottom), 3.0, 3.0);
			}
		}
		Point lastPoint = points[count - 1];
		double lastVal = values[count - 1];
		Brush endBrush = ((lastVal <= 0.0) ? LossBrush : ((lastVal >= 150.0) ? SlowBrush : LineBrush));
		dc.DrawEllipse(endBrush, null, lastPoint, 3.5, 3.5);
		FormattedText maxLabel = MakeText(Math.Round(maxVal) + "ms");
		dc.DrawText(maxLabel, new Point(2.0, plotTop - maxLabel.Height / 2.0));
		FormattedText minLabel = MakeText(Math.Round(minVal) + "ms");
		dc.DrawText(minLabel, new Point(2.0, plotBottom - minLabel.Height / 2.0));
		FormattedText nowLabel = MakeText("now");
		dc.DrawText(nowLabel, new Point(Math.Max(plotLeft, plotRight - nowLabel.Width), plotBottom + 1.0));
		FormattedText oldestLabel = MakeText("-" + (count - 1));
		dc.DrawText(oldestLabel, new Point(plotLeft, plotBottom + 1.0));
	}
}
