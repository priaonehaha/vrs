﻿// Copyright © 2010 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Test.Framework;
using VirtualRadar.Interface.Database;
using VirtualRadar.Interface.StandingData;
using VirtualRadar.Interface.WebSite;
using VirtualRadar.Interface;

namespace Test.VirtualRadar.WebSite
{
    // These tests deal with the production of the ReportRows.json file.
    public partial class WebSiteTests
    {
        #region Private enums - ReportJsonType, SingleAircraftReport
        /// <summary>
        /// An enumeration of the different types of JSON object that a report rows request can return.
        /// </summary>
        enum ReportJsonClass
        {
            /// <summary>
            /// The report rows describe the many flights that were undertaken by a single aircraft.
            /// </summary>
            Aircraft,

            /// <summary>
            /// The report rows describes many flights undertaken by many aircraft.
            /// </summary>
            Flight,
        }

        /// <summary>
        /// An enumeration of the known single aircraft reports.
        /// </summary>
        enum SingleAircraftReport
        {
            Icao,

            Registration,
        }
        #endregion

        #region Private class - ReportRowsAddress
        class ReportRowsAddress
        {
            public string Page { get { return "/ReportRows.json"; } }
            public string Report { get; set; }
            public int? FromRow { get; set; }
            public int? ToRow { get; set; }
            public string SortField1 { get; set; }
            public string SortField2 { get; set; }
            public bool? SortAscending1 { get; set; }
            public bool? SortAscending2 { get; set; }
            public bool UseAlternativeCallsigns { get; set; }

            public DateFilter Date { get; set; }
            public StringFilter Icao24 { get; set; }
            public StringFilter Registration { get; set; }
            public StringFilter Callsign { get; set; }
            public BoolFilter<bool> IsEmergency { get; set; }
            public BoolFilter<bool> IsMilitary { get; set; }
            public StringFilter Operator { get; set; }
            public StringFilter Country { get; set; }
            public BoolFilter<WakeTurbulenceCategory> WakeTurbulenceCategory { get; set; }
            public BoolFilter<Species> Species { get; set; }
            public StringFilter Type { get; set; }
            public NumericFilter<int> FirstAltitude { get; set; }
            public NumericFilter<int> LastAltitude { get; set; }

            public ReportRowsAddress()
            {
            }

            public string Address
            {
                get
                {
                    Dictionary<string, string> queryValues = new Dictionary<string,string>();

                    AddQueryString(queryValues, "rep", Report);
                    AddQueryString(queryValues, "fromRow", FromRow);
                    AddQueryString(queryValues, "toRow", ToRow);
                    AddQueryString(queryValues, "sort1", SortField1);
                    AddQueryString(queryValues, "sort2", SortField2);
                    AddQueryString(queryValues, "sort1dir", SortAscending1, (bool? value) => { return value.Value ? "asc" : "desc"; } );
                    AddQueryString(queryValues, "sort2dir", SortAscending2, (bool? value) => { return value.Value ? "asc" : "desc"; } );
                    AddQueryString(queryValues, "altCall", UseAlternativeCallsigns ? "1" : "0");

                    if(Date != null)                    Date.AddQueryValues("date-", queryValues);
                    if(Icao24 != null)                  Icao24.AddQueryValues("icao-", queryValues);
                    if(Registration != null)            Registration.AddQueryValues("reg-", queryValues);
                    if(Callsign != null)                Callsign.AddQueryValues("call-", queryValues);
                    if(IsEmergency != null)             IsEmergency.AddQueryValues("emg-", queryValues);
                    if(IsMilitary != null)              IsMilitary.AddQueryValues("mil-", queryValues);
                    if(Operator != null)                Operator.AddQueryValues("op-", queryValues);
                    if(Country != null)                 Country.AddQueryValues("cou-", queryValues);
                    if(WakeTurbulenceCategory != null)  WakeTurbulenceCategory.AddQueryValues("wtc-", queryValues);
                    if(Species != null)                 Species.AddQueryValues("spc-", queryValues);
                    if(Type != null)                    Type.AddQueryValues("typ-", queryValues);
                    if(FirstAltitude != null)           FirstAltitude.AddQueryValues("falt-", queryValues);
                    if(LastAltitude != null)            LastAltitude.AddQueryValues("lalt-", queryValues);

                    StringBuilder queryString = new StringBuilder();
                    foreach(var kvp in queryValues) {
                        queryString.AppendFormat("{0}{1}={2}", queryString.Length == 0 ? "?" : "&", kvp.Key, HttpUtility.UrlEncode(kvp.Value));
                    }

                    return String.Format("{0}{1}", Page, queryString);
                }
            }

            private void AddQueryString<T>(Dictionary<string, string> queryValues, string key, T value, Func<T, string> toString = null)
            {
                if(value != null) queryValues.Add(key, toString == null ? value.ToString() : toString(value));
            }

            public void SetFromWorksheet(string propertyName, string queryValue)
            {
                switch(propertyName) {
                    case "FromDate":
                        if(Date == null) Date = new DateFilter(null, null, false);
                        if(!String.IsNullOrEmpty(queryValue)) Date.Lower = DateTime.ParseExact("yyyy-MM-dd", queryValue, CultureInfo.InvariantCulture);
                        break;
                    case "ToDate":
                        if(Date == null) Date = new DateFilter(null, null, false);
                        if(!String.IsNullOrEmpty(queryValue)) Date.Upper = DateTime.ParseExact("yyyy-MM-dd", queryValue, CultureInfo.InvariantCulture);
                        break;
                    case "FromFirstAltitude":
                        if(FirstAltitude == null) FirstAltitude = new NumericFilter<int>(null, null, false);
                        if(!String.IsNullOrEmpty(queryValue)) FirstAltitude.Lower = int.Parse(queryValue, CultureInfo.InvariantCulture);
                        break;
                    case "ToFirstAltitude":
                        if(FirstAltitude == null) FirstAltitude = new NumericFilter<int>(null, null, false);
                        if(!String.IsNullOrEmpty(queryValue)) FirstAltitude.Upper = int.Parse(queryValue, CultureInfo.InvariantCulture);
                        break;
                    case "FromLastAltitude":
                        if(LastAltitude == null) LastAltitude = new NumericFilter<int>(null, null, false);
                        if(!String.IsNullOrEmpty(queryValue)) LastAltitude.Lower = int.Parse(queryValue, CultureInfo.InvariantCulture);
                        break;
                    case "ToLastAltitude":
                        if(LastAltitude == null) LastAltitude = new NumericFilter<int>(null, null, false);
                        if(!String.IsNullOrEmpty(queryValue)) LastAltitude.Upper = int.Parse(queryValue, CultureInfo.InvariantCulture);
                        break;
                    case "Icao24":                  Icao24 = new StringFilter(queryValue, FilterCondition.Equals, false); break;
                    case "Registration":            Registration = new StringFilter(queryValue, FilterCondition.Equals, false); break;
                    case "Callsign":                Callsign = new StringFilter(queryValue, FilterCondition.Equals, false); break;
                    case "IsEmergency":             IsEmergency = new BoolFilter<bool>(queryValue == "True", false); break;
                    case "IsMilitary":              IsMilitary = new BoolFilter<bool>(queryValue == "True", false); break;
                    case "Operator":                Operator = new StringFilter(queryValue, FilterCondition.Equals, false); break;
                    case "Country":                 Country = new StringFilter(queryValue, FilterCondition.Equals, false); break;
                    case "WakeTurbulenceCategory":  WakeTurbulenceCategory = new BoolFilter<WakeTurbulenceCategory>((WakeTurbulenceCategory)Enum.Parse(typeof(WakeTurbulenceCategory), queryValue), false); break;
                    case "Species":                 Species = new BoolFilter<Species>((Species)Enum.Parse(typeof(Species), queryValue), false); break;
                    case "Type":                    Type = new StringFilter(queryValue, FilterCondition.Equals, false); break;
                    default:                        throw new NotImplementedException(String.Format("Unknown property name {0}", propertyName));
                }
            }
        }
        #endregion

