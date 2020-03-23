using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WindowsInput;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace MapRefresh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IMouseSim
    {
        #region Private members
        private readonly ViewpointProvider _viewpointProvider;
        #endregion

        private bool _startAutomatic = false;
        private bool _useOldversion = false;
        InitConfig _configuration;
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            _configuration = new InitConfig();

            //string url = "https://services2.geodataonline.no/arcgis/rest/services/Geocache_UTM33_EUREF89/GeocacheBasis/MapServer";
            ////url = "https://services.geodataonline.no/arcgis/rest/services/Geocache_WMAS_WGS84/GeocacheBasis/MapServer";
            //var imageryTiledLayer = new ArcGISTiledLayer(new Uri(url));
            //// create a basemap from the tiled layer

            //_map.Basemap = new Basemap(imageryTiledLayer);
            //MyMapView.Map = _map;

            _viewpointProvider = new ViewpointProvider(_configuration.Configuration.ActualViewpoints);
            ZoomSimulator = new ZoomSimulator(_viewpointProvider, new ZoomProvider(MyMapView), this);
            ZoomSimulator.ProcessFinished += ZoomSimulator_ProcessFinished;
            LegacyZoomProvider lzp = new LegacyZoomProvider(LegacyMap);
            LegacyZoomSimulator = new ZoomSimulator(_viewpointProvider, lzp, this);
            LegacyZoomSimulator.ProcessFinished += LegacyZoomSimulator_ProcessFinished;

            InitializeMap();
            
        }

        private void LegacyZoomSimulator_ProcessFinished(object sender, EventArgs e)
        {
            if (_startAutomatic) Application.Current.Shutdown();
        }

        private void ZoomSimulator_ProcessFinished(object sender, EventArgs e)
        {
            if (_startAutomatic) Application.Current.Shutdown();
        }


        Map _map = null;
        private async void InitializeMap()
        {
            var map = await _configuration.GetMap();
            _startAutomatic = _configuration.Configuration.startAutomatic;
            _useOldversion = _configuration.Configuration.oldVersion;
            MyMapView.Map = map;
            _map = map;
            map.LoadStatusChanged += _map_LoadStatusChanged;
            LegacyMapLoader.MapLoaded += Map_Loaded;
            await map.LoadAsync();
            tabMaps.SelectedIndex = 1;
        }

     
        private void Map_Loaded(object sender, EventArgs e)
        {
            LegacyMapLoader.MapLoaded -= Map_Loaded;
            if (_startAutomatic)
            {
                if (!_useOldversion)
                {
                    tabMaps.SelectedIndex = 0;
                    buttonz_Click(this, null);
                }
                else
                {
                    tabMaps.SelectedIndex = 1;
                    StartSimulateLegacy(this, null);
                }
            }
            else tabMaps.SelectedIndex = 0;
         
        }

        private void _map_LoadStatusChanged(object sender, Esri.ArcGISRuntime.LoadStatusEventArgs e)
        {
            if (sender is Map map && e.Status == Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                Dispatcher.BeginInvoke((Action)(() => LegacyMapLoader.InitializeLegacy(LegacyMap, map)));
            }
        }

        public static readonly DependencyProperty LegacyZoomSimulatorProperty = DependencyProperty.Register("LegacyZoomSimulator", typeof(ZoomSimulator), typeof(MainWindow), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty ZoomSimulatorProperty = DependencyProperty.Register("ZoomSimulator", typeof(ZoomSimulator), typeof(MainWindow), new FrameworkPropertyMetadata(null));
        


        public static readonly DependencyProperty SummaryProperty = DependencyProperty.Register("Summary", typeof(string), typeof(MainWindow), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty UseWheelProperty = DependencyProperty.Register("UseWheel", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(false));

        public bool UseWheel
        {
            get
            {
                return (bool)GetValue(UseWheelProperty);
            }
            set
            {
                SetValue(UseWheelProperty, value);
            }
        }
        public ZoomSimulator ZoomSimulator
        {
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
            get
            {
                return (ZoomSimulator)GetValue(LegacyZoomSimulatorProperty);
            }
            set
            {
                SetValue(LegacyZoomSimulatorProperty, value);
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

        private async void buttonz_Click(object sender, RoutedEventArgs e)
        {
             await ZoomSimulator.RunZoomSimulation(UseWheel ? SimulationMode.Wheel : SimulationMode.SetView);
        }

        private async void StartSimulateLegacy(object sender, RoutedEventArgs e)
        {
            await LegacyZoomSimulator.RunZoomSimulation(UseWheel ? SimulationMode.Wheel : SimulationMode.SetView);
        }
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
            //Trace.WriteLine($"{e.Delta} {e.Source} {e.GetPosition(this)}");
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if(e.Key == Key.Space)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    try
                    {
                        var targetFile = "Viewpoints.json";
                        var json = string.Join($",{Environment.NewLine}", _viewpoints.Select(JsonAsString));
                        File.WriteAllText(targetFile, json);
                        MessageBox.Show(this, $"Saved to file {targetFile}");
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                    }
                }
                else
                {
                    var viewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
                    Trace.WriteLine(viewpoint.ToJson());
                    _viewpoints.Add(viewpoint);
                }
            }
        }

        private string JsonAsString(Viewpoint arg)
        {
            return $"\"{arg.ToJson().Replace("\"", "\\\"")}\"";
        }

        private List<Viewpoint> _viewpoints = new List<Viewpoint>();
        #region Simulate wheel
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        private static void SetCursor(double x, double y)
        {
            // Left boundary
            var xL = (int)App.Current.MainWindow.Left;
            // Right boundary
            var xR = xL + (int)App.Current.MainWindow.Width;
            // Top boundary
            var yT = (int)App.Current.MainWindow.Top;
            // Bottom boundary
            var yB = yT + (int)App.Current.MainWindow.Height;

            x += xL;
            y += yT;

            if (x < xL)
            {
                x = xL;
            }
            else if (x > xR)
            {
                x = xR;
            }

            if (y < yT)
            {
                y = yT;
            }
            else if (y > yB)
            {
                y = yB;
            }

            SetCursorPos((int)x, (int)y);
        }

        private int _tickDelay = 30;

        public async Task WheelZoomOut()
        {
            InputSimulator ss = new InputSimulator();
            for (int i = 0; i < 9; i++)
            {
                // Perform 9 calls to emulate a full roll of the wheel
                ss.Mouse//.MoveMouseTo(pos.X, pos.Y)
                    .VerticalScroll(-1);
                await Task.Delay(_tickDelay);
            }
        }
        public async Task WheelZoomIn()
        {
            SetMouseInCenter();
            InputSimulator ss = new InputSimulator();
            for (int i = 0; i < 9; i++)
            {

                // Perform 9 calls to emulate a full roll of the wheel
                ss.Mouse//.MoveMouseTo(pos.X, pos.Y)
                    .VerticalScroll(1);
                await Task.Delay(_tickDelay);
            }
        }

        private void SetMouseInCenter()
        {
            FrameworkElement target = MyMapView;
            if(!target.IsVisible)
            {
                target = LegacyMap;
            }
            if (!target.IsKeyboardFocused)
            {
                target.Focus();
            }
            var position = new Point(target.ActualWidth / 2d, MyMapView.ActualHeight / 2d);

            var pos = target.TranslatePoint(position, this);
            pos = this.PointToScreen(pos);
            //Trace.WriteLine(pos);
            SetCursor(pos.X, pos.Y);
            //Trace.WriteLine(Mouse.GetPosition(this));
        }

        #endregion
    }

    public interface IMouseSim
    {
        Task WheelZoomIn();
        Task WheelZoomOut();
    }

    #region ZoomSimulator
    public class ZoomSimulator : INotifyPropertyChanged
    {
        #region Private members
        private int _totalTiles;
        private TimeSpan _totalTime;
        private Visibility _isRunning = Visibility.Collapsed;
        private readonly ViewpointProvider _viewpointProvider;
        private readonly IMouseSim _mouseSimulator;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly string _logFile;
        private bool _zoomIn;
        private int _fullProgress;
        private int _currentProgress;
        private SimulationMode _currentMode;
        private int _execution;
        #endregion

        public ZoomSimulator(ViewpointProvider viewpointProvider, ZoomProviderBase zoomProvider, IMouseSim mouseSimulator)
        {
            _viewpointProvider = viewpointProvider;
            Provider = zoomProvider;
            _mouseSimulator = mouseSimulator;
            Items = new ObservableCollection<LevelDetails>();
            _logFile = Path.Combine(MainWindow.AssemblyDirectory, $"ZoomLog-{zoomProvider.RuntimeVersion}.csv");
        }

        #region Public properties
        public ZoomProviderBase Provider { get; }
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

        public TimeSpan TotalTime
        {
            get { return _totalTime; }
            set
            {
                if (_totalTime == value)
                    return;
                _totalTime = value;
                OnPropertyChanged();
            }
        }


        public int TotalTiles
        {
            get { return _totalTiles; }
            set
            {
                if (_totalTiles == value)
                    return;
                _totalTiles = value;
                OnPropertyChanged();
            }
        }

        public int FullProgress
        {
            get => _fullProgress;
            set
            {
                if (_fullProgress != value)
                {
                    _fullProgress = value;
                    OnPropertyChanged(nameof(FullProgress));
                }
            }
        }

        public int CurrentProgress
        {
            get => _currentProgress;
            set
            {
                if (_currentProgress != value)
                {
                    _currentProgress = value;
                    OnPropertyChanged(nameof(CurrentProgress));
                }
            }
        }
        #endregion
        public async Task RunZoomSimulation(SimulationMode mode)
        {
            _currentMode = mode;
            FullProgress = _viewpointProvider.Count;
            CurrentProgress = 0;
            TotalTime = TimeSpan.Zero;
            TotalTiles = 0;
            Items.Clear();
            IsRunning = Visibility.Visible;
            _viewpointProvider.Reset();
            await ExecuteZoom(mode);
        }

        private async Task<bool> ExecuteZoom(SimulationMode mode)
        {
            // TODO: do multiple iterations?
            var viewpoint = _viewpointProvider.GetNext();
            if (viewpoint == null)
            {
                ProcessFinished(this, null);
                return false;                
            }

            await Task.Delay(500);
            if (mode == SimulationMode.SetView)
            {
                Provider.DrawFinished += _zoomProvider_DrawFinished;
                _watch.Restart();
                Trace.WriteLine($"Subscribed {_currentMode}");
                await Provider.ZoomTo(viewpoint);
            }
            else
            {
                var targetViewpoint = new Viewpoint((MapPoint)viewpoint.TargetGeometry, _zoomIn ? 625 : 50000);
                await Provider.ZoomTo(targetViewpoint);

                Provider.DrawFinished += _zoomProvider_DrawFinished;
                Trace.WriteLine($"Subscribed {_currentMode}");
                _watch.Restart();
                for (int i = 0; i < 9; i++)
                {
                    _watch.Stop();
                    await Task.Delay(200);
                    _watch.Start();
                    if (_zoomIn)
                    {
                        await _mouseSimulator.WheelZoomIn();
                    }
                    else
                    {
                        await _mouseSimulator.WheelZoomOut();
                    }
                }

                _zoomIn = !_zoomIn;
            }

            return true;
        }

        private async void _zoomProvider_DrawFinished(object sender, DrawFinishEventArgs e)
        {
            CurrentProgress++;
            _watch.Stop();
            TotalTiles += e.RequestedTiles;
            TotalTime += _watch.Elapsed;
            Provider.DrawFinished -= _zoomProvider_DrawFinished;
            Trace.WriteLine($"Unsubscribed {_currentMode}");
            if (_watch.ElapsedMilliseconds > 0)
            {
                var x = new LevelDetails
                {
                    Center = (MapPoint)e.Viewpoint.TargetGeometry,
                    Duration = _watch.Elapsed,
                    RuntimeVersion = e.Runtime,
                    Scale = (int)e.Viewpoint.TargetScale,
                    TileCount = e.RequestedTiles,
                    TimeStamp = e.TimeStamp,
                    Execution = ++_execution
                };
                LogData(x);
            }
            if (!await ExecuteZoom(_currentMode))
            {
                IsRunning = Visibility.Collapsed;
            }
        }

        private void LogData(LevelDetails details)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() => Items.Insert(0, details)));
            // runtime version; Elapsed time; tile count; scale; geometry
            LogToCsv($"{details.TimeStamp.ToString("- yyyy-MM-dd HH:mm:ss.ffff -", CultureInfo.InvariantCulture)};{details.RuntimeVersion};{_currentMode};{(int)details.Duration.TotalMilliseconds};{details.TileCount};{details.Scale};{details.Center};{details.Execution}");
            // TODO: Log to CVS
        }

        private void LogToCsv(string text)
        {
            bool logHeader = !File.Exists(_logFile);
            using (StreamWriter sw = File.AppendText(_logFile))
            {
                if(logHeader)
                {
                    sw.WriteLine("timestamp;runtimeversion;mode;duration;tilecount;scale;center;execution");
                }
                sw.WriteLine(text);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public event ProcessFinishedEventHandler ProcessFinished;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public delegate void ProcessFinishedEventHandler(object sender, EventArgs e);

        

    }

    public enum SimulationMode
    {
        SetView,
        Wheel
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
            TimeStamp = DateTime.Now;
        }
        #endregion

        #region Public properties
        public Viewpoint Viewpoint { get; }
        public int RequestedTiles { get; }
        public string Runtime { get; }
        public DateTime TimeStamp { get; }
        #endregion
    }

    public class LevelDetails
    {
        public int TileCount { get; set; }
        public string RuntimeVersion { get; set; }
        public TimeSpan Duration { get; set; }
        public MapPoint Center { get; set; }
        public int Scale { get; set; }
        public DateTime TimeStamp { get; set; }
        public int Execution { get; set; }
    }

    public abstract class ZoomProviderBase : INotifyPropertyChanged
    {
        public event EventHandler<DrawFinishEventArgs> DrawFinished;

        protected void OnDrawFinished(Viewpoint viewpoint, int tileCount, string runtime)
        {
            DrawFinished?.Invoke(this, new DrawFinishEventArgs(viewpoint, tileCount, runtime));
        }

        public abstract Task ZoomTo(Viewpoint viewpoint);

        public abstract string RuntimeVersion { get; }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract bool IsZoomDurationActive { get; set; }
        
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
        private bool _isZoomDurationActive;

        #endregion

        #region Constructor
        public ZoomProvider(MapView mapView)
        {
            _mapView = mapView;
            _runtimeVersion = mapView.GetType().Assembly.GetName().Version.ToString();
        }

        public override string RuntimeVersion => _runtimeVersion;

        #endregion

        public override bool IsZoomDurationActive
        {
            get => _isZoomDurationActive;
            set
            {
                if (_isZoomDurationActive != value)
                {
                    _isZoomDurationActive = value;
                    OnPropertyChanged(nameof(IsZoomDurationActive));
                }
            }
        }

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
            if (IsZoomDurationActive)
            {
                await _mapView.SetViewpointAsync(viewpoint);
            }
            else
            {
                await _mapView.SetViewpointAsync(viewpoint, TimeSpan.Zero);
            }
        }

        private void ArcGISHttpClientHandler_HttpRequestBegin(object sender, System.Net.Http.HttpRequestMessage e)
        {
            _tileCount++;
        }
    }
    #endregion

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool isChecked)
            {
                return !isChecked;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
