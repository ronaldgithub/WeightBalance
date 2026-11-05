using System;

namespace WeightBalance.Models;

public sealed record RunwaySurface(string Name, double Factor)
{
    public override string ToString() => Name;
}

public sealed record PerformanceResult(
    double TableDistance,      // m, over 15 m obstacle, from the Hp/OAT table
    double TableRun,           // m, estimated: TableDistance × baseline run / baseline distance
    bool   UsesUpperBound,     // a contributing table cell is "<baseline"; the baseline was used
    double SurfaceFactor,
    double WindCorrection,     // m, added to both distances
    double SlopeCorrection,    // m, added to both distances
    double Distance,           // m, final distance over 15 m obstacle
    double Run);               // m, final ground run (estimate)

// POH §5.3.6 Take-off and §5.3.10 Landing distance. Both pages use the same table
// layout and the same wind corrections; they differ in the values below.
public sealed class RunwayPerformance
{
    public const double HeadwindPerKnot  = 6.0;   // m subtracted per knot of headwind
    public const double TailwindPerKnot  = 24.0;  // m added per knot of tailwind
    public const double MaxTailwindKnots = 10.0;  // tailwind correction only goes this far

    private static readonly double[] OatAxis =
        [-30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35];

    private static readonly double[] HpAxis =
        [0, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000];

    // A cell the POH prints as "<470" or "<475". The baseline distance is used for it,
    // which is conservative.
    private const double U = 0;

    // POH §5.3.6. Conditions: flaps T/O, full power, rotate at VLOF = 55 KIAS.
    public static readonly RunwayPerformance Takeoff = new(
        baselineRun: 325, baselineDistance: 470,
        slopePerDegreeUpslope: 30,
        surfaces: [new("Concrete", 1.0), new("Dry grass", 1.2), new("Wet grass", 1.3)],
        distanceTable: new double[,]
        {
            // -30  -25  -20  -15  -10   -5    0    5   10   15   20   25   30   35
            {    U,   U,   U,   U,   U,   U,   U,   U,   U, 470, 475, 480, 485, 490 }, // 0 ft
            {    U,   U, 470, 475, 480, 490, 495, 500, 505, 510, 510, 510, 510, 510 }, // 1000 ft
            {  500, 510, 515, 520, 525, 525, 525, 525, 525, 530, 535, 540, 550, 555 }, // 2000 ft
            {  540, 540, 540, 540, 545, 555, 560, 565, 570, 570, 570, 575, 575, 575 }, // 3000 ft
            {  565, 575, 580, 590, 590, 590, 590, 590, 595, 605, 610, 620, 625, 630 }, // 4000 ft
            {  610, 610, 610, 615, 620, 630, 640, 645, 650, 655, 655, 660, 665, 670 }, // 5000 ft
            {  645, 655, 665, 670, 675, 680, 680, 685, 695, 700, 705, 710, 720, 725 }, // 6000 ft
            {  700, 700, 705, 715, 720, 730, 735, 745, 750, 755, 765, 770, 780, 785 }, // 7000 ft
            {  750, 755, 765, 775, 780, 790, 800, 805, 815, 820, 830, 840, 845, 855 }, // 8000 ft
        });

    // POH §5.3.10. Conditions: flaps LD, idle power, approach at 55 KIAS, braked run.
    // Slope works the other way round from takeoff: upslope shortens the landing.
    public static readonly RunwayPerformance Landing = new(
        baselineRun: 225, baselineDistance: 475,
        slopePerDegreeUpslope: -13,
        surfaces: [new("Concrete", 1.0), new("Dry grass", 1.2), new("Wet grass", 1.4)],
        distanceTable: new double[,]
        {
            // -30  -25  -20  -15  -10   -5    0    5   10   15   20   25   30   35
            {    U,   U,   U,   U,   U,   U,   U,   U,   U, 475, 480, 480, 485, 485 }, // 0 ft
            {    U,   U,   U, 475, 480, 480, 480, 485, 485, 490, 495, 495, 500, 500 }, // 1000 ft
            {  475, 480, 485, 485, 490, 495, 495, 500, 505, 505, 510, 515, 515, 520 }, // 2000 ft
            {  490, 495, 500, 505, 505, 510, 515, 515, 520, 525, 525, 530, 535, 535 }, // 3000 ft
            {  510, 510, 515, 520, 525, 525, 530, 535, 535, 540, 545, 545, 550, 555 }, // 4000 ft
            {  525, 530, 535, 535, 540, 545, 550, 550, 555, 560, 560, 565, 570, 570 }, // 5000 ft
            {  545, 545, 550, 555, 560, 560, 565, 570, 575, 575, 580, 585, 590, 590 }, // 6000 ft
            {  560, 565, 570, 575, 575, 580, 585, 590, 595, 595, 600, 605, 605, 610 }, // 7000 ft
            {  580, 585, 590, 595, 595, 600, 605, 610, 615, 615, 620, 625, 630, 630 }, // 8000 ft
        });

