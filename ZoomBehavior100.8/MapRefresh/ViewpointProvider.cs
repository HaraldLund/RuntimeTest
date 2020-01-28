using System;
using System.Collections.Generic;
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

        private void BuildViewpoints()
        {
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":1000,\"targetGeometry\":{\"x\":262285,\"y\":6652464.5,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":20000,\"targetGeometry\":{\"x\":253315,\"y\":6654300,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":1815,\"targetGeometry\":{\"x\":262160,\"y\":6649608,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":58017,\"targetGeometry\":{\"x\":269672.5,\"y\":6649969,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":3882,\"targetGeometry\":{\"x\":265936,\"y\":6648557,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":5202,\"targetGeometry\":{\"x\":472844,\"y\":7381382,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":19758,\"targetGeometry\":{\"x\":463790,\"y\":7569163,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":2155,\"targetGeometry\":{\"x\":707583,\"y\":7765954,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":13759,\"targetGeometry\":{\"x\":-8894,\"y\":6577979,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":97361,\"targetGeometry\":{\"x\":34708,\"y\":6928132,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":11418,\"targetGeometry\":{\"x\":-24734,\"y\":6850905,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":3443,\"targetGeometry\":{\"x\":-31933,\"y\":6844529,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":51662,\"targetGeometry\":{\"x\":266348,\"y\":7036630,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":2771,\"targetGeometry\":{\"x\":243093,\"y\":7082664,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":1898,\"targetGeometry\":{\"x\":298397,\"y\":7138339,\"spatialReference\":{\"wkid\":25833}}}"));
            _viewpoints.Add(Viewpoint.FromJson("{\"rotation\":0,\"scale\":121294,\"targetGeometry\":{\"x\":483345,\"y\":7463257,\"spatialReference\":{\"wkid\":25833}}}"));

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