        #region Report test helpers - ReportJsonType, SingleAircraftReportName, ConfigureDatabaseForSingleAircraftReport
        /// <summary>
        /// Returns the type of the top-level JSON class associated with the report type passed across.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Type ReportJsonType(ReportJsonClass type)
        {
            switch(type) {
                case ReportJsonClass.Aircraft:  return typeof(AircraftReportJson);
                case ReportJsonClass.Flight:    return typeof(FlightReportJson);
                default:                        throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns the identifier of the single aircraft report passed across.
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        private string SingleAircraftReportType(SingleAircraftReport report)
        {
            switch(report) {
                case SingleAircraftReport.Icao:         return "icao";
                case SingleAircraftReport.Registration: return "reg";
                default:                                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Sets up the database mock to return _DatabaseAircraft when the identifier is passed to it. Optionally configures the report address to pass
        /// the same (or a different) identifer to the report.
        /// </summary>
        /// <param name="report"></param>
        /// <param name="expectedIdentifier"></param>
        /// <param name="addIdentiferToAddress"></param>
        /// <param name="addressIdentifier"></param>
        private void ConfigureDatabaseForSingleAircraftReport(SingleAircraftReport report, string expectedIdentifier = "ANYTHING", bool addIdentiferToAddress = true, string addressIdentifier = null)
        {
            switch(report) {
                case SingleAircraftReport.Icao:
                    _BaseStationDatabase.Setup(db => db.GetAircraftByCode(expectedIdentifier)).Returns(_DatabaseAircraft);
                    _DatabaseAircraft.ModeS = expectedIdentifier;
                    _ReportRowsAddress.Icao24 = new StringFilter(addIdentiferToAddress ? expectedIdentifier : addressIdentifier, FilterCondition.Equals, false);
                    break;
                case SingleAircraftReport.Registration:
                    _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration(expectedIdentifier)).Returns(_DatabaseAircraft);
                    _DatabaseAircraft.Registration = expectedIdentifier;
                    _ReportRowsAddress.Registration = new StringFilter(addIdentiferToAddress ? expectedIdentifier : addressIdentifier, FilterCondition.Equals, false);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion

        #region Date report
        [TestMethod]
        public void WebSite_ReportRows_DateReport_Generates_Correct_JSON_When_No_Rows_Match()
        {
            Do_ReportRows_Report_Generates_Correct_JSON_When_No_Rows_Match("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Adds_Correct_Cache_Control_Header()
        {
            Do_ReportRows_Report_Adds_Correct_Cache_Control_Header("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Only_Returns_Json_If_Reports_Are_Permitted()
        {
            Do_ReportRows_Report_Only_Returns_Json_If_Reports_Are_Permitted("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Returns_Count_Of_Rows_Matching_Criteria()
        {
            Do_WebSite_ReportRows_Report_Returns_Count_Of_Rows_Matching_Criteria("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Returns_Details_Of_Exceptions_Raised_During_Report_Generation()
        {
            Do_WebSite_ReportRows_Report_Returns_Details_Of_Exceptions_Raised_During_Report_Generation("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Logs_Exceptions_Raised_During_Report_Generation()
        {
            Do_WebSite_ReportRows_Report_Logs_Exceptions_Raised_During_Report_Generation("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Returns_Processing_Time()
        {
            Do_WebSite_ReportRows_Report_Returns_Processing_Time("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Returns_Images_Available_Flags()
        {
            Do_WebSite_ReportRows_Report_Returns_Images_Available_Flags("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "FlightsReportDateLimits$")]
        public void WebSite_ReportRows_DateReport_Returns_Date_Ranges_Used()
        {
            _Configuration.InternetClientSettings.CanRunReports = true;
            _ReportRowsAddress.Report = "date";

            var worksheet = new ExcelWorksheetData(TestContext);
            // if(!worksheet.NBool("Isolate").GetValueOrDefault()) return;
            var callsign = worksheet.String("Callsign");
            var registration = worksheet.String("Registration");
            var icao24 = worksheet.String("Icao24");

            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.Date = new DateFilter(worksheet.NDateTime("RequestStart"), worksheet.NDateTime("RequestEnd"), false);
            _ReportRowsAddress.Callsign = callsign != null ? new StringFilter(worksheet.String("Callsign"), FilterCondition.Equals, false) : null;
            _ReportRowsAddress.Registration = registration != null ? new StringFilter(worksheet.String("Registration"), FilterCondition.Equals, false) : null;
            _ReportRowsAddress.Icao24 = icao24 != null ? new StringFilter(worksheet.String("Icao24"), FilterCondition.Equals, false) : null;

            _Provider.Setup(p => p.UtcNow).Returns(worksheet.DateTime("Today"));

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address, worksheet.Bool("IsInternetClient"));

            var actualStart = worksheet.NDateTime("ActualStart");
            var actualEnd = worksheet.NDateTime("ActualEnd");
            Assert.AreEqual(actualStart.GetValueOrDefault().Year == 1 ? null : actualStart.Value.Date.ToString("yyyy-MM-dd"), json.FromDate);
            Assert.AreEqual(actualEnd.GetValueOrDefault().Year == 1 ? null : actualEnd.Value.Date.ToString("yyyy-MM-dd"), json.ToDate);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Passes_Same_Criteria_To_CountRows_And_FetchRows()
        {
            _ReportRowsAddress.Report = "date";

            SearchBaseStationCriteria searchCriteria = null;
            _BaseStationDatabase.Setup(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>())).Returns((SearchBaseStationCriteria c) => { searchCriteria = c; return 1; });

            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetFlights(searchCriteria, -1, -1, null, It.IsAny<bool>(), null, It.IsAny<bool>()), Times.Once());
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "FlightsReportDateLimits$")]
        public void WebSite_ReportRows_DateReport_Sets_Correct_Limits_On_Date_Ranges()
        {
            _Configuration.InternetClientSettings.CanRunReports = true;
            var worksheet = new ExcelWorksheetData(TestContext);
            // if(!worksheet.NBool("Isolate").GetValueOrDefault()) return;
            var callsign = worksheet.String("Callsign");
            var registration = worksheet.String("Registration");
            var icao24 = worksheet.String("Icao24");

            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.Date = new DateFilter(worksheet.NDateTime("RequestStart"), worksheet.NDateTime("RequestEnd"), false);
            _ReportRowsAddress.Callsign = callsign != null ? new StringFilter(worksheet.String("Callsign"), FilterCondition.Equals, false) : null;
            _ReportRowsAddress.Registration = registration != null ? new StringFilter(worksheet.String("Registration"), FilterCondition.Equals, false) : null;
            _ReportRowsAddress.Icao24 = icao24 != null ? new StringFilter(worksheet.String("Icao24"), FilterCondition.Equals, false) : null;

            _Provider.Setup(p => p.UtcNow).Returns(worksheet.DateTime("Today"));

            SearchBaseStationCriteria searchCriteria = null;
            _BaseStationDatabase.Setup(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>())).Returns((SearchBaseStationCriteria c) => { searchCriteria = c; return 1; });

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address, worksheet.Bool("IsInternetClient"));

            var actualStart = worksheet.NDateTime("ActualStart");
            var actualEnd = worksheet.NDateTime("ActualEnd");

            if(actualStart == null) Assert.IsTrue(searchCriteria.Date == null || searchCriteria.Date.LowerValue == null);
            else                    Assert.AreEqual(actualStart.Value, searchCriteria.Date.LowerValue);

            if(actualEnd == null)   Assert.IsTrue(searchCriteria.Date == null || searchCriteria.Date.UpperValue == null);
            else                    Assert.AreEqual(actualEnd.Value, searchCriteria.Date.UpperValue);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Passes_Range_And_Sort_Criteria_To_FetchRows()
        {
            _ReportRowsAddress.Report = "date";

            _ReportRowsAddress.FromRow = 10;
            _ReportRowsAddress.ToRow = 11;
            _ReportRowsAddress.SortField1 = "Ff1";
            _ReportRowsAddress.SortField2 = "Ff2";
            _ReportRowsAddress.SortAscending1 = true;
            _ReportRowsAddress.SortAscending2 = false;
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), 10, 11, "Ff1", true, "Ff2", false), Times.Once());

            _ReportRowsAddress.SortAscending1 = false;
            _ReportRowsAddress.SortAscending2 = true;
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));
            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), 10, 11, "Ff1", false, "Ff2", true), Times.Once());
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Passes_UseAlternativeCallsigns_Criteria_To_FetchRows()
        {
            _ReportRowsAddress.Report = "date";

            SearchBaseStationCriteria criteria = null;
            _BaseStationDatabase.Setup(r => r.GetFlights(It.IsAny<SearchBaseStationCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>())).Callback((SearchBaseStationCriteria sbsc, int x1, int x2, string x3, bool x4, string x5, bool x6) => {
                criteria = sbsc;
            });

            _ReportRowsAddress.UseAlternativeCallsigns = true;
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            Assert.IsTrue(criteria.UseAlternateCallsigns);

            _ReportRowsAddress.UseAlternativeCallsigns = false;
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            Assert.IsFalse(criteria.UseAlternateCallsigns);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Informs_Caller_Of_Primary_Sort_Column_Used()
        {
            Do_WebSite_ReportRows_Report_Informs_Caller_Of_Primary_Sort_Column_Used("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Fetches_All_Rows_When_NonDatabase_Criteria_Is_Used()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.FromRow = 12;
            _ReportRowsAddress.FromRow = 42;

            _ReportRowsAddress.Species = new BoolFilter<Species>(Species.Gyrocopter, false);
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            _BaseStationDatabase.Verify(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>()), Times.Never());
            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), -1, -1, It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());

            _ReportRowsAddress.Species = null;
            _ReportRowsAddress.WakeTurbulenceCategory = new BoolFilter<WakeTurbulenceCategory>(WakeTurbulenceCategory.Medium, false);
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            _BaseStationDatabase.Verify(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>()), Times.Never());
            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), -1, -1, It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));

            _ReportRowsAddress.WakeTurbulenceCategory = null;
            _ReportRowsAddress.IsMilitary = new BoolFilter<bool>(true, false);
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            _BaseStationDatabase.Verify(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>()), Times.Never());
            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), -1, -1, It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(3));
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Still_Sorts_On_Database_When_NonDatabase_Criteria_Is_Used()
        {
            _ReportRowsAddress.Report = "date";

            _ReportRowsAddress.IsMilitary = new BoolFilter<bool>(true, false);
            _ReportRowsAddress.SortField1 = "abc";
            _ReportRowsAddress.SortField2 = "xyz";

            _ReportRowsAddress.SortAscending1 = true;
            _ReportRowsAddress.SortAscending2 = false;
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), -1, -1, "abc", true, "xyz", false), Times.Once());

            _ReportRowsAddress.SortAscending1 = false;
            _ReportRowsAddress.SortAscending2 = true;
            SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            _BaseStationDatabase.Verify(db => db.GetFlights(It.IsAny<SearchBaseStationCriteria>(), -1, -1, "abc", false, "xyz", true), Times.Once());
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Honours_IsMilitary_NonDatabase_Criteria()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.IsMilitary = new BoolFilter<bool>(true, false);
            AddBlankDatabaseFlights(3);
            _DatabaseFlights[0].Aircraft.ModeS = "A";
            _DatabaseFlights[1].Aircraft.ModeS = "B";
            _DatabaseFlights[2].Aircraft.ModeS = "C";

            var codeBlockA = (CodeBlock)null;
            var codeBlockB = new CodeBlock();
            var codeBlockC = new CodeBlock();

            codeBlockB.IsMilitary = true;

            _StandingDataManager.Setup(m => m.FindCodeBlock("A")).Returns(codeBlockA);
            _StandingDataManager.Setup(m => m.FindCodeBlock("B")).Returns(codeBlockB);
            _StandingDataManager.Setup(m => m.FindCodeBlock("C")).Returns(codeBlockC);

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            Assert.AreEqual(1, json.Flights.Count);
            Assert.AreEqual(1, json.Aircraft.Count);
            Assert.AreEqual("B", json.Aircraft[0].Icao);
            Assert.AreEqual(1, json.CountRows);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Honours_Species_NonDatabase_Criteria()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.Species = new BoolFilter<Species>(Species.Seaplane, false);
            AddBlankDatabaseFlights(3);
            _DatabaseFlights[0].Aircraft.ICAOTypeCode = "A";
            _DatabaseFlights[1].Aircraft.ICAOTypeCode = "B";
            _DatabaseFlights[2].Aircraft.ICAOTypeCode = "C";

            var aircraftTypeA = (AircraftType)null;
            var aircraftTypeB = new AircraftType() { Species = Species.Landplane };
            var aircraftTypeC = new AircraftType() { Species = Species.Seaplane };

            _StandingDataManager.Setup(m => m.FindAircraftType("A")).Returns(aircraftTypeA);
            _StandingDataManager.Setup(m => m.FindAircraftType("B")).Returns(aircraftTypeB);
            _StandingDataManager.Setup(m => m.FindAircraftType("C")).Returns(aircraftTypeC);

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            Assert.AreEqual(1, json.Flights.Count);
            Assert.AreEqual(1, json.Aircraft.Count);
            Assert.AreEqual("C", json.Aircraft[0].IcaoTypeCode);
            Assert.AreEqual(1, json.CountRows);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Honours_WakeTurbulenceCategory_NonDatabase_Criteria()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.WakeTurbulenceCategory = new BoolFilter<WakeTurbulenceCategory>(WakeTurbulenceCategory.Heavy, false);
            AddBlankDatabaseFlights(3);
            _DatabaseFlights[0].Aircraft.ICAOTypeCode = "A";
            _DatabaseFlights[1].Aircraft.ICAOTypeCode = "B";
            _DatabaseFlights[2].Aircraft.ICAOTypeCode = "C";

            var aircraftTypeA = new AircraftType() { WakeTurbulenceCategory = WakeTurbulenceCategory.Heavy };
            var aircraftTypeB = (AircraftType)null;
            var aircraftTypeC = new AircraftType() { WakeTurbulenceCategory = WakeTurbulenceCategory.Medium };

            _StandingDataManager.Setup(m => m.FindAircraftType("A")).Returns(aircraftTypeA);
            _StandingDataManager.Setup(m => m.FindAircraftType("B")).Returns(aircraftTypeB);
            _StandingDataManager.Setup(m => m.FindAircraftType("C")).Returns(aircraftTypeC);

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            Assert.AreEqual(1, json.Flights.Count);
            Assert.AreEqual(1, json.Aircraft.Count);
            Assert.AreEqual("A", json.Aircraft[0].IcaoTypeCode);
            Assert.AreEqual(1, json.CountRows);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Honours_Combination_NonDatabase_Criteria()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.IsMilitary = new BoolFilter<bool>(true, false);
            _ReportRowsAddress.Species = new BoolFilter<Species>(Species.Helicopter, false);
            _ReportRowsAddress.WakeTurbulenceCategory = new BoolFilter<WakeTurbulenceCategory>(WakeTurbulenceCategory.Light, false);
            AddBlankDatabaseFlights(4);
            _DatabaseFlights[0].Aircraft.ModeS = _DatabaseFlights[0].Aircraft.ICAOTypeCode = "A";
            _DatabaseFlights[1].Aircraft.ModeS = _DatabaseFlights[1].Aircraft.ICAOTypeCode = "B";
            _DatabaseFlights[2].Aircraft.ModeS = _DatabaseFlights[2].Aircraft.ICAOTypeCode = "C";  // <-- this will be the only one where all criteria match
            _DatabaseFlights[3].Aircraft.ModeS = _DatabaseFlights[3].Aircraft.ICAOTypeCode = "D";

            var codeBlockA = new CodeBlock() { IsMilitary = true };
            var codeBlockB = new CodeBlock() { IsMilitary = true };
            var codeBlockC = new CodeBlock() { IsMilitary = true };
            var codeBlockD = new CodeBlock() { IsMilitary = false };

            _StandingDataManager.Setup(m => m.FindCodeBlock("A")).Returns(codeBlockA);
            _StandingDataManager.Setup(m => m.FindCodeBlock("B")).Returns(codeBlockB);
            _StandingDataManager.Setup(m => m.FindCodeBlock("C")).Returns(codeBlockC);
            _StandingDataManager.Setup(m => m.FindCodeBlock("D")).Returns(codeBlockD);

            var aircraftTypeA = new AircraftType() { Species = Species.Helicopter, WakeTurbulenceCategory = WakeTurbulenceCategory.Medium, };
            var aircraftTypeB = new AircraftType() { Species = Species.Gyrocopter, WakeTurbulenceCategory = WakeTurbulenceCategory.Light, };
            var aircraftTypeC = new AircraftType() { Species = Species.Helicopter, WakeTurbulenceCategory = WakeTurbulenceCategory.Light, };
            var aircraftTypeD = new AircraftType() { Species = Species.Helicopter, WakeTurbulenceCategory = WakeTurbulenceCategory.Light, };

            _StandingDataManager.Setup(m => m.FindAircraftType("A")).Returns(aircraftTypeA);
            _StandingDataManager.Setup(m => m.FindAircraftType("B")).Returns(aircraftTypeB);
            _StandingDataManager.Setup(m => m.FindAircraftType("C")).Returns(aircraftTypeC);
            _StandingDataManager.Setup(m => m.FindAircraftType("D")).Returns(aircraftTypeD);

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            Assert.AreEqual(1, json.Flights.Count);
            Assert.AreEqual(1, json.Aircraft.Count);
            Assert.AreEqual("C", json.Aircraft[0].Icao);
            Assert.AreEqual(1, json.CountRows);
        }