    private readonly double[,] _distanceTable;

    private RunwayPerformance(double baselineRun, double baselineDistance, double slopePerDegreeUpslope,
                              RunwaySurface[] surfaces, double[,] distanceTable)
    {
        BaselineRun = baselineRun;
        BaselineDistance = baselineDistance;
        SlopePerDegreeUpslope = slopePerDegreeUpslope;
        Surfaces = surfaces;
        _distanceTable = distanceTable;
    }

    public double BaselineRun { get; }           // m, concrete, 0 ft, 15 °C, zero wind
    public double BaselineDistance { get; }      // m, same conditions, over 15 m (50 ft)
    public double SlopePerDegreeUpslope { get; } // m per degree of upslope; negative = shorter
    public RunwaySurface[] Surfaces { get; }

    /// <summary>
    /// Applies POH corrections: bilinear table lookup, then the surface factor, then
    /// wind and slope. Returns null with a reason when the POH data does not cover the
    /// inputs; nothing is extrapolated.
    /// </summary>
    /// <param name="windKnots">Positive = headwind, negative = tailwind.</param>
    /// <param name="slopeDegrees">Positive = upslope, negative = downslope.</param>
    public PerformanceResult? Calculate(double pressureAltitudeFt, double oatC, RunwaySurface surface,
                                        double windKnots, double slopeDegrees, out string? error)
    {
        error = null;
        if (pressureAltitudeFt < HpAxis[0] || pressureAltitudeFt > HpAxis[^1])
            error = $"Pressure altitude is outside the POH table ({HpAxis[0]:F0} to {HpAxis[^1]:F0} ft).";
        else if (oatC < OatAxis[0] || oatC > OatAxis[^1])
            error = $"OAT is outside the POH table ({OatAxis[0]:F0} to {OatAxis[^1]:F0} °C).";
        else if (-windKnots > MaxTailwindKnots)
            error = $"The POH tailwind correction only goes up to {MaxTailwindKnots:F0} kt.";
        if (error != null) return null;

        double tableDistance = LookUp(pressureAltitudeFt, oatC, out bool usesUpperBound);
        double tableRun      = tableDistance * BaselineRun / BaselineDistance;

        double wind  = windKnots >= 0 ? -windKnots * HeadwindPerKnot : -windKnots * TailwindPerKnot;
        double slope = slopeDegrees * SlopePerDegreeUpslope;

        double distance = tableDistance * surface.Factor + wind + slope;
        double run      = tableRun      * surface.Factor + wind + slope;

        if (run <= 0)
        {
            error = "Wind and slope corrections reduce the ground run to zero. Check the inputs.";
            return null;
        }

        return new PerformanceResult(tableDistance, tableRun, usesUpperBound, surface.Factor,
                                     wind, slope, distance, run);
    }

    private double LookUp(double hp, double oat, out bool usesUpperBound)
    {
        var (row, rowFraction) = Bracket(HpAxis, hp);
        var (col, colFraction) = Bracket(OatAxis, oat);

        double result = 0;
        usesUpperBound = false;
        for (int r = 0; r <= 1; r++)
        for (int c = 0; c <= 1; c++)
        {
            double weight = (r == 0 ? 1 - rowFraction : rowFraction)
                          * (c == 0 ? 1 - colFraction : colFraction);
            if (weight == 0) continue;

            double cell = _distanceTable[row + r, col + c];
            if (cell == U)
            {
                usesUpperBound = true;
                cell = BaselineDistance;
            }
            result += weight * cell;
        }
        return result;
    }

    private static (int Index, double Fraction) Bracket(double[] axis, double x)
    {
        int i = 0;
        while (i < axis.Length - 2 && x > axis[i + 1]) i++;
        return (i, (x - axis[i]) / (axis[i + 1] - axis[i]));
    }
}
