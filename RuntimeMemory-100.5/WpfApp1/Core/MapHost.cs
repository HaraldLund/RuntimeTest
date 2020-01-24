using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;

namespace WpfApp1.Core
{
    #region MapHost
    public class MapHost : ObservableObject
    {
        #region Constructor
        public MapHost()
        {
            Maps = new ObservableCollection<MapViewModel>();
            GraphicsProvider = new GraphicsProvider();
        }
        #endregion

        #region Public properties
        public ObservableCollection<MapViewModel> Maps { get; }
        public GraphicsProvider GraphicsProvider { get; }
        #endregion

        public ICommand ClearDataCommand =>_clearDataCommand ?? (_clearDataCommand = new InternalRelayCommand(param => OnClearDataCommand()));

        private void OnClearDataCommand()
        {
            GraphicsProvider.Purge();
        }

        private ICommand _addPolygonsCommand;
        public ICommand AddPolygonsCommand => _addPolygonsCommand ?? (_addPolygonsCommand = new InternalRelayCommand(param => OnAddPolygonsCommand()));

        private void OnAddPolygonsCommand()
        {
            var points = GetPoints();
            foreach (var point in points)
            {
                var polygon = GeometryEngine.BufferGeodetic(point, 3, LinearUnits.Kilometers);
                GraphicsProvider.Publish(new MapObject(polygon) { LayerId = LayerKeys.Polygon.ToString() });
            }
        }

        private ICommand _addPointsCommand;
        public ICommand AddPointsCommand => _addPointsCommand ?? (_addPointsCommand = new InternalRelayCommand(param => OnAddPointsCommand()));

        private void OnAddPointsCommand()
        {
            var points = GetPoints();
            foreach (var point in points)
            {
                GraphicsProvider.Publish(new MapObject(point) { LayerId = LayerKeys.Points.ToString()});
            }
        }

        private ICommand _addPolylinesCommand;
        public ICommand AddPolylinesCommand => _addPolylinesCommand ?? (_addPolylinesCommand = new InternalRelayCommand(param => OnAddPolylinesCommand()));

        private void OnAddPolylinesCommand()
        {
            var points = GetPoints();
            foreach(var point in points)
            {
                var linePoints = new List<MapPoint>();
                for (int i = 0; i < 30; i++)
                {
                    linePoints.AddRange(GeometryEngine.MoveGeodetic(new[] {point}, 500, LinearUnits.Meters,
                        _random.Next(0, 360),
                        AngularUnits.Degrees, GeodeticCurveType.Geodesic));
                }
                
                var polyLine = new PolylineBuilder(linePoints, SpatialReferences.Wgs84);
                GraphicsProvider.Publish(new MapObject(polyLine.ToGeometry()) { LayerId = LayerKeys.Polyline.ToString() });
            }
        }

        private readonly Random _random = new Random();
        private List<MapPoint> GetPoints(int count = 300)
        {
            var items = new List<MapPoint>();

            var mapViewModel = Maps.FirstOrDefault();
            if(mapViewModel == null)
            {
                MessageBox.Show("You need a map to continue");
                return items;
            }
            var baseLayer = mapViewModel.Map.Basemap.BaseLayers.First();
            var viewpointExtent = (Envelope)GeometryEngine.Project(baseLayer.FullExtent.Extent, SpatialReferences.Wgs84);
            
            for (int i = 0; i < count; i++)
            {
                items.Add(GetLocation(viewpointExtent));
            }
            return items;
        }

        private MapPoint GetLocation(Envelope viewpointExtent)
        {
            double x = viewpointExtent.XMin + (_random.NextDouble() * viewpointExtent.Width);
            double y = viewpointExtent.YMin + (_random.NextDouble() * viewpointExtent.Height);
            var point = new MapPoint(x, y, viewpointExtent.SpatialReference);
            return (MapPoint)GeometryEngine.Project(point, SpatialReferences.Wgs84);
        }

        #region CreateMapCommand
        private ICommand _createMapCommand;

        public ICommand CreateMapCommand => _createMapCommand ??
                                            (_createMapCommand =
                                                new InternalRelayCommand(param => OnCreateMapCommand()));
        private async void OnCreateMapCommand()
        {
            var map = new MapViewModel(GraphicsProvider);
            Maps.Add(map);
            await map.Initialize();
        }
        #endregion

