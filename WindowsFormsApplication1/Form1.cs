using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        string _measurementDirectory = @"C:\Users\Visible\Documents\GitHub\brfskagagard\data\measurements\";
        string _loginsDirectory = @"C:\Users\Visible\Documents\GitHub\brfskagagard\data\logins\";
        string _outputDirectory = @"C:\Users\Visible\Documents\GitHub\brfskagagard\measurements\";

        List<Appartment> apartments = new List<Appartment>();
        int selectedApartmentNumber = -1;
        Appartment selectedApartment = null;

        public Form1()
        {
            InitializeComponent();

            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;

            backgroundWorker2.DoWork += BackgroundWorker2_DoWork;
            backgroundWorker2.ProgressChanged += BackgroundWorker2_ProgressChanged;
            backgroundWorker2.RunWorkerCompleted += BackgroundWorker2_RunWorkerCompleted;

            backgroundWorker3.DoWork += BackgroundWorker3_DoWork;
            backgroundWorker3.ProgressChanged += BackgroundWorker3_ProgressChanged;
            backgroundWorker3.RunWorkerCompleted += BackgroundWorker3_RunWorkerCompleted;

            backgroundWorker4.DoWork += BackgroundWorker4_DoWork;
            backgroundWorker4.ProgressChanged += BackgroundWorker4_ProgressChanged;
            backgroundWorker4.RunWorkerCompleted += BackgroundWorker4_RunWorkerCompleted;

            backgroundWorker5.DoWork += BackgroundWorker5_DoWork;
            backgroundWorker5.ProgressChanged += BackgroundWorker5_ProgressChanged;
            backgroundWorker5.RunWorkerCompleted += BackgroundWorker5_RunWorkerCompleted;


            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            textBox2.Enabled = false;
        }

        private void BackgroundWorker5_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            textBox2.Enabled = true;
        }

        private void BackgroundWorker5_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var str = e.UserState as string;
            textBox1.AppendText(str + "\r\n");
        }

        private void BackgroundWorker5_DoWork(object sender, DoWorkEventArgs e)
        {
            var directory = _outputDirectory;
            GenerateApartmentMeasureFiles(directory);
            GenerateApartmentCosts(directory);
            GenerateApartmentWarning(directory);
        }
        private void GenerateApartmentWarning(string directory)
        {
            var fileContent = new StringBuilder(File.ReadAllText(directory + "warning-template.html"));

            var tmp = apartments.OrderBy(OrderByCost).Reverse().ToList();
            var firstEvenOrMore = true;

            //double totalMonthCost = 0;
            //double totalSchablonCost = 0;

            //double totalLastYearMonthCost = 0;
            //double totalLastYearSchablonCost = 0;

            StringBuilder markup = new StringBuilder();

            markup.AppendLine("<h3>Lägenheter som förbrukar mer än dess schablonbelopp</h3>");
            foreach (Appartment apartment in tmp)
            {
                var date = DateTime.Now.AddMonths(-1);

                var numberOfDaysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
                // Calculate schablon cost (Currently at 90kr / m2) each year.
                var schablonCost = (double)((Decimal.Divide((apartment.Size * 90), 365)) * numberOfDaysInMonth);
                var monthCost = apartment.GetLastMonthHeatMeasure().Cost + apartment.GetLastMonthWarmWaterMeasure().Cost;

                var dateLastYear = date.AddYears(-1);

                var numberOfDaysInMonthLastYear = DateTime.DaysInMonth(dateLastYear.Year, dateLastYear.Month);
                // Calculate schablon cost (Currently at 90kr / m2) each year.
                var lastYearSchablonCost = (double)((Decimal.Divide((apartment.Size * 90), 365)) * numberOfDaysInMonthLastYear);
                var lastYearMonthCost = apartment.GetMeasurmentForSameMonthLastYear(apartment.GetLastMonthHeatMeasure()).Cost + apartment.GetMeasurmentForSameMonthLastYear(apartment.GetLastMonthWarmWaterMeasure()).Cost;

                if (monthCost > schablonCost)
                {
                    markup.AppendFormat("<a href=\"{0}.html\" style=\"color:red\">Lägenhet {0}</a> -{1} kr<br/>", apartment.Number, (monthCost - schablonCost).ToString("0.00"));
                }
            }

            markup.AppendLine("<h3>Lägenheter som har ovanligt låg Värmeförbrukning</h3>");
            markup.AppendLine("<p>(Visar lägenheter som har mindre än 1 kWh i förbrukning)</p>");

            var tmp2 = apartments.OrderBy(OrderByHeatCosumption).Reverse().ToList();
            foreach (var apartment in tmp2)
            {
                var consumption = apartment.GetLastMonthHeatMeasure().Consumption;
                if (consumption < 1.0)
                {
                    markup.AppendFormat("<a href=\"{0}.html\" style=\"color:orange\">Lägenhet {0}</a> - {1} kWh<br/>", apartment.Number, consumption.ToString("0.00"));
                }
            }

            markup.AppendLine("<h3>Lägenheter som har ovanligt låg varmvattensförbrukning</h3>");
            markup.AppendLine("<p>(Visar lägenheter som har mindre än 0,2 m³ i förbrukning. >0,2 m³ kan anses normalt enligt Tina på Minol)</p>");
            var tmp3 = apartments.OrderBy(OrderByWarmWaterCosumption).Reverse().ToList();
            foreach (var apartment in tmp3)
            {
                var consumption = apartment.GetLastMonthWarmWaterMeasure().Consumption;
                if (consumption < 0.2)
                {
                    markup.AppendFormat("<a href=\"{0}.html\" style=\"color:orange\">Lägenhet {0}</a> - {1} m³<br/>", apartment.Number, consumption.ToString("0.00"));
                }
            }


            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            fileContent.Replace("{measurement-month-name}", textInfo.ToTitleCase(apartments.FirstOrDefault().HeatMeasurments.FirstOrDefault().Period));

            fileContent.Replace("{apartment-list}", markup.ToString());
            //fileContent.Replace("{total-month-cost}", totalMonthCost.ToString("0.00"));
            //fileContent.Replace("{total-month-schablon}", totalSchablonCost.ToString("0.00"));

            //fileContent.Replace("{total-last-year-month-cost}", totalLastYearMonthCost.ToString("0.00"));
            //fileContent.Replace("{total-last-year-month-schablon}", totalLastYearSchablonCost.ToString("0.00"));

            File.WriteAllText(directory + "apartment-warning.html", fileContent.ToString());
        }

        private void GenerateApartmentCosts(string directory)
        {
            var fileContent = new StringBuilder(File.ReadAllText(directory + "cost-template.html"));

            var tmp = apartments.OrderBy(OrderByCost).Reverse().ToList();
            var firstEvenOrMore = true;

            double totalMonthCost = 0;
            double totalSchablonCost = 0;

            double totalLastYearMonthCost = 0;
            double totalLastYearSchablonCost = 0;

            StringBuilder markup = new StringBuilder();
            foreach (Appartment apartment in tmp)
            {
                var date = DateTime.Now.AddMonths(-1);

                var numberOfDaysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
                // Calculate schablon cost (Currently at 90kr / m2) each year.
                var schablonCost = (double)((Decimal.Divide((apartment.Size * 90), 365)) * numberOfDaysInMonth);
                var monthCost = apartment.GetLastMonthHeatMeasure().Cost + apartment.GetLastMonthWarmWaterMeasure().Cost;

                totalMonthCost += monthCost;
                totalSchablonCost += schablonCost;

                var dateLastYear = date.AddYears(-1);

                var numberOfDaysInMonthLastYear = DateTime.DaysInMonth(dateLastYear.Year, dateLastYear.Month);
                // Calculate schablon cost (Currently at 90kr / m2) each year.
                var lastYearSchablonCost = (double)((Decimal.Divide((apartment.Size * 90), 365)) * numberOfDaysInMonthLastYear);
                var lastYearMonthCost = apartment.GetMeasurmentForSameMonthLastYear(apartment.GetLastMonthHeatMeasure()).Cost + apartment.GetMeasurmentForSameMonthLastYear(apartment.GetLastMonthWarmWaterMeasure()).Cost;

                totalLastYearMonthCost += lastYearMonthCost;
                totalLastYearSchablonCost += lastYearSchablonCost;

                if (monthCost > schablonCost)
                {
                    markup.AppendFormat("<a href=\"{0}.html\" style=\"color:red\">Lägenhet {0}</a> -{1} kr<br/>", apartment.Number, (monthCost - schablonCost).ToString("0.00"));
                }
                else
                {
                    if (firstEvenOrMore)
                    {
                        firstEvenOrMore = false;
                        markup.AppendLine("<h3>Lägenheter som håller sig inom schablonbeloppet</h3>");
                    }
                    markup.AppendFormat("<a href=\"{0}.html\" style=\"color:green\">Lägenhet {0}</a> +{1} kr<br/>", apartment.Number, (schablonCost - monthCost).ToString("0.00"));
                }
            }

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            fileContent.Replace("{measurement-month-name}", textInfo.ToTitleCase(apartments.FirstOrDefault().HeatMeasurments.FirstOrDefault().Period));

            fileContent.Replace("{apartment-list}", markup.ToString());
            fileContent.Replace("{total-month-cost}", totalMonthCost.ToString("0.00"));
            fileContent.Replace("{total-month-schablon}", totalSchablonCost.ToString("0.00"));

            fileContent.Replace("{total-last-year-month-cost}", totalLastYearMonthCost.ToString("0.00"));
            fileContent.Replace("{total-last-year-month-schablon}", totalLastYearSchablonCost.ToString("0.00"));

            File.WriteAllText(directory + "apartment-cost.html", fileContent.ToString());
        }

        private object OrderByHeatCosumption(Appartment apartment)
        {
            return apartment.GetLastMonthHeatMeasure().Consumption;
        }
        private object OrderByWarmWaterCosumption(Appartment apartment)
        {
            return apartment.GetLastMonthWarmWaterMeasure().Consumption;
        }

        private object OrderByCost(Appartment apartment)
        {
            var date = DateTime.Now.AddMonths(-1);

            var numberOfDaysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            // Calculate schablon cost (Currently at 90kr / m2) each year.
            var schablonCost = (System.Decimal.Divide((apartment.Size * 90), 365)) * numberOfDaysInMonth;
            var monthCost = apartment.GetLastMonthHeatMeasure().Cost + apartment.GetLastMonthWarmWaterMeasure().Cost;
            // Populate WarmWater
            return (double)monthCost - (double)schablonCost;
        }

        private void GenerateApartmentMeasureFiles(string directory)
        {
            var fileContent = File.ReadAllText(directory + "measurement-template.html");

            foreach (Appartment apartment in apartments)
            {
                var heatMeasurement = apartment.GetLastMonthHeatMeasure();
                var heatLastYearMeasurement = apartment.GetMeasurmentForSameMonthLastYear(heatMeasurement);
                var warmWaterMeasurement = apartment.GetLastMonthWarmWaterMeasure();
                var warmWaterLastYearMeasurement = apartment.GetMeasurmentForSameMonthLastYear(warmWaterMeasurement);
                var similarApartments = apartments.Where(a => a.Size == apartment.Size && a.Number != apartment.Number).GroupBy(a => a.Size).OrderBy(g => g.Key).FirstOrDefault().ToList();
                var buildingApartments = apartments.Where(a => a.Building == apartment.Building && a.Number != apartment.Number).GroupBy(a => a.Building).OrderBy(g => g.Key).FirstOrDefault().ToList();

                var markup = new StringBuilder(fileContent);
                markup.Replace("{apartment-number}", apartment.Number.ToString());

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                markup.Replace("{measurement-month-name}", textInfo.ToTitleCase(heatMeasurement.Period));

                PopulateLastMonthMeasure(warmWaterMeasurement, warmWaterLastYearMeasurement, markup);
                PopulateApartments("similar", MeasurmentTypes.Warmwater, similarApartments, markup);
                PopulateApartments("building", MeasurmentTypes.Warmwater, buildingApartments, markup);

                PopulateLastMonthMeasure(heatMeasurement, heatLastYearMeasurement, markup);
                PopulateApartments("similar", MeasurmentTypes.Heat, similarApartments, markup);
                PopulateApartments("building", MeasurmentTypes.Heat, buildingApartments, markup);

                PopulateLastTotalPrice(apartment, heatMeasurement, warmWaterMeasurement, markup);

                PopulateMinoWebLogin(apartment.Number, markup);

                File.WriteAllText(directory + apartment.Number + ".html", markup.ToString());
            }
        }

        private void PopulateMinoWebLogin(int apartmentNumber, StringBuilder markup)
        {
            var json = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(MinoWebLogin));
            var file = new FileInfo(_loginsDirectory + apartmentNumber.ToString() + ".json");
            var updated = false;
            if (file.Exists)
            {
                using (var stream = file.OpenRead())
                {
                    var loginInfo = json.ReadObject(stream) as MinoWebLogin;
                    if (loginInfo != null)
                    {
                        markup.Replace("{minoweb-user}", loginInfo.UserName);
                        markup.Replace("{minoweb-password}", loginInfo.Password);
                        updated = true;
                    }
                }
            }

            if (!updated)
            {
                markup.Replace("{minoweb-user}", "...................");
                markup.Replace("{minoweb-password}", "...................");
            }

            markup.Replace("www.minoweb.se", "<a href=\"http://www.minoweb.se\" target=\"_blank\">www.minoweb.se</a>");
        }

        private void PopulateLastTotalPrice(Appartment apartment, Measurment heatMeasurement, Measurment warmWaterMeasurement, StringBuilder markup)
        {
            var date = DateTime.Now.AddMonths(-1);

            var numberOfDaysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            // Calculate schablon cost (Currently at 90kr / m2) each year.
            var schablonCost = (System.Decimal.Divide((apartment.Size * 90), 365)) * numberOfDaysInMonth;
            var monthCost = heatMeasurement.Cost + warmWaterMeasurement.Cost;
            // Populate WarmWater
            double percent = (double)monthCost / (double)schablonCost;
            var degrees = (int)(percent * 360);
            if (percent < 1)
            {
                if (degrees > 180)
                {
                    markup.Replace("{measurement-month-cost-piece-big}", monthCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-piece-small}", schablonCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-consumption-deg}", (360 - degrees).ToString());

                    markup.Replace("{measurement-month-cost-piece-big-img}", "/img/graph-color-04b.png");
                    markup.Replace("{measurement-month-cost-piece-small-img}", "/img/graph-color-06.png");
                }
                else
                {
                    markup.Replace("{measurement-month-cost-piece-big}", schablonCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-piece-small}", monthCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-consumption-deg}", (degrees).ToString());

                    markup.Replace("{measurement-month-cost-piece-big-img}", "/img/graph-color-06.png");
                    markup.Replace("{measurement-month-cost-piece-small-img}", "/img/graph-color-04b.png");
                }
            }
            else
            {
                // Cost more then schablon
                percent = (double)schablonCost / monthCost;
                degrees = (int)(percent * 360);
                if (degrees > 180)
                {
                    markup.Replace("{measurement-month-cost-piece-big}", monthCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-piece-small}", schablonCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-consumption-deg}", (360 - degrees).ToString());

                    markup.Replace("{measurement-month-cost-piece-big-img}", "/img/graph-color-04b.png");
                    markup.Replace("{measurement-month-cost-piece-small-img}", "/img/graph-color-05.png");
                }
                else
                {
                    markup.Replace("{measurement-month-cost-piece-big}", schablonCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-piece-small}", monthCost.ToString("0.00"));
                    markup.Replace("{measurement-month-cost-consumption-deg}", (degrees).ToString());

                    markup.Replace("{measurement-month-cost-piece-big-img}", "/img/graph-color-05.png");
                    markup.Replace("{measurement-month-cost-piece-small-img}", "/img/graph-color-04b.png");
                }

            }

            markup.Replace("{measurement-month-totalprice}", (monthCost).ToString("0.00"));
        }

        private static void PopulateApartments(string prefix, MeasurmentTypes measurementType, List<Appartment> apartments, StringBuilder markup)
        {
            var type = "";
            switch (measurementType)
            {
                case MeasurmentTypes.Warmwater:
                    type = "warmwater";
                    break;
                case MeasurmentTypes.Heat:
                    type = "heat";
                    break;
            }

            // Populate WarmWater
            var lastMonthAverage = apartments.Sum(a => (measurementType == MeasurmentTypes.Warmwater ? a.GetLastMonthWarmWaterMeasure() : a.GetLastMonthHeatMeasure()).Consumption) / apartments.Count;
            var lastYearAverage = apartments.Sum(a => a.GetMeasurmentForSameMonthLastYear((measurementType == MeasurmentTypes.Warmwater ? a.GetLastMonthWarmWaterMeasure() : a.GetLastMonthHeatMeasure() )).Consumption) / apartments.Count;

            double percent = lastMonthAverage / (lastYearAverage + lastMonthAverage);
            var degrees = (int)(percent * 360);
            if (degrees >= 180)
            {
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-big}", lastMonthAverage.ToString("0.00"));
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-small}", lastYearAverage.ToString("0.00"));
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-consumption-deg}", (360 - degrees).ToString());

                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-big-img}", "/img/graph-color-01.png");
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-small-img}", "/img/graph-color-03b.png");
            }
            else
            {
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-big}", lastYearAverage.ToString("0.00"));
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-small}", lastMonthAverage.ToString("0.00"));
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-consumption-deg}", (degrees).ToString());

                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-big-img}", "/img/graph-color-03b.png");
                markup.Replace("{measurement-" + prefix + "-month-" + type + "-piece-small-img}", "/img/graph-color-01.png");
            }
            markup.Replace("{measurement-" + prefix + "-month-" + type + "-consumption}", lastMonthAverage.ToString("0.00"));
        }

        private static void PopulateLastMonthMeasure(Measurment currentMeasurement, Measurment lastYearMeasurement, StringBuilder markup)
        {
            var type = "";
            switch (currentMeasurement.Type)
            {
                case MeasurmentTypes.Warmwater:
                    type = "warmwater";
                    break;
                case MeasurmentTypes.Heat:
                    type = "heat";
                    break;
            }

            if (lastYearMeasurement.Consumption == 0 && currentMeasurement.Consumption == 0)
            {
                markup.Replace("{measurement-month-" + type + "-piece-big}", currentMeasurement.Consumption.ToString("0.00"));
                markup.Replace("{measurement-month-" + type + "-piece-small}", lastYearMeasurement.Consumption.ToString("0.00"));
                markup.Replace("{measurement-month-" + type + "-consumption-deg}", (180).ToString());

                markup.Replace("{measurement-month-" + type + "-piece-big-img}", "/img/graph-color-01.png");
                markup.Replace("{measurement-month-" + type + "-piece-small-img}", "/img/graph-color-03b.png");
            }
            else
            {
                // Populate WarmWater
                double percent = currentMeasurement.Consumption / (lastYearMeasurement.Consumption + currentMeasurement.Consumption);
                var degrees = (int)(percent * 360);
                if (degrees > 180)
                {

                    markup.Replace("{measurement-month-" + type + "-piece-big}", currentMeasurement.Consumption.ToString("0.00"));
                    markup.Replace("{measurement-month-" + type + "-piece-small}", lastYearMeasurement.Consumption.ToString("0.00"));
                    markup.Replace("{measurement-month-" + type + "-consumption-deg}", (360 - degrees).ToString());

                    markup.Replace("{measurement-month-" + type + "-piece-big-img}", "/img/graph-color-01.png");
                    markup.Replace("{measurement-month-" + type + "-piece-small-img}", "/img/graph-color-03b.png");
                }
                else
                {
                    markup.Replace("{measurement-month-" + type + "-piece-big}", lastYearMeasurement.Consumption.ToString("0.00"));
                    markup.Replace("{measurement-month-" + type + "-piece-small}", currentMeasurement.Consumption.ToString("0.00"));
                    markup.Replace("{measurement-month-" + type + "-consumption-deg}", (degrees).ToString());

                    markup.Replace("{measurement-month-" + type + "-piece-big-img}", "/img/graph-color-03b.png");
                    markup.Replace("{measurement-month-" + type + "-piece-small-img}", "/img/graph-color-01.png");
                }
            }
            markup.Replace("{measurement-month-" + type + "-consumption}", currentMeasurement.Consumption.ToString("0.00"));
        }

        private void BackgroundWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button3.Enabled = true;
            button4.Enabled = true;
            textBox2.Enabled = true;
        }

        private void BackgroundWorker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var str = e.UserState as string;
            textBox1.AppendText(str + "\r\n");
        }

        private void BackgroundWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            var yourApartment = apartments.FirstOrDefault(a => a.Number == selectedApartmentNumber);
            backgroundWorker4.ReportProgress(1, "Your Apartment (" + selectedApartmentNumber + ")");
            backgroundWorker4.ReportProgress(1, "\tHeat Consumption");
            var yourAppartmentLastMonthHeat = yourApartment.GetLastMonthHeatMeasure();
            backgroundWorker4.ReportProgress(1, "\t\tLast Month(" + yourAppartmentLastMonthHeat.Period + "): " + yourAppartmentLastMonthHeat.Consumption.ToString("0.00") + " (" + yourAppartmentLastMonthHeat.Cost.ToString("C2") + ")");
            var heatLastYear = yourApartment.GetMeasurmentForSameMonthLastYear(yourAppartmentLastMonthHeat);
            backgroundWorker4.ReportProgress(1, "\t\tSame Period Last Year(" + heatLastYear.Period + "): " + heatLastYear.Consumption.ToString("0.00") + " (" + heatLastYear.Cost.ToString("C2") + ")");
            backgroundWorker4.ReportProgress(1, "\t\tAvg: " + yourApartment.GetAverageHeatConsumption().ToString("0.00"));

            backgroundWorker4.ReportProgress(1, "\tWarmwater Consumption");
            var yourAppartmentLastMonthWarmWater = yourApartment.GetLastMonthWarmWaterMeasure();
            backgroundWorker4.ReportProgress(1, "\t\tLast Month: " + yourAppartmentLastMonthWarmWater.Consumption.ToString("0.00") + " (" + yourAppartmentLastMonthWarmWater.Cost.ToString("C2") + ")");
            var warmwaterLastYear = yourApartment.GetMeasurmentForSameMonthLastYear(yourAppartmentLastMonthWarmWater);
            backgroundWorker4.ReportProgress(1, "\t\tSame Period Last Year(" + warmwaterLastYear.Period + "): " + warmwaterLastYear.Consumption.ToString("0.00") + " (" + warmwaterLastYear.Cost.ToString("C2") + ")");
            backgroundWorker4.ReportProgress(1, "\t\tAvg: " + yourApartment.GetAverageWarmWaterConsumption().ToString("0.00"));

            backgroundWorker4.ReportProgress(1, "");

            var groups = apartments.Where(a => a.Size == yourApartment.Size).GroupBy(a => a.Size).OrderBy(g => g.Key);
            foreach (IGrouping<int, Appartment> group in groups)
            {
                backgroundWorker4.ReportProgress(1, group.Key.ToString() + " m² => Number of Apartments: " + group.Count());
                var appartmentsInGroup = group.ToList();
                var heatLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthHeatMeasure().Consumption) / appartmentsInGroup.Count;
                var heatLastYearAverage = appartmentsInGroup.Sum(a => a.GetMeasurmentForSameMonthLastYear(a.GetLastMonthHeatMeasure()).Consumption) / appartmentsInGroup.Count;
                var heatOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageHeatConsumption()) / appartmentsInGroup.Count;
                backgroundWorker4.ReportProgress(1, "\tHeat Consumption");
                backgroundWorker4.ReportProgress(1, "\t\tAvg Last Month: " + heatLastMonthAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tSame Period Last Year: " + heatLastYearAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tAvg: " + heatOverallAverage.ToString("0.00"));
                var warmWaterLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthWarmWaterMeasure().Consumption) / appartmentsInGroup.Count;
                var warmWaterLastYearAverage = appartmentsInGroup.Sum(a => a.GetMeasurmentForSameMonthLastYear(a.GetLastMonthWarmWaterMeasure()).Consumption) / appartmentsInGroup.Count;
                var warmWaterOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageWarmWaterConsumption()) / appartmentsInGroup.Count;
                backgroundWorker4.ReportProgress(1, "\tWarmwater Consumption");
                backgroundWorker4.ReportProgress(1, "\t\tAvg Last Month: " + warmWaterLastMonthAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tSame Period Last Year: " + warmWaterLastYearAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tAvg: " + warmWaterOverallAverage.ToString("0.00"));
            }

            backgroundWorker4.ReportProgress(1, "");

            var groups2 = apartments.Where(a => a.Building == yourApartment.Building).GroupBy(a => a.Building).OrderBy(g => g.Key);
            foreach (IGrouping<string, Appartment> group in groups2)
            {
                backgroundWorker4.ReportProgress(1, group.Key.ToString() + " => Number of Apartments: " + group.Count());
                var appartmentsInGroup = group.ToList();
                var heatLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthHeatMeasure().Consumption) / appartmentsInGroup.Count;
                var heatLastYearAverage = appartmentsInGroup.Sum(a => a.GetMeasurmentForSameMonthLastYear(a.GetLastMonthHeatMeasure()).Consumption) / appartmentsInGroup.Count;
                var heatOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageHeatConsumption()) / appartmentsInGroup.Count;
                backgroundWorker4.ReportProgress(1, "\tHeat Consumption");
                backgroundWorker4.ReportProgress(1, "\t\tAvg Last Month: " + heatLastMonthAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tSame Period Last Year: " + heatLastYearAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tAvg: " + heatOverallAverage.ToString("0.00"));
                var warmWaterLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthWarmWaterMeasure().Consumption) / appartmentsInGroup.Count;
                var warmWaterLastYearAverage = appartmentsInGroup.Sum(a => a.GetMeasurmentForSameMonthLastYear(a.GetLastMonthWarmWaterMeasure()).Consumption) / appartmentsInGroup.Count;
                var warmWaterOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageWarmWaterConsumption()) / appartmentsInGroup.Count;
                backgroundWorker4.ReportProgress(1, "\tWarmwater Consumption");
                backgroundWorker4.ReportProgress(1, "\t\tAvg Last Month: " + warmWaterLastMonthAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tSame Period Last Year: " + warmWaterLastYearAverage.ToString("0.00"));
                backgroundWorker4.ReportProgress(1, "\t\tAvg: " + warmWaterOverallAverage.ToString("0.00"));
            }

            backgroundWorker4.ReportProgress(1, "");

            var date = DateTime.Now.AddMonths(-1);

            var days = DateTime.DaysInMonth(date.Year, date.Month);

            backgroundWorker4.ReportProgress(1, "DaysInMonth: " + days);
            //var executablePath = new FileInfo(Application.ExecutablePath);
            //var directory = new DirectoryInfo(executablePath.Directory.FullName + Path.DirectorySeparatorChar + "analyze" + Path.DirectorySeparatorChar);

        }

        private void BackgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button3.Enabled = true;
            //button4.Enabled = true;
            textBox2.Enabled = true;
        }

        private void BackgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var str = e.UserState as string;
            textBox1.AppendText(str + "\r\n");
        }

        private void BackgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {

            var groups2 = apartments.GroupBy(a => a.Building).OrderBy(g => g.Key);
            foreach (IGrouping<string, Appartment> group in groups2)
            {
                backgroundWorker3.ReportProgress(1, group.Key.ToString() + " => Number of Apartments: " + group.Count());
                var appartmentsInGroup = group.ToList();
                var heatLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthHeatMeasure().Consumption) / appartmentsInGroup.Count;
                var heatOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageHeatConsumption()) / appartmentsInGroup.Count;
                var heatTotal = appartmentsInGroup.Sum(a => a.GetLastMonthHeatMeasure().Consumption);
                backgroundWorker3.ReportProgress(1, "\tHeat Consumption");
                backgroundWorker3.ReportProgress(1, "\t\tAvg Last Month: " + heatLastMonthAverage.ToString("0.00"));
                backgroundWorker3.ReportProgress(1, "\t\tAvg: " + heatOverallAverage.ToString("0.00"));
                backgroundWorker3.ReportProgress(1, "\t\tTotal Last Month: " + heatTotal.ToString("0.00"));
                var warmWaterLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthWarmWaterMeasure().Consumption) / appartmentsInGroup.Count;
                var warmWaterOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageWarmWaterConsumption()) / appartmentsInGroup.Count;
                var warmWaterTotal = appartmentsInGroup.Sum(a => a.GetLastMonthWarmWaterMeasure().Consumption);
                backgroundWorker3.ReportProgress(1, "\tWarmwater Consumption");
                backgroundWorker3.ReportProgress(1, "\t\tAvg Last Month: " + warmWaterLastMonthAverage.ToString("0.00"));
                backgroundWorker3.ReportProgress(1, "\t\tAvg: " + warmWaterOverallAverage.ToString("0.00"));
                backgroundWorker3.ReportProgress(1, "\t\tTotal Last Month: " + warmWaterTotal.ToString("0.00"));
            }
            backgroundWorker3.ReportProgress(1, "");

            backgroundWorker3.ReportProgress(1, "General Information:");
            backgroundWorker3.ReportProgress(1, "\tHeat Consumption");
            var generalHeatTotal = apartments.Sum(a => a.GetLastMonthHeatMeasure().Consumption);
            backgroundWorker3.ReportProgress(1, "\t\tTotal Last Month: " + generalHeatTotal.ToString("0.00"));
            backgroundWorker3.ReportProgress(1, "\tWarmwater Consumption");
            var generalWarmWaterTotal = apartments.Sum(a => a.GetLastMonthWarmWaterMeasure().Consumption);
            backgroundWorker3.ReportProgress(1, "\t\tTotal Last Month: " + generalWarmWaterTotal.ToString("0.00"));

            backgroundWorker3.ReportProgress(1, "");

            var groups = apartments.GroupBy(a => a.Size).OrderBy(g => g.Key);
            foreach (IGrouping<int, Appartment> group in groups)
            {
                backgroundWorker3.ReportProgress(1, group.Key.ToString() + " m² => Number of Apartments: " + group.Count());
                var appartmentsInGroup = group.ToList();
                var heatLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthHeatMeasure().Consumption) / appartmentsInGroup.Count;
                var heatOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageHeatConsumption()) / appartmentsInGroup.Count;
                backgroundWorker3.ReportProgress(1, "\tHeat Consumption");
                backgroundWorker3.ReportProgress(1, "\t\tAvg Last Month: " + heatLastMonthAverage.ToString("0.00"));
                backgroundWorker3.ReportProgress(1, "\t\tAvg: " + heatOverallAverage.ToString("0.00"));
                var warmWaterLastMonthAverage = appartmentsInGroup.Sum(a => a.GetLastMonthWarmWaterMeasure().Consumption) / appartmentsInGroup.Count;
                var warmWaterOverallAverage = appartmentsInGroup.Sum(a => a.GetAverageWarmWaterConsumption()) / appartmentsInGroup.Count;
                backgroundWorker3.ReportProgress(1, "\tWarmwater Consumption");
                backgroundWorker3.ReportProgress(1, "\t\tAvg Last Month: " + warmWaterLastMonthAverage.ToString("0.00"));
                backgroundWorker3.ReportProgress(1, "\t\tAvg: " + warmWaterOverallAverage.ToString("0.00"));
            }


            //var executablePath = new FileInfo(Application.ExecutablePath);
            //var directory = new DirectoryInfo(executablePath.Directory.FullName + Path.DirectorySeparatorChar + "analyze" + Path.DirectorySeparatorChar);
        }

        private void BackgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button2.Enabled = true;
            button3.Enabled = true;
            //button4.Enabled = true;
            button5.Enabled = true;
            textBox2.Enabled = true;
        }

        private void BackgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var str = e.UserState as string;
            textBox1.AppendText(str + "\r\n");
        }

        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            apartments.Clear();

            var executablePath = new FileInfo(Application.ExecutablePath);
            var directory = new DirectoryInfo(_measurementDirectory);
            //var directory = new DirectoryInfo(executablePath.Directory.FullName + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "measurements" + Path.DirectorySeparatorChar);
            var files = directory.GetFiles("*.json");


            var json = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Appartment));
            foreach (var file in files)
            {
                using (var stream = file.OpenRead())
                {
                    var appartment = json.ReadObject(stream) as Appartment;
                    if (appartment != null)
                    {
                        apartments.Add(appartment);
                        backgroundWorker2.ReportProgress(1, appartment.ToString());
                    }
                }
            }

            backgroundWorker2.ReportProgress(1, "Number of appartments: " + apartments.Count);
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
            button3.Enabled = true;
            //button4.Enabled = true;
            button5.Enabled = true;
            textBox2.Enabled = true;
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var str = e.UserState as string;
            textBox1.AppendText(str + "\r\n");
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            apartments.Clear();

            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://minoweb.se/");

                    if (!Login(client))
                    {
                        // TODO: We where unable to login, do something about this.
                        backgroundWorker1.ReportProgress(1, "Failed login!");
                        return;
                    }
                    backgroundWorker1.ReportProgress(1, "logged in!");

                    var buildingNumbers = new int[] { 5, 6, 7 };

                    foreach (var buildingNumber in buildingNumbers)
                    {
                        try
                        {
                            apartments.AddRange(GetAppartmentsForBuilding(client, buildingNumber));
                        }
                        catch (Exception ex)
                        {
                            backgroundWorker1.ReportProgress(1, ex.ToString());
                        }
                    }

                    foreach (Appartment item in apartments)
                    {
                        backgroundWorker1.ReportProgress(1, item.ToString());
                        var json = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Appartment));

                        using (var stream = File.OpenWrite(_measurementDirectory + Path.DirectorySeparatorChar + item.Number + ".json"))
                        {
                            json.WriteObject(stream, item);
                            stream.Flush();
                        }

                    }

                    backgroundWorker1.ReportProgress(1, "Number of appartments: " + apartments.Count);
                }
            }
        }

        private List<Appartment> GetAppartmentsForBuilding(HttpClient client, int buildingNumber)
        {
            var appartments = new List<Appartment>();

            var data = client.GetStringAsync("/Localities/Localities.aspx?objectid=387&text=12201" + buildingNumber.ToString()).Result;
            if (!string.IsNullOrEmpty(data))
            {
                var matches = Regex.Matches(data, "(?<test>LocalityDetails\\.aspx\\?objectid=387&amp;localityid=[^\"]+)");
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        var group = match.Groups["test"];
                        if (group.Success)
                        {
                            var appartment = new Appartment();
                            switch (buildingNumber)
                            {
                                case 5:
                                    appartment.Building = "Skagafjordsgatan 11";
                                    break;
                                case 6:
                                    appartment.Building = "Surtsögatan 4";
                                    break;
                                case 7:
                                    appartment.Building = "Oddegatan 10";
                                    break;
                            }

                            appartment.DetailUrl = client.BaseAddress + "Localities/" + group.Value.Replace("&amp;", "&");

                            var uri = new Uri(appartment.DetailUrl);

                            var date = DateTime.Now;

                            date.ToString("yyyy-MM");
                            var endDate = date.ToString("yyyy-MM") + "-01";

                            date = date.AddYears(-2).AddMonths(-1);
                            var startDate = date.ToString("yyyy-MM") + "-01";

                            appartment.MeasurmentsUrl = client.BaseAddress + "Localities/LocalityOverview.aspx" + uri.Query + string.Format("&startdate={0}&enddate={1}&show=important", startDate, endDate);

                            var pos = match.Index + match.Length;
                            var index = data.IndexOf("</tr>", pos);
                            if (index > 0)
                            {
                                var subData = data.Substring(pos, index - pos);

                                // Appartment Number
                                var numberMatch = Regex.Match(subData, "(?<test>" + buildingNumber + "[0-9]+)");
                                if (numberMatch.Success)
                                {
                                    var numberGroup = numberMatch.Groups["test"];
                                    if (numberGroup.Success)
                                    {
                                        int appartmentNumber;
                                        if (int.TryParse(numberGroup.Value, out appartmentNumber))
                                        {
                                            appartment.Number = appartmentNumber;
                                        }
                                    }

                                }

                                // Appartment Size
                                var sizeMatch = Regex.Match(subData, "(?<test>[0-9]+) m&#178;");
                                if (sizeMatch.Success)
                                {
                                    var sizeGroup = sizeMatch.Groups["test"];
                                    if (sizeGroup.Success)
                                    {
                                        int appartmentSize;
                                        if (int.TryParse(sizeGroup.Value, out appartmentSize))
                                        {
                                            appartment.Size = appartmentSize;
                                        }
                                    }

                                }

                                try
                                {
                                    GetMeasurments(client, appartment);
                                }
                                catch (Exception ex)
                                {
                                    backgroundWorker1.ReportProgress(1, ex.ToString());
                                }

                                appartments.Add(appartment);
                            }
                        }
                    }
                }
            }
            return appartments;
        }

        private void GetMeasurments(HttpClient client, Appartment appartment)
        {
            var heatMeasurments = new List<Measurment>();
            var warmWaterMeasurments = new List<Measurment>();

            if (string.IsNullOrEmpty(appartment.MeasurmentsUrl))
            {
                return;
            }

            var data = client.GetStringAsync(appartment.MeasurmentsUrl).Result;

            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            //var currentYear = DateTime.Now.ToString("yy");
            //var prev1Year = DateTime.Now.AddYears(-1).ToString("yy");
            //var prev2Year = DateTime.Now.AddYears(-2).ToString("yy");

            var matches = Regex.Matches(data, ">(?<test>Varmvatten|Värme)");
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var group = match.Groups["test"];
                    if (group.Success)
                    {
                        var measurement = new Measurment();



                        //DateTime date;
                        //if (DateTime.TryParse(group.Value, out date))
                        //{
                        //    measurement.Period = date.AddMonths(-1).ToString("yyyy-MM");
                        //}

                        var pos = group.Index + group.Length;
                        var index = data.IndexOf("</tr>", pos);

                        var subData = data.Substring(pos, index - pos);

                        if (group.Value == "Varmvatten")
                        {
                            measurement.Type = MeasurmentTypes.Warmwater;
                            // WATER
                            // "(?<test>[0-9,]+ SEK\/m³), (?<test2>[0-9,]+ SEK för (?<test3>[a-z]+))">(?<test4>[0-9,]+ m³)"
                            //MATCH 1:
                            //test = `57,60 SEK / m³`
                            //test2 = `70,04 SEK för oktober`
                            //test3 = `oktober`
                            //test4	= `1,216 m³`
                            var measureMatch = Regex.Match(subData, "(?<test>[0-9,]+ SEK\\/m³), (?<test2>[0-9,]+) SEK för (?<test3>[a-z]+)\">(?<test4>[0-9,]+) m³");
                            if (measureMatch.Success)
                            {
                                var price = measureMatch.Groups["test"].Value;
                                var strCostForPeriod = measureMatch.Groups["test2"].Value;
                                var periodLabel = measureMatch.Groups["test3"].Value;
                                var strConsumption = measureMatch.Groups["test4"].Value;

                                measurement.PriceRate = price;

                                double costForPeriod;
                                if (double.TryParse(strCostForPeriod, out costForPeriod))
                                {
                                    measurement.Cost = costForPeriod;
                                }

                                measurement.Period = periodLabel;

                                double consumption;
                                if (double.TryParse(strConsumption, out consumption))
                                {
                                    measurement.Consumption = consumption;
                                }

                                warmWaterMeasurments.Add(measurement);
                            }
                        }
                        else
                        {
                            measurement.Type = MeasurmentTypes.Heat;
                            // HEAT
                            // "(?<test>[0-9,]+ SEK\/kWh), (?<test2>[0-9,]+ SEK för (?<test3>[a-z]+))">(?<test4>[0-9,]+ kWh)"
                            //MATCH 1
                            //test = `0,96 SEK / kWh`
                            //test2 = `0,00 SEK för oktober`
                            //test3 = `oktober`
                            //test4 = `0 kWh`
                            var measureMatch = Regex.Match(subData, "(?<test>[0-9,]+ SEK\\/kWh), (?<test2>[0-9,]+) SEK för (?<test3>[a-z]+)\">(?<test4>[0-9,]+) kWh");
                            if (measureMatch.Success)
                            {
                                var price = measureMatch.Groups["test"].Value;
                                var strCostForPeriod = measureMatch.Groups["test2"].Value;
                                var periodLabel = measureMatch.Groups["test3"].Value;
                                var strConsumption = measureMatch.Groups["test4"].Value;

                                measurement.PriceRate = price;

                                double costForPeriod;
                                if (double.TryParse(strCostForPeriod, out costForPeriod))
                                {
                                    measurement.Cost = costForPeriod;
                                }

                                measurement.Period = periodLabel;

                                double consumption;
                                if (double.TryParse(strConsumption, out consumption))
                                {
                                    measurement.Consumption = consumption;
                                }

                                heatMeasurments.Add(measurement);
                            }
                        }
                    }
                }
            }

            appartment.HeatMeasurments = heatMeasurments;
            appartment.WarmwaterMeasurments = warmWaterMeasurments;
        }

        private static bool Login(HttpClient client)
        {
            var data = client.GetStringAsync("/Account/Login.aspx").Result;

            var eventValidation = "";
            var viewState = "";
            var generator = "";
            if (!string.IsNullOrEmpty(data))
            {

                var match = Regex.Match(data, "id=\"__EVENTVALIDATION\" value=\"(?<test>[^\\\"]+)");
                if (match.Success)
                {
                    var group = match.Groups["test"];
                    if (group.Success)
                    {
                        eventValidation = group.Value;
                    }
                }

                match = Regex.Match(data, "id=\"__VIEWSTATE\" value=\"(?<test>[^\\\"]+)");
                if (match.Success)
                {
                    var group = match.Groups["test"];
                    if (group.Success)
                    {
                        viewState = group.Value;
                    }
                }

                match = Regex.Match(data, "id=\"__VIEWSTATEGENERATOR\" value=\"(?<test>[^\\\"]+)");
                if (match.Success)
                {
                    var group = match.Groups["test"];
                    if (group.Success)
                    {
                        generator = group.Value;
                    }
                }
            }



            SortedList<string, string> parameters = new SortedList<string, string>()
            {
                { "__EVENTTARGET", "" },
                { "__EVENTARGUMENT", "" },
                { "__LASTFOCUS", "" },
                { "__VIEWSTATE", viewState },
                { "__VIEWSTATEGENERATOR", "" },
                { "__EVENTVALIDATION", eventValidation },
                { "ctl00$LanguageDropDownList", "sv-SE" },
                { "ctl00$MainContent$LoginUser$UserName", System.Configuration.ConfigurationSettings.AppSettings.Get("username") },
                { "ctl00$MainContent$LoginUser$Password", System.Configuration.ConfigurationSettings.AppSettings.Get("password") },
                { "ctl00$MainContent$LoginUser$LoginButton", "Logga in" }
            };


            var response = client.PostAsync("/Account/Login.aspx", new FormUrlEncodedContent(parameters)).Result;
            return response.IsSuccessStatusCode;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            textBox1.Clear();
            backgroundWorker1.RunWorkerAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            textBox1.Clear();
            backgroundWorker2.RunWorkerAsync();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button4.Enabled = false;
            textBox2.Enabled = false;
            textBox1.Clear();
            backgroundWorker3.RunWorkerAsync();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button4.Enabled = false;
            textBox2.Enabled = false;
            textBox1.Clear();
            backgroundWorker4.RunWorkerAsync();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            var isValid = Regex.IsMatch(textBox2.Text, "^[0-9]+$");
            if (isValid)
            {
                var tmp = apartments.FirstOrDefault(a => a.Number.ToString() == textBox2.Text);
                isValid = tmp != null;
                if (isValid)
                {
                    selectedApartmentNumber = tmp.Number;
                }
            }

            button3.Enabled = isValid;
            button4.Enabled = isValid;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button4.Enabled = false;
            button3.Enabled = false;
            textBox2.Enabled = false;
            textBox1.Clear();
            backgroundWorker5.RunWorkerAsync();
        }
    }
}
