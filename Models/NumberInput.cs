using System.Globalization;

namespace WeightBalance.Models;

public static class NumberInput
{
    // Returns null for anything that is not a plain number. Commas are rejected, not
    // read as decimals or thousands: "1,5" must not become 15, and "1,000" could mean
    // 1 or 1000.
    public static double? Parse(string? text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
        && double.IsFinite(value)
            ? value
            : null;
}
