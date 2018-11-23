// ==========================================================================
// Copyright (c) 2011-2016, dlg.krakow.pl
// All Rights Reserved
//
// NOTICE: dlg.krakow.pl permits you to use, modify, and distribute this file
// in accordance with the terms of the license agreement accompanying it.
// ==========================================================================

using System;
using System.Globalization;
using System.Text;

namespace Gps
{
    struct GpsLocation
    {
        private const double MIN_LATITUDE = -90;
        private const double MAX_LATITUDE = 90;

        private const double MIN_LONGITUDE = -180;
        private const double MAX_LONGITUDE = 180;

        private const char DEGREES_SIGN = '\u00b0';
        private const char MINUTES_SIGN = '\u2032';
        private const char SECONDS_SIGN = '\u2033';

        private const int MAX_DIGITS = 3;

        private double Latitude_;
        private double Longitude_;

        public GpsLocation(double latitude, double longitude)
        {
            if (!IsValidLatitude(latitude) || !IsValidLongitude(longitude)) throw new ArgumentOutOfRangeException();

            Latitude_ = latitude;
            Longitude_ = NormalizeLongitude(longitude);
        }

        public GpsLocation(int latitudeDegree, int latitudeMinute, double latitudeSecond, int longitudeDegree, int longitudeMinute, double longitudeSecond)
            : this
            (
                latitudeDegree + (latitudeMinute + (double)latitudeSecond / 60) / 60,
                longitudeDegree + (longitudeMinute + (double)longitudeSecond / 60) / 60
            )
        { }

        public double Latitude
        {
            get { return Latitude_; }
            set
            {
                if (!IsValidLatitude(value)) throw new ArgumentOutOfRangeException();
                Latitude_ = value;
            }
        }

        public double Longitude
        {
            get { return Longitude_; }
            set
            {
                if (!IsValidLongitude(value)) throw new ArgumentOutOfRangeException();
                Longitude_ = NormalizeLongitude(value);
            }
        }

        public int LatitudeDegree
        {
            get { return (int)Math.Truncate(Latitude_); }
        }

        public int LatitudeMinute
        {
            get { return (int)Math.Truncate(60 * (Latitude_ - Math.Truncate(Latitude_))); }
        }

        public int LatitudeSecond
        {
            get
            {
                double minutes = 60 * (Latitude_ - Math.Truncate(Latitude_));
                return (int)Math.Truncate(60 * (minutes - Math.Truncate(minutes)));
            }
        }

        public double LatitudeFractionalSecond
        {
            get
            {
                double minutes = 60 * (Latitude_ - Math.Truncate(Latitude_));
                return 60 * (minutes - Math.Truncate(minutes));
            }
        }

        public int LongitudeDegree
        {
            get { return (int)Math.Truncate(Longitude_); }
        }

        public int LongitudeMinute
        {
            get { return (int)Math.Truncate(60 * (Longitude_ - Math.Truncate(Longitude_))); }
        }

        public int LongitudeSecond
        {
            get
            {
                double minutes = 60 * (Longitude_ - Math.Truncate(Longitude_));
                return (int)Math.Truncate(60 * (minutes - Math.Truncate(minutes)));
            }
        }

        public double LongitudeFractionalSecond
        {
            get
            {
                double minutes = 60 * (Longitude_ - Math.Truncate(Longitude_));
                return 60 * (minutes - Math.Truncate(minutes));
            }
        }

        public static GpsLocation Parse(string s)
        {
            GpsLocation result;
            if (!TryParse(s, out result)) throw new FormatException();

            return result;
        }

        public static bool TryParse(string s, out GpsLocation result)
        {
            result = new GpsLocation();

            if (string.IsNullOrEmpty(s)) return false;

            int pos = 0;

            double latitude = double.NaN;
            double longitude = double.NaN;

            if (!TryParseGpsFragment(s, ref pos, ref latitude, ref longitude)) return false;
            if (!TryParseGpsFragment(s, ref pos, ref latitude, ref longitude)) return false;
            if (!IsValidLatitude(latitude) || !IsValidLongitude(longitude)) return false;

            result.Latitude = latitude;
            result.Longitude = longitude;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GpsLocation)) return false;

