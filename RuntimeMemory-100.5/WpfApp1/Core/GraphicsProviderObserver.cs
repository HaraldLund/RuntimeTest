using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.UI;

namespace WpfApp1.Core
{
    public class GraphicsProviderObserver : IGraphicsProviderObserver
    {
        #region Private members
        private readonly IGraphicsProvider _graphicsProvider;
        private readonly GraphicsOverlay _overlay;
        private IDisposable _graphicsProviderSubscription;

        #endregion

        public GraphicsProviderObserver(IGraphicsProvider graphicsProvider, GraphicsOverlay overlay)
        {
            _graphicsProvider = graphicsProvider;
            _overlay = overlay;
        }

        public void OnPublish(IMapObject source)
        {
            // No need to update as the sample doesn't do that
            _overlay.Graphics.Add(new Graphic(source.Graphic.Geometry, source.Graphic.Attributes));
        }

        public void OnCompleted()
        {
            _graphicsProviderSubscription?.Dispose();
            _overlay.Graphics.Clear();
        }

        public string LayerId => _overlay.Id;
        public void OnPurge()
        {
            _overlay.Graphics.Clear();
        }

        public void Initialize()
        {
            _graphicsProviderSubscription = _graphicsProvider.Subscribe(this);
        }
    }

    public interface IGraphicsProviderObserver
    {
        void OnPublish(IMapObject source);
        void OnCompleted();

        string LayerId { get; }
        void OnPurge();
    }
}
