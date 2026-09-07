using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompanyCalendar
{
    public partial class ColorPickerWindow : Window
    {
        private double _hue;
        private double _saturation = 1.0;
        private double _value = 1.0;

        private bool _isUpdating;
        private bool _isDraggingColorField;
        private bool _isDraggingHue;


        public string SelectedColorHex { get; private set; } = "#FF0000";

        public Color SelectedColor { get; private set; } = Colors.Red;


        public ColorPickerWindow(string initialHex)
        {
            InitializeComponent();

            Loaded += ColorPickerWindow_Loaded;

            SetFromHex(initialHex);
        }


        private void ColorPickerWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateInterface();
        }


        // =========================================================
        // COLOR FIELD
        // =========================================================

        private void ColorField_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _isDraggingColorField = true;

            ColorField.CaptureMouse();

            UpdateSaturationValue(
                e.GetPosition(ColorField));
        }


        private void ColorField_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isDraggingColorField)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _isDraggingColorField = false;
                ColorField.ReleaseMouseCapture();
                return;
            }

            UpdateSaturationValue(
                e.GetPosition(ColorField));
        }


        private void UpdateSaturationValue(
            Point position)
        {
            if (ColorField.ActualWidth <= 0 ||
                ColorField.ActualHeight <= 0)
            {
                return;
            }


            double x =
                Math.Clamp(
                    position.X,
                    0,
                    ColorField.ActualWidth);

            double y =
                Math.Clamp(
                    position.Y,
                    0,
                    ColorField.ActualHeight);


            _saturation =
                x / ColorField.ActualWidth;

            _value =
                1.0 - (y / ColorField.ActualHeight);


            UpdateInterface();
        }


        // =========================================================
        // HUE FIELD
        // =========================================================

        private void HueField_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _isDraggingHue = true;

            HueField.CaptureMouse();

            UpdateHue(
                e.GetPosition(HueField));
        }


        private void HueField_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isDraggingHue)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _isDraggingHue = false;
                HueField.ReleaseMouseCapture();
                return;
            }

            UpdateHue(
                e.GetPosition(HueField));
        }


        private void UpdateHue(
            Point position)
        {
            if (HueField.ActualHeight <= 0)
            {
                return;
            }


            double y =
                Math.Clamp(
                    position.Y,
                    0,
                    HueField.ActualHeight);


            _hue =
                (y / HueField.ActualHeight) * 360.0;


            if (_hue >= 360.0)
            {
                _hue = 0.0;
            }


            UpdateInterface();
        }


        // =========================================================
        // HEX
        // =========================================================

        private void HexTextBox_TextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isUpdating)
            {
                return;
            }


            string value =
                HexTextBox.Text.Trim();


            if (!TryParseHexColor(
                value,
                out Color color))
            {
                return;
            }


            RgbToHsv(
                color,
                out _hue,
                out _saturation,
                out _value);


            SelectedColor = color;

            SelectedColorHex =
                ColorToHex(color);


            UpdateInterface(
                updateHexTextBox: false);
        }


        // =========================================================
        // UI UPDATE
        // =========================================================

        private void UpdateInterface(
            bool updateHexTextBox = true)
        {
            _isUpdating = true;


            Color hueColor =
                HsvToRgb(
                    _hue,
                    1.0,
                    1.0);


            HueBackground.Fill =
                new SolidColorBrush(hueColor);


            SelectedColor =
                HsvToRgb(
                    _hue,
                    _saturation,
                    _value);


            SelectedColorHex =
                ColorToHex(SelectedColor);


            SelectedColorPreview.Background =
                new SolidColorBrush(SelectedColor);


            RedValueText.Text =
                SelectedColor.R.ToString();

            GreenValueText.Text =
                SelectedColor.G.ToString();

            BlueValueText.Text =
                SelectedColor.B.ToString();


            if (updateHexTextBox)
            {
                HexTextBox.Text =
                    SelectedColorHex;
            }


            UpdateMarkers();


            _isUpdating = false;
        }


        private void UpdateMarkers()
        {
            if (ColorField.ActualWidth > 0 &&
                ColorField.ActualHeight > 0)
            {
                double x =
                    _saturation *
                    ColorField.ActualWidth;

                double y =
                    (1.0 - _value) *
                    ColorField.ActualHeight;


                ColorMarker.Margin =
                    new Thickness(
                        x - 8,
                        y - 8,
                        0,
                        0);

                ColorMarker.HorizontalAlignment =
                    HorizontalAlignment.Left;

                ColorMarker.VerticalAlignment =
                    VerticalAlignment.Top;
            }


            if (HueField.ActualHeight > 0)
            {
                double y =
                    (_hue / 360.0) *
                    HueField.ActualHeight;


                HueMarker.Margin =
                    new Thickness(
                        0,
                        y - 2,
                        0,
                        0);
            }
        }


        // =========================================================
        // INITIAL COLOR
        // =========================================================

        private void SetFromHex(
            string initialHex)
        {
            if (!TryParseHexColor(
                initialHex,
                out Color color))
            {
                color = Colors.Red;
            }


            RgbToHsv(
                color,
                out _hue,
                out _saturation,
                out _value);


            SelectedColor = color;

            SelectedColorHex =
                ColorToHex(color);


            if (IsLoaded)
            {
                UpdateInterface();
            }
        }


        // =========================================================
        // BUTTONS
        // =========================================================

        private void OkButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }


        // =========================================================
        // HEX HELPERS
        // =========================================================

        private static bool TryParseHexColor(
            string value,
            out Color color)
        {
            color = Colors.Transparent;


            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }


            value = value.Trim();


            if (!value.StartsWith("#"))
            {
                value = "#" + value;
            }


            if (value.Length != 7)
            {
                return false;
            }


            try
            {
                byte r =
                    Convert.ToByte(
                        value.Substring(1, 2),
                        16);

                byte g =
                    Convert.ToByte(
                        value.Substring(3, 2),
                        16);

                byte b =
                    Convert.ToByte(
                        value.Substring(5, 2),
                        16);


                color =
                    Color.FromRgb(
                        r,
                        g,
                        b);


                return true;
            }
            catch
            {
                return false;
            }
        }


        private static string ColorToHex(
            Color color)
        {
            return
                $"#{color.R:X2}" +
                $"{color.G:X2}" +
                $"{color.B:X2}";
        }


        // =========================================================
        // HSV -> RGB
        // =========================================================

        private static Color HsvToRgb(
            double hue,
            double saturation,
            double value)
        {
            hue =
                ((hue % 360) + 360) % 360;


            double chroma =
                value * saturation;

            double x =
                chroma *
                (1 -
                 Math.Abs(
                     ((hue / 60.0) % 2) - 1));

            double m =
                value - chroma;


            double r1;
            double g1;
            double b1;


            if (hue < 60)
            {
                r1 = chroma;
                g1 = x;
                b1 = 0;
            }
            else if (hue < 120)
            {
                r1 = x;
                g1 = chroma;
                b1 = 0;
            }
            else if (hue < 180)
            {
                r1 = 0;
                g1 = chroma;
                b1 = x;
            }
            else if (hue < 240)
            {
                r1 = 0;
                g1 = x;
                b1 = chroma;
            }
            else if (hue < 300)
            {
                r1 = x;
                g1 = 0;
                b1 = chroma;
            }
            else
            {
                r1 = chroma;
                g1 = 0;
                b1 = x;
            }


            byte r =
                (byte)Math.Round(
                    (r1 + m) * 255);

            byte g =
                (byte)Math.Round(
                    (g1 + m) * 255);

            byte b =
                (byte)Math.Round(
                    (b1 + m) * 255);


            return Color.FromRgb(
                r,
                g,
                b);
        }


        // =========================================================
        // RGB -> HSV
        // =========================================================

        private static void RgbToHsv(
            Color color,
            out double hue,
            out double saturation,
            out double value)
        {
            double r =
                color.R / 255.0;

            double g =
                color.G / 255.0;

            double b =
                color.B / 255.0;


            double max =
                Math.Max(
                    r,
                    Math.Max(g, b));

            double min =
                Math.Min(
                    r,
                    Math.Min(g, b));

            double delta =
                max - min;


            if (delta == 0)
            {
                hue = 0;
            }
            else if (max == r)
            {
                hue =
                    60 *
                    (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                hue =
                    60 *
                    (((b - r) / delta) + 2);
            }
            else
            {
                hue =
                    60 *
                    (((r - g) / delta) + 4);
            }


            if (hue < 0)
            {
                hue += 360;
            }


            saturation =
                max == 0
                    ? 0
                    : delta / max;


            value =
                max;
        }
    }
}