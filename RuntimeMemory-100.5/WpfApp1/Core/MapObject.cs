using System;
using System.ComponentModel;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;

namespace WpfApp1.Core
{
    public class MapObject : ObservableObject, IMapObject
    {
        public MapObject(Geometry geometry)
        {
            Graphic = new Graphic
            {
                Geometry = geometry
            };
            Id = Guid.NewGuid();
        }
        public Guid Id { get; }
        public string LayerId { get; set; }
        public Graphic Graphic { get; }
    }

    #region IMapObject
    public interface IMapObject
    {
        Guid Id { get; }
        string LayerId { get; set; }
        Graphic Graphic { get; }
    }
    #endregion

    #region ObservableObject
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion
}