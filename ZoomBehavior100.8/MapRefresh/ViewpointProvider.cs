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
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":1000,\"targetGeometry\":{\"x\":262285,\"y\":6652464.5,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":20000,\"targetGeometry\":{\"x\":253315,\"y\":6654300,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":1815,\"targetGeometry\":{\"x\":262160,\"y\":6649608,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":58017,\"targetGeometry\":{\"x\":269672.5,\"y\":6649969,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":3882,\"targetGeometry\":{\"x\":265936,\"y\":6648557,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":5202.7936851390341,\"targetGeometry\":{\"x\":472844.57661087962,\"y\":7381382.183796606,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":19758.362133418072,\"targetGeometry\":{\"x\":463790.84258105466,\"y\":7569163.6131091453,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":2155.8598328803923,\"targetGeometry\":{\"x\":707583.17676256341,\"y\":7765954.4928378016,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":13759.864731007252,\"targetGeometry\":{\"x\":-8894.1623732669905,\"y\":6577979.5305379881,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":97361.181387697347,\"targetGeometry\":{\"x\":34708.668807642534,\"y\":6928132.8525058292,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":11418.888550116595,\"targetGeometry\":{\"x\":-24734.587451196625,\"y\":6850905.4942463711,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":3443.5320063980007,\"targetGeometry\":{\"x\":-31933.414376911402,\"y\":6844529.970835058,\"spatialReference\":{\"wkid\":25833}}}"));
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
    }
}
