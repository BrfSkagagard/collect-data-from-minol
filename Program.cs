using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace MinoWebCollector
{
    class Program
    {
        static private string gitFolder = @"C:\Users\Mattias\Documents\GitHub\";
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                gitFolder = args[0];
            }
            //WriteSettings(gitFolder, new MinoWebLogin
            //{
            //    UserName = "",
            //    Password = ""
            //});
            //return;

            var login = ReadSettings(gitFolder);

            var apartments = new List<Apartment>();

            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://minoweb.se/");

                    if (!Login(client, login))
                    {
                        // TODO: We where unable to login, do something about this.
                        Console.WriteLine("Failed login!");
                        return;
                    }
                    Console.WriteLine("logged in!");

                    var buildingNumbers = new int[] { 5, 6, 7 };

                    foreach (var buildingNumber in buildingNumbers)
                    {
                        try
                        {
                            apartments.AddRange(GetApartmentsForBuilding(client, buildingNumber));
                        }
                        catch (Exception ex)
                        {
                            //backgroundWorker1.ReportProgress(1, ex.ToString());
                        }
                    }

                    var folderBoard = gitFolder + "brfskagagard-styrelsen" + Path.DirectorySeparatorChar;
                    var folderBoardExists = Directory.Exists(folderBoard);
                    foreach (Apartment item in apartments)
                    {
                        //backgroundWorker1.ReportProgress(1, item.ToString());
                        var json = new DataContractJsonSerializer(typeof(Apartment));

                        var folder = gitFolder + "brfskagagard-lgh" + item.Number + Path.DirectorySeparatorChar;
                        var folderExists = Directory.Exists(folder);

                        // We only want to update repositories that we know about (read: that we have created)
                        if (folderExists)
                        {
                            using (
                                var stream =
                                    File.Create(folder + "minol-apartment-measurement.json"))
                            {
                                json.WriteObject(stream, item);
                                stream.Flush();
                            }
                        }

                        // We only want to update repositories that we know about (read: that we have created)
                        if (folderBoardExists)
                        {
                            using (
                                var stream =
                                    File.Create(folderBoard + "minol-apartment-measurement-" + item.Number + ".json"))
                            {
                                json.WriteObject(stream, item);
                                stream.Flush();
                            }
                        }


                    }

                    Console.WriteLine("Number of appartments: " + apartments.Count);
                }
            }


            //var folders = Directory.GetDirectories(gitFolder, "brfskagagard-lgh*");
            //foreach (string folder in folders)
            //{
            //    // SyncRepository(folder, login.Token, author);
            //}
        }

        private static MinoWebLogin ReadSettings(string gitFolder)
        {
            var stream = System.IO.File.OpenRead(gitFolder + "minol-setting.json");

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(MinoWebLogin));

            var setting = serializer.ReadObject(stream) as MinoWebLogin;
            stream.Close();
            return setting;
        }

        private static void WriteSettings(string gitFolder, MinoWebLogin login)
        {
            var stream = System.IO.File.Create(gitFolder + "minol-setting.json");

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(MinoWebLogin));

            serializer.WriteObject(stream, login);
            stream.Flush();
            stream.Close();
        }

        private static bool Login(HttpClient client, MinoWebLogin login)
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
                { "ctl00$MainContent$LoginUser$UserName", login.UserName },
                { "ctl00$MainContent$LoginUser$Password", login.Password },
                { "ctl00$MainContent$LoginUser$LoginButton", "Logga in" }
            };

            var response = client.PostAsync("/Account/Login.aspx", new FormUrlEncodedContent(parameters)).Result;
            return response.IsSuccessStatusCode;
        }

        static private List<Apartment> GetApartmentsForBuilding(HttpClient client, int buildingNumber)
        {
            var appartments = new List<Apartment>();

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
                            var appartment = new Apartment();
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
                                    //backgroundWorker1.ReportProgress(1, ex.ToString());
                                }

                                appartments.Add(appartment);
                            }
                        }
                    }
                }
            }
            return appartments;
        }

        static private void GetMeasurments(HttpClient client, Apartment apartment)
        {
            var heatMeasurments = new List<Measurment>();
            var warmWaterMeasurments = new List<Measurment>();

            if (string.IsNullOrEmpty(apartment.MeasurmentsUrl))
            {
                return;
            }

            var data = client.GetStringAsync(apartment.MeasurmentsUrl).Result;

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

            apartment.HeatMeasurments = heatMeasurments;
            apartment.WarmwaterMeasurments = warmWaterMeasurments;
        }
    }
}
