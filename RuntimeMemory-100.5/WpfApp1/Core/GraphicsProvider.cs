using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1.Core
{
    public class GraphicsProvider : IGraphicsProvider
    {
        #region Private members
        private readonly GraphicsProviderCache _cache = new GraphicsProviderCache();
        private readonly List<IGraphicsProviderObserver> _observers = new List<IGraphicsProviderObserver>();
        #endregion

        public void Publish(params IMapObject[] items)
        {
            foreach (var graphic in items)
            {
                var layerId = graphic.LayerId;
                _cache.Push(graphic.LayerId, graphic);
                foreach (var observer in _observers.ToList())
                {
                    if (observer.LayerId == layerId)
                    {
                        observer.OnPublish(graphic);
                    }
                }
            }
        }

        public IDisposable Subscribe(IGraphicsProviderObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
                // Provide observer with existing data.
                var graphics = _cache.GetGraphics(observer.LayerId);
                foreach (var singleGraphic in graphics)
                {
                    observer.OnPublish(singleGraphic);
                }
            }
            return new GraphicsProviderUnsubscriber(_observers, observer);
        }

        public void Purge()
        {
            _cache.Clear();
            foreach (var observer in _observers.ToList())
            {
                observer.OnPurge();
            }
        }
    }

    #region IGraphicsProvider
    public interface IGraphicsProvider
    {
        void Publish(params IMapObject[] items);
        IDisposable Subscribe(IGraphicsProviderObserver observer);
    }
    #endregion

    #region GraphicsProviderCache
    public class GraphicsProviderCache
    {
        #region Private members
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, IMapObject>> _layerCache = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, IMapObject>>();
        #endregion

        /// <summary>
        /// Push a new or existing object to a layer cache.
        /// </summary>
        /// <param name="layerIdentifier"></param>
        /// <param name="graphic"></param>
        public void Push(string layerIdentifier, IMapObject graphic)
        {
            Guid id = graphic.Id;
            string layerId = layerIdentifier;
            if (!_layerCache.TryGetValue(layerId, out var graphicCache))
            {
                graphicCache = new ConcurrentDictionary<Guid, IMapObject>();
                _layerCache.TryAdd(layerId, graphicCache);
            }

            if (graphicCache.TryGetValue(id, out var existingGraphic))
            {
                graphicCache[id] = graphic;
            }
            else
            {
                graphicCache.TryAdd(id, graphic);
            }
        }

        public List<IMapObject> GetGraphics(string layerId)
        {
            var list = new List<IMapObject>();
            if (_layerCache.TryGetValue(layerId, out var graphicCache))
            {
                list.AddRange(graphicCache.Values.ToList());
            }

            return list;
        }

        public void Clear()
        {
            _layerCache.Clear();
        }
    }
    #endregion

    #region GraphicsProviderUnsubscriber
    internal class GraphicsProviderUnsubscriber : IDisposable
    {
        #region Private members
        private readonly List<IGraphicsProviderObserver> _observers;
        private readonly IGraphicsProviderObserver _observer;
        #endregion

        #region Constructor

        public GraphicsProviderUnsubscriber(List<IGraphicsProviderObserver> observers, IGraphicsProviderObserver observer)
        {
            _observers = observers;
            _observer = observer;
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            if (_observers?.Contains(_observer) == true)
            {
                _observers.Remove(_observer);
            }
        }
        #endregion
    }
    #endregion
}