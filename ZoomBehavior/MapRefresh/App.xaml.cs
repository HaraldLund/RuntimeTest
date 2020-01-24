using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MapRefresh
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.SetLicense("runtimelite,1000,rud1819811630,none,S080TK8EL72MF5KHT074");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to register ArcGIS runtime ClientId " + ex.Message);
            }
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            base.OnStartup(e);
        }
        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
            e.Handled = true;
        }
    }
}
