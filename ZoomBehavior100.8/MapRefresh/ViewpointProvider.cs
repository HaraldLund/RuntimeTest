using System;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace MapRefresh
{
    public class ViewpointProvider
    {
        #region Private members
        private readonly List<Viewpoint> _viewpoints = new List<Viewpoint>();
        private readonly Queue<Viewpoint> _queue = new Queue<Viewpoint>();
        #endregion

        public ViewpointProvider()
        {
            BuildViewpoints();
        }

        public int Count => _viewpoints.Count;

        private void BuildViewpoints()
        {
            // TODO: Load from config
            _viewpoints.Add(CreateZoomOsloPolygons());
            _viewpoints.Add(CreateZoomOutOsloPolygons()); 
            _viewpoints.Add(CreateZoomOutOsloCenterPolygons());
            _viewpoints.Add(CreateZoomOutOsloEastPolygons());
            _viewpoints.Add(CreateZoomInOsloCenterEastPolygons());
        }

        public void Reset()
        {
            _queue.Clear();
            foreach (var item in _viewpoints)
            {
                _queue.Enqueue(item);
            }
        }

        public Viewpoint GetNext()
        {
            if(_queue.Count > 0)
            {
                return _queue.Dequeue();
            }
            return null;
        }

        #region Creating polygons
        private Viewpoint CreateZoomOsloPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(266437, 6651261, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266437, 6653668, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(258133, 6651261, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(258133, 6653668, new SpatialReference(25833)));

            return new Viewpoint(new Polygon(lsPoints));
        }

        private Viewpoint CreateZoomOutOsloPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(223062, 6635196, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(223062, 6673404, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(283568, 6635196, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(283568, 6673404, new SpatialReference(25833)));

            return new Viewpoint(new Polygon(lsPoints));
        }

        private Viewpoint CreateZoomOutOsloCenterPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(261914, 6649447, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(261914, 6649769, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(262406, 6649769, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(262406, 6649447, new SpatialReference(25833)));

            return new Viewpoint(new Polygon(lsPoints));
        }


        private Viewpoint CreateZoomOutOsloEastPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(261813, 6645050, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(261813, 6654888, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(277532, 6654888, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(277532, 6645050, new SpatialReference(25833)));
            return new Viewpoint(new Polygon(lsPoints));
        }

        private Viewpoint CreateZoomInOsloCenterEastPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(265410, 6648252, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(265410, 6648862, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266462, 6648862, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266462, 6648252, new SpatialReference(25833)));
            return new Viewpoint(new Polygon(lsPoints));
        }
        #endregion
    }
}
