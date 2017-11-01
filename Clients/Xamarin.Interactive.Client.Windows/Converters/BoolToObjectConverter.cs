using System;
using System.Globalization;
using System.Windows.Data;

namespace Xamarin.Interactive.Client.Windows.Converters
{
    class BoolToObjectConverter : BoolToTypeConverter<object> { }

    class BoolToTypeConverter<T> : IValueConverter
    {
        // Require setting TrueValue/FalseValue because false may be Hidden or Collapsed, in most cases.
        // Could also flip them for an inverse converter.
        public T FalseValue { get; set; }
        public T TrueValue { get; set; }

        public virtual object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val;
            if (value == null)
                val = false;
            // Treat non-zero integers as truthy
            else if (value is int)
                val = ((int) value) != 0;
            // Allow treating other non-bool non-null values as truthy
            else if (!(value is bool))
                val = true;
            else
                val = (bool) value;

            if ("Inverse" == parameter?.ToString ())
                val = !val;

            return val ? TrueValue : FalseValue;
        }

        public virtual object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals (TrueValue);
        }
    }
}
