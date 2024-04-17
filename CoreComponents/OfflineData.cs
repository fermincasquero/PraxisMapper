﻿using Google.OpenLocationCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using NetTopologySuite.Algorithm.Match;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PraxisCore
{
    public class OfflineData
    {
        static object zipLock = new object();
        public class OfflineDataV2
        {
            public string olc { get; set; } //PlusCode
            public Dictionary<string, List<OfflinePlaceEntry>> entries { get; set; }
            public Dictionary<int, string> nameTable { get; set; } //id, name
        }

        public class OfflinePlaceEntry
        {
            public int? nid { get; set; } = null; //nametable id
            public int tid { get; set; } //terrain id, which style entry this place is
            public int gt { get; set; } //geometry type. 1 = point, 2 = line OR hollow shape, 3 = filled shape.
            public string p { get; set; } //Points, local to the given PlusCode. If human-readable, is string pairs, if not is base64 encoded integers.
            public double? size { get; set; } //Removed after sorting.
            public int? layerOrder { get; set; } //removed after sorting as well.
        }

        public class OfflineDataV2Min//Still a Cell6 to draw, but minimized as much as possible.
        {
            public string olc { get; set; } //PlusCode
            public Dictionary<string, List<MinOfflineData>> entries { get; set; }
            public Dictionary<int, string> nameTable { get; set; } //id, name
        }

        public class MinOfflineData
        {
            public string c { get; set; } //Point Center, as a pluscode? Or pixel coords? Probably pixel coords
            public int r { get; set; }  //radius for a circle representing roughly the place, in pixels on the client image (1 Cell11 or 12)
            public int? nid { get; set; } = null; //nametable id, as regular offline data.
            public int tid { get; set; } //terrain id, which style entry this place is

        }


        public static double simplifyRes = 0.0000078125; //default = cell12Lat
        public static double xRes = 0.0000078125; //default = cell12Lon
        public static double yRes = 0.000005; //default = cell12Lat
        public static string[] styles = ["suggestedmini", "adminBoundsFilled"];
        public static string filePath = "";

        //TODO: rename this to 'makeofflinezipfile", since making json is going to become its own function this one will zip up.
        public static void MakeOfflineJson(string plusCode, Polygon bounds = null, bool saveToFile = true, ZipArchive inner_zip = null, List<DbTables.Place> places = null)
        {
            //Make offline data for PlusCode6s, repeatedly if the one given is a 4 or 2.
            if (bounds == null)
            {
                var dbB = new PraxisContext();
                var settings = dbB.ServerSettings.FirstOrDefault();
                bounds = new GeoArea(settings.SouthBound, settings.WestBound, settings.NorthBound, settings.EastBound).ToPolygon();
                dbB.Dispose();
            }

            var area = plusCode.ToPolygon();
            if (!area.Intersects(bounds))
                return;

            if (plusCode.Length < 6)
            {
                if (!PraxisCore.Place.DoPlacesExist(plusCode.ToGeoArea(), places))
                    return;

                if (plusCode.Length == 4)
                {
                    //NOTE: may do processing directly to zip file now.
                    //CHECK: if we have a zip file, unzip it for processing.
                    Directory.CreateDirectory(filePath + plusCode.Substring(0, 2));
                    if (!File.Exists(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip"))
                    {
                        inner_zip = new ZipArchive(File.Create(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip"), ZipArchiveMode.Update);
                    }
                    else
                        inner_zip = ZipFile.Open(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip", ZipArchiveMode.Update);

                    try
                    {
                        //Far future TODO: Work out out to start loading the next set of places from the DB while processing data for the current set
                        Console.WriteLine("Loading places for " + plusCode);
                        Stopwatch load = Stopwatch.StartNew();
                        places = Place.GetPlaces(plusCode.ToGeoArea(), skipTags: true);
                        //For the really big areas, if we crop it once here, should save about 3 minutes of processing later.
                        foreach (var place in places)
                            place.ElementGeometry = place.ElementGeometry.Intersection(area); 
                        load.Stop();
                        Console.WriteLine("Places for " + plusCode + " loaded in " + load.Elapsed);
                    }
                    catch (Exception ex)
                    {
                        //Do nothing, we'll load places up per Cell6 if we can't pull the whole Cell4 into RAM.
                        Console.WriteLine("Places for " + plusCode + " wouldn't load, doing it per Cell6");
                        places = null;
                    }

                    //This block isnt limited by IO, and will suffer from swapping threads constantly.
                    ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
                    Parallel.ForEach(GetCellCombos(), po, pair =>
                    {
                        MakeOfflineJson(plusCode + pair, bounds, saveToFile, inner_zip, places);
                    });

                    bool removeFile = false;
                    if (inner_zip.Entries.Count == 0)
                        removeFile = true;

                    if (inner_zip != null)
                        inner_zip.Dispose();

                    if (removeFile)
                        File.Delete(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip");

                    //ADDED: Because a LOT of these files are incredibly small, they take up a proprotionally HUGE amount of disk space thats actually empty.
                    //so we now zip the files after they're created, so that all 64,000 1kb files can be as small as 64MB instead of 64GB of slack space.
                    //But only do this is there are files at all.
                    //var files = Directory.EnumerateFiles(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2));
                    //if (files.Any())
                    //{
                    //    Log.WriteLog("Zipping data for " + plusCode.Substring(0, 4));
                    //    var zip = ZipFile.Open(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip", ZipArchiveMode.Create);
                    //    foreach (var file in files)
                    //    {
                    //        zip.CreateEntryFromFile(file, Path.GetFileName(file));
                    //        File.Delete(file);
                    //    }
                    //    zip.Dispose();
                    //}
                    return;
                }
                else
                {
                    var doneCell2s = File.ReadAllText("lastOfflineEntry.txt");
                    if (doneCell2s.Contains(plusCode) && plusCode != "")
                        return;

                    foreach (var pair in GetCellCombos())
                        MakeOfflineJson(plusCode + pair, bounds, saveToFile, inner_zip, places);
                    File.AppendAllText("lastOfflineEntry.txt", "|" + plusCode);
                    return;
                }
            }

            //This is to let us be resumable if this stop for some reason, and to keep the number of files in a folder manageable.
            //Each file is a Cell6, so a Cell4 folder has 200 files, and a Cell2 folder would have 40,000

            //NOTE: This is getting replaced with the merge logic from now on. Breakpoints will have to be on the Cell2 level if you want to resume.
            //if (File.Exists(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2) + "\\" + plusCode + ".json"))
            //return;

            Directory.CreateDirectory(filePath + plusCode.Substring(0, 2));
            //Directory.CreateDirectory(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2));

            var sw = Stopwatch.StartNew();
            var finalData = MakeEntries(plusCode, string.Join(",", styles), places);
            if (finalData == null || (finalData.nameTable == null && finalData.entries.Count == 0))
                return;

            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            string data = JsonSerializer.Serialize(finalData, jso);

            if (saveToFile)
            {
                lock (zipLock)
                {
                    Stream entryStream;
                    //if (File.Exists(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2) + "\\" + plusCode + ".json"))
                    var entry = inner_zip.GetEntry(plusCode + ".json");
                    if (entry != null)
                    {
                        //Still having issues with this, particularly when the PREVIOUS run wrote a smaller entry.
                        entryStream = entry.Open();
                        OfflineDataV2 existingData = JsonSerializer.Deserialize<OfflineDataV2>(entryStream);
                        var dataSize = data.Length;
                        finalData = MergeOfflineFiles(finalData, existingData);
                        data = JsonSerializer.Serialize(finalData, jso); //Need to re-serialize it here, THATS the issue.
                        if (data.Length < dataSize)
                        {
                            Debugger.Break();
                            Console.WriteLine("Data got smaller after merging! check this out!");
                        }

                        entryStream.Position = 0;
                        entryStream.SetLength(data.Length);
                        //entryStream.Close();
                        //entry.Delete();
                        //entry = inner_zip.CreateEntry(plusCode + ".json");
                        //entryStream = entry.Open();
                    }
                    else
                    {
                        entry = inner_zip.CreateEntry(plusCode + ".json");
                        entryStream = entry.Open();
                    }

                    using (var streamWriter = new StreamWriter(entryStream))
                        streamWriter.Write(data);
                    entryStream.Close();
                    entryStream.Dispose();
                }



                sw.Stop();
                Log.WriteLog("Created and saved offline data for " + plusCode + " in " + sw.Elapsed);
            }
            else
            {
                GenericData.SetAreaData(plusCode, "offlineV2", data);
            }
        }


        //TODO: test out performance by passing in the list of places to use from Cell4 vs loading hte Cell6 from DB each call.
        public static OfflineDataV2 MakeEntries(string plusCode, string stylesToUse, List<DbTables.Place> places = null)
        {
            //TODO: Most of the stuff from here on should be put into its own function that can be called from this big recursive function
            //OR from the offline plugin for a single area and return data. 
            //May also want an alternate version that queries all places first, then loops based on placeData key matching the styles. 
            //should see if thats faster on some of these very slow blocks I keep hitting.


            using var db = new PraxisContext();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            var cell = plusCode.ToGeoArea();
            var area = plusCode.ToPolygon();

            var styles = stylesToUse.Split(",");

            //Adding variables here so that an instance can process these at higher or lower accuracy if desired. Higher accuracy or not simplifying items
            //will make larger files but the output would be a closer match to the server's images.


            var min = cell.Min;
            Dictionary<string, int> nametable = new Dictionary<string, int>(); //name, id
            var nameIdCounter = 0;

            //Console.WriteLine(plusCode + ":Checking if places exist");

            if (!PraxisCore.Place.DoPlacesExist(cell, places))
                return null;
            //Console.WriteLine(plusCode + ":places found");

            var finalData = new OfflineDataV2();
            finalData.olc = plusCode;
            finalData.entries = new Dictionary<string, List<OfflinePlaceEntry>>();
            foreach (var style in styles)
            {
                //Console.WriteLine(plusCode + ":getting places with " + style);
                var placeData = PraxisCore.Place.GetPlaces(cell, source: places, styleSet: style, dataKey: style, skipTags: true);
                //Console.WriteLine(plusCode + ":places got - " + placeData.Count);

                if (placeData.Count == 0)
                    continue;
                List<OfflinePlaceEntry> entries = new List<OfflinePlaceEntry>(placeData.Count);
                var names = placeData.Where(p => !string.IsNullOrWhiteSpace(p.Name) && !nametable.ContainsKey(p.Name)).Select(p => p.Name).Distinct();
                foreach (var name in names)
                    nametable.Add(name, ++nameIdCounter);
                //nametable = nametable.Union(placeData.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Select(p => p.Name).Distinct().ToDictionary(k => k, v => ++nameIdCounter)).Distinct().ToDictionary();

                //foreach (var place in placeData)
                Parallel.ForEach(placeData, (place) => //This set is NOT done in parallel because we need to keep them ordered.
                {
                    var sizeOrder = place.DrawSizeHint; //TODO: add this in somewhere so I can order by size and run these in parallel.
                    //POTENTIAL TODO: I may need to crop all places first, then sort by their total area to process these largest-to-smallest on the client
                    var geo = place.ElementGeometry.Intersection(area);
                    if (simplifyRes > 0)
                        geo = geo.Simplify(simplifyRes);
                    if (geo.IsEmpty)
                        return; // continue; //Probably an element on the border thats getting pulled in by buffer.

                    int? nameID = null;
                    if (!string.IsNullOrWhiteSpace(place.Name))
                    {
                        if (nametable.TryGetValue(place.Name, out var nameval))
                            nameID = nameval;
                        //else
                        //{
                        //    nametable.Add(place.Name, ++nameIdCounter);
                        //    nameID = nameIdCounter;
                        //}
                    }


                    var styleEntry = TagParser.allStyleGroups[style][place.StyleName];

                    //I'm locking these geometry items to a tile, So I convert these points in the geometry to integers, effectively
                    //letting me draw Cell11 pixel-precise points from this info, and is shorter stringified for JSON vs floats/doubles.
                    var coordSets = GetCoordEntries(geo, cell.Min, xRes, yRes); //Original human-readable strings
                                                                                //var coordSets = GetCoordEntriesInt(place, cell.Min, xRes, yRes); //base64 encoded integers. Uses about half the space unzipped, works out the same zipped.
                    foreach (var coordSet in coordSets)
                    {
                        if (coordSet == "")
                            //if (coordSet.Count == 0)
                            continue;

                        //Now to encode the ints.
                        //byte[] encoded = new byte[coordSet.Count * 4];
                        //for (int x = 0; x < coordSet.Count; x++)
                        //{
                        //    var tempByte = new byte[4];
                        //    BinaryPrimitives.TryWriteInt32LittleEndian(tempByte, coordSet[x]);
                        //    tempByte.CopyTo(encoded, x * 4);
                        //}
                        //var stringifiedInts = Convert.ToBase64String(encoded);

                        //System.Diagnostics.Debugger.Break();
                        var offline = new OfflinePlaceEntry();
                        offline.nid = nameID;
                        offline.tid = styleEntry.MatchOrder; //Client will need to know what this ID means from the offline style endpoint output.

                        offline.gt = geo.GeometryType == "Point" ? 1 : geo.GeometryType == "LineString" ? 2 : styleEntry.PaintOperations.All(p => p.FillOrStroke == "stroke") ? 2 : 3;
                        offline.p = coordSet;
                        offline.size = sizeOrder;
                        offline.layerOrder = styleEntry.PaintOperations.Min(p => p.LayerId);
                        entries.Add(offline);
                    }
                });
                //TODO: determine why one south america place was null.
                //Smaller number layers get drawn first, and bigger places get drawn first.
                finalData.entries[style] = entries.Where(e => e != null).OrderBy(e => e.layerOrder).ThenByDescending(e => e.size).ToList();
                foreach (var e in finalData.entries[style])
                {
                    //Dont save this to the output file.
                    e.size = null;
                    e.layerOrder = null;
                }
            }

            if (finalData.entries.Count == 0)
                return null;

            finalData.nameTable = nametable.Count > 0 ? nametable.ToDictionary(k => k.Value, v => v.Key) : null;

            return finalData;
        }


        public static List<string> GetCoordEntries(Geometry geo, GeoPoint min, double xRes = ConstantValues.resolutionCell11Lon, double yRes = ConstantValues.resolutionCell11Lat)
        {
            List<string> points = new List<string>();

            if (geo.GeometryType == "MultiPolygon")
            {
                foreach (var poly in ((MultiPolygon)geo).Geometries) //This should be the same as the Polygon code below.
                {
                    points.AddRange(GetPolygonPoints(poly as Polygon, min, xRes, yRes));
                }
            }
            else if (geo.GeometryType == "Polygon")
            {
                points.AddRange(GetPolygonPoints(geo as Polygon, min, xRes, yRes));
            }
            else
                points.Add(string.Join("|", geo.Coordinates.Select(c => (int)Math.Round((c.X - min.Longitude) / xRes) + "," + ((int)Math.Round((c.Y - min.Latitude) / yRes)))));

            if (points.Count == 0)
            {
                //System.Diagnostics.Debugger.Break();
            }

            return points;
        }

        public static List<string> GetPolygonPoints(Polygon p, GeoPoint min, double xRes = ConstantValues.resolutionCell11Lon, double yRes = ConstantValues.resolutionCell11Lat)
        {
            List<string> results = new List<string>();
            if (p.Holes.Length == 0)
                results.Add(string.Join("|", p.Coordinates.Select(c => (int)Math.Round((c.X - min.Longitude) / xRes) + "," + ((int)Math.Round((c.Y - min.Latitude) / yRes)))));
            else
            {
                //Split this polygon  into smaller pieces, split on the center of each hole present longitudinally
                //West to east direction chosen arbitrarily.
                var westEdge = p.Coordinates.Min(c => c.X);
                var northEdge = p.Coordinates.Max(c => c.Y);
                var southEdge = p.Coordinates.Min(c => c.Y);

                List<double> splitPoints = new List<double>();
                foreach (var hole in p.Holes.OrderBy(h => h.Centroid.X))
                    splitPoints.Add(hole.Centroid.X);

                foreach (var point in splitPoints)
                {
                    try
                    {
                        var splitPoly = new GeoArea(southEdge, westEdge, northEdge, point).ToPolygon();
                        var subPoly = p.Intersection(splitPoly);

                        //Still need to check that we have reasonable geometry here.
                        if (subPoly.GeometryType == "Polygon")
                            results.AddRange(GetPolygonPoints(subPoly as Polygon, min, xRes, yRes));
                        else if (subPoly.GeometryType == "MultiPolygon")
                        {
                            foreach (var p2 in ((MultiPolygon)subPoly).Geometries)
                                results.AddRange(GetPolygonPoints(p2 as Polygon, min, xRes, yRes));
                        }
                        else
                            Log.WriteLog("Offline proccess error: Got geoType " + subPoly.GeometryType + ", which wasnt expected");
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLog("Offline proccess error: " + ex.Message);
                    }
                    westEdge = point;
                }
            }
            return results.Distinct().ToList(); //In the unlikely case splitting ends up processing the same part twice
        }

        static List<string> GetCellCombos()
        {
            var list = new List<string>(400);
            foreach (var Yletter in OpenLocationCode.CodeAlphabet)
                foreach (var Xletter in OpenLocationCode.CodeAlphabet)
                {
                    list.Add(String.Concat(Yletter, Xletter));
                }

            return list;
        }

        static List<string> GetCell2Combos()
        {
            var list = new List<string>(400);
            foreach (var Yletter in OpenLocationCode.CodeAlphabet.Take(9))
                foreach (var Xletter in OpenLocationCode.CodeAlphabet.Take(18))
                {
                    list.Add(String.Concat(Yletter, Xletter));
                }

            return list;
        }

        public static void MakeMinimizedOfflineData(string plusCode, Polygon bounds = null, bool saveToFile = true, ZipArchive inner_zip = null, List<DbTables.Place> places = null)
        {
            //This produces JSON with 1 row per item and a few fields:
            //Name (possibly a table saved separately), PlusCode (centerpoint), radius (SQUARE SHAPED, but calculated based on the envelope for non-points), and terrain type.
            //This is worth considering for games that DONT need geometry and can do a little bit of lookup on their own.
            //This may also be created per Cell2/4/6 block for comparison vs drawable data.
            //I may also reduce this to a Cell10 resolution?

            //Minimized data could be drawn at the Cell11 resolution, or Cell10, since it's not intended to be displayed to the user.

            //Make offline data for PlusCode6s, repeatedly if the one given is a 4 or 2.
            if (bounds == null)
            {
                var dbB = new PraxisContext();
                var settings = dbB.ServerSettings.FirstOrDefault();
                bounds = new GeoArea(settings.SouthBound, settings.WestBound, settings.NorthBound, settings.EastBound).ToPolygon();
                dbB.Dispose();
            }

            var area = plusCode.ToPolygon();
            if (!area.Intersects(bounds))
                return;

            //Hmm. Is this actually any different from just exporting data as before, but cropping every element to its envelope? (or a 
            //it might not be, and thats still 

            if (plusCode.Length < 6)
            {
                if (!PraxisCore.Place.DoPlacesExist(plusCode.ToGeoArea(), places))
                    return;

                if (plusCode.Length == 4)
                {
                    if (!File.Exists(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip"))
                    {
                        inner_zip = new ZipArchive(File.Create(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip"), ZipArchiveMode.Update);

                        //Log.WriteLog("Unzipping existing data for " + plusCode.Substring(0, 4));
                        ////unzip that file
                        //var fs = File.OpenRead(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip");
                        //ZipFile.ExtractToDirectory(fs, filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2));
                        //fs.Close();
                        //File.Delete(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip");
                        //inner_zip = ZipFile.Open(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip", ZipArchiveMode.Update);
                    }
                    else
                        inner_zip = ZipFile.Open(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip", ZipArchiveMode.Update);


                    try
                    {
                        Stopwatch load = Stopwatch.StartNew();
                        places = Place.GetPlaces(plusCode.ToGeoArea(), dataKey: "suggestedmini", styleSet: "suggestedmini", skipTags: true);
                        load.Stop();
                        Console.WriteLine("Places loaded in " + load.Elapsed);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Places for " + plusCode + " wouldn't load, doing it per Cell6");
                        places = null;
                    }

                    //This block isnt limited by IO, and will suffer from swapping threads constantly.
                    ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
                    Parallel.ForEach(GetCellCombos(), po, pair =>
                    {
                        MakeMinimizedOfflineData(plusCode + pair, bounds, saveToFile, inner_zip, places);
                    });

                    bool removeFile = false;
                    if (inner_zip.Entries.Count == 0)
                        removeFile = true;

                    if (inner_zip != null)
                        inner_zip.Dispose();

                    if (removeFile)
                        File.Delete(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(0, 4) + ".zip");

                    return;
                }
                else if (plusCode.Length == 2)
                {
                    var doneCell2s = File.ReadAllText("lastOfflineEntry.txt");
                    if (doneCell2s.Contains(plusCode) && plusCode != "")
                        return;

                    Directory.CreateDirectory(filePath + plusCode.Substring(0, 2));
                    foreach (var pair in GetCellCombos())
                        MakeMinimizedOfflineData(plusCode + pair, bounds, saveToFile);

                    File.AppendAllText("lastOfflineEntry.txt", "|" + plusCode);
                    return;
                }
                else
                {
                    //Called with an empty string, to mean 'run for all Cell2s'
                    foreach (var pair in GetCell2Combos())
                        MakeMinimizedOfflineData(plusCode + pair, bounds, saveToFile);

                    return;
                }
            }

            //This is to let us be resumable if this stop for some reason, and to keep the number of files in a folder manageable.
            //Each file is a Cell6, so a Cell4 folder has 200 files, and a Cell2 folder would have 40,000
            //if (File.Exists(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2) + "\\" + plusCode + ".json"))
            //return;


            //Directory.CreateDirectory(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2));

            var sw = Stopwatch.StartNew();
            var finalData = MakeMinimizedOfflineEntries(plusCode, string.Join(",", styles), places);
            if (finalData == null || (finalData.nameTable == null && finalData.entries.Count == 0))
                return;

            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            string data = JsonSerializer.Serialize(finalData, jso);

            if (saveToFile)
            {
                lock (zipLock)
                {
                    Stream entryStream;
                    //if (File.Exists(filePath + plusCode.Substring(0, 2) + "\\" + plusCode.Substring(2, 2) + "\\" + plusCode + ".json"))
                    var entry = inner_zip.GetEntry(plusCode + ".json");
                    if (entry != null)
                    {
                        entryStream = entry.Open();
                        OfflineDataV2Min existingData = JsonSerializer.Deserialize<OfflineDataV2Min>(entryStream);
                        finalData = MergeMinimumOfflineFiles(finalData, existingData);
                        entryStream.Position = 0;
                        //entry.Delete();
                        //entry = inner_zip.CreateEntry(plusCode + ".json");
                        //entryStream = entry.Open();
                    }
                    else
                    {
                        entry = inner_zip.CreateEntry(plusCode + ".json");
                        entryStream = entry.Open();
                    }


                    using (var streamWriter = new StreamWriter(entryStream))
                        streamWriter.Write(data);
                    entryStream.Close();
                    entryStream.Dispose();
                }
            }


            else
            {
                GenericData.SetAreaData(plusCode, "offlineV2", data);
            }


            sw.Stop();
            Log.WriteLog("Created and saved minimized offline data for " + plusCode + " in " + sw.Elapsed);
        }

        public static OfflineDataV2Min MakeMinimizedOfflineEntries(string plusCode, string stylesToUse, List<DbTables.Place> places = null)
        {
            using var db = new PraxisContext();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            var styles = stylesToUse.Split(",");

            var cell = plusCode.ToGeoArea();
            var area = plusCode.ToPolygon();
            //var cellPoly = cell.ToPolygon();

            //Minimized offline files do not benefit from variables, they're fairly fixed and changing anything here doesn't really help.

            var min = cell.Min;
            Dictionary<string, int> nametable = new Dictionary<string, int>(); //name, id
            var nameIdCounter = 0;

            if (!PraxisCore.Place.DoPlacesExist(cell, places))
                return null;

            const double innerRes = ConstantValues.resolutionCell10;

            var finalData = new OfflineDataV2Min();
            finalData.olc = plusCode;
            finalData.entries = new Dictionary<string, List<MinOfflineData>>();
            foreach (var style in styles)
            {
                //Console.WriteLine(plusCode + ":getting places with " + style);
                var placeData = PraxisCore.Place.GetPlaces(cell, source: places, styleSet: style, dataKey: style, skipTags: true);
                //Console.WriteLine(plusCode + ":places got - " + placeData.Count);

                if (placeData.Count == 0)
                    continue;
                //List<MinOfflineData> entries = new List<MinOfflineData>(placeData.Count);
                ConcurrentBag<MinOfflineData> entries = new ConcurrentBag<MinOfflineData>();
                var names = placeData.Where(p => !string.IsNullOrWhiteSpace(p.Name) && !nametable.ContainsKey(p.Name)).Select(p => p.Name).Distinct();
                foreach (var name in names)
                    nametable.Add(name, ++nameIdCounter);
                //nametable = nametable.Union(placeData.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Select(p => p.Name).Distinct().ToDictionary(k => k, v => ++nameIdCounter)).Distinct().ToDictionary();

                //foreach (var place in placeData)
                Parallel.ForEach(placeData, (place) =>
                {

                    //This is a catch-fix for a different issue, where apparently some closed lineStrings aren't converted to polygons on load.
                    if (place.ElementGeometry.GeometryType == "LineString" && ((LineString)place.ElementGeometry).IsClosed)
                        place.ElementGeometry = Singletons.geometryFactory.CreatePolygon(place.ElementGeometry.Coordinates);

                    //POTENTIAL TODO: I may need to crop all places first, then sort by their total area to process these largest-to-smallest on the client
                    Geometry geo = Singletons.geometryFactory.CreateEmpty(Dimension.Surface);
                    try
                    {
                        geo = place.ElementGeometry.Intersection(area);
                    }
                    catch (Exception ex)
                    {
                        //Do nothing for now.
                    }
                    if (geo.IsEmpty)
                        return; // continue; //Probably an element on the border thats getting pulled in by buffer.

                    int? nameID = null;
                    if (!string.IsNullOrWhiteSpace(place.Name))
                    {
                        if (nametable.TryGetValue(place.Name, out var nameval))
                            nameID = nameval;
                    }

                    var styleEntry = TagParser.allStyleGroups[style][place.StyleName];
                    if (geo.GeometryType == "Point")
                    {
                        var offline = new MinOfflineData();
                        offline.nid = nameID;
                        offline.c = (int)Math.Round((geo.Coordinate.X - min.Longitude) / innerRes) + "," + ((int)Math.Round((geo.Coordinate.Y - min.Latitude) / innerRes));
                        offline.r = 2; //5 == Cell11 resolution. Use 22 for Cell12, use 2 for Cell10 (the space the point is in, and the ones surrounding it.
                        offline.tid = styleEntry.MatchOrder;
                        entries.Add(offline);
                    }
                    else if (geo.GeometryType == "Polygon")
                    {
                        var offline = new MinOfflineData();
                        offline.nid = nameID;
                        offline.c = (int)Math.Round((geo.Centroid.X - min.Longitude) / innerRes) + "," + ((int)Math.Round((geo.Centroid.Y - min.Latitude) / innerRes));
                        //First attempt at formula: Nearly ensures the full area will be covered, but dramatically overshoots almost all areas.
                        //offline.r = (int)Math.Round(((geo.EnvelopeInternal.Width + geo.EnvelopeInternal.Height) * 0.5) / innerRes);
                        offline.r = Math.Max(2, (int)Math.Round(Math.Sqrt(geo.Area / Math.PI) / ConstantValues.resolutionCell10)); //Get area in degrees, conver to Cell10 pixels, minimum 2.
                        offline.tid = styleEntry.MatchOrder;
                        entries.Add(offline);
                    }
                    else if (geo.GeometryType == "MultiPolygon")
                    {
                        foreach (var p in ((MultiPolygon)geo).Geometries)
                        {
                            var offline = new MinOfflineData();
                            offline.nid = nameID;
                            offline.c = (int)Math.Round((p.Centroid.X - min.Longitude) / innerRes) + "," + ((int)Math.Round((p.Centroid.Y - min.Latitude) / innerRes));
                            offline.r = Math.Max(2, (int)Math.Round(Math.Sqrt(p.Area / Math.PI) / ConstantValues.resolutionCell10)); //Get area in degrees, conver to Cell10 pixels, minimum 2.
                            offline.tid = styleEntry.MatchOrder;
                            entries.Add(offline);
                        }
                    }
                    else if (geo.GeometryType == "LineString")
                    {
                        var lp = geo as LineString;
                        if (lp.IsClosed) //Treat this as a polygon.
                        {
                            var offline = new MinOfflineData();
                            offline.nid = nameID;
                            offline.c = (int)Math.Round((lp.Centroid.X - min.Longitude) / innerRes) + "," + ((int)Math.Round((lp.Centroid.Y - min.Latitude) / innerRes));
                            offline.r = (int)Math.Round(((lp.EnvelopeInternal.Width + lp.EnvelopeInternal.Height) * 0.5) / innerRes); //Area is 0 on lines, so use the old formula
                            offline.tid = styleEntry.MatchOrder;
                            entries.Add(offline);
                        }
                        else
                        {
                            //We're gonna assumed this is a named trail. Make the start and end of it Points (radius = 2) with the trail's name.
                            var offline = new MinOfflineData();
                            offline.nid = nameID;
                            offline.c = (int)Math.Round((geo.Coordinates.First().X - min.Longitude) / innerRes) + "," + ((int)Math.Round((geo.Coordinates.First().Y - min.Latitude) / innerRes));
                            offline.r = 2;
                            offline.tid = styleEntry.MatchOrder;
                            entries.Add(offline);

                            var offline2 = new MinOfflineData();
                            offline2.nid = nameID;
                            offline2.c = (int)Math.Round((geo.Coordinates.Last().X - min.Longitude) / innerRes) + "," + ((int)Math.Round((geo.Coordinates.Last().Y - min.Latitude) / innerRes));
                            offline2.r = 2;
                            offline2.tid = styleEntry.MatchOrder;
                            entries.Add(offline2);
                        }
                    }
                    else if (place.ElementGeometry.GeometryType == "MultiLineString")
                    {
                        //Not totally sure why this would show up, but again assume its segments of a named trail
                        var mls = place.ElementGeometry as MultiLineString;
                        foreach (var line in mls)
                        {
                            var offline = new MinOfflineData();
                            offline.nid = nameID;
                            offline.c = (int)Math.Round((geo.Coordinates.First().X - min.Longitude) / innerRes) + "," + ((int)Math.Round((geo.Coordinates.First().Y - min.Latitude) / innerRes));
                            offline.r = 2;
                            offline.tid = styleEntry.MatchOrder;
                            entries.Add(offline);

                            var offline2 = new MinOfflineData();
                            offline2.nid = nameID;
                            offline2.c = (int)Math.Round((geo.Coordinates.Last().X - min.Longitude) / innerRes) + "," + ((int)Math.Round((geo.Coordinates.Last().Y - min.Latitude) / innerRes));
                            offline2.r = 2;
                            offline2.tid = styleEntry.MatchOrder;
                            entries.Add(offline2);
                        }
                    }
                });
                var finalEntries = entries.OrderByDescending(e => e.r).ToList(); //so they'll be drawn biggest to smallest for sure.
                finalData.entries[style] = finalEntries;
            }

            if (finalData.entries.Count == 0)
                return null;
            finalData.nameTable = nametable.Count > 0 ? nametable.ToDictionary(k => k.Value, v => v.Key) : null;

            return finalData;

        }

        public static OfflineDataV2Min MergeMinimumOfflineFiles(OfflineDataV2Min existing, OfflineDataV2Min adding)
        {
            //Step 1: Update name table

            Dictionary<int, int> newNameMap = new Dictionary<int, int>(); //<addingTableKey, exisitngTAbleKey>
            if (existing.nameTable == null)
                existing.nameTable = new Dictionary<int, string>();

            int maxKey = existing.nameTable.Count();
            if (adding.nameTable != null)
            {
                foreach (var name in adding.nameTable)
                {
                    if (existing.nameTable.ContainsValue(name.Value))
                    {
                        newNameMap.Add(name.Key, existing.nameTable.First(n => n.Value == name.Value).Key);
                    }
                    else
                    {
                        existing.nameTable.Add(++maxKey, name.Value);
                        newNameMap.Add(name.Key, maxKey);
                    }
                }
            }

            //Step 2: merge sets of entries
            foreach (var entryList in adding.entries)
            {
                if (!existing.entries.ContainsKey(entryList.Key))
                    existing.entries.Add(entryList.Key, entryList.Value);
                else if (existing.entries[entryList.Key] != null)
                {
                    //merge entries
                    var list1 = existing.entries[entryList.Key];
                    var list2 = entryList.Value;
                    list2 = list2.Select(e => new MinOfflineData() { c = e.c, r = e.r, tid = e.tid, nid = e.nid.HasValue ? newNameMap[e.nid.Value] : null }).ToList();
                    //Remove duplicates
                    var remove = list2.Where(l2 => list1.Any(l1 => l1.c == l2.c && l1.nid == l2.nid && l1.tid == l2.tid && l1.r == l2.r)).ToList();
                    foreach (var r in remove)
                        list2.Remove(r);

                    existing.entries[entryList.Key].AddRange(list2);
                }
            }

            if (existing.nameTable.Count == 0 && (adding.nameTable == null || adding.nameTable.Count == 0))
                existing.nameTable = null;

            return existing;
        }


        public static OfflineDataV2 MergeOfflineFiles(OfflineDataV2 existing, OfflineDataV2 adding)
        {
            //Step 1: Update name table

            Dictionary<int, int> newNameMap = new Dictionary<int, int>(); //<addingTableKey, exisitngTAbleKey>
            if (existing.nameTable == null)
                existing.nameTable = new Dictionary<int, string>();

            int maxKey = existing.nameTable.Count();
            if (adding.nameTable != null)
            {
                foreach (var name in adding.nameTable)
                {
                    if (existing.nameTable.ContainsValue(name.Value))
                    {
                        newNameMap.Add(name.Key, existing.nameTable.First(n => n.Value == name.Value).Key);
                    }
                    else
                    {
                        existing.nameTable.Add(++maxKey, name.Value);
                        newNameMap.Add(name.Key, maxKey);
                    }
                }
            }

            //Step 2: merge sets of entries
            foreach (var entryList in adding.entries)
            {
                if (!existing.entries.ContainsKey(entryList.Key))
                    existing.entries.Add(entryList.Key, entryList.Value);
                else if (existing.entries[entryList.Key] != null)
                {
                    //merge entries
                    var list1 = existing.entries[entryList.Key];
                    var list2 = entryList.Value;
                    list2 = list2.Select(e => new OfflinePlaceEntry() { p = e.p, gt = e.gt, tid = e.tid, nid = e.nid.HasValue ? newNameMap[e.nid.Value] : null }).ToList();
                    //Remove duplicates
                    var remove = list2.Where(l2 => list1.Any(l1 => l1.p == l2.p && l1.nid == l2.nid && l1.tid == l2.tid && l1.gt == l2.gt)).ToList();
                    foreach (var r in remove)
                        list2.Remove(r);

                    existing.entries[entryList.Key].AddRange(list2);
                }
            }

            if (existing.nameTable.Count == 0 && (adding.nameTable == null || adding.nameTable.Count == 0))
                existing.nameTable = null;

            return existing;
        }
    }
}
