using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadencyTask1.Classes
{
    public class City
    {
        public string CityName { get; set; }
        public List<Service> Services { get; set; }
        public decimal Total { get; set; }

        public City(string cityName)
        {
            CityName = cityName;
            Services = new List<Service>();
            Total = 0;
        }
    }
}
