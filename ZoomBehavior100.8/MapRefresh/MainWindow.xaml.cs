using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using MapRefresh.View.ViewModel;

namespace MapRefresh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Private members
        private ICommand _clearRequestsCommand;
        private string _navigationId = Guid.NewGuid().ToString();
        private readonly ConcurrentQueue<string> _temporaryRequestList = new ConcurrentQueue<string>();
        private readonly ConcurrentDictionary<string, TileRequestDetails> _requestDictionary =
            new ConcurrentDictionary<string, TileRequestDetails>();
        private Envelope _initialExtent;
        private ICommand _setViewCommand;

        private Map _map = new Map();
        private bool _ZoomingTriggered = false;
        private int _ZoomIntervall = 0;
        private List<Polygon> _ZoomAreas = new List<Polygon>();
        private  DateTime _startTime;
        private int _repeats = 5;
        private  int _current = 1;
        private List<int> _seconds = new List<int>();
        private string _File;


        #endregion

        public MainWindow()
        {
            Items = new ObservableCollection<string>();
            DataContext = this;
            Esri.ArcGISRuntime.Http.ArcGISHttpClientHandler.HttpRequestBegin += ArcGISHttpClientHandler_HttpRequestBegin;
            InitializeComponent();

            string url = "https://services2.geodataonline.no/arcgis/rest/services/Geocache_UTM33_EUREF89/GeocacheBasis/MapServer";
            //url = "https://services.geodataonline.no/arcgis/rest/services/Geocache_WMAS_WGS84/GeocacheBasis/MapServer";
            var imageryTiledLayer = new ArcGISTiledLayer(new Uri(url));
            // create a basemap from the tiled layer

            _map.Basemap = new Basemap(imageryTiledLayer);
            MyMapView.Map = _map;
            MyMapView.DrawStatusChanged += MyMapView_DrawStatusChanged;
            BuildZoomAreas();
        }


        private void MyMapView_DrawStatusChanged(object sender, DrawStatusChangedEventArgs e)
        {
            UpdateStatus(e.Status == DrawStatus.Completed);
        }

        public delegate void DrawFinishedHandler(Object sender, EventArgs e);
        public event DrawFinishedHandler DrawFinished;

        public void OnDrawFinished(Object sender, EventArgs e)
        {

            if (DrawFinished != null)
            {
                if (_ZoomingTriggered)
                {
                    doZooming(_ZoomIntervall);
                }
            }
        }

        // private void OnDrawFinished(Object sender, GPSPositionEventArgs e)

        public void UpdateStatus(bool isComplete)
        {
            Dispatcher.Invoke(delegate ()
            {
                // Show the activity indicator if the map is drawing
                if (isComplete == false)
                {
                    ActivityIndicator.IsEnabled = true;
                    ActivityIndicator.Visibility = Visibility.Visible;
                }
                else
                {
                    ActivityIndicator.IsEnabled = false;
                    ActivityIndicator.Visibility = Visibility.Collapsed;
                    UpdateTileCount(DateTime.Now);

                    if (DrawFinished != null)
                    {
                        _ZoomIntervall++;
                        DrawFinished(this, new EventArgs());
                    }
                }
            });
        }

        private void UpdateTileCount(DateTime endTime)
        {
            var navId = _navigationId;
            _navigationId = Guid.NewGuid().ToString();
            var tileRequestDetails = _requestDictionary.ToList()
                .Select(m => m.Value)
                .Where(m => m.NavigationId == navId)
                .OrderBy(m => m.Level)
                .ToList();
            _requestDictionary.Clear();
            Items.Clear();
            
            StringBuilder csvfile = new StringBuilder();
            StringBuilder builder = new StringBuilder();
            string runtimeversion =  Assembly.GetAssembly(typeof(Map)).GetName().Version.ToString();
            builder.AppendLine($"RuntimeVersion:{runtimeversion} ");
            if (tileRequestDetails.Count == 0)
                builder.AppendLine($"Missing Tile Information");
            else
                builder.AppendLine(
                $"{tileRequestDetails.Sum(m => m.Tiles)} tiles between LoD {tileRequestDetails.Min(m => m.Level)} -> {tileRequestDetails.Max(m => m.Level)}");
            if (_ZoomIntervall < _ZoomAreas.Count)
            {
                csvfile.AppendLine($"Zoom Extent; Number; {_ZoomIntervall.ToString()};");
                builder.AppendLine($"Zoom Extent; Number; {_ZoomIntervall.ToString()};");
                csvfile.AppendLine($"{_ZoomAreas[_ZoomIntervall].Parts[0].Points[0].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[0].Y.ToString()};{_ZoomAreas[_ZoomIntervall].Parts[0].Points[1].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[1].Y.ToString()};{_ZoomAreas[_ZoomIntervall].Parts[0].Points[2].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[2].Y.ToString()};{_ZoomAreas[_ZoomIntervall].Parts[0].Points[3].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[3].Y.ToString()}");
                csvfile.AppendLine($"Zoom Duration Seconds;;;");
                csvfile.AppendLine($"{_seconds[_ZoomIntervall].ToString()};;;");

            }

            csvfile.AppendLine($"Level;Layer;Tiles;Total Tiles");
            if (tileRequestDetails.Count != 0)
            {
                foreach (var lod in tileRequestDetails.Select(m => m.Level).Distinct())
                {
                    var forLevel = tileRequestDetails.Where(m => m.Level == lod)
                        .ToList();
                    builder.AppendLine($"\tLevel {lod} - Total tiles {forLevel.Sum(m => m.Tiles)}");
                    foreach (var item in forLevel.OrderBy(m => m.MapService))
                    {
                        builder.AppendLine($"\t\t{item.Tiles} tiles - ({item.MapService})");
                        csvfile.AppendLine($"{lod};{item.MapService};{item.Tiles};{forLevel.Sum(m => m.Tiles)}");
                    }
                }
            }

            TimeSpan duration = endTime.Subtract(_startTime);

            if (_ZoomingTriggered && _ZoomIntervall < _ZoomAreas.Count)
            {
                if (tileRequestDetails.Count != 0)
                    csvfile.AppendLine($"Sum tiles between LoD {tileRequestDetails.Min(m => m.Level)} -> {tileRequestDetails.Max(m => m.Level)}:;;;{tileRequestDetails.Sum(m => m.Tiles)}");
                else
                    csvfile.AppendLine($"Missing Tile Information");
                csvfile.AppendLine($"Duration drawing:;;;{duration.ToString(@"hh\:mm\:ss\.ffffff")}");
                builder.AppendLine($"\t\t Duration: {duration.ToString(@"hh\:mm\:ss\.ffffff")}");
                using (StreamWriter sw = File.AppendText(_File))
                {
                    sw.WriteLineAsync(csvfile.ToString());
                }
            }

            Summary = builder.ToString();
            int i = 1;
            while (!_temporaryRequestList.IsEmpty)
            {
                if (_temporaryRequestList.TryDequeue(out var item))
                {
                    Items.Add($"#{i++} {item}");
                }
            }

        }

        private void ArcGISHttpClientHandler_HttpRequestBegin(object sender, System.Net.Http.HttpRequestMessage e)
        {
            if (!e.RequestUri.PathAndQuery.Contains("/MapServer/tile/"))
            {
                return;
            }

            var requestDetails = TileRequestDetails.Parse(e.RequestUri, _navigationId);
            UpdateCount(requestDetails);
            _temporaryRequestList.Enqueue($"{requestDetails.MapService} LoD {requestDetails.Level}");
        }


        private void UpdateCount(TileRequestDetails requestDetails)
        {
            string key = $"{requestDetails.Level}_{requestDetails.NavigationId}_{requestDetails.MapService}";
            if (_requestDictionary.TryGetValue(key, out var info))
            {
                info.Tiles = info.Tiles + 1;
            }
            else
            {
                requestDetails.Tiles = 1;
                _requestDictionary.TryAdd(key, requestDetails);
            }
        }

        public static readonly DependencyProperty IsZoomOverrideProperty = DependencyProperty.Register("IsZoomOverride", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty SummaryProperty = DependencyProperty.Register("Summary", typeof(string), typeof(MainWindow), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty UseAnimationProperty = DependencyProperty.Register("UseAnimation", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(true));

        public bool UseAnimation
        {
            get
            {
                return (bool)GetValue(UseAnimationProperty);
            }
            set
            {
                SetValue(UseAnimationProperty, value);
            }
        }
        public bool IsZoomOverride
        {
            get
            {
                return (bool)GetValue(IsZoomOverrideProperty);
            }
            set
            {
                SetValue(IsZoomOverrideProperty, value);
            }
        }
        public string Summary
        {
            get
            {
                return (string)GetValue(SummaryProperty);
            }
            set
            {
                SetValue(SummaryProperty, value);
            }
        }
        public ObservableCollection<string> Items { get; }

        public ICommand ClearRequestsCommand => _clearRequestsCommand ??
                                                (_clearRequestsCommand =
                                                    new RelayCommand(param => OnClearRequestsCommand()));

        private void OnClearRequestsCommand()
        {
            Items.Clear();
            Summary = null;
        }

        public ICommand SetViewCommand => _setViewCommand ??
                                                (_setViewCommand =
                                                    new RelayCommand(param => OnSetViewCommand()));

        private async void OnSetViewCommand()
        {


            var center = GetRandomPoint();
            var scale = 625;
            if ((MyMapView.MapScale < 200000))
            {
                scale = 1000000;
            }
            if (UseAnimation)
            {
                await MyMapView.SetViewpointAsync(new Viewpoint(center, scale));
            }
            else
            {
                await MyMapView.SetViewpointAsync(new Viewpoint(center, scale), TimeSpan.Zero);
            }
        }

        private MapPoint GetRandomPoint()
        {

            if (_initialExtent == null)
            {
                _initialExtent = new Envelope(-247299.989220551, 6492055.69864222, 432816.894880079, 6992299.56471097, new SpatialReference(25833));
            }
            var x = new Random().Next((int)_initialExtent.XMin, (int)_initialExtent.XMax);
            var y = new Random().Next((int)_initialExtent.YMin, (int)_initialExtent.YMax);
            var center = new MapPoint(x, y, _initialExtent.SpatialReference);
            return center;
        }



        private void BuildZoomAreas()
        {
            _ZoomAreas.Add(CreateZoomOsloPolygons());
            _seconds.Add(1);
            _ZoomAreas.Add(CreateZoomOutOsloPolygons());
            _seconds.Add(2);
            _ZoomAreas.Add(CreateZoomOutOsloCenterPolygons());
            _seconds.Add(3);
            _ZoomAreas.Add(CreateZoomOutOsloEastPolygons());
            _seconds.Add(1);
            _ZoomAreas.Add(CreateZoomInOsloCenterEastPolygons());
            _seconds.Add(1);
        }

        private Polygon CreateZoomOsloPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(266437, 6651261, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266437, 6653668, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(258133, 6651261, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(258133, 6653668, new SpatialReference(25833)));

            return new Polygon(lsPoints);

        }

        private Polygon CreateZoomOutOsloPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(223062, 6635196, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(223062, 6673404, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(283568, 6635196, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(283568, 6673404, new SpatialReference(25833)));

            return new Polygon(lsPoints);

        }

        private Polygon CreateZoomOutOsloCenterPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(261914, 6649447, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(261914, 6649769, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(262406, 6649769, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(262406, 6649447, new SpatialReference(25833)));

            return new Polygon(lsPoints);

        }


        private Polygon CreateZoomOutOsloEastPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(261813, 6645050, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(261813, 6654888, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(277532, 6654888, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(277532, 6645050, new SpatialReference(25833)));

            return new Polygon(lsPoints);

        }

        private Polygon CreateZoomInOsloCenterEastPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(265410, 6648252, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(265410, 6648862, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266462, 6648862, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266462, 6648252, new SpatialReference(25833)));

            return new Polygon(lsPoints);

        }



        private async void doWheelZoomSim(int area, int seconds = 3)
        {
            // Create a new Viewpoint using the specified geometry
            Viewpoint viewpoint = new Viewpoint(_ZoomAreas[area]);


            try
            {

                // Set Viewpoint of MapView to the Viewpoint created above and animate to it using a timespan of 5 seconds
                _startTime = DateTime.Now;
                await MyMapView.SetViewpointAsync(viewpoint, TimeSpan.FromSeconds(seconds));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }

        }


        private void doZooming(int interval)
        {
            if (interval >= _ZoomAreas.Count && _current < _repeats)
            {
                _ZoomIntervall = 0;
                interval = 0;
                _current++;
            }
            if (interval >= _ZoomAreas.Count && _current >= _repeats)
            {
                _ZoomingTriggered = false;
                DrawFinished -= new DrawFinishedHandler(OnDrawFinished);
                MyMapView.SetViewpoint(
                new Viewpoint(MyMapView.Map.AllLayers[0].FullExtent));                
                return;
            }

            int delay = _seconds[interval];
            if (delay == 0) delay++;
            //System.Threading.Thread.Sleep(delay * 2000);
            Task.Run(() => doWheelZoomSim(interval, _seconds[interval])); 

        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            // MyMapView.DrawStatusChanged += MyMapView_DrawStatusChanged;

            // Navigate to full extent of the first baselayer before animating to specified geometry  

            MyMapView.SetViewpoint(
                new Viewpoint(MyMapView.Map.AllLayers[0].FullExtent));
            DrawFinished += new DrawFinishedHandler(OnDrawFinished);
            _ZoomingTriggered = true;
            _ZoomIntervall = 0;
            _current = 1;
            _File = Path.Combine(AssemblyDirectory, $"ZoomLog{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.csv");
            using (StreamWriter sw = File.AppendText(_File))
            {
                sw.WriteLineAsync(Assembly.GetAssembly(typeof(Map)).GetName().Version.ToString());
            };
           doZooming(_ZoomIntervall);
            //MyMapView.DrawStatusChanged -= MyMapView_DrawStatusChanged;

        }

    }
}
