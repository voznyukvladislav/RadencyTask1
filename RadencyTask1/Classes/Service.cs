using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadencyTask1.Classes
{
    public class Service
    {
        public string Name { get; set; }
        public List<Payer> Payers { get; set; }
        public decimal Total { get; set; }
        public Service(string name)
        {
            Name = name;
            Payers = new List<Payer>();
            Total = 0;
        }
    }
}
