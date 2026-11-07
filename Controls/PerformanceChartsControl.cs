using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WeightBalance.ViewModels;

namespace WeightBalance.Controls;

// Two charts for one Takeoff/Landing tab, drawn to the same metre scale:
// 1. a side view of the run and the distance over the 15 m obstacle,
// 2. stacked bars showing how table value, surface, wind and slope add up.
// Takeoff reads left to right from brakes-off. Landing is mirrored: the obstacle is on
// the left and the aircraft rolls out to the right.
public class PerformanceChartsControl : Control
{
    public static readonly StyledProperty<PerformanceChartData?> DataProperty =
        AvaloniaProperty.Register<PerformanceChartsControl, PerformanceChartData?>(nameof(Data));

    public PerformanceChartData? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    static PerformanceChartsControl() => AffectsRender<PerformanceChartsControl>(DataProperty);

    private static readonly IBrush PanelBrush  = new SolidColorBrush(Color.FromRgb(20, 24, 32));
    private static readonly IBrush TextBrush   = new SolidColorBrush(Color.FromRgb(214, 220, 228));
    private static readonly IBrush MutedBrush  = new SolidColorBrush(Color.FromRgb(140, 150, 162));
    private static readonly IBrush DarkText    = new SolidColorBrush(Color.FromRgb(16, 21, 28));
    private static readonly IBrush BaseBrush   = new SolidColorBrush(Color.FromRgb(106, 160, 216));
    private static readonly IBrush SurfBrush   = new SolidColorBrush(Color.FromRgb(108, 195, 131));
    private static readonly IBrush WindBrush   = new SolidColorBrush(Color.FromRgb(240, 162, 88));
    private static readonly IBrush SlopeBrush  = new SolidColorBrush(Color.FromRgb(184, 142, 224));
    private static readonly IBrush GroundBrush = new SolidColorBrush(Color.FromRgb(108, 122, 137));

    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.FromRgb(51, 65, 79)), 1);
    private static readonly Pen GroundPen = new(GroundBrush, 2);
    private static readonly Pen GridPen   = new(new SolidColorBrush(Color.FromRgb(45, 55, 68)), 1);
    private static readonly Pen DimPen    = new(MutedBrush, 1);
    private static readonly Pen PathPen   = new(BaseBrush, 2.5);
    private static readonly Pen PostPen   = new(WindBrush, 3);
    private static readonly Pen SegEdge   = new(new SolidColorBrush(Color.FromRgb(20, 24, 32)), 1);

    // Layout, in device-independent pixels from the top of the control.
    private const double X0 = 72;            // metre 0
    private const double RightMargin = 52;   // room for the "834 m" end labels
    private const double Ground = 108;       // ground line of the side view
    private const double ObstacleHeight = 40; // drawn height, not to scale
    private const double Row1 = 196, Row2 = 230, RowHeight = 22;
    private const double LegendY = 262, AxisY = 284;

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width;
        dc.DrawRectangle(PanelBrush, BorderPen, new Rect(0.5, 0.5, w - 1, Bounds.Height - 1), 4, 4);

        if (Data is not { } d)
        {
            var msg = Text("Enter valid inputs to see the charts", 11, MutedBrush);
            dc.DrawText(msg, new Point((w - msg.Width) / 2, Bounds.Height / 2 - 8));
            return;
        }

        double plotW = Math.Max(50, w - X0 - RightMargin);
        var segsDistance = BuildSegments(d.TableDistance, d.SurfaceDistance, d, out double peakDistance);
        var segsRun      = BuildSegments(d.TableRun,      d.SurfaceRun,      d, out double peakRun);

        double extent = Math.Max(Math.Max(peakDistance, peakRun), Math.Max(d.Distance, 1));
        double axisMax = Math.Max(500, Math.Ceiling(extent / 100) * 100);
        double scale = plotW / axisMax;

        DrawProfile(dc, d, scale);
        DrawBars(dc, d, scale, axisMax, plotW, segsDistance, segsRun);
    }

    // ── Chart 1: side view ───────────────────────────────────────────────
    private void DrawProfile(DrawingContext dc, PerformanceChartData d, double scale)
    {
        dc.DrawText(Text("Profile, horizontal scale in metres", 11, TextBrush, bold: true), new Point(8, 6));

        // Position along the runway in takeoff terms (0 = brakes off, Distance = obstacle).
        // Landing is mirrored so the obstacle sits on the left.
        double X(double m) => X0 + (d.IsLanding ? d.Distance - m : m) * scale;

        double xStart = X(0), xLift = X(d.Run), xObst = X(d.Distance);
        double span = d.Distance - d.Run;

        dc.DrawLine(GroundPen, new Point(X0 - 8, Ground), new Point(Bounds.Width - RightMargin + 8, Ground));

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(xStart, Ground), false);
            ctx.LineTo(new Point(xLift, Ground));
            ctx.CubicBezierTo(
                new Point(X(d.Run + 0.40 * span), Ground),
                new Point(X(d.Run + 0.69 * span), Ground - 0.53 * ObstacleHeight),
                new Point(xObst, Ground - ObstacleHeight));
            ctx.EndFigure(false);
        }
        dc.DrawGeometry(null, PathPen, geo);

        dc.DrawLine(PostPen, new Point(xObst, Ground), new Point(xObst, Ground - ObstacleHeight));
        var obstacleLabel = Text("15 m (50 ft) obstacle", 9.5, WindBrush, bold: true);
        dc.DrawText(obstacleLabel, new Point(d.IsLanding ? xObst - 2 : xObst + 2 - obstacleLabel.Width,
                                             Ground - ObstacleHeight - 15));

        dc.DrawEllipse(BaseBrush, null, new Point(xStart, Ground), 3, 3);
        dc.DrawEllipse(PanelBrush, new Pen(BaseBrush, 2), new Point(xLift, Ground), 3.5, 3.5);

        // Point labels sit above the ground line, on the outer side of each point.
        string startText = d.IsLanding ? "stop" : "brakes off";
        string liftText  = d.IsLanding ? "touchdown" : "lift-off";
        var startLabel = Text(startText, 9.5, MutedBrush);
        var liftLabel  = Text(liftText,  9.5, MutedBrush);
        double ly = Ground - 17;
        dc.DrawText(startLabel, new Point(d.IsLanding ? xStart - startLabel.Width : xStart, ly));
        dc.DrawText(liftLabel,  new Point(d.IsLanding ? xLift : xLift - liftLabel.Width, ly));

        // Dimension lines under the ground.
        string runWord = d.IsLanding ? "Landing run (estimate)" : "Takeoff run (estimate)";
        DrawDimension(dc, Math.Min(xStart, xLift), Math.Max(xStart, xLift), Ground + 14,
                      $"{runWord} {Metres(d.Run)} m");
        DrawDimension(dc, Math.Min(xStart, xObst), Math.Max(xStart, xObst), Ground + 40,
                      $"Distance over 15 m obstacle {Metres(d.Distance)} m");
    }

    private static void DrawDimension(DrawingContext dc, double x1, double x2, double y, string label)
    {
        dc.DrawLine(DimPen, new Point(x1, y), new Point(x2, y));
        dc.DrawLine(DimPen, new Point(x1, y - 4), new Point(x1, y + 4));
        dc.DrawLine(DimPen, new Point(x2, y - 4), new Point(x2, y + 4));
        var text = Text(label, 10, TextBrush, bold: true);
        dc.DrawText(text, new Point((x1 + x2) / 2 - text.Width / 2, y + 4));
    }

    // ── Chart 2: stacked bars ────────────────────────────────────────────
    private sealed record Segment(double From, double To, IBrush Brush, string Label);

    private static List<Segment> BuildSegments(double table, double surface, PerformanceChartData d, out double peak)
    {
        var list = new List<Segment>();
        double cum = 0;
        double highest = 0;

        void Add(double value, IBrush brush)
        {
            if (Math.Abs(value) < 0.05) return;
            double from = value >= 0 ? cum : cum + value;
            double to   = value >= 0 ? cum + value : cum;
            list.Add(new Segment(from, to, brush, value.ToString(list.Count == 0 ? "0" : "+0;−0", CultureInfo.InvariantCulture)));
            cum += value;
            highest = Math.Max(highest, to);
        }

        Add(table, BaseBrush);
        Add(surface, SurfBrush);
        Add(d.Wind, WindBrush);
        Add(d.Slope, SlopeBrush);
        peak = highest;
        return list;
    }

    private void DrawBars(DrawingContext dc, PerformanceChartData d, double scale, double axisMax, double plotW,
                          List<Segment> distance, List<Segment> run)
    {
        dc.DrawText(Text("How the numbers add up (m)", 11, TextBrush, bold: true), new Point(8, Ground + 68));

        for (double m = 0; m <= axisMax + 0.1; m += axisMax <= 600 ? 100 : 200)
        {
            double x = X0 + m * scale;
            dc.DrawLine(GridPen, new Point(x, Row1 - 8), new Point(x, AxisY));
            var t = Text(m.ToString("0", CultureInfo.InvariantCulture), 9.5, MutedBrush);
            dc.DrawText(t, new Point(x - t.Width / 2, AxisY + 4));
        }
        dc.DrawLine(DimPen, new Point(X0, AxisY), new Point(X0 + plotW, AxisY));

        DrawRow(dc, Row1, "Distance", "over 15 m", d.Distance, distance, scale);
        DrawRow(dc, Row2, "Run", "estimate", d.Run, run, scale);

        // Legend: names only; the values are inside the bars and in the table on the left.
        double x0 = 8;
        foreach (var (name, brush) in new[]
                 {
                     ("POH table", BaseBrush), (d.SurfaceLabel, SurfBrush), ("Wind", WindBrush), ("Slope", SlopeBrush),
                 })
        {
            dc.DrawRectangle(brush, null, new Rect(x0, LegendY + 2, 9, 9), 2, 2);
            var t = Text(name, 9.5, MutedBrush);
            dc.DrawText(t, new Point(x0 + 13, LegendY));
            x0 += 13 + t.Width + 14;
        }
    }

    private static void DrawRow(DrawingContext dc, double y, string name, string sub, double total,
                                List<Segment> segments, double scale)
    {
        var n = Text(name, 10.5, TextBrush, bold: true);
        var s = Text(sub, 9, MutedBrush);
        dc.DrawText(n, new Point(X0 - 8 - n.Width, y + 1));
        dc.DrawText(s, new Point(X0 - 8 - s.Width, y + 13));

        foreach (var seg in segments)
        {
            var rect = new Rect(X0 + seg.From * scale, y, Math.Max(1, (seg.To - seg.From) * scale), RowHeight);
            dc.DrawRectangle(seg.Brush, SegEdge, rect);
            var label = Text(seg.Label, 9.5, DarkText, bold: true);
            if (rect.Width >= label.Width + 8)
                dc.DrawText(label, new Point(rect.X + (rect.Width - label.Width) / 2, y + (RowHeight - label.Height) / 2));
        }

        var end = Text($"{Metres(total)} m", 10.5, TextBrush, bold: true);
        double right = segments.Count == 0 ? X0 : X0 + Math.Max(total, segments[^1].To) * scale;
        dc.DrawText(end, new Point(right + 6, y + (RowHeight - end.Height) / 2));
    }

    private static string Metres(double m) => m.ToString("0", CultureInfo.InvariantCulture);

    private static FormattedText Text(string text, double size, IBrush brush, bool bold = false) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            bold ? new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold) : Typeface.Default,
            size, brush);
}