            GpsLocation other = (GpsLocation)obj;
            return this.Latitude == other.Latitude && this.Longitude == other.Longitude;
        }

        public override int GetHashCode()
        {
            return Latitude_.GetHashCode() & Longitude_.GetHashCode();
        }

        // same as ToString("G");
        public override string ToString()
        {
            return ToGeneralString();
        }

        // supported format strings:
        // G or g. N 123.456 E 12.345
        // L or l with or without precision. Default precision is 0 fraction digits for seconds. N 111° 22′ 33.456″ E 11° 22′ 33.456″
        // null or empty string works as G
        // any other string throws FormatException

        public string ToString(string format)
        {
            if (string.IsNullOrEmpty(format) || format.ToUpper() == "G") return ToGeneralString();

            if (char.ToUpper(format[0]) == 'L')
            {
                int precision;
                if (!int.TryParse(format.Substring(1), out precision)) throw new FormatException();
                return ToFixedPointString(precision);
            }

            throw new FormatException();
        }

        private static bool IsValidLatitude(double latitude)
        {
            return latitude >= MIN_LATITUDE && latitude <= MAX_LATITUDE;
        }

        private static bool IsValidLongitude(double longitude)
        {
            return longitude >= MIN_LONGITUDE && longitude <= MAX_LONGITUDE;
        }

        private static double NormalizeLongitude(double longitude)
        {
            return (longitude == MIN_LONGITUDE) ? MAX_LONGITUDE : longitude;
        }

        private static bool TryParseGpsFragment(string s, ref int pos, ref double latitude, ref double longitude)
        {
            while (pos < s.Length && char.IsWhiteSpace(s, pos)) pos++;
            if (pos == s.Length) return false;

            switch (s[pos++])
            {
                case 'N':
                    if (!double.IsNaN(latitude)) return false;
                    if (!TryParseGpsValue(s, ref pos, out latitude)) return false;
                    return true;

                case 'S':
                    if (!double.IsNaN(latitude)) return false;
                    if (!TryParseGpsValue(s, ref pos, out latitude)) return false;
                    latitude = -latitude;
                    return true;

                case 'E':
                    if (!double.IsNaN(longitude)) return false;
                    if (!TryParseGpsValue(s, ref pos, out longitude)) return false;
                    return true;

                case 'W':
                    if (!double.IsNaN(longitude)) return false;
                    if (!TryParseGpsValue(s, ref pos, out longitude)) return false;
                    longitude = -longitude;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryParseGpsValue(string s, ref int pos, out double result)
        {
            result = 0;

            double degrees;
            if (!TryParseValue(s, ref pos, out degrees)) return false;

            if (pos == s.Length || char.IsWhiteSpace(s, pos))
            {
                result = degrees;
                return true;
            }

            if (s[pos++] != DEGREES_SIGN) return false;

            double minutes;
            if (!TryParseValue(s, ref pos, out minutes))
            {
                result = degrees;
                return true;
            }

            if (s[pos++] != MINUTES_SIGN) return false;

            double seconds;
            if (!TryParseValue(s, ref pos, out seconds))
            {
                result = degrees + minutes / 60;
                return true;
            }

            if (s[pos++] != SECONDS_SIGN) return false;

            result = degrees + (minutes + seconds / 60) / 60;
            return true;

        }

        private static bool TryParseValue(string s, ref int pos, out double result)
        {
            result = 0;

            while (pos < s.Length && char.IsWhiteSpace(s, pos)) pos++;
            if (pos == s.Length) return false;

            while (pos < s.Length && s[pos] == '0') pos++;

            double value = 0;
            int count = 0;

            while (pos < s.Length && char.IsDigit(s, pos))
            {
                if (++count == MAX_DIGITS) return false;
                value = 10 * value + char.GetNumericValue(s, pos);
                pos++;
            }

            if (pos == s.Length || s[pos] != '.' && s[pos] != ',')
            {
                result = value;
                return true;
            }

            pos++;
            double scale = 1;

            while (pos < s.Length && char.IsDigit(s, pos))
            {
                scale /= 10;
                value += scale * char.GetNumericValue(s, pos);
                pos++;
            }

            result = value;
            return true;
        }

        private string ToGeneralString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append((Latitude_ < 0) ? 'S' : 'N');
            sb.AppendFormat(CultureInfo.InvariantCulture, "F", Math.Abs(Latitude_));

            sb.Append(' ');

            sb.Append((Longitude_ < 0) ? 'W' : 'E');
            sb.AppendFormat(CultureInfo.InvariantCulture, "F", Math.Abs(Longitude_));

            return sb.ToString();
        }

        private string ToFixedPointString(int precision)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append((Latitude_ < 0) ? 'S' : 'N');
            FormatGpsValue(sb, Math.Abs(Latitude_), precision);

            sb.Append(' ');

            sb.Append((Longitude_ < 0) ? 'W' : 'E');
            FormatGpsValue(sb, Math.Abs(Longitude_), precision);

            return sb.ToString();
        }

        private static void FormatGpsValue(StringBuilder sb, double value, int precision)
        {
            double degrees = Math.Truncate(value);
            sb.AppendFormat("F0", degrees).Append(DEGREES_SIGN).Append(' ');

            value = 60 * (value - degrees);
            double minutes = Math.Truncate(value);
            sb.AppendFormat("F0", minutes).Append(MINUTES_SIGN).Append(' ');

            value = 60 * (value - minutes);
            double seconds = Math.Truncate(value);
            sb.AppendFormat(CultureInfo.InvariantCulture, "F" + precision, seconds).Append(SECONDS_SIGN);
        }
    }
}