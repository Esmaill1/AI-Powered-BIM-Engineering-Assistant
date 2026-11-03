namespace RuleCheck.Core.Units;

/// <summary>
/// Converts rule thresholds into Revit internal units (feet, ft², ft³, radians),
/// which is how <see cref="Snapshot.ParamValue.AsDouble"/> is stored.
/// </summary>
public static class UnitConverter
{
    private static readonly IReadOnlyDictionary<string, double> ToInternalFactor =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["mm"] = 1 / 304.8,
            ["cm"] = 1 / 30.48,
            ["m"] = 1 / 0.3048,
            ["in"] = 1 / 12.0,
            ["ft"] = 1.0,
            ["mm2"] = 1 / (304.8 * 304.8),
            ["m2"] = 1 / (0.3048 * 0.3048),
            ["ft2"] = 1.0,
            ["m3"] = 1 / (0.3048 * 0.3048 * 0.3048),
            ["ft3"] = 1.0,
            ["deg"] = Math.PI / 180.0,
            ["rad"] = 1.0,
        };

    public static IReadOnlyCollection<string> SupportedUnits => (IReadOnlyCollection<string>)ToInternalFactor.Keys;

    /// <summary>Converts <paramref name="value"/> from <paramref name="unit"/> to internal units. Null unit = unitless (unchanged).</summary>
    public static double ToInternal(double value, string? unit)
    {
        if (string.IsNullOrEmpty(unit)) return value;
        return ToInternalFactor.TryGetValue(unit, out var factor)
            ? value * factor
            : throw new ArgumentException($"Unsupported unit '{unit}'.", nameof(unit));
    }
}
