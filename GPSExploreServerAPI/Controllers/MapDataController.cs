﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using DatabaseAccess;
using Google.OpenLocationCode;
using GPSExploreServerAPI.Classes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Caching.Memory;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using SQLitePCL;
using static DatabaseAccess.DbTables;
using GPSExploreServerAPI.Classes;

namespace GPSExploreServerAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MapDataController : ControllerBase
    {
        private static MemoryCache cache;

        public MapDataController()
        {
            if (cache == null)
            {
                var options = new MemoryCacheOptions();
                options.SizeLimit = 1024; //1k entries. that's 2.5 4-digit plus code blocks. If an entry is 300kb on average, this is 300MB of RAM for caching. T3.small has 2GB total, t3.medium has 4GB.
                cache = new MemoryCache(options);
            }
        }

        public static double resolution10 = .000125; //as defined
        //Manual map edits:
        //none
        //TODO:
        //none currently.

        //Cell8Data function removed, significantly out of date.
        //remaking it would mean slightly changes to a copy of Cell6Info

        [HttpGet]
        [Route("/[controller]/cell6Info/{plusCode6}")]
        public string Cell6Info(string plusCode6) //The current primary function used by the app.
        {
            //Send over the plus code to look up.
            PerformanceTracker pt = new PerformanceTracker("Cell6info");
            var codeString6 = plusCode6;
            string cachedResults = "";
            if (cache.TryGetValue(codeString6, out cachedResults))
            {
                pt.Stop(codeString6);
                return cachedResults;
            }
            var box = OpenLocationCode.DecodeValid(codeString6);

            var db = new DatabaseAccess.GpsExploreContext();
            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326); //SRID matches Plus code values.
            var places = MapSupport.GetPlaces(OpenLocationCode.DecodeValid(codeString6));  //All the places in this 6-code
            var spoi = db.SinglePointsOfInterests.Where(sp => sp.PlusCode6 == codeString6).ToList();

            StringBuilder sb = new StringBuilder();
            //pluscode6 //first 6 digits of this pluscode. each line below is the last 4 that have an area type.
            //pluscode4|name|type  //less data transmitted, an extra string concat per entry phone-side.
            sb.AppendLine(codeString6);

            if (places.Count == 0 && spoi.Count == 0)
                return sb.ToString();

            //add spois first, instead of checking 400 times if the list has entries. The app will load the first entry for each 10cell, and fail to insert a duplicate later in the return value.
            foreach (var s in spoi)
                sb.AppendLine(s.PlusCode.Substring(6, 4) + "|" + s.name + "|" + s.NodeType); 

            //optimization. Split the main area into 4 smaller thing when checking in loops. Approximately twice as fast as checking the whole area every time.
            //Notes:
            //StringBuilders isn't thread-safe, so each thread needs its own, and their results combined later.

            //optimization code pass 2. Much smaller, more flexible.
            int splitcount = 2; //creates 4 entries, each 200x200 10cells wide. Should be evenly divisible into 400 or cells will be missed. (Options: 2, TBD)
            List<MapData>[] placeArray;
            GeoArea[] areaArray;
            StringBuilder[] sbArray = new StringBuilder[splitcount * splitcount];
            MapSupport.SplitArea(box, splitcount, places, out placeArray, out areaArray); //~26ms for 2, 260ms for 50 (which is an unreasonable value, since 20 is another pair of pluscode digits)
            int loopSize = 400 / splitcount;
            System.Threading.Tasks.Parallel.For(0,  placeArray.Length, (i) =>
            {
                StringBuilder localSB = new StringBuilder();
                for (double xx = 0; xx < loopSize; xx += 1)
                {
                    for (double yy = 0; yy < loopSize; yy += 1)
                    {
                        //Southwest quadrant, places1
                        double x = areaArray[i].Min.Longitude + (resolution10 * xx);
                        double y = areaArray[i].Min.Latitude + (resolution10 * yy);

                        var placesFound = FindPlacesIn10Cell(x, y, ref placeArray[i]);
                        if (!string.IsNullOrWhiteSpace(placesFound))
                            localSB.AppendLine(placesFound);
                    }
                }
                sbArray[i] = localSB;
            });

            foreach (StringBuilder sbPartial in sbArray)
                sb.Append(sbPartial.ToString());

            string results = sb.ToString();
            var options = new MemoryCacheEntryOptions();
            options.SetSize(1);
            cache.Set(codeString6, results, options);

            pt.Stop(codeString6); //track which cell this was, so I can compare easier in the database.
            return results;
        }

        [HttpGet]
        [Route("/[controller]/cell6Info/{lat}/{lon}")]
        public string Cell6Info(double lat, double lon)
        {
            var codeRequested = new OpenLocationCode(lat, lon);
            var sixCell = codeRequested.CodeDigits.Substring(0, 6);
            return Cell6Info(sixCell);
        }

        public string FindPlacesIn10Cell(double x, double y, ref List<MapData> places)
        {
            //The only remaining optimzation here is to attempt to find a function that's faster than Intersects on linestrings, and if there is one to check when to use which.
            var box = new GeoArea(new GeoPoint(y, x), new GeoPoint(y + resolution10, x + resolution10));
            var entriesHere = MapSupport.GetPlaces(box, places);

            if (entriesHere.Count() == 0)
            {
                return "";
            }
            else
            {
                //Generally, if there's a smaller shape inside a bigger shape, the smaller one should take priority.
                var olc = new OpenLocationCode(y, x).CodeDigits.Substring(6, 4); //This takes lat, long, Coordinate takes X, Y. This line is correct.
                var smallest = entriesHere.Where(e => e.place.Area == entriesHere.Min(e => e.place.Area)).First();
                return olc + "|" + smallest.name + "|" + smallest.type;
            }
        }

        [HttpGet]
        [Route("/[controller]/test")]
        public string TestDummyEndpoint()
        {
            //For debug purposes to confirm the server is running and reachable.
            return "OK";
        }

        [HttpGet]
        [Route("/[controller]/PerfTest")]
        public void PerfTest()
        {
            //testing C# side code here, to make sure i have that sorted out.
            //For debug purposes to confirm the server is running and reachable.
            //i have lat/lon coords, whats there?
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random r = new Random();

            List<long> intersectsPolygonRuntimes = new List<long>(50);
            List<long> containsPointRuntimes = new List<long>(50);
            List<long> AlgorithmRuntimes = new List<long>(50);
            List<long> precachedAlgorithmRuntimes = new List<long>(50);

            //tryint to determine the fastest way to search areas. Pull a 6-cell's worth of data from the DB, then parse it into 10cells.
            //Option 1: make a box, check Intersects.
            //Option 2: make a point, check Contains. (NOTE: a polygon does not Contain() its boundaries, so a point directly on a boundary line will not be identified)
            //Option 3: try NetTopologySuite.Algorithm.Locate.IndexedPointInAreaLocator ?
            //Option 4: consider using Contains against something like NetTopologySuite.Geometries.Prepared.PreparedGeometryFactory().Prepare(geom) instead of just Place? This might be outdated

            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326); //SRID matches Plus code values. //share this here, so i compare the actual algorithms instead of this boilerplate, mandatory entry.
            var db = new GpsExploreContext();

            for (int i = 0; i < 50; i++)
            {
                //randomize lat and long to somewhere in Ohio.
                //42, -80 NE
                //38, -84 SW
                //so 38 + (0-4), -84 = (0-4) coords.
                //double lat = 38 + (r.NextDouble() * 4);
                //double lon = -84 + (r.NextDouble() * 4);
                //Global scale testing.
                double lat = 90 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
                double lon = 180 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
                var olc = OpenLocationCode.Encode(lat, lon);
                var codeString = olc.Substring(0, 6);
                sw.Restart();
                var box = OpenLocationCode.DecodeValid(codeString);
                var cord1 = new Coordinate(box.Min.Longitude, box.Min.Latitude);
                var cord2 = new Coordinate(box.Min.Longitude, box.Max.Latitude);
                var cord3 = new Coordinate(box.Max.Longitude, box.Max.Latitude);
                var cord4 = new Coordinate(box.Max.Longitude, box.Min.Latitude);
                var cordSeq = new Coordinate[5] { cord4, cord3, cord2, cord1, cord4 };
                var location = factory.CreatePolygon(cordSeq); //the 6 cell.

                var places = db.MapData.Where(md => md.place.Intersects(location)).ToList();
                double resolution10 = .000125; //as defined
                for (double x = box.Min.Longitude; x <= box.Max.Longitude; x += resolution10)
                {
                    for (double y = box.Min.Latitude; y <= box.Max.Latitude; y += resolution10)
                    {
                        //also remember these coords start at the lower-left, so i can add the resolution to get the max bounds
                        var olcInner = new OpenLocationCode(y, x); //This takes lat, long, Coordinate takes X, Y. This line is correct.
                        var cordSeq2 = new Coordinate[5] { new Coordinate(x, y), new Coordinate(x + resolution10, y), new Coordinate(x + resolution10, y + resolution10), new Coordinate(x, y + resolution10), new Coordinate(x, y) };
                        var poly2 = factory.CreatePolygon(cordSeq2);
                        var entriesHere = places.Where(md => md.place.Intersects(poly2)).ToList();

                    }
                }
                sw.Stop(); //measuring time it takes to parse a 6-cell down to 10-cells.and wou
                intersectsPolygonRuntimes.Add(sw.ElapsedMilliseconds);
            }

            for (int i = 0; i < 50; i++)
            {
                //randomize lat and long to somewhere in Ohio.
                //42, -80 NE
                //38, -84 SW
                //so 38 + (0-4), -84 = (0-4) coords.
                //double lat = 38 + (r.NextDouble() * 4);
                //double lon = -84 + (r.NextDouble() * 4);
                double lat = 90 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
                double lon = 180 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
                var olc = OpenLocationCode.Encode(lat, lon);
                var codeString = olc.Substring(0, 6);
                sw.Restart();
                var box = OpenLocationCode.DecodeValid(codeString);
                var cord1 = new Coordinate(box.Min.Longitude, box.Min.Latitude);
                var cord2 = new Coordinate(box.Min.Longitude, box.Max.Latitude);
                var cord3 = new Coordinate(box.Max.Longitude, box.Max.Latitude);
                var cord4 = new Coordinate(box.Max.Longitude, box.Min.Latitude);
                var cordSeq = new Coordinate[5] { cord4, cord3, cord2, cord1, cord4 };
                var location = factory.CreatePolygon(cordSeq); //the 6 cell.

                var places = db.MapData.Where(md => md.place.Intersects(location)).ToList();
                double resolution10 = .000125; //as defined
                for (double x = box.Min.Longitude; x <= box.Max.Longitude; x += resolution10)
                {
                    for (double y = box.Min.Latitude; y <= box.Max.Latitude; y += resolution10)
                    {
                        //Option 2, is Contains on a point faster?
                        var location2 = factory.CreatePoint(new Coordinate(x, y));
                        var places2 = places.Where(md => md.place.Contains(location)).ToList();

                    }
                }
                sw.Stop(); //measuring time it takes to parse a 6-cell down to 10-cells.and wou
                containsPointRuntimes.Add(sw.ElapsedMilliseconds);
            }

            //This loop errors out
            //for (int i = 0; i < 50; i++)
            //{
            //    //randomize lat and long to somewhere in Ohio.
            //    //42, -80 NE
            //    //38, -84 SW
            //    //so 38 + (0-4), -84 = (0-4) coords.
            //    //double lat = 38 + (r.NextDouble() * 4);
            //    //double lon = -84 + (r.NextDouble() * 4);
            //    double lat = 90 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
            //    double lon = 180 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
            //    var olc = OpenLocationCode.Encode(lat, lon);
            //    var codeString = olc.Substring(0, 6);
            //    sw.Restart();
            //    var box = OpenLocationCode.DecodeValid(codeString);
            //    var cord1 = new Coordinate(box.Min.Longitude, box.Min.Latitude);
            //    var cord2 = new Coordinate(box.Min.Longitude, box.Max.Latitude);
            //    var cord3 = new Coordinate(box.Max.Longitude, box.Max.Latitude);
            //    var cord4 = new Coordinate(box.Max.Longitude, box.Min.Latitude);
            //    var cordSeq = new Coordinate[5] { cord4, cord3, cord2, cord1, cord4 };
            //    var location = factory.CreatePolygon(cordSeq); //the 6 cell.

            //    var places = db.MapData.Where(md => md.place.Intersects(location)).ToList();
            //    //var indexedIn = places.Select(md => new { md, Index = new NetTopologySuite.Algorithm.Locate.IndexedPointInAreaLocator(md.place) }).ToList();
            //    double resolution10 = .000125; //as defined
            //    for (double x = box.Min.Longitude; x <= box.Max.Longitude; x += resolution10)
            //    {
            //        for (double y = box.Min.Latitude; y <= box.Max.Latitude; y += resolution10)
            //        {
            //            //Option 2, is Contains on a point faster?
            //            var location2 = new Coordinate(x, y);
            //            var places3 = indexedIn.Where(i => i.Index.Locate(location2) == Location.Interior);
            //        }
            //    }
            //    sw.Stop(); //measuring time it takes to parse a 6-cell down to 10-cells.and wou
            //    AlgorithmRuntimes.Add(sw.ElapsedMilliseconds);
            //}

            for (int i = 0; i < 50; i++)
            {
                //randomize lat and long to somewhere in Ohio.
                //42, -80 NE
                //38, -84 SW
                //so 38 + (0-4), -84 = (0-4) coords.
                //double lat = 38 + (r.NextDouble() * 4);
                //double lon = -84 + (r.NextDouble() * 4);
                double lat = 90 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
                double lon = 180 * r.NextDouble() * (r.Next() % 2 == 0 ? 1 : -1);
                var olc = OpenLocationCode.Encode(lat, lon);
                var codeString = olc.Substring(0, 6);
                sw.Restart();
                var box = OpenLocationCode.DecodeValid(codeString);
                var cord1 = new Coordinate(box.Min.Longitude, box.Min.Latitude);
                var cord2 = new Coordinate(box.Min.Longitude, box.Max.Latitude);
                var cord3 = new Coordinate(box.Max.Longitude, box.Max.Latitude);
                var cord4 = new Coordinate(box.Max.Longitude, box.Min.Latitude);
                var cordSeq = new Coordinate[5] { cord4, cord3, cord2, cord1, cord4 };
                var location = factory.CreatePolygon(cordSeq); //the 6 cell.

                //var places = db.MapData.Where(md => md.place.Intersects(location)).ToList();
                var indexedIn = db.MapData.Where(md => md.place.Contains(location)).Select(md => new NetTopologySuite.Algorithm.Locate.IndexedPointInAreaLocator(md.place)).ToList();
                var fakeCoord = new Coordinate(lon, lat);
                foreach (var ii in indexedIn)
                    ii.Locate(fakeCoord); //force index creation on all items now instead of later.

                double resolution10 = .000125; //as defined
                for (double x = box.Min.Longitude; x <= box.Max.Longitude; x += resolution10)
                {
                    for (double y = box.Min.Latitude; y <= box.Max.Latitude; y += resolution10)
                    {
                        //Option 2, is Contains on a point faster?
                        var location2 = new Coordinate(x, y);
                        var places3 = indexedIn.Where(i => i.Locate(location2) == Location.Interior);
                    }
                }
                sw.Stop(); //measuring time it takes to parse a 6-cell down to 10-cells.and wou
                precachedAlgorithmRuntimes.Add(sw.ElapsedMilliseconds);
            }

            //these commented numbers are out of date.
            //var a = AlgorithmRuntimes.Average(); //557-680ms. half my current idea, but why is it so slow when i try on the main function?
            var b = intersectsPolygonRuntimes.Average(); //967ms. Most consistent results.
            var c = containsPointRuntimes.Average(); //700-1678ms. That's extremely swingy.
            var d = precachedAlgorithmRuntimes.Average(); //778ms, is faster but only operates on a point, when i really want an area.


            return;

        }

        public static void Get8CellBitmap(string plusCode8)
        {
            //Load terrain data for an 8cell, turn it into a bitmap
            //Will load these bitmaps on the 8cell grid in the game, so you can see what's around you in a bigger area.
            //server will create and load these. Possibly cache them.

            //requires a list of colors to use.
            //
        }
    }
}
