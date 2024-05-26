using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.UI;

namespace BluetoothDMM
{
    public class CircularProgress : Shape
    {
        static CircularProgress()
        {
            var Clr = System.Drawing.Color.Yellow;
            Brush myGreenBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255,Clr.R,Clr.G,Clr.B));//Color.FromArgb(155, 255, 176, 37));
            myGreenBrush.Freeze();
            
            StrokeProperty.OverrideMetadata(
                typeof(CircularProgress),
                new FrameworkPropertyMetadata(myGreenBrush));
            FillProperty.OverrideMetadata(
                typeof(CircularProgress),
                new FrameworkPropertyMetadata(Brushes.Transparent));

            StrokeThicknessProperty.OverrideMetadata(
                typeof(CircularProgress),
                new FrameworkPropertyMetadata(1.0));
        }

        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }
        public double EndAngle
        {
            get { return (double)GetValue(EndAngleProperty); }
            set { SetValue(EndAngleProperty, value); }
        }
        // Value (0-100)
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public int Type
        {
            get { return (int)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        #region 종료 각도 속성 - StartAngleProperty

        /// <summary>
        /// 종료 각도 속성
        /// </summary>
        public static readonly FrameworkPropertyMetadata StartAngleMetadata = new FrameworkPropertyMetadata(
                    30.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    new CoerceValueCallback(StartValue));   // Coerce value callback

        #endregion

        #region 종료 각도 속성 - EndAngleProperty

        /// <summary>
        /// 종료 각도 속성
        /// </summary>
        public static readonly FrameworkPropertyMetadata EndAngleMetadata = new FrameworkPropertyMetadata(
                    30.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    new CoerceValueCallback(EndValue));   // Coerce value callback

        #endregion

        // DependencyProperty - Value (0 - 100)
        private static FrameworkPropertyMetadata valueMetadata =
                new FrameworkPropertyMetadata(
                    30.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    new CoerceValueCallback(CoerceValue));   // Coerce value callback
                                                             // DependencyProperty - Value (0 - 100)
        private static FrameworkPropertyMetadata typeMetadata =
                new FrameworkPropertyMetadata(
                    0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    new CoerceValueCallback(TypeValue));   // Coerce value callback

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(int), typeof(CircularProgress), typeMetadata);

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(CircularProgress), valueMetadata);

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(CircularProgress), StartAngleMetadata);

        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register("EndAngle", typeof(double), typeof(CircularProgress), EndAngleMetadata);

        private static object TypeValue(DependencyObject depObj, object baseVal)
        {
            int val = (int)baseVal;
            val = Math.Min(val, 1);
            val = Math.Max(val, 0);
            return val;
        }
        private static object CoerceValue(DependencyObject depObj, object baseVal)
        {
            double val = (double)baseVal;
            val = Math.Min(val, 99.999);
            val = Math.Max(val, 0.0);
            return val;
        }

        private static object StartValue(DependencyObject depObj, object baseVal)
        {
            double val = (double)baseVal;
            val = Math.Min(val, 359.999);
            val = Math.Max(val, -90.0);
            return val;
        }
        private static object EndValue(DependencyObject depObj, object baseVal)
        {
            double val = (double)baseVal;
            val = Math.Min(val, 359.999);
            val = Math.Max(val, -90.0);
            return val;
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                int type = Type;
                double multiplerTop = 1;
                double multiplerBottom = 0.965;
                double change = 1;
                if (type == 1)
                {
                    multiplerTop = 0.98;
                    multiplerBottom = 0.945;
                }
                double startAngle = StartAngle + 90;
                double Differ = Math.Abs(EndAngle - StartAngle);
                double endAngle = startAngle - ((Value / 100.0) * (Differ)+0.2);

                double maxWidth = Math.Max(0.0, RenderSize.Width - StrokeThickness);
                double maxHeight = Math.Max(0.0, RenderSize.Height - StrokeThickness);

                double xTopStart = maxWidth * multiplerTop / 2.0 * Math.Cos(startAngle * Math.PI / 180.0);
                double yTopStart = maxHeight * multiplerTop / 2.0 * Math.Sin(startAngle * Math.PI / 180.0);

                double xTopEnd = maxWidth * multiplerTop / 2.0 * Math.Cos(endAngle * Math.PI / 180.0);
                double yTopEnd = maxHeight * multiplerTop / 2.0 * Math.Sin(endAngle * Math.PI / 180.0);

                double xBottomStart = maxWidth * multiplerBottom / 2.0 * Math.Cos(startAngle * Math.PI / 180.0);
                double yBottomStart = maxHeight * multiplerBottom / 2.0 * Math.Sin(startAngle * Math.PI / 180.0);

                double xBottomEnd = maxWidth * multiplerBottom / 2.0 * Math.Cos(endAngle * Math.PI / 180.0);
                double yBottomEnd = maxHeight * multiplerBottom / 2.0 * Math.Sin(endAngle * Math.PI / 180.0);

                double xPinEnd = maxWidth * multiplerBottom / 2.0 * Math.Cos((endAngle + 0.3) * Math.PI / 180.0);
                double yPinEnd = maxHeight * multiplerBottom  / 2.0 * Math.Sin((endAngle + 0.3) * Math.PI / 180.00);

                StreamGeometry geom = new StreamGeometry();
                using (StreamGeometryContext ctx = geom.Open())
                {
                    ctx.BeginFigure(
                        new Point((RenderSize.Width / 2.0) + xBottomStart,
                                  (RenderSize.Height / 2.0) - yBottomStart),
                        true,   // Filled
                        false);  // Closed
                    ctx.ArcTo(
                        new Point((RenderSize.Width / 2.0) + xBottomEnd,
                                  (RenderSize.Height / 2.0) - yBottomEnd),
                        new Size(maxWidth / (2.0/multiplerBottom), maxHeight / (2.0/multiplerBottom)),
                        0.0,     // rotationAngle
                        (startAngle - endAngle) > 180,   // greater than 180 deg?
                        SweepDirection.Clockwise,
                        true,    // isStroked
                        true);

                    ctx.LineTo(new Point((RenderSize.Width / 2.0) + xTopEnd, (RenderSize.Height / 2.0) - yTopEnd), true, false);
                    if (Type == 0)
                    {
                        ctx.ArcTo(
                            new Point((RenderSize.Width / 2.0) + xTopStart,
                                      (RenderSize.Height / 2.0) - yTopStart),
                            new Size(maxWidth / (2.0 / multiplerTop), maxHeight / (2.0 / multiplerTop)),
                            0.0,     // rotationAngle
                            (startAngle - endAngle) > 180,   // greater than 180 deg?
                            SweepDirection.Counterclockwise,
                            true,    // isStroked
                            false);
                        ctx.LineTo(new Point((RenderSize.Width / 2.0) + xBottomStart, (RenderSize.Height / 2.0) - yBottomStart), true, false);
                    } else
                    {
                        ctx.LineTo(new Point((RenderSize.Width / 2.0) + xPinEnd, (RenderSize.Height / 2.0) - yPinEnd), true, false);
                        ctx.ArcTo(
                            new Point((RenderSize.Width / 2.0) + xBottomStart,
                                      (RenderSize.Height / 2.0) - yBottomStart),
                            new Size(maxWidth / (2.0 / multiplerBottom), maxHeight / (2.0 / multiplerBottom)),
                            0.0,     // rotationAngle
                            (startAngle - endAngle) > 180,   // greater than 180 deg?
                            SweepDirection.Counterclockwise,
                            true,    // isStroked
                            false);
                    }
                }

                return geom;
            }
        }
    }
}
