using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI.Controls;



namespace MapRefresh
{

    [Serializable]
    public class webMap
    {
      
        public string url { get; set; }
        public string item { get; set; }
    }

    [Serializable]
    public class Config
    {
        public bool usetileCache { get; set; }
        public string basemap { get; set; }
        public webMap webMap { get; set; }
                             
    }

    public class InitConfig
    {
        public Config configuration { get { return _config; }  }
        private Config _config;
        public InitConfig()
        {           
            string fileName = Path.Combine(MainWindow.AssemblyDirectory, $"Config.json");
            string jsonString = File.ReadAllText(fileName);
            _config =  JsonSerializer.Deserialize<Config>(jsonString);
            
        }

        public void SetMap(MapView mv)
        {
            GetMap(mv);
           
        }

        private async void GetMap(MapView mv)
        {
            if (_config.usetileCache)
            {

                var imageryTiledLayer = new ArcGISTiledLayer(new Uri(_config.basemap));
                Map map = new Map();
                map.Basemap = new Basemap(imageryTiledLayer);
                mv.Map = map;               
            }
            else
            {
             
                try
                {
                    var Portal = await ArcGISPortal.CreateAsync(new Uri(_config.webMap.url));
                    PortalItem webMapItem = null;
                    webMapItem = await PortalItem.CreateAsync(Portal, _config.webMap.item);
                    Map map = new Map(webMapItem);
                    mv.Map = map;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Kan ikke laste kart; Portal inneholder ikke WebMap med Id {_config.webMap.item}: {ex.Message}");
                }
              
              
            }

        }

    }


}
