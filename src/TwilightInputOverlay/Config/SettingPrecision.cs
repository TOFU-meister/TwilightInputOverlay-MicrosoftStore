using System;
using System.Globalization;

namespace TwilightInputOverlay
{
    internal static class SettingPrecision
    {
        internal static float Round(float value) => MathF.Round(value, 2, MidpointRounding.AwayFromZero);

        internal static string Display(float value)
            => Round(value).ToString("F2", CultureInfo.CurrentCulture);

        internal static string Serialize(ref float value)
        {
            value = Round(value);
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        internal static bool TryParse(string text, out float value)
        {
            if ((float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                || float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                && float.IsFinite(value))
            {
                value = Round(value);
                return true;
            }
            return false;
        }
    }
}
