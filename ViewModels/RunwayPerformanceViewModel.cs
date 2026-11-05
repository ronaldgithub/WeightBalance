using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using WeightBalance.Models;

namespace WeightBalance.ViewModels;

public sealed record PerformanceBreakdownRow(string Label, string Distance, string Run);
public sealed record PerformanceTableCell(string Text, bool IsHeader, bool IsUsed);

// Backs both the Takeoff and the Landing tab.
public partial class RunwayPerformanceViewModel : ObservableObject
{
    private readonly RunwayPerformance _performance;

    public RunwayPerformanceViewModel(RunwayPerformance performance, string title, string conditions,
                                      string runLabel, string distanceLabel)
    {
        _performance = performance;
        _surface = performance.Surfaces[0];
        Title = title;
        Conditions = conditions;
        RunLabel = runLabel;
        DistanceLabel = distanceLabel;
    }

    // Inputs stay as text so an empty or invalid field shows an error rather than
    // silently becoming 0, which would make the distances look better than they are.
    [ObservableProperty] private string _pressureAltitudeText = "0";
    [ObservableProperty] private string _oatText              = "15";
    [ObservableProperty] private string _windText             = "0";
    [ObservableProperty] private string _slopeText            = "0";
    [ObservableProperty] private RunwaySurface _surface;

    // Set by MainViewModel from the Weight & Balance totals.
    [ObservableProperty] private string _massText    = "";
    [ObservableProperty] private string _massWarning = "";

    partial void OnPressureAltitudeTextChanged(string value) => NotifyOutputs();
    partial void OnOatTextChanged(string value)              => NotifyOutputs();
    partial void OnWindTextChanged(string value)             => NotifyOutputs();
    partial void OnSlopeTextChanged(string value)            => NotifyOutputs();
    partial void OnSurfaceChanged(RunwaySurface value)       => NotifyOutputs();

    public string Title { get; }
    public string Conditions { get; }
    public string RunLabel { get; }
    public string DistanceLabel { get; }
    public IReadOnlyList<RunwaySurface> Surfaces => _performance.Surfaces;

    public string RunNote =>
        Invariant($"The run is an estimate: the POH table covers only the distance over 15 m, so the {_performance.BaselineRun:0} m run is scaled by {_performance.BaselineRun:0}/{_performance.BaselineDistance:0}.");

    public string Error { get { Evaluate(out var error); return error ?? ""; } }

    public string RunText      => FormatMetres(Evaluate(out _)?.Run);
    public string DistanceText => FormatMetres(Evaluate(out _)?.Distance);

    public string TableNote =>
        Evaluate(out _)?.UsesUpperBound == true
            ? Invariant($"The POH prints \"<{_performance.BaselineDistance:0}\" for these conditions. {_performance.BaselineDistance:0} m is used, so the true figure may be lower.")
            : "";

    public IReadOnlyList<PerformanceBreakdownRow> Breakdown
    {
        get
        {
            if (Evaluate(out _) is not { } r) return [];

            double surfaceDistance = r.TableDistance * (r.SurfaceFactor - 1);
            double surfaceRun      = r.TableRun      * (r.SurfaceFactor - 1);
            double wind  = NumberInput.Parse(WindText)  ?? 0;
            double slope = NumberInput.Parse(SlopeText) ?? 0;

            string windLabel = wind >= 0
                ? Invariant($"Headwind {wind:0.##} kt (−{RunwayPerformance.HeadwindPerKnot:0} m/kt)")
                : Invariant($"Tailwind {-wind:0.##} kt (+{RunwayPerformance.TailwindPerKnot:0} m/kt)");

            double slopeRate = slope >= 0 ? _performance.SlopePerDegreeUpslope : -_performance.SlopePerDegreeUpslope;
            string slopeLabel = Invariant($"{(slope >= 0 ? "Upslope" : "Downslope")} {Math.Abs(slope):0.##}° ({slopeRate:+0;−0} m/°)");

            return
            [
                new(Invariant($"POH table at {NumberInput.Parse(PressureAltitudeText):0.##} ft, {NumberInput.Parse(OatText):0.##} °C"),
                    r.TableDistance.ToString("F0", CultureInfo.InvariantCulture),
                    r.TableRun.ToString("F0", CultureInfo.InvariantCulture)),
                new(Invariant($"{Surface.Name} (×{r.SurfaceFactor:0.0})"), Signed(surfaceDistance), Signed(surfaceRun)),
                new(windLabel,  Signed(r.WindCorrection),  Signed(r.WindCorrection)),
                new(slopeLabel, Signed(r.SlopeCorrection), Signed(r.SlopeCorrection)),
            ];
        }
    }

    // The POH table in reading order, with its row and column headers, for a 15-column grid.
    public IReadOnlyList<PerformanceTableCell> TableCells
    {
        get
        {
            var used = NumberInput.Parse(PressureAltitudeText) is { } hp && NumberInput.Parse(OatText) is { } oat
                ? RunwayPerformance.ContributingCells(hp, oat)
                : [];

            var cells = new List<PerformanceTableCell> { new("Hp", true, false) };
            cells.AddRange(RunwayPerformance.OatAxis.Select(t => new PerformanceTableCell(Invariant($"{t:0}"), true, false)));

            for (int row = 0; row < RunwayPerformance.HpAxis.Length; row++)
            {
                cells.Add(new(Invariant($"{RunwayPerformance.HpAxis[row]:0}"), true, false));
                for (int col = 0; col < RunwayPerformance.OatAxis.Length; col++)
                {
                    int r = row, c = col;
                    cells.Add(new(_performance.CellText(row, col), false, used.Any(u => u.Row == r && u.Col == c)));
                }
            }
            return cells;
        }
    }

    private PerformanceResult? Evaluate(out string? error)
    {
        double? hp    = NumberInput.Parse(PressureAltitudeText);
        double? oat   = NumberInput.Parse(OatText);
        double? wind  = NumberInput.Parse(WindText);
        double? slope = NumberInput.Parse(SlopeText);

        error = hp    == null ? InvalidNumber("Pressure altitude")
              : oat   == null ? InvalidNumber("OAT")
              : wind  == null ? InvalidNumber("Wind")
              : slope == null ? InvalidNumber("Slope")
              : null;
        if (error != null) return null;

        return _performance.Calculate(hp!.Value, oat!.Value, Surface, wind!.Value, slope!.Value, out error);
    }

    private static string InvalidNumber(string field) => $"{field}: enter a number, using a dot for decimals.";

    private static string FormatMetres(double? metres) =>
        metres is { } m ? m.ToString("F0", CultureInfo.InvariantCulture) + " m" : "—";

    private static string Signed(double metres) =>
        metres.ToString("+0;−0;0", CultureInfo.InvariantCulture);

    private static string Invariant(FormattableString text) => FormattableString.Invariant(text);

    private void NotifyOutputs()
    {
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(RunText));
        OnPropertyChanged(nameof(DistanceText));
        OnPropertyChanged(nameof(TableNote));
        OnPropertyChanged(nameof(Breakdown));
        OnPropertyChanged(nameof(TableCells));
    }
}
