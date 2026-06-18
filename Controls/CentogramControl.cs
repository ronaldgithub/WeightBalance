using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WeightBalance.Models;

namespace WeightBalance.Controls;

public class CentogramControl : Control
{
    // ── Styled properties ────────────────────────────────────────────────
    public static readonly StyledProperty<double> TakeoffMassProperty =
        AvaloniaProperty.Register<CentogramControl, double>(nameof(TakeoffMass));
    public static readonly StyledProperty<double> TakeoffCgPctProperty =
        AvaloniaProperty.Register<CentogramControl, double>(nameof(TakeoffCgPct));
    public static readonly StyledProperty<double> LandingMassProperty =
        AvaloniaProperty.Register<CentogramControl, double>(nameof(LandingMass));
    public static readonly StyledProperty<double> LandingCgPctProperty =
        AvaloniaProperty.Register<CentogramControl, double>(nameof(LandingCgPct));

    public double TakeoffMass  { get => GetValue(TakeoffMassProperty);  set => SetValue(TakeoffMassProperty, value); }
    public double TakeoffCgPct { get => GetValue(TakeoffCgPctProperty); set => SetValue(TakeoffCgPctProperty, value); }
    public double LandingMass  { get => GetValue(LandingMassProperty);  set => SetValue(LandingMassProperty, value); }
    public double LandingCgPct { get => GetValue(LandingCgPctProperty); set => SetValue(LandingCgPctProperty, value); }

    static CentogramControl()
    {
        AffectsRender<CentogramControl>(
            TakeoffMassProperty, TakeoffCgPctProperty,
            LandingMassProperty, LandingCgPctProperty);
    }

    // ── Plot margins ─────────────────────────────────────────────────────
    private const double ML = 52, MR = 20, MT = 50, MB = 58;

    private double PW => Bounds.Width  - ML - MR;
    private double PH => Bounds.Height - MT - MB;

    // Coordinate mapping: data space → screen space
    private Point ToScreen(double cgPct, double mass) => new(
        ML + (cgPct - AircraftConstants.ChartMinCgPct)
           / (AircraftConstants.ChartMaxCgPct - AircraftConstants.ChartMinCgPct) * PW,
        MT + (1.0 - (mass - AircraftConstants.ChartMinMass)
                  / (AircraftConstants.ChartMaxMass - AircraftConstants.ChartMinMass)) * PH);

    // ── Brushes / pens ───────────────────────────────────────────────────
    private static readonly IBrush PlotBg        = new SolidColorBrush(Color.FromRgb(22, 22, 34));
    private static readonly IBrush MinorGridBrush = new SolidColorBrush(Color.FromRgb(42, 42, 62));
    private static readonly IBrush MajorGridBrush = new SolidColorBrush(Color.FromRgb(65, 65, 90));
    private static readonly IBrush AxisBrush      = new SolidColorBrush(Color.FromRgb(140, 140, 165));
    private static readonly IBrush EnvFill        = new SolidColorBrush(Color.FromArgb(70, 0, 210, 80));
    private static readonly IBrush EnvStroke      = new SolidColorBrush(Color.FromRgb(0, 200, 80));
    private static readonly IBrush TakeoffBrush   = new SolidColorBrush(Color.FromRgb(0, 200, 210));
    private static readonly IBrush LandingBrush   = new SolidColorBrush(Color.FromRgb(255, 150, 30));
    private static readonly IBrush LabelBrush     = new SolidColorBrush(Color.FromRgb(200, 200, 220));
    private static readonly IBrush HintBrush      = new SolidColorBrush(Color.FromRgb(120, 120, 150));
    private static readonly IBrush OobRingBrush   = new SolidColorBrush(Color.FromRgb(255, 80, 80));
    private static readonly IBrush MtowLineBrush  = new SolidColorBrush(Color.FromArgb(180, 220, 60, 60));

    private static readonly Pen MinorGridPen  = new(MinorGridBrush,  0.5);
    private static readonly Pen MajorGridPen  = new(MajorGridBrush,  1.0);
    private static readonly Pen AxisPen       = new(AxisBrush,       1.5);
    private static readonly Pen EnvPen        = new(EnvStroke,       2.0);
    private static readonly Pen OobPen        = new(OobRingBrush,    1.5);
    private static readonly Pen MtowPen       = new(MtowLineBrush,   1.0,
                                                   new DashStyle([4, 3], 0));
    private static readonly Pen ConnectPen    = new(new SolidColorBrush(Color.FromArgb(160, 80, 80, 80)),
                                                   1.5, new DashStyle([5, 4], 0));

