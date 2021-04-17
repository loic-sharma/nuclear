using System;
using System.ComponentModel;
using System.Globalization;
using NuGet.Versioning;

namespace Nuclear
{
    public class NuclearVersionRange
    {
        private readonly FloatRange _range;

        public NuclearVersionRange(FloatRange range)
        {
            _range = range;
        }

        public bool IsPrereleaseRange => _range.FloatBehavior switch
        {
            NuGetVersionFloatBehavior.None => false,
            NuGetVersionFloatBehavior.Revision => false,
            NuGetVersionFloatBehavior.Patch => false,
            NuGetVersionFloatBehavior.Minor => false,
            NuGetVersionFloatBehavior.Major => false,

            NuGetVersionFloatBehavior.Prerelease => true,
            NuGetVersionFloatBehavior.AbsoluteLatest => true,
            NuGetVersionFloatBehavior.PrereleaseRevision => true,
            NuGetVersionFloatBehavior.PrereleasePatch => true,
            NuGetVersionFloatBehavior.PrereleaseMinor => true,
            NuGetVersionFloatBehavior.PrereleaseMajor => true,

            _ => throw new NotImplementedException($"Unknown float behavior {_range.FloatBehavior}")
        };

        public bool Includes(NuGetVersion version)
        {
            // NuGet accepts stable versions given range 1.0.0-*.
            // This makes sense for NuGet, but we don't want to delete stable versions
            // if we were given a prerelease version range.
            if (IsPrereleaseRange && !version.IsPrerelease)
            {
                return false;
            }

            return _range.Satisfies(version);
        }

        public override string ToString() => _range.ToString();
    }

    public class NuclearVersionRangeConverter : TypeConverter
    {
        private const string ErrorHint = "Please use floating version notation instead. Examples: 1.0.0-*, or 1.*";

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                if (FloatRange.TryParse(stringValue, out var floatRange))
                {
                    return new NuclearVersionRange(floatRange);
                }

                if (VersionRange.TryParse(stringValue, out _))
                {
                    throw new NotSupportedException($"'{stringValue}' uses the interval notation. " + ErrorHint);
                }
            }

            throw new ArgumentException($"'{value}' is not a valid version range. " + ErrorHint);
        }
    }
}
