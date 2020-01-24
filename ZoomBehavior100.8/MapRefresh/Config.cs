using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapRefresh
{
    public class Coordinate
    {
        public List<double> XY { get; set; }        
    }

    public class Extents
    {

        public int Repetitons { get; set; }
        public List<Coordinate> Coordinates { get; set; }
        public int wkid;
    }

    public class ZoomAreas
    {
        public int Repetitons { get; set; }
        public List<Extents> extents { get; set; }
        

    }
    public class Config
    {
        public string basemap { get; set; }
        public ZoomAreas zoomareas { get; set; }
    }
}