    // ── Rendering entry point ────────────────────────────────────────────
    public override void Render(DrawingContext dc)
    {
        if (Bounds.Width < 150 || Bounds.Height < 150) return;

        DrawBackground(dc);
        DrawGrid(dc);
        DrawEnvelope(dc);
        DrawMtowLine(dc);
        DrawAxesAndLabels(dc);
        DrawMeterMarkers(dc);
        DrawPoints(dc);
        DrawTitle(dc);
        DrawLegend(dc);
    }

    private void DrawBackground(DrawingContext dc)
        => dc.DrawRectangle(PlotBg, null, new Rect(ML, MT, PW, PH));

    private void DrawGrid(DrawingContext dc)
    {
        // Vertical lines (% MAC)
        for (double pct = AircraftConstants.ChartMinCgPct;
             pct <= AircraftConstants.ChartMaxCgPct + 0.001; pct += 1.0)
        {
            bool major = Math.Abs(pct % 5) < 0.01;
            var top    = ToScreen(pct, AircraftConstants.ChartMaxMass);
            var bottom = ToScreen(pct, AircraftConstants.ChartMinMass);
            dc.DrawLine(major ? MajorGridPen : MinorGridPen, top, bottom);
        }

        // Horizontal lines (kg)
        for (double mass = AircraftConstants.ChartMinMass;
             mass <= AircraftConstants.ChartMaxMass + 0.001; mass += 10.0)
        {
            bool major = Math.Abs(mass % 50) < 0.01;
            var left  = ToScreen(AircraftConstants.ChartMinCgPct, mass);
            var right = ToScreen(AircraftConstants.ChartMaxCgPct, mass);
            dc.DrawLine(major ? MajorGridPen : MinorGridPen, left, right);
        }
    }

    private void DrawEnvelope(DrawingContext dc)
    {
        var verts = AircraftConstants.EnvelopeVertices;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(ToScreen(verts[0].CgPct, verts[0].Mass), true);
            for (int i = 1; i < verts.Length; i++)
                ctx.LineTo(ToScreen(verts[i].CgPct, verts[i].Mass));
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(EnvFill, EnvPen, geo);
    }

    private void DrawMtowLine(DrawingContext dc)
    {
        // Dashed red reference line at MTOW
        var left  = ToScreen(AircraftConstants.ChartMinCgPct, AircraftConstants.MaxTakeOffWeight);
        var right = ToScreen(AircraftConstants.ChartMaxCgPct, AircraftConstants.MaxTakeOffWeight);
        dc.DrawLine(MtowPen, left, right);
    }

    private void DrawAxesAndLabels(DrawingContext dc)
    {
        var plotRect = new Rect(ML, MT, PW, PH);

        // Plot border
        dc.DrawRectangle(null, AxisPen, plotRect);

        // Y-axis tick labels (every 50 kg)
        for (double mass = AircraftConstants.ChartMinMass;
             mass <= AircraftConstants.ChartMaxMass + 0.001; mass += 50.0)
        {
            var pt = ToScreen(AircraftConstants.ChartMinCgPct, mass);
            var ft = MakeText(((int)mass).ToString(), 10.5, LabelBrush);
            dc.DrawText(ft, new Point(ML - ft.Width - 5, pt.Y - ft.Height / 2));
        }

        // Y-axis title "(kg)"
        var yTitle = MakeText("(kg)", 10, HintBrush);
        dc.DrawText(yTitle, new Point(ML - yTitle.Width - 4, MT - yTitle.Height - 2));

        // X-axis tick labels (every 5%)
        for (double pct = AircraftConstants.ChartMinCgPct;
             pct <= AircraftConstants.ChartMaxCgPct + 0.001; pct += 5.0)
        {
            var pt = ToScreen(pct, AircraftConstants.ChartMinMass);
            var ft = MakeText($"{(int)pct}%", 10.5, LabelBrush);
            dc.DrawText(ft, new Point(pt.X - ft.Width / 2, pt.Y + 4));
        }

        // X-axis title
        var xTitle = MakeText("% MAC", 11, HintBrush);
        dc.DrawText(xTitle, new Point(ML + PW / 2 - xTitle.Width / 2, MT + PH + 36));
    }