        [TestMethod]
        [DataSource("Data Source='BaseStationTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "GetFlightsRows$")]
        public void WebSite_ReportRows_DateReport_Extracts_Correct_Rows_From_Full_Set_When_NonDatabase_Criteria_Are_Used()
        {
            // This uses the same set of test data as used by the unit tests on the database objects because the same behaviour needs to be reproduced

            var worksheet = new ExcelWorksheetData(TestContext);

            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.IsMilitary = new BoolFilter<bool>(true, false);

            int flightCount = worksheet.Int("Flights");
            AddBlankDatabaseFlights(flightCount);
            for(var i = 0;i < flightCount;++i) {
                _DatabaseFlights[i].Aircraft.ModeS = "A";
                _DatabaseFlights[i].Callsign = (i + 1).ToString();
            }

            var codeBlock = new CodeBlock() { IsMilitary = true };
            _StandingDataManager.Setup(m => m.FindCodeBlock("A")).Returns(codeBlock);

            _ReportRowsAddress.FromRow = worksheet.Int("StartRow");
            _ReportRowsAddress.ToRow = worksheet.Int("EndRow");
            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            var rows = "";
            foreach(var flight in json.Flights) {
                rows = String.Format("{0}{1}{2}", rows, rows.Length == 0 ? "" : ",", flight.Callsign);
            }

            Assert.AreEqual(flightCount, json.CountRows);
            Assert.AreEqual(worksheet.Int("ExpectedCount"), json.Flights.Count);
            Assert.AreEqual(worksheet.EString("ExpectedRows"), rows);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Returns_Rows_From_FetchRows()
        {
            Do_WebSite_ReportRows_Report_Returns_Rows_From_FetchRows("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ReportFlightJson$")]
        public void WebSite_ReportRows_DateReport_Transcribes_Flights_From_Database_To_Json()
        {
            Do_WebSite_ReportRows_Report_Transcribes_Flights_From_Database_To_Json("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Sets_Flight_Row_Numbers_Correctly()
        {
            Do_WebSite_ReportRows_Report_Sets_Flight_Row_Numbers_Correctly("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ReportAircraftJson$")]
        public void WebSite_ReportRows_DateReport_Returns_Aircraft_From_FetchRows()
        {
            Do_WebSite_ReportRows_Report_Returns_Aircraft_From_FetchRows("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Fills_Aircraft_HasPicture_Correctly()
        {
            _ReportRowsAddress.Report = "date";

            AddBlankDatabaseFlights(2);
            _DatabaseFlights[0].Aircraft.Registration = "A";
            _DatabaseFlights[1].Aircraft.Registration = "B";

            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "A")).Returns((PictureDetail)null);
            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "B")).Returns(new PictureDetail() { FileName = "B picture filename", Width = 640, Height = 480 });

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            Assert.AreEqual(2, json.Aircraft.Count);

            var jsonA = json.Aircraft.Where(a => a.Registration == "A").Single();
            Assert.IsFalse(jsonA.HasPicture);
            Assert.AreEqual(0, jsonA.PictureWidth);
            Assert.AreEqual(0, jsonA.PictureHeight);

            var jsonB = json.Aircraft.Where(a => a.Registration == "B").Single();
            Assert.IsTrue(jsonB.HasPicture);
            Assert.AreEqual(640, jsonB.PictureWidth);
            Assert.AreEqual(480, jsonB.PictureHeight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Ignores_Aircraft_Pictures_For_Internet_Clients_If_Configuration_Specifies()
        {
            Do_WebSite_ReportRows_Report_Ignores_Aircraft_Pictures_For_Internet_Clients_If_Configuration_Specifies("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Adjusts_HasPicture_To_Suit_Configuration_Changes()
        {
            Do_WebSite_ReportRows_Report_Adjusts_HasPicture_To_Suit_Configuration_Changes("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Looks_Up_ISO8643_Data_For_Aircraft()
        {
            _ReportRowsAddress.Report = "date";

            AddBlankDatabaseFlights(3);
            _DatabaseFlights[0].Aircraft.ICAOTypeCode = null;
            _DatabaseFlights[1].Aircraft.ICAOTypeCode = "";
            _DatabaseFlights[2].Aircraft.ICAOTypeCode = "A";

            var aircraftType = new AircraftType() {
                Engines = "X",
                EngineType = EngineType.Piston,
                Species = Species.Gyrocopter,
                EnginePlacement = EnginePlacement.AftMounted,
                WakeTurbulenceCategory = WakeTurbulenceCategory.Light,
            };

            _StandingDataManager.Setup(m => m.FindAircraftType("A")).Returns(aircraftType);

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address, false);
            var aircraft = json.Aircraft.Where(a => a.IcaoTypeCode == "A").Single();

            _StandingDataManager.Verify(m => m.FindAircraftType(It.IsAny<string>()), Times.Once());
            Assert.AreEqual("X", aircraft.Engines);
            Assert.AreEqual((int)EngineType.Piston, aircraft.EngineType);
            Assert.AreEqual((int)EnginePlacement.AftMounted, aircraft.EnginePlacement);
            Assert.AreEqual((int)Species.Gyrocopter, aircraft.Species);
            Assert.AreEqual((int)WakeTurbulenceCategory.Light, aircraft.WakeTurbulenceCategory);

            foreach(var otherAircraft in json.Aircraft.Where(a => String.IsNullOrEmpty(a.IcaoTypeCode))) {
                Assert.AreEqual(null, otherAircraft.Engines);
                Assert.AreEqual(null, otherAircraft.EngineType);
                Assert.AreEqual(null, otherAircraft.EnginePlacement);
                Assert.AreEqual(null, otherAircraft.Species);
                Assert.AreEqual(null, otherAircraft.WakeTurbulenceCategory);
            }
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Looks_Up_ICAO_Code_Block_For_Aircraft()
        {
            _ReportRowsAddress.Report = "date";

            AddBlankDatabaseFlights(4);
            _DatabaseFlights[0].Aircraft.ModeS = null;
            _DatabaseFlights[1].Aircraft.ModeS = "";
            _DatabaseFlights[2].Aircraft.ModeS = "A";
            _DatabaseFlights[3].Aircraft.ModeS = "B";

            var isMilitary = new CodeBlock() { IsMilitary = true };
            _StandingDataManager.Setup(p => p.FindCodeBlock("A")).Returns(isMilitary);

            var isNotMilitary = new CodeBlock() { IsMilitary = false };
            _StandingDataManager.Setup(p => p.FindCodeBlock("B")).Returns(isNotMilitary);

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address, false);
            Assert.AreEqual(true, json.Aircraft.Where(a => a.Icao == "A").Single().Military);
            Assert.AreEqual(false, json.Aircraft.Where(a => a.Icao == "B").Single().Military);
            Assert.AreEqual(false, json.Aircraft.Where(a => a.Icao == "").Single().Military);
            Assert.AreEqual(false, json.Aircraft.Where(a => a.Icao == null).Single().Military);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Uses_ModeSCountry_From_Database()
        {
            Do_WebSite_ReportRows_Report_Uses_ModeSCountry_From_Database("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Returns_Distinct_Aircraft_Instances()
        {
            _ReportRowsAddress.Report = "date";

            AddBlankDatabaseFlights(3);
            _DatabaseFlights[0].Callsign = "1";
            _DatabaseFlights[1].Callsign = "2";
            _DatabaseFlights[2].Callsign = "3";
            _DatabaseFlights[0].Aircraft.Registration = "A";
            _DatabaseFlights[1].Aircraft.AircraftID =   _DatabaseFlights[0].Aircraft.AircraftID;
            _DatabaseFlights[1].Aircraft.Registration = _DatabaseFlights[0].Aircraft.Registration;
            _DatabaseFlights[2].Aircraft.Registration = "B";

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);

            var flight1 = json.Flights.Where(f => f.Callsign == "1").Single();
            var flight2 = json.Flights.Where(f => f.Callsign == "2").Single();
            var flight3 = json.Flights.Where(f => f.Callsign == "3").Single();

            Assert.AreEqual(2, json.Aircraft.Count);
            Assert.AreEqual("A", json.Aircraft[flight1.AircraftIndex.Value].Registration);
            Assert.AreEqual("A", json.Aircraft[flight2.AircraftIndex.Value].Registration);
            Assert.AreEqual("B", json.Aircraft[flight3.AircraftIndex.Value].Registration);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Copes_If_Flight_Aircraft_Cannot_Be_Found()
        {
            _ReportRowsAddress.Report = "date";

            AddBlankDatabaseFlights(2);
            _DatabaseFlights[0].Callsign = "1";
            _DatabaseFlights[1].Callsign = "2";
            _DatabaseFlights[1].Aircraft = null;

            var json = SendJsonRequest<FlightReportJson>(_ReportRowsAddress.Address);
            var flight1 = json.Flights.Where(f => f.Callsign == "1").Single();
            var flight2 = json.Flights.Where(f => f.Callsign == "2").Single();

            Assert.AreEqual(2, json.Aircraft.Count);
            Assert.AreEqual(false, json.Aircraft[flight1.AircraftIndex.Value].IsUnknown);
            Assert.AreEqual(true, json.Aircraft[flight2.AircraftIndex.Value].IsUnknown);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Fills_Routes_And_Aircraft_Tables()
        {
            Do_WebSite_ReportRows_Report_Fills_Routes_And_Aircraft_Tables("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Writes_Correct_Route_Index_When_There_Is_No_Route()
        {
            Do_WebSite_ReportRows_Report_Writes_Correct_Route_Index_When_There_Is_No_Route("date", ReportJsonClass.Flight);
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Uses_All_Permutations_Of_Callsign_When_Searching_For_Routes()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            AddBlankDatabaseFlights(1);
            _DatabaseFlights[0].Callsign = "A";
            _DatabaseFlights[0].Aircraft.OperatorFlagCode = "COD";
            _RouteCallsigns.Add("A", new List<string>() { "R1", "R2", "R3" });

            var airport1 = new Airport() { IcaoCode = "ICAO1", Name = "A1", Country = "UK", };
            var route1 = new Route() { From = airport1 };

            _StandingDataManager.Setup(m => m.FindRoute("R1")).Returns((Route)null);
            _StandingDataManager.Setup(m => m.FindRoute("R2")).Returns(route1);
            _StandingDataManager.Setup(m => m.FindRoute("R3")).Returns((Route)null);

            dynamic json = SendJsonRequest(ReportJsonType(ReportJsonClass.Flight), _ReportRowsAddress.Address);

            _CallsignParser.Verify(r => r.GetAllRouteCallsigns("A", "COD"), Times.Once());
            _StandingDataManager.Verify(r => r.FindRoute("R1"), Times.Once());
            _StandingDataManager.Verify(r => r.FindRoute("R2"), Times.Once());
            _StandingDataManager.Verify(r => r.FindRoute("R3"), Times.Never());
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Passes_Null_To_CallsignParser_When_Aircraft_Missing()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            AddBlankDatabaseFlights(1);
            // This should never happen...
            _DatabaseFlights[0].Aircraft = null;
            _DatabaseFlights[0].Callsign = "A";

            dynamic json = SendJsonRequest(ReportJsonType(ReportJsonClass.Flight), _ReportRowsAddress.Address);

            _CallsignParser.Verify(r => r.GetAllRouteCallsigns("A", null), Times.Once());
        }

        [TestMethod]
        public void WebSite_ReportRows_DateReport_Passes_Null_To_CallsignParser_When_Aircraft_OperatorCode_Is_Null()
        {
            _ReportRowsAddress.Report = "date";
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            AddBlankDatabaseFlights(1);
            _DatabaseFlights[0].Callsign = "A";
            _DatabaseFlights[0].Aircraft.OperatorFlagCode = null;

            dynamic json = SendJsonRequest(ReportJsonType(ReportJsonClass.Flight), _ReportRowsAddress.Address);

            _CallsignParser.Verify(r => r.GetAllRouteCallsigns("A", null), Times.Once());
        }
        #endregion

        #region ICAO report
        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Generates_Correct_JSON_When_No_Rows_Match()
        {
            Do_ReportRows_Report_Generates_Correct_JSON_When_No_Rows_Match("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Adds_Correct_Cache_Control_Header()
        {
            Do_ReportRows_Report_Adds_Correct_Cache_Control_Header("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Only_Returns_Json_If_Reports_Are_Permitted()
        {
            Do_ReportRows_Report_Only_Returns_Json_If_Reports_Are_Permitted("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Searches_For_Single_Aircraft()
        {
            _ReportRowsAddress.Report = "icao";
            _ReportRowsAddress.Icao24 = new StringFilter("ABC", FilterCondition.Equals, false);

            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetAircraftByCode("ABC"), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetAircraftByCode(It.IsAny<string>()), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetAircraftByRegistration(It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Passes_Single_Aircraft_To_Get_Count_Of_Rows()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Passes_Single_Aircraft_To_Get_Count_Of_Rows(SingleAircraftReport.Icao);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Deals_With_Missing_Aircraft_Criteria_Correctly()
        {
            _ReportRowsAddress.Report = "icao";

            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetAircraftByCode(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(0, json.CountRows);
            Assert.AreEqual(0, json.Flights.Count);
            Assert.AreEqual(0, json.Airports.Count);
            Assert.AreEqual(0, json.Routes.Count);
            Assert.IsTrue(json.Aircraft.IsUnknown);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Copes_When_Aircraft_Cannot_Be_Found()
        {
            _ReportRowsAddress.Report = "icao";
            _ReportRowsAddress.Icao24 = new StringFilter("ABC", FilterCondition.Equals, false);

            _BaseStationDatabase.Setup(db => db.GetAircraftByCode("ABC")).Returns((BaseStationAircraft)null);

            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetCountOfFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>()), Times.Never());
            _BaseStationDatabase.Verify(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>()), Times.Never());

            Assert.AreEqual(0, json.CountRows);
            Assert.AreEqual(0, json.Flights.Count);
            Assert.AreEqual(0, json.Airports.Count);
            Assert.AreEqual(0, json.Routes.Count);
            Assert.IsTrue(json.Aircraft.IsUnknown);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Returns_Count_Of_Rows_Matching_Criteria()
        {
            Do_WebSite_ReportRows_Report_Returns_Count_Of_Rows_Matching_Criteria("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Returns_Details_Of_Exceptions_Raised_During_Report_Generation()
        {
            Do_WebSite_ReportRows_Report_Returns_Details_Of_Exceptions_Raised_During_Report_Generation("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Logs_Exceptions_Raised_During_Report_Generation()
        {
            Do_WebSite_ReportRows_Report_Logs_Exceptions_Raised_During_Report_Generation("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Returns_Processing_Time()
        {
            Do_WebSite_ReportRows_Report_Returns_Processing_Time("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Returns_Images_Available_Flags()
        {
            Do_WebSite_ReportRows_Report_Returns_Images_Available_Flags("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Passes_Same_Criteria_To_CountRows_And_FetchRows()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Passes_Same_Criteria_To_CountRows_And_FetchRows(SingleAircraftReport.Icao);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Passes_Range_And_Sort_Criteria_To_FetchRows()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Passes_Range_And_Sort_Criteria_To_FetchRows(SingleAircraftReport.Icao);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Informs_Caller_Of_Primary_Sort_Column_Used()
        {
            Do_WebSite_ReportRows_Report_Informs_Caller_Of_Primary_Sort_Column_Used("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ReportFlightJson$")]
        public void WebSite_ReportRows_IcaoReport_Transcribes_Flights_From_Database_To_Json()
        {
            Do_WebSite_ReportRows_Report_Transcribes_Flights_From_Database_To_Json("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Sets_Flight_Row_Numbers_Correctly()
        {
            Do_WebSite_ReportRows_Report_Sets_Flight_Row_Numbers_Correctly("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ReportAircraftJson$")]
        public void WebSite_ReportRows_IcaoReport_Returns_Aircraft_From_FetchRows()
        {
            Do_WebSite_ReportRows_Report_Returns_Aircraft_From_FetchRows("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Fills_Aircraft_HasPicture_Correctly()
        {
            _ReportRowsAddress.Report = "icao";
            _ReportRowsAddress.Icao24 = new StringFilter("not null", FilterCondition.Equals, false);

            _BaseStationDatabase.Setup(db => db.GetAircraftByCode(It.IsAny<string>())).Returns(_DatabaseAircraft);
            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "A")).Returns((PictureDetail)null);
            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "B")).Returns(new PictureDetail() { FileName = "B picture filename" });

            _DatabaseAircraft.Registration = "A";
            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsFalse(json.Aircraft.HasPicture);

            _DatabaseAircraft.Registration = "B";
            json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsTrue(json.Aircraft.HasPicture);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Ignores_Aircraft_Pictures_For_Internet_Clients_If_Configuration_Specifies()
        {
            Do_WebSite_ReportRows_Report_Ignores_Aircraft_Pictures_For_Internet_Clients_If_Configuration_Specifies("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Adjusts_HasPicture_To_Suit_Configuration_Changes()
        {
            Do_WebSite_ReportRows_Report_Adjusts_HasPicture_To_Suit_Configuration_Changes("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Looks_Up_ISO8643_Data_For_Aircraft()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Looks_Up_ISO8643_Data_For_Aircraft(SingleAircraftReport.Icao);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Looks_Up_ICAO_Code_Block_For_Aircraft()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Looks_Up_ICAO_Code_Block_For_Aircraft(SingleAircraftReport.Icao);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Uses_ModeSCountry_From_Database()
        {
            Do_WebSite_ReportRows_Report_Uses_ModeSCountry_From_Database("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Fills_Routes_And_Aircraft_Tables()
        {
            Do_WebSite_ReportRows_Report_Fills_Routes_And_Aircraft_Tables("icao", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_IcaoReport_Writes_Correct_Route_Index_When_There_Is_No_Route()
        {
            Do_WebSite_ReportRows_Report_Writes_Correct_Route_Index_When_There_Is_No_Route("icao", ReportJsonClass.Aircraft);
        }
        #endregion

        #region Registration report
        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Generates_Correct_JSON_When_No_Rows_Match()
        {
            Do_ReportRows_Report_Generates_Correct_JSON_When_No_Rows_Match("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Adds_Correct_Cache_Control_Header()
        {
            Do_ReportRows_Report_Adds_Correct_Cache_Control_Header("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Only_Returns_Json_If_Reports_Are_Permitted()
        {
            Do_ReportRows_Report_Only_Returns_Json_If_Reports_Are_Permitted("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Searches_For_Single_Aircraft()
        {
            _ReportRowsAddress.Report = "reg";
            _ReportRowsAddress.Registration = new StringFilter("ABC", FilterCondition.Equals, false);

            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetAircraftByCode(It.IsAny<string>()), Times.Never());
            _BaseStationDatabase.Verify(db => db.GetAircraftByRegistration("ABC"), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetAircraftByRegistration(It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Passes_Single_Aircraft_To_Get_Count_Of_Rows()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Passes_Single_Aircraft_To_Get_Count_Of_Rows(SingleAircraftReport.Registration);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Deals_With_Missing_Aircraft_Criteria_Correctly()
        {
            _ReportRowsAddress.Report = "reg";

            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetAircraftByRegistration(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(0, json.CountRows);
            Assert.AreEqual(0, json.Flights.Count);
            Assert.AreEqual(0, json.Airports.Count);
            Assert.AreEqual(0, json.Routes.Count);
            Assert.IsTrue(json.Aircraft.IsUnknown);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Copes_When_Aircraft_Cannot_Be_Found()
        {
            _ReportRowsAddress.Report = "reg";
            _ReportRowsAddress.Registration = new StringFilter("ABC", FilterCondition.Equals, false);

            _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration("ABC")).Returns((BaseStationAircraft)null);

            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetCountOfFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>()), Times.Never());
            _BaseStationDatabase.Verify(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>()), Times.Never());

            Assert.AreEqual(0, json.CountRows);
            Assert.AreEqual(0, json.Flights.Count);
            Assert.AreEqual(0, json.Airports.Count);
            Assert.AreEqual(0, json.Routes.Count);
            Assert.IsTrue(json.Aircraft.IsUnknown);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Returns_Count_Of_Rows_Matching_Criteria()
        {
            Do_WebSite_ReportRows_Report_Returns_Count_Of_Rows_Matching_Criteria("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Returns_Details_Of_Exceptions_Raised_During_Report_Generation()
        {
            Do_WebSite_ReportRows_Report_Returns_Details_Of_Exceptions_Raised_During_Report_Generation("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Logs_Exceptions_Raised_During_Report_Generation()
        {
            Do_WebSite_ReportRows_Report_Logs_Exceptions_Raised_During_Report_Generation("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Returns_Processing_Time()
        {
            Do_WebSite_ReportRows_Report_Returns_Processing_Time("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Returns_Images_Available_Flags()
        {
            Do_WebSite_ReportRows_Report_Returns_Images_Available_Flags("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Passes_Same_Criteria_To_CountRows_And_FetchRows()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Passes_Same_Criteria_To_CountRows_And_FetchRows(SingleAircraftReport.Registration);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Passes_Range_And_Sort_Criteria_To_FetchRows()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Passes_Range_And_Sort_Criteria_To_FetchRows(SingleAircraftReport.Registration);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Informs_Caller_Of_Primary_Sort_Column_Used()
        {
            Do_WebSite_ReportRows_Report_Informs_Caller_Of_Primary_Sort_Column_Used("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ReportFlightJson$")]
        public void WebSite_ReportRows_RegistrationReport_Transcribes_Flights_From_Database_To_Json()
        {
            Do_WebSite_ReportRows_Report_Transcribes_Flights_From_Database_To_Json("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Sets_Flight_Row_Numbers_Correctly()
        {
            Do_WebSite_ReportRows_Report_Sets_Flight_Row_Numbers_Correctly("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        [DataSource("Data Source='WebSiteTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ReportAircraftJson$")]
        public void WebSite_ReportRows_RegistrationReport_Returns_Aircraft_From_FetchRows()
        {
            Do_WebSite_ReportRows_Report_Returns_Aircraft_From_FetchRows("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Fills_Aircraft_HasPicture_Correctly()
        {
            _ReportRowsAddress.Report = "reg";

            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "A")).Returns((PictureDetail)null);
            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "B")).Returns(new PictureDetail() { FileName = "B picture filename" });

            ConfigureDatabaseForSingleAircraftReport(SingleAircraftReport.Registration, "A");
            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsFalse(json.Aircraft.HasPicture);

            ConfigureDatabaseForSingleAircraftReport(SingleAircraftReport.Registration, "B");
            json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsTrue(json.Aircraft.HasPicture);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Ignores_Aircraft_Pictures_For_Internet_Clients_If_Configuration_Specifies()
        {
            Do_WebSite_ReportRows_Report_Ignores_Aircraft_Pictures_For_Internet_Clients_If_Configuration_Specifies("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Adjusts_HasPicture_To_Suit_Configuration_Changes()
        {
            Do_WebSite_ReportRows_Report_Adjusts_HasPicture_To_Suit_Configuration_Changes("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Looks_Up_ISO8643_Data_For_Aircraft()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Looks_Up_ISO8643_Data_For_Aircraft(SingleAircraftReport.Registration);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Looks_Up_ICAO_Code_Block_For_Aircraft()
        {
            Do_WebSite_ReportRows_SingleAircraftReport_Looks_Up_ICAO_Code_Block_For_Aircraft(SingleAircraftReport.Registration);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Uses_ModeSCountry_From_Database()
        {
            Do_WebSite_ReportRows_Report_Uses_ModeSCountry_From_Database("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Fills_Routes_And_Aircraft_Tables()
        {
            Do_WebSite_ReportRows_Report_Fills_Routes_And_Aircraft_Tables("reg", ReportJsonClass.Aircraft);
        }

        [TestMethod]
        public void WebSite_ReportRows_RegistrationReport_Writes_Correct_Route_Index_When_There_Is_No_Route()
        {
            Do_WebSite_ReportRows_Report_Writes_Correct_Route_Index_When_There_Is_No_Route("reg", ReportJsonClass.Aircraft);
        }
        #endregion

        #region Tests common to all report types
        private void Do_ReportRows_Report_Generates_Correct_JSON_When_No_Rows_Match(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            Assert.AreEqual(0, json.Airports.Count);
            Assert.AreEqual(0, json.CountRows);
            Assert.AreEqual(null, json.ErrorText);
            Assert.AreEqual(0, json.Flights.Count);
            Assert.AreEqual("", json.GroupBy);
            Assert.AreEqual(0.ToString("0.000"), json.ProcessingTime);
            Assert.AreEqual(0, json.Routes.Count);

            switch(reportClass) {
                case ReportJsonClass.Aircraft:  Assert.AreEqual(true, json.Aircraft.IsUnknown); break;
                case ReportJsonClass.Flight:    Assert.AreEqual(0, json.Aircraft.Count); break;
                default:                        throw new NotImplementedException();
            }
        }

        private void Do_ReportRows_Report_Adds_Correct_Cache_Control_Header(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;
            SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            _Response.Verify(r => r.AddHeader("Cache-Control", "max-age=0, no-cache, no-store, must-revalidate"), Times.Once());
        }

        private void Do_ReportRows_Report_Only_Returns_Json_If_Reports_Are_Permitted(string report, ReportJsonClass reportClass)
        {
            _Configuration.InternetClientSettings.CanRunReports = false;
            _ReportRowsAddress.Report = report;
            var jsonType = ReportJsonType(reportClass);

            Assert.IsNull(SendJsonRequest(jsonType, _ReportRowsAddress.Address, true));

            _Configuration.InternetClientSettings.CanRunReports = true;
            _ConfigurationStorage.Raise(e => e.ConfigurationChanged += null, EventArgs.Empty);
            Assert.IsNotNull(SendJsonRequest(jsonType, _ReportRowsAddress.Address, true));

            _Configuration.InternetClientSettings.CanRunReports = false;
            _ConfigurationStorage.Raise(e => e.ConfigurationChanged += null, EventArgs.Empty);
            Assert.IsNotNull(SendJsonRequest(jsonType, _ReportRowsAddress.Address, false));

            _Configuration.InternetClientSettings.CanRunReports = true;
            _ConfigurationStorage.Raise(e => e.ConfigurationChanged += null, EventArgs.Empty);
            Assert.IsNotNull(SendJsonRequest(jsonType, _ReportRowsAddress.Address, false));
        }

        private void Do_WebSite_ReportRows_Report_Returns_Count_Of_Rows_Matching_Criteria(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;

            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _BaseStationDatabase.Setup(db => db.GetCountOfFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>())).Returns(12);
                    _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);
                    break;
                case ReportJsonClass.Flight:
                    _BaseStationDatabase.Setup(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>())).Returns(12);
                    break;
                default:
                    throw new NotImplementedException();
            }

            _BaseStationDatabase.Setup(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>())).Returns(12);

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            Assert.AreEqual(12, json.CountRows);
        }

        private void Do_WebSite_ReportRows_Report_Returns_Details_Of_Exceptions_Raised_During_Report_Generation(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;

            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _BaseStationDatabase.Setup(db => db.GetCountOfFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>())).Callback(() => { throw new InvalidOperationException("Text of message"); } );
                    _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);
                    break;
                case ReportJsonClass.Flight:
                    _BaseStationDatabase.Setup(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>())).Callback(() => { throw new InvalidOperationException("Text of message"); } );
                    break;
                default:
                    throw new NotImplementedException();
            }

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            Assert.IsNotNull(json.ErrorText);
            Assert.IsTrue(json.ErrorText.Contains("Text of message"));
        }

        private void Do_WebSite_ReportRows_Report_Logs_Exceptions_Raised_During_Report_Generation(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;

            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _BaseStationDatabase.Setup(db => db.GetCountOfFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>())).Callback(() => { throw new InvalidOperationException("Message goes here"); } );
                    _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);
                    break;
                case ReportJsonClass.Flight:
                    _BaseStationDatabase.Setup(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>())).Callback(() => { throw new InvalidOperationException("Message goes here"); } );
                    break;
                default:
                    throw new NotImplementedException();
            }

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            _Log.Verify(p => p.WriteLine(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        private void Do_WebSite_ReportRows_Report_Returns_Processing_Time(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;

            DateTime startTime = new DateTime(2001, 2, 3, 4, 5, 6);
            DateTime endTime = startTime.AddMilliseconds(71234);

            int callCount = 0;
            _Provider.Setup(p => p.UtcNow).Returns(() => { return callCount++ < 2 ? startTime : endTime; });

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);
            Assert.AreEqual((71.234M).ToString(), json.ProcessingTime);
        }

        private void Do_WebSite_ReportRows_Report_Returns_Images_Available_Flags(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;

            _Configuration.BaseStationSettings.SilhouettesFolder = "s";
            _Configuration.BaseStationSettings.OperatorFlagsFolder = "o";

            foreach(var silhouettesAvailable in new bool[] { false, true }) {
                foreach(var operatorFlagsAvailable in new bool[] { false, true }) {
                    _Provider.Setup(p => p.DirectoryExists("s")).Returns(silhouettesAvailable);
                    _Provider.Setup(p => p.DirectoryExists("o")).Returns(operatorFlagsAvailable);
                    _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);

                    dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

                    Assert.AreEqual(silhouettesAvailable, json.SilhouettesAvailable);
                    Assert.AreEqual(operatorFlagsAvailable, json.OperatorFlagsAvailable);
                }
            }
        }

        private void Do_WebSite_ReportRows_Report_Informs_Caller_Of_Primary_Sort_Column_Used(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);

            var jsonType = ReportJsonType(reportClass);

            dynamic json = SendJsonRequest(jsonType, _ReportRowsAddress.Address);
            Assert.AreEqual("", json.GroupBy);

            _ReportRowsAddress.SortField1 = "ABC";
            json = SendJsonRequest(jsonType, _ReportRowsAddress.Address);
            Assert.AreEqual("ABC", json.GroupBy);

            _ReportRowsAddress.SortField2 = "XYZ";
            json = SendJsonRequest(jsonType, _ReportRowsAddress.Address);
            Assert.AreEqual("ABC", json.GroupBy);

            _ReportRowsAddress.SortField1 = null;
            json = SendJsonRequest(jsonType, _ReportRowsAddress.Address);
            Assert.AreEqual("XYZ", json.GroupBy);
        }

        private void Do_WebSite_ReportRows_Report_Returns_Rows_From_FetchRows(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);

            switch(reportClass) {
                case ReportJsonClass.Aircraft:  AddBlankDatabaseFlightsForAircraft(10); break;
                case ReportJsonClass.Flight:    AddBlankDatabaseFlights(10); break;
                default:                        throw new NotImplementedException();
            }

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            Assert.AreEqual(10, json.Flights.Count);
        }

        private void Do_WebSite_ReportRows_Report_Transcribes_Flights_From_Database_To_Json(string report, ReportJsonClass reportClass)
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);

            BaseStationFlight flight;
            switch(reportClass) {
                case ReportJsonClass.Aircraft:  AddBlankDatabaseFlightsForAircraft(1); flight = _DatabaseFlightsForAircraft[0]; break;
                case ReportJsonClass.Flight:    AddBlankDatabaseFlights(1); flight = _DatabaseFlights[0]; break;
                default:                        throw new NotImplementedException();
            }

            var databaseProperty = flight.GetType().GetProperty(worksheet.String("DatabaseProperty"));
            var databaseValue = worksheet.EString("DatabaseValue");
            if(databaseValue != null) databaseProperty.SetValue(flight, TestUtilities.ChangeType(databaseValue, databaseProperty.PropertyType, CultureInfo.InvariantCulture), null);

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            Assert.AreEqual(1, json.Flights.Count);
            var jsonFlight = json.Flights[0];

            var jsonProperty = jsonFlight.GetType().GetProperty(worksheet.String("JsonProperty"));
            var expectedValue = TestUtilities.ChangeType(worksheet.EString("JsonValue"), jsonProperty.PropertyType, CultureInfo.InvariantCulture);
            var actualValue = jsonProperty.GetValue(jsonFlight, null);

            Assert.AreEqual(expectedValue, actualValue);
        }

        private void Do_WebSite_ReportRows_Report_Sets_Flight_Row_Numbers_Correctly(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);

            // Row numbers sent in to the report are 0-based, not 1-based.

            switch(reportClass) {
                case ReportJsonClass.Aircraft:  AddBlankDatabaseFlightsForAircraft(2); break;
                case ReportJsonClass.Flight:    AddBlankDatabaseFlights(2); break;
                default:                        throw new NotImplementedException();
            }

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);
            Assert.AreEqual(2, json.Flights.Count);
            Assert.AreEqual(1, json.Flights[0].RowNumber);
            Assert.AreEqual(2, json.Flights[1].RowNumber);

            _ReportRowsAddress.FromRow = 1;
            json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);
            Assert.AreEqual(2, json.Flights.Count);
            Assert.AreEqual(2, json.Flights[0].RowNumber);
            Assert.AreEqual(3, json.Flights[1].RowNumber);

            _ReportRowsAddress.FromRow = 2;
            json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);
            Assert.AreEqual(2, json.Flights.Count);
            Assert.AreEqual(3, json.Flights[0].RowNumber);
            Assert.AreEqual(4, json.Flights[1].RowNumber);

            _ReportRowsAddress.FromRow = 3;
            json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);
            Assert.AreEqual(2, json.Flights.Count);
            Assert.AreEqual(4, json.Flights[0].RowNumber);
            Assert.AreEqual(5, json.Flights[1].RowNumber);
        }

        private void Do_WebSite_ReportRows_Report_Returns_Aircraft_From_FetchRows(string report, ReportJsonClass reportClass)
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("A", FilterCondition.Equals, false);

            BaseStationAircraft aircraft = null;
            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    aircraft = _DatabaseAircraft;
                    _BaseStationDatabase.Setup(db => db.GetAircraftByCode(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    break;
                case ReportJsonClass.Flight:
                    AddBlankDatabaseFlights(1);
                    aircraft = _DatabaseFlights[0].Aircraft;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var databaseProperty = aircraft.GetType().GetProperty(worksheet.String("DatabaseProperty"));
            var databaseValue = worksheet.EString("DatabaseValue");
            if(databaseValue != null) databaseProperty.SetValue(aircraft, TestUtilities.ChangeType(databaseValue, databaseProperty.PropertyType, CultureInfo.InvariantCulture), null);

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);

            ReportAircraftJson jsonAircraft = null;
            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    jsonAircraft = json.Aircraft;
                    break;
                case ReportJsonClass.Flight:
                    Assert.AreEqual(1, json.Aircraft.Count);
                    jsonAircraft = json.Aircraft[0];
                    break;
                default:
                    throw new NotImplementedException();
            }

            var jsonProperty = jsonAircraft.GetType().GetProperty(worksheet.String("JsonProperty"));
            var expectedValue = TestUtilities.ChangeType(worksheet.EString("JsonValue"), jsonProperty.PropertyType, CultureInfo.InvariantCulture);
            var actualValue = jsonProperty.GetValue(jsonAircraft, null);

            Assert.AreEqual(expectedValue, actualValue);
        }

        private void Do_WebSite_ReportRows_Report_Ignores_Aircraft_Pictures_For_Internet_Clients_If_Configuration_Specifies(string report, ReportJsonClass reportClass)
        {
            _Configuration.InternetClientSettings.CanRunReports = true;
            _Configuration.InternetClientSettings.CanShowPictures = false;
            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _DatabaseAircraft.Registration = "A";
                    _BaseStationDatabase.Setup(db => db.GetAircraftByCode(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    break;
                case ReportJsonClass.Flight:
                    AddBlankDatabaseFlights(1);
                    _DatabaseFlights[0].Aircraft.Registration = "A";
                    break;
                default:
                    throw new NotImplementedException();
            }

            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "A")).Returns(new PictureDetail() { FileName = "PIC" });

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address, true);
            switch(reportClass) {
                case ReportJsonClass.Aircraft:  Assert.IsFalse(json.Aircraft.HasPicture); break;
                case ReportJsonClass.Flight:    Assert.IsFalse(json.Aircraft[0].HasPicture); break;
                default:                        throw new NotImplementedException();
            }
        }

        private void Do_WebSite_ReportRows_Report_Adjusts_HasPicture_To_Suit_Configuration_Changes(string report, ReportJsonClass reportClass)
        {
            _Configuration.InternetClientSettings.CanRunReports = true;
            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _DatabaseAircraft.Registration = "A";
                    _BaseStationDatabase.Setup(db => db.GetAircraftByCode(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    break;
                case ReportJsonClass.Flight:
                    AddBlankDatabaseFlights(1);
                    _DatabaseFlights[0].Aircraft.Registration = "A";
                    break;
                default:
                    throw new NotImplementedException();
            }

            _AircraftPictureManager.Setup(m => m.FindPicture(_DirectoryCache.Object, null, "A")).Returns(new PictureDetail() { FileName = "PIC" });

            _Configuration.InternetClientSettings.CanShowPictures = true;
            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address, true);
            switch(reportClass) {
                case ReportJsonClass.Aircraft:  Assert.IsTrue(json.Aircraft.HasPicture); break;
                case ReportJsonClass.Flight:    Assert.IsTrue(json.Aircraft[0].HasPicture); break;
                default:                        throw new NotImplementedException();
            }

            _Configuration.InternetClientSettings.CanShowPictures = false;
            _ConfigurationStorage.Raise(e => e.ConfigurationChanged += null, EventArgs.Empty);
            json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address, true);
            switch(reportClass) {
                case ReportJsonClass.Aircraft:  Assert.IsFalse(json.Aircraft.HasPicture); break;
                case ReportJsonClass.Flight:    Assert.IsFalse(json.Aircraft[0].HasPicture); break;
                default:                        throw new NotImplementedException();
            }

            _Configuration.InternetClientSettings.CanShowPictures = true;
            json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address, false);
            switch(reportClass) {
                case ReportJsonClass.Aircraft:  Assert.IsTrue(json.Aircraft.HasPicture); break;
                case ReportJsonClass.Flight:    Assert.IsTrue(json.Aircraft[0].HasPicture); break;
                default:                        throw new NotImplementedException();
            }

            _Configuration.InternetClientSettings.CanShowPictures = false;
            _ConfigurationStorage.Raise(e => e.ConfigurationChanged += null, EventArgs.Empty);
            json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address, false);
            switch(reportClass) {
                case ReportJsonClass.Aircraft:  Assert.IsTrue(json.Aircraft.HasPicture); break;
                case ReportJsonClass.Flight:    Assert.IsTrue(json.Aircraft[0].HasPicture); break;
                default:                        throw new NotImplementedException();
            }
        }

        private void Do_WebSite_ReportRows_Report_Uses_ModeSCountry_From_Database(string report, ReportJsonClass reportClass)
        {
            // The code block is likely to be more accurate but the database version will be quicker to use as criteria in a report

            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            BaseStationAircraft aircraft = null;
            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _BaseStationDatabase.Setup(db => db.GetAircraftByCode(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    aircraft = _DatabaseAircraft;
                    break;
                case ReportJsonClass.Flight:
                    AddBlankDatabaseFlights(1);
                    aircraft = _DatabaseFlights[0].Aircraft;
                    break;
                default:
                    throw new NotImplementedException();
            }

            aircraft.ModeSCountry = "FromDB";
            aircraft.ModeS = "A";

            var codeBlock = new CodeBlock() { Country = "FromCodeBlock" };
            _StandingDataManager.Setup(p => p.FindCodeBlock(It.IsAny<string>())).Returns(codeBlock);

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address, false);
            switch(reportClass) {
                case ReportJsonClass.Aircraft:  Assert.AreEqual("FromDB", json.Aircraft.ModeSCountry); break;
                case ReportJsonClass.Flight:    Assert.AreEqual("FromDB", json.Aircraft[0].ModeSCountry); break;
                default:                        throw new NotImplementedException();
            }
        }

        private void Do_WebSite_ReportRows_Report_Fills_Routes_And_Aircraft_Tables(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            List<BaseStationFlight> databaseFlights = null;
            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _BaseStationDatabase.Setup(db => db.GetAircraftByCode(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    AddBlankDatabaseFlightsForAircraft(4);
                    databaseFlights = _DatabaseFlightsForAircraft;
                    break;
                case ReportJsonClass.Flight:
                    AddBlankDatabaseFlights(4);
                    databaseFlights = _DatabaseFlights;
                    break;
                default:
                    throw new NotImplementedException();
            }

            databaseFlights[0].Callsign = null; databaseFlights[0].FirstAltitude = 1;
            databaseFlights[1].Callsign = "A";  databaseFlights[1].FirstAltitude = 2;
            databaseFlights[2].Callsign = "B";  databaseFlights[2].FirstAltitude = 3;
            databaseFlights[3].Callsign = "A";  databaseFlights[3].FirstAltitude = 4;

            var airport1 = new Airport() { IcaoCode = "ICAO1", Name = "A1", Country = "UK", };
            var airport2 = new Airport() { IataCode = "IATA2", Name = "A2", };
            var airport3 = new Airport() { IcaoCode = "ICAO3", IataCode = "IATA3", Name = "A3", };
            var airport4 = new Airport() { IcaoCode = "ICAO4", Name = "A4", };

            var route1 = new Route() { From = airport1 };
            var route2 = new Route() { From = airport2, To = airport1, Stopovers = { airport3, airport4 }, };

            _StandingDataManager.Setup(m => m.FindRoute("A")).Returns(route1);
            _StandingDataManager.Setup(m => m.FindRoute("B")).Returns(route2);

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);
            List<ReportFlightJson> jsonFlights = json.Flights;
            var flight1 = jsonFlights.Where(f => f.FirstAltitude == 1).Single();
            var flight2 = jsonFlights.Where(f => f.FirstAltitude == 2).Single();
            var flight3 = jsonFlights.Where(f => f.FirstAltitude == 3).Single();
            var flight4 = jsonFlights.Where(f => f.FirstAltitude == 4).Single();

            Assert.AreEqual(-1, flight1.RouteIndex);
            Assert.AreEqual(flight2.RouteIndex, flight4.RouteIndex);
            Assert.AreNotEqual(flight2.RouteIndex, flight3.RouteIndex);

            _StandingDataManager.Verify(m => m.FindRoute(It.IsAny<string>()), Times.Exactly(2));

            Assert.AreEqual(2, json.Routes.Count);
            Assert.AreEqual(4, json.Airports.Count);

            Assert.AreEqual("A1, UK", json.Airports[json.Routes[flight2.RouteIndex].FromIndex].Name);
            Assert.AreEqual(-1, json.Routes[flight2.RouteIndex].ToIndex);
            Assert.AreEqual(0, json.Routes[flight2.RouteIndex].StopoversIndex.Count);

            Assert.AreEqual("A2", json.Airports[json.Routes[flight3.RouteIndex].FromIndex].Name);
            Assert.AreEqual("A1, UK", json.Airports[json.Routes[flight3.RouteIndex].ToIndex].Name);
            Assert.AreEqual(2, json.Routes[flight3.RouteIndex].StopoversIndex.Count);
            Assert.AreEqual("A3", json.Airports[json.Routes[flight3.RouteIndex].StopoversIndex[0]].Name);
            Assert.AreEqual("A4", json.Airports[json.Routes[flight3.RouteIndex].StopoversIndex[1]].Name);

            List<ReportAirportJson> jsonAirports = json.Airports;
            Assert.AreEqual("ICAO1", jsonAirports.Where(a => a.Name == "A1, UK").Single().Code);
            Assert.AreEqual("IATA2", jsonAirports.Where(a => a.Name == "A2").Single().Code);
            Assert.AreEqual("ICAO3", jsonAirports.Where(a => a.Name == "A3").Single().Code);
        }

        private void Do_WebSite_ReportRows_Report_Writes_Correct_Route_Index_When_There_Is_No_Route(string report, ReportJsonClass reportClass)
        {
            _ReportRowsAddress.Report = report;
            _ReportRowsAddress.Icao24 = _ReportRowsAddress.Registration = new StringFilter("not null", FilterCondition.Equals, false);

            List<BaseStationFlight> databaseFlights;
            switch(reportClass) {
                case ReportJsonClass.Aircraft:
                    _BaseStationDatabase.Setup(db => db.GetAircraftByCode(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    _BaseStationDatabase.Setup(db => db.GetAircraftByRegistration(It.IsAny<string>())).Returns(_DatabaseAircraft);
                    AddBlankDatabaseFlightsForAircraft(2);
                    databaseFlights = _DatabaseFlightsForAircraft;
                    break;
                case ReportJsonClass.Flight:
                    AddBlankDatabaseFlights(2);
                    databaseFlights = _DatabaseFlights;
                    break;
                default:
                    throw new NotImplementedException();
            }
            databaseFlights[0].Callsign = null;
            databaseFlights[1].Callsign = "A";

            _StandingDataManager.Setup(m => m.FindRoute(It.IsAny<string>())).Returns((Route)null);

            dynamic json = SendJsonRequest(ReportJsonType(reportClass), _ReportRowsAddress.Address);
            Assert.AreEqual(-1, json.Flights[0].RouteIndex);
            Assert.AreEqual(-1, json.Flights[1].RouteIndex);
        }
        #endregion

        #region Tests common to all single-aircraft reports
        private void Do_WebSite_ReportRows_SingleAircraftReport_Passes_Single_Aircraft_To_Get_Count_Of_Rows(SingleAircraftReport reportType)
        {
            _ReportRowsAddress.Report = SingleAircraftReportType(reportType);
            ConfigureDatabaseForSingleAircraftReport(reportType);

            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetCountOfFlightsForAircraft(_DatabaseAircraft, It.IsAny<SearchBaseStationCriteria>()), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetCountOfFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>()), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetCountOfFlights(It.IsAny<SearchBaseStationCriteria>()), Times.Never());
        }

        private void Do_WebSite_ReportRows_SingleAircraftReport_Passes_Same_Criteria_To_CountRows_And_FetchRows(SingleAircraftReport reportType)
        {
            _ReportRowsAddress.Report = SingleAircraftReportType(reportType);
            ConfigureDatabaseForSingleAircraftReport(reportType);

            BaseStationAircraft aircraft = null;
            SearchBaseStationCriteria searchCriteria = null;
            _BaseStationDatabase.Setup(db => db.GetCountOfFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>())).Returns((BaseStationAircraft ac, SearchBaseStationCriteria c) => { aircraft = ac; searchCriteria = c; return 1; });

            SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetFlightsForAircraft(aircraft, searchCriteria, -1, -1, null, It.IsAny<bool>(), null, It.IsAny<bool>()), Times.Once());
        }

        private void Do_WebSite_ReportRows_SingleAircraftReport_Passes_Range_And_Sort_Criteria_To_FetchRows(SingleAircraftReport reportType)
        {
            _ReportRowsAddress.Report = SingleAircraftReportType(reportType);
            ConfigureDatabaseForSingleAircraftReport(reportType);

            _ReportRowsAddress.FromRow = 10;
            _ReportRowsAddress.ToRow = 11;
            _ReportRowsAddress.SortField1 = "Ff1";
            _ReportRowsAddress.SortField2 = "Ff2";
            _ReportRowsAddress.SortAscending1 = true;
            _ReportRowsAddress.SortAscending2 = false;
            SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
            _BaseStationDatabase.Verify(db => db.GetFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>(), 10, 11, "Ff1", true, "Ff2", false), Times.Once());

            _ReportRowsAddress.SortAscending1 = false;
            _ReportRowsAddress.SortAscending2 = true;
            SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);

            _BaseStationDatabase.Verify(db => db.GetFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));
            _BaseStationDatabase.Verify(db => db.GetFlightsForAircraft(It.IsAny<BaseStationAircraft>(), It.IsAny<SearchBaseStationCriteria>(), 10, 11, "Ff1", false, "Ff2", true), Times.Once());
        }

        private void Do_WebSite_ReportRows_SingleAircraftReport_Looks_Up_ISO8643_Data_For_Aircraft(SingleAircraftReport reportType)
        {
            _ReportRowsAddress.Report = SingleAircraftReportType(reportType);
            ConfigureDatabaseForSingleAircraftReport(reportType);

            var aircraftType = new AircraftType() {
                Engines = "X",
                EngineType = EngineType.Piston,
                Species = Species.Gyrocopter,
                WakeTurbulenceCategory = WakeTurbulenceCategory.Light,
            };
            _StandingDataManager.Setup(m => m.FindAircraftType("A")).Returns(aircraftType);

            _DatabaseAircraft.ICAOTypeCode = null;
            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            _StandingDataManager.Verify(m => m.FindAircraftType(It.IsAny<string>()), Times.Never());
            Assert.AreEqual(null, json.Aircraft.Engines);
            Assert.AreEqual(null, json.Aircraft.EngineType);
            Assert.AreEqual(null, json.Aircraft.Species);
            Assert.AreEqual(null, json.Aircraft.WakeTurbulenceCategory);

            _DatabaseAircraft.ICAOTypeCode = "";
            json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            _StandingDataManager.Verify(m => m.FindAircraftType(It.IsAny<string>()), Times.Never());
            Assert.AreEqual(null, json.Aircraft.Engines);
            Assert.AreEqual(null, json.Aircraft.EngineType);
            Assert.AreEqual(null, json.Aircraft.Species);
            Assert.AreEqual(null, json.Aircraft.WakeTurbulenceCategory);

            _DatabaseAircraft.ICAOTypeCode = "A";
            json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            _StandingDataManager.Verify(m => m.FindAircraftType(It.IsAny<string>()), Times.Once());
            Assert.AreEqual("X", json.Aircraft.Engines);
            Assert.AreEqual((int)EngineType.Piston, json.Aircraft.EngineType);
            Assert.AreEqual((int)Species.Gyrocopter, json.Aircraft.Species);
            Assert.AreEqual((int)WakeTurbulenceCategory.Light, json.Aircraft.WakeTurbulenceCategory);
        }

        private void Do_WebSite_ReportRows_SingleAircraftReport_Looks_Up_ICAO_Code_Block_For_Aircraft(SingleAircraftReport reportType)
        {
            _ReportRowsAddress.Report = SingleAircraftReportType(reportType);
            ConfigureDatabaseForSingleAircraftReport(reportType);

            var isMilitary = new CodeBlock() { IsMilitary = true };
            _StandingDataManager.Setup(p => p.FindCodeBlock("A")).Returns(isMilitary);

            var isNotMilitary = new CodeBlock() { IsMilitary = false };
            _StandingDataManager.Setup(p => p.FindCodeBlock("B")).Returns(isNotMilitary);

            _DatabaseAircraft.ModeS = null; // if this happens it's a DB error, but still, nice to know what would happen :)
            var json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsFalse(json.Aircraft.Military);

            _DatabaseAircraft.ModeS = "A";
            json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsTrue(json.Aircraft.Military);

            _DatabaseAircraft.ModeS = "B";
            json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsFalse(json.Aircraft.Military);

            _DatabaseAircraft.ModeS = "UNKNOWN";
            json = SendJsonRequest<AircraftReportJson>(_ReportRowsAddress.Address);
            Assert.IsFalse(json.Aircraft.Military);
        }
        #endregion
    }
}
