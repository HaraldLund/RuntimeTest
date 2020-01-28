using System;
using System.Threading.Tasks;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System.Windows.Controls;

namespace MapRefresh
{
    public class LegacyZoomProvider : ZoomProviderBase
    {
        #region Private members
        private readonly Map _mapView;
        private readonly string _runtime;
        private Esri.ArcGISRuntime.Mapping.Viewpoint _currentViewpoint;
        private TextBlock _txtBox;
        #endregion

        public LegacyZoomProvider(ESRI.ArcGIS.Client.Map mapView)
        {
            _mapView = mapView;
            _runtime = mapView.GetType().Assembly.GetName().Version.ToString();
        }

        public override string RuntimeVersion => _runtime;

        public TextBlock SetTextBlockControl { set { _txtBox = value; } }
        public override async Task ZoomTo(Esri.ArcGISRuntime.Mapping.Viewpoint viewpoint)
        {
            _currentViewpoint = viewpoint;
            _mapView.Progress += _mapView_Progress;
            if (viewpoint.TargetGeometry is Esri.ArcGISRuntime.Geometry.MapPoint point)
            {
                var mapPoint = new MapPoint(point.X, point.Y, new SpatialReference(point.SpatialReference.Wkid));
                var resolution = Geometry.GetResolution(viewpoint.TargetScale, mapPoint.SpatialReference);
                await _mapView.Dispatcher.BeginInvoke((Action)(()=>_mapView.ZoomToResolution(resolution, mapPoint)));
            }
            //await _mapView.SetViewpointAsync(viewpoint);
        }

        private void _mapView_Progress(object sender, ProgressEventArgs e)
        {
            _txtBox.Text = e.Progress.ToString();
            if (e.Progress == 100)
            {
                _mapView.Progress -= _mapView_Progress;
                OnDrawFinished(_currentViewpoint, 0, _runtime);
            }
        }
    }
}