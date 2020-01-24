using System.Windows;
using System.Windows.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using WpfApp1.Core;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = new MapHost();
            InitializeComponent();
        }

        private void MapView_LayerViewStateChanged(object sender, Esri.ArcGISRuntime.Mapping.LayerViewStateChangedEventArgs e)
        {
            if(e.LayerViewState == null || e.LayerViewState.Status != LayerViewStatus.Active)
            {
                return;
            }
            if (sender is MapView mapView)
            {
                mapView.LayerViewStateChanged -= MapView_LayerViewStateChanged;
                var graphicsBinding = BindingOperations.GetBinding(mapView, GeoView.GraphicsOverlaysProperty);
                if(graphicsBinding != null)
                {
                    //graphicsBinding.UpdateTarget();
                }
            }
        }

        private void MapView_NavigationCompleted(object sender, System.EventArgs e)
        {
            if (!(sender is MapView mapView))
            {
                return;
            }

            if(mapView.DataContext is MapViewModel dataContext)
            {
                var binding = new Binding("GraphicOverlays")
                {
                    Source = dataContext
                };
                BindingOperations.SetBinding(mapView, GeoView.GraphicsOverlaysProperty, binding);
                mapView.SpatialReferenceChanged -= MapView_NavigationCompleted;
            }
        }
    }
}