        #region DeleteMapCommand
        private ICommand _deleteMapCommand;
        private static ICommand _clearDataCommand;

        public ICommand DeleteMapCommand => _deleteMapCommand ??
                                            (_deleteMapCommand =
                                                new InternalRelayCommand(param => OnDeleteMapCommand(param as MapViewModel)));

        private void OnDeleteMapCommand(MapViewModel map)
        {
            map.OnClose();
            Maps.Remove(map);
            GC.Collect();
        }
        #endregion
    }
    #endregion
    public enum LayerKeys
    {
        Points,
        Polygon,
        Polyline
    }
    #region MapViewModel
    public class MapViewModel : ObservableObject
    {
        #region Private members
        private readonly List<GraphicsProviderObserver> _observers = new List<GraphicsProviderObserver>();
        private readonly IGraphicsProvider _graphicsProvider;
        private Map _map;
        #endregion

        #region Constructor
        public MapViewModel(IGraphicsProvider graphicsProvider)
        {
            _graphicsProvider = graphicsProvider;
            CreateOverlays();
        }

        #endregion

        #region Create and add overlay
        private void CreateOverlays()
        {
            var pointLayer = new GraphicsOverlay
            {
                Id = LayerKeys.Points.ToString(),
                Renderer = new SimpleRenderer
                {
                    Symbol = new SimpleMarkerSymbol
                    {
                        Color = System.Drawing.Color.Red,
                        Size = 15
                    }
                }
            };
            var polygonOverlay = new GraphicsOverlay
            {
                Id = LayerKeys.Polygon.ToString(),
                Renderer = new SimpleRenderer
                {
                    Symbol = new SimpleFillSymbol
                    {
                        Color = System.Drawing.Color.FromArgb(120, System.Drawing.Color.Black),
                        Style = SimpleFillSymbolStyle.Solid,
                        Outline = new SimpleLineSymbol
                        {
                            Color = System.Drawing.Color.FromArgb(170, System.Drawing.Color.Black),
                            Style = SimpleLineSymbolStyle.Solid,
                            Width = 2
                        }
                    }
                }
            };
            var polylineOverlay = new GraphicsOverlay
            {
                Id = LayerKeys.Polyline.ToString(),
                Renderer = new SimpleRenderer
                {
                    Symbol = new SimpleLineSymbol
                    {
                        Color = System.Drawing.Color.FromArgb(170, System.Drawing.Color.Blue),
                        Style = SimpleLineSymbolStyle.Solid,
                        Width = 4
                    }
                }
            };
            AddOverlays(pointLayer, polygonOverlay, polylineOverlay);
        }

        private void AddOverlays(params GraphicsOverlay[] overlays)
        {
            var overlayCollection = new GraphicsOverlayCollection();
            foreach (var overlay in overlays)
            {
                var observer = new GraphicsProviderObserver(_graphicsProvider, overlay);
                _observers.Add(observer);
                overlayCollection.Add(overlay);
            }
            GraphicOverlays = overlayCollection;
        }
        #endregion

        #region Public properties
        public Map Map
        {
            get
            {
                return _map;
            }
            set
            {
                if (_map == value)
                {
                    return;
                }

                _map = value;
                OnPropertyChanged(nameof(Map));
            }
        }
        private GraphicsOverlayCollection _graphicOverlays;
        public GraphicsOverlayCollection GraphicOverlays
        {
            get => _graphicOverlays;
            private set
            {
                if (_graphicOverlays == value)
                {
                    return;
                }

                _graphicOverlays = value;
                OnPropertyChanged(nameof(GraphicOverlays));
            }
        }
        #endregion

        private Map CreateMap()
        {
            var map = new Map
            {
                MinScale = 20480000
            };
            map.InitialViewpoint = new Viewpoint(new MapPoint(10, 59, SpatialReferences.Wgs84), map.MinScale);
            var baseLayer = new ArcGISTiledLayer
            {
                Source = new Uri("https://services2.geodataonline.no/arcgis/rest/services/Geocache_UTM32_EUREF89/GeocacheGraatone/MapServer")
            };
            map.Basemap.BaseLayers.Add(baseLayer);

            return map;
        }

        public async Task Initialize()
        {
            Map = CreateMap();
            await Map.LoadAsync();
            foreach (var observer in _observers)
            {
                observer.Initialize();
            }
        }

        public void OnClose()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
        }
    }
    #endregion


}
