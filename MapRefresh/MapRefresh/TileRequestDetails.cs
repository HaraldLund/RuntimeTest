using System;

namespace MapRefresh
{
    public class TileRequestDetails
    {
        private TileRequestDetails()
        {

        }

        #region Parse

        public static TileRequestDetails Parse(Uri requestUri, string navigationId)
        {
            var pathSegments = requestUri.Segments;
            var tileIndex = Array.IndexOf(pathSegments, "tile/");
            var level = pathSegments[tileIndex + 1];

            return new TileRequestDetails
            {
                MapService = pathSegments[tileIndex - 2].Replace("/",""),
                NavigationId = navigationId,
                Level = int.Parse(level.Substring(0, level.Length - 1))
            };
        }

        #endregion

        #region Public properties

        public string NavigationId { get; set; }
        public int Level { get; set; }
        public string MapService { get; set; }
        public int Tiles { get; set; }
        #endregion
    }
}