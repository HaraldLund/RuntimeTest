using System;
using ESRI.ArcGIS.Client;

namespace MapRefresh
{
    public static class LegacyMapLoader
    {

        public static void InitializeLegacy(ESRI.ArcGIS.Client.Map mapView, Esri.ArcGISRuntime.Mapping.Map sourceMap)
        {
            AddLayers("Base layers", mapView, sourceMap.Basemap.BaseLayers);
            AddLayers("Operational layers", mapView, sourceMap.Basemap.BaseLayers);
        }

        private static void AddLayers(string groupName, Map map, Esri.ArcGISRuntime.Mapping.LayerCollection sourceLayer)
        {
            var group = new GroupLayer
            {
                DisplayName = groupName
            };
            foreach (var layer in sourceLayer)
            {
                var converted = Convert(layer);
                group.ChildLayers.Add(converted);
            }
            map.Layers.Add(group);
        }
        private static Layer Convert(Esri.ArcGISRuntime.Mapping.ILayerContent item)
        {
            Layer layer = null;


            if (item is Esri.ArcGISRuntime.Mapping.ArcGISTiledLayer tile)
            {
                layer = new ArcGISTiledMapServiceLayer
                {
                    Url = tile.Source.ToString()
                };
            }

            if (item is Esri.ArcGISRuntime.Mapping.ArcGISMapImageLayer dynamic)
            {
                layer = new ArcGISDynamicMapServiceLayer
                {
                    Url = dynamic.Source.ToString(),
                };
            }

            if (layer != null)
            {
                layer.Visible = item.IsVisible;
                layer.ID = item.Name;
                layer.InitializationFailed += InitializationFailed;
            }

            return layer;
        }

        private static void InitializationFailed(object sender, EventArgs e)
        {
        }
    }
}