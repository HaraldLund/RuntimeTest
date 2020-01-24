using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
//using System.Windows.Shapes;


namespace MapRefresh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Private members
        private ICommand _clearRequestsCommand;
        private string _navigationId = Guid.NewGuid().ToString();
        private readonly ConcurrentQueue<string> _temporaryRequestList = new ConcurrentQueue<string>();
        private readonly ConcurrentDictionary<string, TileRequestDetails> _requestDictionary =
            new ConcurrentDictionary<string, TileRequestDetails>();
        private Envelope _initialExtent;
        private ICommand _setViewCommand;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private Polygon _Oslo;

        private void CreateEdinbouroghPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(-354262.156621384, 7548092.94093301, new SpatialReference(3857)));
            lsPoints.Add(new MapPoint(-354262.156621384, 7548901.50684376, new SpatialReference(3857)));
            lsPoints.Add(new MapPoint(-353039.164455303, 7548092.94093301, new SpatialReference(3857)));
            lsPoints.Add(new MapPoint(-353039.164455303, 7548901.50684376, new SpatialReference(3857)));
                      
            _Oslo = new Polygon(lsPoints);
            
        }

        private void CreateZoomOsloPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(266437, 6651261, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266437, 6653668, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(258133, 6651261, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(258133, 6653668, new SpatialReference(25833)));

            _Oslo = new Polygon(lsPoints);

        }

        private void CreateZoomOutOsloPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(223062, 6635196, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(223062, 6673404, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(283568, 6635196, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(283568, 6673404, new SpatialReference(25833)));

            _Oslo = new Polygon(lsPoints);

        }

        private void CreateZoomOutOsloCenterPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(261914, 6649447, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(261914, 6649769, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(262406, 6649769, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(262406, 6649447, new SpatialReference(25833)));

            _Oslo = new Polygon(lsPoints);

        }


        private void CreateZoomOutOsloEastPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(261813, 6645050, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(261813, 6654888, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(277532, 6654888, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(277532, 6645050, new SpatialReference(25833)));

            _Oslo = new Polygon(lsPoints);

        }

        private void CreateZoomInOsloCenterEastPolygons()
        {
            List<MapPoint> lsPoints = new List<MapPoint>();
            lsPoints.Add(new MapPoint(265410, 6648252, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(265410, 6648862, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266462, 6648862, new SpatialReference(25833)));
            lsPoints.Add(new MapPoint(266462, 6648252, new SpatialReference(25833)));

            _Oslo = new Polygon(lsPoints);

        }

        private static DateTime _time;

        private async void doWheelZoomSim(int seconds = 3)
        {
            // Create a new Viewpoint using the specified geometry
            Viewpoint viewpoint = new Viewpoint(_Oslo);


            try
            {             

                // Set Viewpoint of MapView to the Viewpoint created above and animate to it using a timespan of 5 seconds
                _time = DateTime.Now;
                await MyMapView.SetViewpointAsync(viewpoint, TimeSpan.FromSeconds(seconds));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }

        }



        private void MyMapView_DrawStatusChanged(object sender, DrawStatusChangedEventArgs e)
        {
            UpdateStatus(e.Status == DrawStatus.Completed);
        }

        public void UpdateStatus(bool isComplete)
        {
            Dispatcher.Invoke(delegate ()
            {
                // Show the activity indicator if the map is drawing
                if (isComplete == false)
                {
                }
                else
                {
                    //UpdateTileCount();
                }
            });
        }

        private void UpdateTileCount()
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
            if (tileRequestDetails.Count == 0)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(
                $"{tileRequestDetails.Sum(m => m.Tiles)} tiles between LoD {tileRequestDetails.Min(m => m.Level)} -> {tileRequestDetails.Max(m => m.Level)}");
            foreach (var lod in tileRequestDetails.Select(m => m.Level).Distinct())
            {
                var forLevel = tileRequestDetails.Where(m => m.Level == lod)
                    .ToList();
                builder.AppendLine($"\tLevel {lod} - Total tiles {forLevel.Sum(m => m.Tiles)}");
                foreach (var item in forLevel.OrderBy(m => m.MapService))
                {
                    builder.AppendLine($"\t\t{item.Tiles} tiles - ({item.MapService})");
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

        public static readonly DependencyProperty IsZoomOverrideProperty = DependencyProperty.Register("IsZoomOverride", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(true));


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

        //private void OnDrawStatusChanged(object sender, DrawStatusChangedEventArgs e)
        //{          

        //    // Update the load status information
        //    Dispatcher.Invoke(delegate ()
        //    {
        //        // Show the activity indicator if the map is drawing
        //        if (e.Status == DrawStatus.Completed)
        //        {
        //            DateTime endTime = DateTime.Now;
        //            TimeSpan span = endTime.Subtract(_time);
        //            string res = span.Minutes.ToString() + ":" + span.Seconds.ToString() + "." + span.Milliseconds.ToString();
                  
                    
        //        }
        //    });
        //}


        private void doZooming()
        {
          

            CreateZoomOsloPolygons();
            doWheelZoomSim(1);
            System.Threading.Thread.Sleep(3000);
            CreateZoomOutOsloPolygons();
            doWheelZoomSim(2);
            System.Threading.Thread.Sleep(3000);
            CreateZoomOutOsloCenterPolygons();
            doWheelZoomSim(1);
            System.Threading.Thread.Sleep(3000);
            CreateZoomOutOsloEastPolygons();
            doWheelZoomSim(2);
            System.Threading.Thread.Sleep(3000);
            CreateZoomInOsloCenterEastPolygons();
            doWheelZoomSim(3);
            System.Threading.Thread.Sleep(3000);
            CreateZoomOutOsloPolygons();
            doWheelZoomSim(2);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            MyMapView.DrawStatusChanged += MyMapView_DrawStatusChanged;

            // Navigate to full extent of the first baselayer before animating to specified geometry
            MyMapView.SetViewpoint(
                new Viewpoint(MyMapView.Map.AllLayers[0].FullExtent));
            for (int i=0;i<6;i++) doZooming();

            MyMapView.DrawStatusChanged -= MyMapView_DrawStatusChanged;

        }


        // Map initialization logic is contained in MapViewModel.cs
    }
}
