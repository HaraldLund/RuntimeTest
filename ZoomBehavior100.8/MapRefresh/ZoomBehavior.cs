using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.UI.Controls;
using System.Windows.Interactivity;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;

namespace MapRefresh
{
    public class ZoomBehavior : Behavior<MapView>
    {
        #region Dependency properties
        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register("IsVisible", typeof(Visibility), typeof(ZoomBehavior), new FrameworkPropertyMetadata(Visibility.Hidden));
        public Visibility IsVisible
        {
            get
            {
                return (Visibility)GetValue(IsVisibleProperty);
            }
            set
            {
                SetValue(IsVisibleProperty, value);
            }
        }
        public static readonly DependencyProperty TargetScaleProperty = DependencyProperty.Register("TargetScale", typeof(string), typeof(ZoomBehavior), new FrameworkPropertyMetadata(null));

        public string TargetScale
        {
            get
            {
                return (string)GetValue(TargetScaleProperty);
            }
            set
            {
                SetValue(TargetScaleProperty, value);
            }
        }

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(ZoomBehavior), new FrameworkPropertyMetadata(false));
        public bool IsEnabled
        {
            get
            {
                return (bool)GetValue(IsEnabledProperty);
            }
            set
            {
                SetValue(IsEnabledProperty, value);
            }
        }
        #endregion

        #region Attach and detach
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        }
        #endregion

        private int _deltas;
        private async void AssociatedObject_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if(!IsEnabled)
            {
                return;
            }
            e.Handled = true;

            var mapView = AssociatedObject;
            var screenPosition = e.GetPosition(AssociatedObject);
            if(IsCursorOutsideControl(screenPosition))
            {
                HideTargetScale();
                return;
            }
            _deltas += e.Delta;
            var direction = GetDirection(_deltas);
            double factor = (mapView.InteractionOptions.ZoomFactor * Math.Abs(_deltas / 120));
            double scale = GetNewScale(factor, direction);

            Trace.WriteLine($"{scale}");
            // TODO: stretch or shrink current map image for visual indication of direction/scale
            UpdateTargetScale(scale);

            var delta = _deltas;
            await Task.Delay(400);
            if(delta != _deltas)
            {
                // Accumulate multiple deltas before starting the actual zoom
                return;
            }

            _deltas = 0;
            var screenLocation = mapView.ScreenToLocation(screenPosition);
            var centerLocation = (MapPoint)mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).TargetGeometry;

            var distance = GeometryEngine.DistanceGeodetic(screenLocation, centerLocation, LinearUnits.Meters, AngularUnits.Degrees,
                GeodeticCurveType.Geodesic);
            double distanceValue = GetNewValue(distance.Distance, factor, direction);

            var targetCenter = GeometryEngine.MoveGeodetic(new[] {screenLocation}, distanceValue, LinearUnits.Meters,
                distance.Azimuth1, distance.AzimuthUnit, GeodeticCurveType.Geodesic)
                .First();

            await mapView.SetViewpointAsync(
                new Viewpoint(targetCenter, scale), TimeSpan.Zero);

            HideTargetScale();

        }

        private void UpdateTargetScale(double scale)
        {
            SetCurrentValue(TargetScaleProperty, $"1:{scale:### ### ###}");
            SetCurrentValue(IsVisibleProperty, Visibility.Visible);
        }

        private bool IsCursorOutsideControl(Point screenPosition)
        {
            return screenPosition.X < 0 || screenPosition.Y < 0 || screenPosition.X > AssociatedObject.Width ||
                   screenPosition.Y > AssociatedObject.Height;
        }

        private void HideTargetScale()
        {
            SetCurrentValue(IsVisibleProperty, Visibility.Collapsed);
            _deltas = 0;
        }

        private double GetNewScale(double factor, ZoomDirection direction)
        {
            double scale = GetNewValue(AssociatedObject.MapScale, factor, direction);
            return Math.Max(625, scale);
        }

        private double GetNewValue(double value, double factor, ZoomDirection direction)
        {
            if (direction == ZoomDirection.In)
            {
                return value / factor;
            }

            return value * factor;
        }

        private ZoomDirection GetDirection(int delta)
        {
            var isIn = delta >= 0;
            if (AssociatedObject.InteractionOptions.WheelZoomDirection == WheelZoomDirection.Reversed) 
            {
                isIn = !isIn;
            }
            return isIn ? ZoomDirection.In : ZoomDirection.Out;
        }



        public enum ZoomDirection
        {
            In,
            Out
        }
    }
}
