using System.Collections.Generic;
using System.Linq;

namespace MinoWebCollector
{
    public class Apartment
    {
        public string Building { get; set; }
        public int Number { get; set; }
        public string DetailUrl { get; set; }
        public string MeasurmentsUrl { get; set; }
        public int Size { get; set; }
        public List<Measurment> HeatMeasurments { get; set; }
        public List<Measurment> WarmwaterMeasurments { get; set; }

        public Apartment()
        {
            HeatMeasurments = new List<Measurment>();
            WarmwaterMeasurments = new List<Measurment>();
        }

        public Measurment GetLastMonthHeatMeasure()
        {
            return this.HeatMeasurments.FirstOrDefault();
        }
        public Measurment GetLastMonthWarmWaterMeasure()
        {
            return this.WarmwaterMeasurments.FirstOrDefault();
        }

        public Measurment GetMeasurmentForSameMonthLastYear(Measurment measurment)
        {
            List<Measurment> measurments = null;
            switch (measurment.Type)
            {
                case MeasurmentTypes.Warmwater:
                    measurments = this.WarmwaterMeasurments;
                    break;
                case MeasurmentTypes.Heat:
                    measurments = this.HeatMeasurments;
                    break;
            }

            var sameMonthLastYear = measurments.SkipWhile(m => m != measurment).Skip(1).FirstOrDefault(m => m.Period == measurment.Period);
            return sameMonthLastYear;
        }

        public double GetAverageHeatConsumption()
        {
            return this.HeatMeasurments.Average(h => h.Consumption);
        }
        public double GetAverageWarmWaterConsumption()
        {
            return this.WarmwaterMeasurments.Average(h => h.Consumption);
        }

        public override string ToString()
        {
            return string.Format("{2} - {0} ({1} m²) - {3},{4}{5}{6}", this.Number, this.Size, this.Building, this.HeatMeasurments.Count, this.WarmwaterMeasurments.Count, string.Join("", this.HeatMeasurments), string.Join("", this.WarmwaterMeasurments));
        }
    }
}