﻿using System;
using System.Threading.Tasks;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;

namespace MapRefresh
{
    public class LegacyZoomProvider : ZoomProviderBase
    {
        #region Private members
        private readonly Map _mapView;
        private readonly string _runtime;
        private Esri.ArcGISRuntime.Mapping.Viewpoint _currentViewpoint;

        #endregion

        public LegacyZoomProvider(ESRI.ArcGIS.Client.Map mapView)
        {
            _mapView = mapView;
            _runtime = mapView.GetType().Assembly.GetName().Version.ToString();
        }

        public override string RuntimeVersion => _runtime;

        public override async Task ZoomTo(Esri.ArcGISRuntime.Mapping.Viewpoint viewpoint)
        {
            _currentViewpoint = viewpoint;
            _mapView.Progress += _mapView_Progress;
            if (viewpoint.TargetGeometry is Esri.ArcGISRuntime.Geometry.MapPoint point)
            {
                var mapPoint = new MapPoint(point.X, point.Y, new SpatialReference(point.SpatialReference.Wkid));
                var extent = GetExtent(viewpoint.TargetScale, mapPoint);
                await _mapView.Dispatcher.BeginInvoke((Action)(() => _mapView.ZoomTo(extent)));
            }
        }

        private Envelope GetExtent(double viewpointTargetScale, MapPoint point)
        {
            var currentRes = _mapView.Resolution;
            var targetRes = Geometry.GetResolution(viewpointTargetScale, point.SpatialReference);
            var factor = (targetRes / currentRes);
            var width = (_mapView.Extent.Width * factor) / 2d;
            var height = (_mapView.Extent.Height * factor) / 2d ;
            return new Envelope(point.X-width, point.Y-height, point.X + width, point.Y + height)
            {
                SpatialReference = point.SpatialReference
            };
        }

        private void _mapView_Progress(object sender, ProgressEventArgs e)
        {
            if (e.Progress == 100)
            {
                _mapView.Progress -= _mapView_Progress;
                OnDrawFinished(_currentViewpoint, 0, _runtime);
            }
        }
    }
}