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
        public bool startAutomatic { get; set; }
        public bool oldVersion { get; set; }
        public bool usetileCache { get; set; }
        public string basemap { get; set; }
        public webMap webMap { get; set; }
        public IList<string> viewpoints { get; set; }

        public IList<Viewpoint> ActualViewpoints
        {
            get { return viewpoints?.Select(Viewpoint.FromJson).ToList(); }
        }
    }

    public class InitConfig
    {
        public Config Configuration { get { return _config; }  }
        private Config _config;
        public InitConfig()
        {           
            string fileName = Path.Combine(MainWindow.AssemblyDirectory, $"Config.json");
            string jsonString = File.ReadAllText(fileName);
            _config =  JsonSerializer.Deserialize<Config>(jsonString);
            
        }

        public async Task<Map> GetMap()
        {
            if (_config.usetileCache)
            {
                var imageryTiledLayer = new ArcGISTiledLayer(new Uri(_config.basemap));
                Map map = new Map();
                map.Basemap = new Basemap(imageryTiledLayer);
                return map;
            }

            try
            {
                var Portal = await ArcGISPortal.CreateAsync(new Uri(_config.webMap.url));
                PortalItem webMapItem = null;
                webMapItem = await PortalItem.CreateAsync(Portal, _config.webMap.item);
                Map map = new Map(webMapItem);
                return map;
            }
            catch (Exception ex)
            {
                throw new Exception($"Kan ikke laste kart; Portal inneholder ikke WebMap med Id {_config.webMap.item}: {ex.Message}");
            }

        }

    }


}
