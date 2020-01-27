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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WindowsInput;
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
    public partial class MainWindow : IMouseSim
    {
        #region Private members
        private ICommand _clearRequestsCommand;
        private Map _map = new Map();
        private readonly ViewpointProvider _viewpointProvider;
        #endregion

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            string url = "https://services2.geodataonline.no/arcgis/rest/services/Geocache_UTM33_EUREF89/GeocacheBasis/MapServer";
            //url = "https://services.geodataonline.no/arcgis/rest/services/Geocache_WMAS_WGS84/GeocacheBasis/MapServer";
            var imageryTiledLayer = new ArcGISTiledLayer(new Uri(url));
            // create a basemap from the tiled layer

            _map.Basemap = new Basemap(imageryTiledLayer);
            MyMapView.Map = _map;
            _viewpointProvider = new ViewpointProvider();
            ZoomSimulator = new ZoomSimulator(_viewpointProvider, new ZoomProvider(MyMapView), this);
            LegacyZoomSimulator = new ZoomSimulator(_viewpointProvider, new LegacyZoomProvider(LegacyMap), this);
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
                Trace.WriteLine(MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).ToJson());
            }
        }

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
        private Visibility _isRunning = Visibility.Collapsed;
        private readonly ViewpointProvider _viewpointProvider;
        private readonly ZoomProviderBase _zoomProvider;
        private readonly IMouseSim _mouseSimulator;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly string _logFile;
        #endregion

        public ZoomSimulator(ViewpointProvider viewpointProvider, ZoomProviderBase zoomProvider, IMouseSim mouseSimulator)
        {
            _viewpointProvider = viewpointProvider;
            _zoomProvider = zoomProvider;
            _mouseSimulator = mouseSimulator;
            Items = new ObservableCollection<LevelDetails>();
            _logFile = Path.Combine(MainWindow.AssemblyDirectory, $"ZoomLog-{zoomProvider.RuntimeVersion}.csv");
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
        private SimulationMode _currentMode;
        public async Task RunZoomSimulation(SimulationMode mode)
        {
            _currentMode = mode;
            Items.Clear();
            IsRunning = Visibility.Visible;
            _viewpointProvider.Reset();
            await ExecuteZoom(mode);
        }

        private bool _zoomIn;
        private async Task<bool> ExecuteZoom(SimulationMode mode)
        {
            // TODO: do multiple iterations?
            var viewpoint = _viewpointProvider.GetNext();
            if (viewpoint == null)
            {
                return false;
            }

            await Task.Delay(500);
            if (mode == SimulationMode.SetView)
            {
                _zoomProvider.DrawFinished += _zoomProvider_DrawFinished;
                Trace.WriteLine("Subscribed");
                _watch.Restart();
                await _zoomProvider.ZoomTo(viewpoint);
            }
            else
            {
                var targetViewpoint = new Viewpoint((MapPoint)viewpoint.TargetGeometry, _zoomIn ? 625 : 50000);
                //var zoom = _zoomProvider.ZoomTo(targetViewpoint);
                await _zoomProvider.ZoomTo(targetViewpoint);
                _watch.Restart();
                _zoomProvider.DrawFinished += _zoomProvider_DrawFinished;
                //Trace.WriteLine("Subscribed");
                for(int i = 0; i < 8; i++)
                {
                    await Task.Delay(200);
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
            _watch.Stop();
            if (_currentMode == SimulationMode.Wheel)
            {
                _zoomProvider.DrawFinished -= _zoomProvider_DrawFinished;
                //Trace.WriteLine("Unsubscribed");
            }
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
            if (!await ExecuteZoom(_currentMode))
            {
                if (_currentMode == SimulationMode.SetView)
                {
                    _zoomProvider.DrawFinished -= _zoomProvider_DrawFinished;
                    //Trace.WriteLine("Unsubscribed");
                }
                IsRunning = Visibility.Collapsed;
            }
        }

        private void LogData(LevelDetails details)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() => Items.Insert(0, details)));
            // runtime version; Elapsed time; tile count; scale; geometry
            LogToCsv($"{details.RuntimeVersion};{_currentMode};{(int)details.Duration.TotalMilliseconds};{details.TileCount};{details.Scale};{details.Center}");
            // TODO: Log to CVS
        }

        private void LogToCsv(string text)
        {
            bool logHeader = !File.Exists(_logFile);
            using (StreamWriter sw = File.AppendText(_logFile))
            {
                if(logHeader)
                {
                    sw.WriteLine("runtimeversion;mode;duration;tilecount;scale;center");
                }
                sw.WriteLine(text);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
