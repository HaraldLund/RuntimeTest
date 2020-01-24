using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
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

        private Map _map = new Map();
        private bool _ZoomingTriggered = false;
        private int _ZoomIntervall = 0;
        private  DateTime _startTime;
        private int _repeats = 5;
        private  int _currentRepeat = 1;
        private List<int> _seconds = new List<int>();
        private string _File;
        private readonly ViewpointProvider _viewpointProvider;
        

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
            _viewpointProvider = new ViewpointProvider();
            ZoomSimulator = new ZoomSimulator(_viewpointProvider, new ZoomProvider(MyMapView));
            LegacyZoomSimulator = new ZoomSimulator(_viewpointProvider, new LegacyZoomProvider(LegacyMap));
            _map.LoadStatusChanged += _map_LoadStatusChanged;
        }
        public static readonly DependencyProperty LegacyZoomSimulatorProperty = DependencyProperty.Register("LegacyZoomSimulator", typeof(ZoomSimulator), typeof(MainWindow), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty ZoomSimulatorProperty = DependencyProperty.Register("ZoomSimulator", typeof(ZoomSimulator), typeof(MainWindow), new FrameworkPropertyMetadata(null));
        
        

        private void _map_LoadStatusChanged(object sender, Esri.ArcGISRuntime.LoadStatusEventArgs e)
        {
            if (sender is Map map && e.Status == Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                Dispatcher.BeginInvoke((Action)(() => LegacyMapLoader.InitializeLegacy(LegacyMap, map)));
            }
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
                    doZooming();
                }
            }
        }

        // private void OnDrawFinished(Object sender, GPSPositionEventArgs e)

        public void UpdateStatus(bool isComplete)
        {
            Dispatcher.Invoke(delegate ()
            {
                // Show the activity indicator if the map is drawing
                if (isComplete)
                {
                    UpdateTileCount(DateTime.Now);

                    if (DrawFinished != null)
                    {
                        // TODO: Replaced by ZoomProvider
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
            string runtimeversion = Assembly.GetAssembly(typeof(Map)).GetName().Version.ToString();
            builder.AppendLine($"RuntimeVersion:{runtimeversion} ");
            if (tileRequestDetails.Count == 0)
                builder.AppendLine($"Missing Tile Information");
            else
                builder.AppendLine(
                $"{tileRequestDetails.Sum(m => m.Tiles)} tiles between LoD {tileRequestDetails.Min(m => m.Level)} -> {tileRequestDetails.Max(m => m.Level)}");
            if (_ZoomIntervall < _viewpointProvider.Count)
            {
                csvfile.AppendLine($"Zoom Extent; Number; {_ZoomIntervall.ToString()};");
                builder.AppendLine($"Zoom Extent; Number; {_ZoomIntervall.ToString()};");
                //csvfile.AppendLine($"{_viewpointProvider[_ZoomIntervall].Parts[0].Points[0].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[0].Y.ToString()};{_ZoomAreas[_ZoomIntervall].Parts[0].Points[1].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[1].Y.ToString()};{_ZoomAreas[_ZoomIntervall].Parts[0].Points[2].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[2].Y.ToString()};{_ZoomAreas[_ZoomIntervall].Parts[0].Points[3].X.ToString()}, {_ZoomAreas[_ZoomIntervall].Parts[0].Points[3].Y.ToString()}");
                csvfile.AppendLine($"Zoom Duration Seconds;;;");
                //csvfile.AppendLine($"{_seconds[_ZoomIntervall].ToString()};;;");

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

            if (_ZoomingTriggered && _ZoomIntervall < _viewpointProvider.Count)
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

        public static readonly DependencyProperty SummaryProperty = DependencyProperty.Register("Summary", typeof(string), typeof(MainWindow), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty UseAnimationProperty = DependencyProperty.Register("UseAnimation", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(true));


        public ZoomSimulator ZoomSimulator
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (ZoomSimulator)GetValue(ZoomSimulatorProperty);
            }
            set
            {
                SetValue(ZoomSimulatorProperty, value);
            }
        }
        public ZoomSimulator LegacyZoomSimulator
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (ZoomSimulator)GetValue(LegacyZoomSimulatorProperty);
            }
            set
            {
                SetValue(LegacyZoomSimulatorProperty, value);
            }
        }
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

        private async void doWheelZoomSim(Viewpoint viewpoint)
        {
            try
            {
                // Set Viewpoint of MapView to the Viewpoint created above and animate to it using a timespan of 5 seconds
                _startTime = DateTime.Now;
                await MyMapView.SetViewpointAsync(viewpoint);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }

        }


        private void doZooming()
        {
            var nextViewpoint = _viewpointProvider.GetNext();
            if (nextViewpoint == null)
            {
                if (_currentRepeat < _repeats)
                {
                    _viewpointProvider.Reset();
                    nextViewpoint = _viewpointProvider.GetNext();
                    _currentRepeat++;
                }
                else
                {
                    _ZoomingTriggered = false;
                    DrawFinished -= new DrawFinishedHandler(OnDrawFinished);
                    MyMapView.SetViewpoint(
                        new Viewpoint(MyMapView.Map.AllLayers[0].FullExtent));
                    return;
                }
            }

            int delay = 0; // TODO: get sleep from provider?
            if (delay == 0) delay++;
            //System.Threading.Thread.Sleep(delay * 2000);
            Task.Run(() => doWheelZoomSim(nextViewpoint)); 

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
            _currentRepeat = 1;
            _File = Path.Combine(AssemblyDirectory, $"ZoomLog{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.csv");
            using (StreamWriter sw = File.AppendText(_File))
            {
                sw.WriteLineAsync(Assembly.GetAssembly(typeof(Map)).GetName().Version.ToString());
            };
            _viewpointProvider.Reset();
            doZooming();
            //MyMapView.DrawStatusChanged -= MyMapView_DrawStatusChanged;

        }

        private async void buttonz_Click(object sender, RoutedEventArgs e)
        {
             await ZoomSimulator.Run();
        }

        private async void StartSimulateLegacy(object sender, RoutedEventArgs e)
        {
            await LegacyZoomSimulator.Run();
        }
    }



    #region ZoomSimulator
    public class ZoomSimulator : INotifyPropertyChanged
    {
        #region Private members
        private Visibility _isRunning = Visibility.Collapsed;
        private readonly ViewpointProvider _viewpointProvider;
        private readonly ZoomProviderBase _zoomProvider;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly string _logFile;
        #endregion

        public ZoomSimulator(ViewpointProvider viewpointProvider, ZoomProviderBase zoomProvider)
        {
            _viewpointProvider = viewpointProvider;
            _zoomProvider = zoomProvider;
            Items = new ObservableCollection<LevelDetails>();
            _logFile = Path.Combine(MainWindow.AssemblyDirectory, $"ZoomLog-{zoomProvider.RuntimeVersion}.csv");
            LogToCsv("runtimeversion;duration;tilecount;scale;center");
        }

        public ObservableCollection<LevelDetails> Items { get; }

        public Visibility IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (_isRunning == value)
                    return;
                _isRunning = value;
                OnPropertyChanged();
            }
        }


        public async Task Run()
        {
            Items.Clear();
            IsRunning = Visibility.Visible;
            _viewpointProvider.Reset();
            await ExecuteZoom();
        }

        private async Task<bool> ExecuteZoom()
        {
            // TODO: do multiple iterations?
            var viewpoint = _viewpointProvider.GetNext();
            if (viewpoint == null)
            {
                return false;
            }

            await Task.Delay(100);
            _zoomProvider.DrawFinished += _zoomProvider_DrawFinished;
            _watch.Restart();
            await _zoomProvider.ZoomTo(viewpoint);
            return true;
        }

        private async void _zoomProvider_DrawFinished(object sender, DrawFinishEventArgs e)
        {
            _watch.Stop();
            if (_watch.ElapsedMilliseconds > 0)
            {
                var x = new LevelDetails
                {
                    Center = (MapPoint)e.Viewpoint.TargetGeometry,
                    Duration = _watch.Elapsed,
                    RuntimeVersion = e.Runtime,
                    Scale = (int)e.Viewpoint.TargetScale,
                    TileCount = e.RequestedTiles
                };
                LogData(x);
            }
            if (!await ExecuteZoom())
            {
                _zoomProvider.DrawFinished -= _zoomProvider_DrawFinished;
                IsRunning = Visibility.Collapsed;
            }
        }

        private void LogData(LevelDetails details)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() => Items.Insert(0, details)));
            // runtime version; Elapsed time; tile count; scale; geometry
            LogToCsv($"{details.RuntimeVersion};{(int)details.Duration.TotalMilliseconds};{details.TileCount};{details.Scale};{details.Center}");
            // TODO: Log to CVS
        }

        private void LogToCsv(string text)
        {
            using (StreamWriter sw = File.AppendText(_logFile))
            {
                sw.WriteLine(text);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion

    #region ZoomProviderBase, DrawFinishEventArgs, LevelDetails
    public class DrawFinishEventArgs : EventArgs
    {
        #region Constructor
        public DrawFinishEventArgs(Viewpoint viewpoint, int requestedTiles, string runtime)
        {
            Viewpoint = viewpoint;
            RequestedTiles = requestedTiles;
            Runtime = runtime;
        }
        #endregion

        #region Public properties
        public Viewpoint Viewpoint { get; }
        public int RequestedTiles { get; }
        public string Runtime { get; }

        #endregion
    }

    public class LevelDetails
    {
        public int TileCount { get; set; }
        public string RuntimeVersion { get; set; }
        public TimeSpan Duration { get; set; }
        public MapPoint Center { get; set; }
        public int Scale { get; set; }
    }

    public abstract class ZoomProviderBase
    {
        public event EventHandler<DrawFinishEventArgs> DrawFinished;

        protected void OnDrawFinished(Viewpoint viewpoint, int tileCount, string runtime)
        {
            DrawFinished?.Invoke(this, new DrawFinishEventArgs(viewpoint, tileCount, runtime));
        }

        public abstract Task ZoomTo(Viewpoint viewpoint);

        public abstract string RuntimeVersion { get; }
    }
    #endregion

    #region ZoomProvider
    public class ZoomProvider : ZoomProviderBase
    {
        #region Private members
        private readonly MapView _mapView;
        private Viewpoint _currentViewpoint;
        private int _tileCount;
        private readonly string _runtimeVersion;
        #endregion

        #region Constructor
        public ZoomProvider(MapView mapView)
        {
            _mapView = mapView;
            _runtimeVersion = mapView.GetType().Assembly.GetName().Version.ToString();
        }

        public override string RuntimeVersion => _runtimeVersion;

        #endregion

        private void _mapView_DrawStatusChanged(object sender, DrawStatusChangedEventArgs e)
        {
            if (e.Status == DrawStatus.Completed)
            {
                Esri.ArcGISRuntime.Http.ArcGISHttpClientHandler.HttpRequestBegin -= ArcGISHttpClientHandler_HttpRequestBegin;
                _mapView.DrawStatusChanged -= _mapView_DrawStatusChanged;
                OnDrawFinished(_currentViewpoint, _tileCount, _runtimeVersion);
                _tileCount = 0;
            }
        }

        public override async Task ZoomTo(Viewpoint viewpoint)
        {
            _currentViewpoint = viewpoint;
            _mapView.DrawStatusChanged += _mapView_DrawStatusChanged;
            Esri.ArcGISRuntime.Http.ArcGISHttpClientHandler.HttpRequestBegin += ArcGISHttpClientHandler_HttpRequestBegin;
            await _mapView.SetViewpointAsync(viewpoint);
        }

        private void ArcGISHttpClientHandler_HttpRequestBegin(object sender, System.Net.Http.HttpRequestMessage e)
        {
            _tileCount++;
        }
    }
    #endregion


}
