using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    public class Measurment
    {
        public MeasurmentTypes Type { get; set; }
        public string PriceRate { get; set; }
        public string Period { get;  set; }
        public double Consumption { get; set; }
        public double Cost { get; set; }

        public override string ToString()
        {
            return string.Format("\r\n\t{2} - {0} - {1}", this.Period, this.Consumption, this.Type);
        }
    }

    public enum MeasurmentTypes
    {
        Warmwater,
        Heat
    }
}
