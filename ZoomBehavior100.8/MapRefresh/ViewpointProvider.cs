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