    private void DrawMeterMarkers(DrawingContext dc)
    {
        // 0.876 m at 20% MAC, 1.034 m at 32% MAC (from POH §2.7)
        DrawMeterMark(dc, 20.0, "0.876 m");
        DrawMeterMark(dc, 32.0, "1.034 m");
    }

    private void DrawMeterMark(DrawingContext dc, double cgPct, string label)
    {
        var pt   = ToScreen(cgPct, AircraftConstants.ChartMinMass);
        var ft   = MakeText(label, 9.5, HintBrush);
        double x = pt.X - ft.Width / 2;
        double y = pt.Y + 20;
        // Small tick
        dc.DrawLine(new Pen(HintBrush, 1), new Point(pt.X, pt.Y + 4), new Point(pt.X, y - 2));
        dc.DrawText(ft, new Point(x, y));
    }

    private void DrawTitle(DrawingContext dc)
    {
        var title = MakeText("Max MTOW: 750 kg  /  1650 lbs", 11.5, LabelBrush);
        dc.DrawText(title, new Point(ML + PW / 2 - title.Width / 2, 6));

        var sub = MakeText("Massa (kg) vs CG % MAC — Sonaca S2", 10, HintBrush);
        dc.DrawText(sub, new Point(ML + PW / 2 - sub.Width / 2, 22));
    }

    private void DrawPoints(DrawingContext dc)
    {
        var tkPt = ToScreen(TakeoffCgPct, TakeoffMass);
        var ldPt = ToScreen(LandingCgPct, LandingMass);

        bool tkIn = IsInPlotArea(TakeoffCgPct, TakeoffMass);
        bool ldIn = IsInPlotArea(LandingCgPct, LandingMass);

        // Connecting line (draw first, under dots)
        if (tkIn && ldIn)
            dc.DrawLine(ConnectPen, tkPt, ldPt);

        // Takeoff
        if (tkIn)
        {
            dc.DrawEllipse(TakeoffBrush, null, tkPt, 7, 7);
            if (!IsPointInEnvelope(TakeoffMass, TakeoffCgPct))
                dc.DrawEllipse(null, OobPen, tkPt, 11, 11);

            // Mass label
            var lbl = MakeText($"{TakeoffMass:F0} kg", 9.5, TakeoffBrush);
            dc.DrawText(lbl, new Point(tkPt.X + 10, tkPt.Y - lbl.Height / 2));
        }

        // Landing
        if (ldIn)
        {
            dc.DrawEllipse(LandingBrush, null, ldPt, 7, 7);
            if (!IsPointInEnvelope(LandingMass, LandingCgPct))
                dc.DrawEllipse(null, OobPen, ldPt, 11, 11);

            var lbl = MakeText($"{LandingMass:F0} kg", 9.5, LandingBrush);
            dc.DrawText(lbl, new Point(ldPt.X + 10, ldPt.Y - lbl.Height / 2));
        }
    }

    private void DrawLegend(DrawingContext dc)
    {
        double lx = ML + PW - 120;
        double ly = MT + 8;

        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 28, 28, 45)), null,
            new Rect(lx - 6, ly - 4, 118, 46));

        // Takeoff dot
        dc.DrawEllipse(TakeoffBrush, null, new Point(lx + 6, ly + 8), 5, 5);
        dc.DrawText(MakeText("Takeoff", 10, LabelBrush), new Point(lx + 14, ly + 0));

        // Landing dot
        dc.DrawEllipse(LandingBrush, null, new Point(lx + 6, ly + 28), 5, 5);
        dc.DrawText(MakeText("Landing", 10, LabelBrush), new Point(lx + 14, ly + 20));
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private static FormattedText MakeText(string text, double size, IBrush brush) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            Typeface.Default, size, brush);

    private bool IsInPlotArea(double cgPct, double mass) =>
        cgPct >= AircraftConstants.ChartMinCgPct - 2 &&
        cgPct <= AircraftConstants.ChartMaxCgPct + 2 &&
        mass  >= AircraftConstants.ChartMinMass  - 50 &&
        mass  <= AircraftConstants.ChartMaxMass  + 50;

    private static bool IsPointInEnvelope(double mass, double cgPct)
    {
        var v = AircraftConstants.EnvelopeVertices;
        int n = v.Length;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            bool crosses = (v[i].Mass > mass) != (v[j].Mass > mass) &&
                           cgPct < v[j].CgPct + (v[i].CgPct - v[j].CgPct)
                                   * (mass - v[j].Mass) / (v[i].Mass - v[j].Mass);
            if (crosses) inside = !inside;
        }
        return inside;
    }
}
