﻿using Google.OpenLocationCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries.Prepared;
using PraxisCore;
using PraxisMapper.Classes;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using static PraxisCore.DbTables;
using static PraxisCore.Place;

namespace PraxisMapper.Controllers
{
    //DataController: For handling generic get/set commands for data by location.
    //The part that actually allows for games to be made using PraxisMapper without editing code. (give or take styles until a style editor exists on an admin interface)

    [Route("[controller]")]
    [ApiController]
    public class DataController : Controller
    {
        static DateTime lastExpiryPass = DateTime.Now.AddSeconds(-1);
        static ConcurrentDictionary<string, ReaderWriterLockSlim> locks = new ConcurrentDictionary<string, ReaderWriterLockSlim>();

        private readonly IConfiguration Configuration;
        private static IMemoryCache cache;

        public DataController(IConfiguration configuration, IMemoryCache memoryCacheSingleton)
        {
            Configuration = configuration;
            cache = memoryCacheSingleton;

            if (lastExpiryPass < DateTime.Now)
            {
                var db = new PraxisContext();
                db.Database.ExecuteSqlRaw("DELETE FROM PlaceGameData WHERE expiration IS NOT NULL AND expiration < NOW()");
                db.Database.ExecuteSqlRaw("DELETE FROM AreaGameData WHERE expiration IS NOT NULL AND expiration < NOW()");
                db.Database.ExecuteSqlRaw("DELETE FROM PlayerData WHERE expiration IS NOT NULL AND expiration < NOW()");
                lastExpiryPass = DateTime.Now.AddMinutes(30);
            }
        }

        [HttpPut]
        [Route("/[controller]/SetPlusCodeData/{plusCode}/{key}/{value}")]
        [Route("/[controller]/SetPlusCodeData/{plusCode}/{key}/{value}/{expiresIn}")]
        [Route("/[controller]/PlusCode/{plusCode}/{key}/{value}")]
        [Route("/[controller]/PlusCode/{plusCode}/{key}/{value}/{expiresIn}")]
        [Route("/[controller]/Area/{plusCode}/{key}/{value}")]
        [Route("/[controller]/Area/{plusCode}/{key}/{value}/{expiresIn}")]
        public bool SetPlusCodeData(string plusCode, string key, string value, double? expiresIn = null)
        {
            if (!DataCheck.IsInBounds(cache.Get<IPreparedGeometry>("serverBounds"), OpenLocationCode.DecodeValid(plusCode)))
                return false;
            return GenericData.SetAreaData(plusCode, key, value, expiresIn);
        }

        [HttpGet]
        [Route("/[controller]/GetPlusCodeData/{plusCode}/{key}")]
        [Route("/[controller]/PlusCode/{plusCode}/{key}")]
        [Route("/[controller]/Area/{plusCode}/{key}")]
        public void GetPlusCodeData(string plusCode, string key)
        {
            if (!DataCheck.IsInBounds(cache.Get<IPreparedGeometry>("serverBounds"), OpenLocationCode.DecodeValid(plusCode)))
                return;

            var data = GenericData.GetAreaData(plusCode, key); //TODO: this requires writing bytes directly to Response.BodyWriter for all other, similar functions.
            Response.BodyWriter.WriteAsync(data);
            Response.CompleteAsync();
            return;
        }

        [HttpPut]
        [Route("/[controller]/SetPlayerData/{deviceId}/{key}/{value}")]
        [Route("/[controller]/SetPlayerData/{deviceId}/{key}/{value}/{expiresIn}")]
        [Route("/[controller]/Player/{deviceId}/{key}/{value}")]
        [Route("/[controller]/Player/{deviceId}/{key}/{value}/{expiresIn}")]
        public bool SetPlayerData(string deviceId, string key, string value, double? expiresIn = null)
        {
            return GenericData.SetPlayerData(deviceId, key, value, expiresIn);
        }

        [HttpGet]
        [Route("/[controller]/GetPlayerData/{deviceId}/{key}")]
        [Route("/[controller]/Player/{deviceId}/{key}")]
        public void GetPlayerData(string deviceId, string key)
        {
            var data = GenericData.GetPlayerData(deviceId, key);
            Response.BodyWriter.WriteAsync(data);
            Response.CompleteAsync();
            return;
        }

        [HttpPut]
        [Route("/[controller]/SetElementData/{elementId}/{key}/{value}")]
        [Route("/[controller]/SetElementData/{elementId}/{key}/{value}/{expiresIn}")]
        [Route("/[controller]/Element/{elementId}/{key}/{value}")]
        [Route("/[controller]/Element/{elementId}/{key}/{value}/{expiresIn}")]
        [Route("/[controller]/Place/{elementId}/{key}/{value}")]
        [Route("/[controller]/Place/{elementId}/{key}/{value}/{expiresIn}")]
        public bool SetStoredElementData(Guid elementId, string key, string value, double? expiresIn = null)
        {
            return GenericData.SetPlaceData(elementId, key, value, expiresIn);
        }

        [HttpGet]
        [Route("/[controller]/GetElementData/{elementId}/{key}")]
        [Route("/[controller]/Element/{elementId}/{key}")]
        [Route("/[controller]/Place/{elementId}/{key}")]
        public void GetElementData(Guid elementId, string key)
        {
            var data = GenericData.GetPlaceData(elementId, key);
            Response.BodyWriter.WriteAsync(data);
            Response.CompleteAsync();
            return;
        }

        [HttpGet]
        [Route("/[controller]/GetAllPlayerData/{deviceId}")]
        [Route("/[controller]/Player/All/{deviceId}")]
        public string GetAllPlayerData(string deviceId)
        {
            var data = GenericData.GetAllPlayerData(deviceId);
            StringBuilder sb = new StringBuilder();
            foreach (var d in data)
                sb.Append(d.deviceId).Append("|").Append(d.key).Append("|").AppendLine(d.value);

            return sb.ToString();
        }

        [HttpGet]
        [Route("/[controller]/GetAllDataInPlusCode/{plusCode}")]
        [Route("/[controller]/PlusCode/All/{plusCode}")]
        [Route("/[controller]/Area/All/{plusCode}")]
        public string GetAllDataInPlusCode(string plusCode)
        {
            if (!DataCheck.IsInBounds(cache.Get<IPreparedGeometry>("serverBounds"), OpenLocationCode.DecodeValid(plusCode)))
                return "";
            var data = GenericData.GetAllDataInArea(plusCode);
            StringBuilder sb = new StringBuilder();
            foreach (var d in data)
                sb.Append(d.plusCode).Append("|").Append(d.key).Append("|").AppendLine(d.value);

            return sb.ToString();
        }

        [HttpGet]
        [Route("/[controller]/GetAllDataInElement/{elementId}/")]
        [Route("/[controller]/Element/All/{elementId}/")]
        [Route("/[controller]/Place/All/{elementId}/")]
        public string GetAllDataInOsmElement(Guid elementId)

        {
            var data = GenericData.GetAllDataInPlace(elementId);
            StringBuilder sb = new StringBuilder();
            foreach (var d in data)
                sb.Append(d.elementId).Append("|").Append(d.key).Append("|").AppendLine(d.value);

            return sb.ToString();
        }

        [HttpPut]
        [Route("/[controller]/SetGlobalData/{key}/{value}")]
        [Route("/[controller]/Global/{key}/{value}")]
        public bool SetGlobalData(string key, string value)
        {
            return GenericData.SetGlobalData(key, value);
        }

        [HttpGet]
        [Route("/[controller]/GetGlobalData/{key}")]
        [Route("/[controller]/Global/{key}")]
        public void GetGlobalData(string key)
        {
            var data = GenericData.GetGlobalData(key);
            Response.BodyWriter.WriteAsync(data);
            Response.CompleteAsync();
            return;
        }

        [HttpGet]
        [Route("/[controller]/GetServerBounds")]
        public string GetServerBounds()
        {
            var bounds = cache.Get<ServerSetting>("settings");
            return bounds.SouthBound + "|" + bounds.WestBound + "|" + bounds.NorthBound + "|" + bounds.EastBound;
        }

        [HttpPut]
        [Route("/[controller]/IncrementPlayerData/{deviceId}/{key}/{changeAmount}")]
        [Route("/[controller]/Player/Increment/{deviceId}/{key}/{changeAmount}")]
        public void IncrementPlayerData(string deviceId, string key, double changeAmount, double? expirationTimer = null)
        {
            PerformanceTracker pt = new PerformanceTracker("IncrementPlayerData");
            string lockKey = deviceId + key;
            locks.TryAdd(lockKey, new ReaderWriterLockSlim());
            var thisLock = locks[lockKey];
            thisLock.EnterWriteLock();
                var data = GenericData.GetPlayerData(deviceId, key);
                double val = 0;
                Double.TryParse(data.ToString(), out val);
                val += changeAmount;
                GenericData.SetPlayerData(deviceId, key, val.ToString(), expirationTimer);
            thisLock.ExitWriteLock();

            if (thisLock.WaitingWriteCount == 0)
                locks.TryRemove(lockKey, out thisLock);

            pt.Stop();
        }

        [HttpPut]
        [Route("/[controller]/IncrementGlobalData/{key}/{changeAmount}")]
        [Route("/[controller]/Global/Increment/{key}/{changeAmount}")]
        public void IncrementGlobalData(string key, double changeAmount)
        {
            PerformanceTracker pt = new PerformanceTracker("IncrementGlobalData");
            locks.TryAdd(key, new ReaderWriterLockSlim());
            var thisLock = locks[key];
            thisLock.EnterWriteLock();
                var data = GenericData.GetGlobalData(key);
                double val = 0;
                Double.TryParse(data.ToString(), out val);
                val += changeAmount;
                GenericData.SetGlobalData(key, val.ToString());
            thisLock.ExitWriteLock();

            if (thisLock.WaitingWriteCount == 0)
                locks.TryRemove(key, out thisLock);

            pt.Stop();
        }

        [HttpPut]
        [Route("/[controller]/IncrementPlusCodeData/{plusCode}/{key}/{changeAmount}")]
        [Route("/[controller]/PlusCode/Increment/{plusCode}/{key}/{changeAmount}")]
        [Route("/[controller]/Area/Increment/{plusCode}/{key}/{changeAmount}")]
        public void IncrementPlusCodeData(string plusCode, string key, double changeAmount, double? expirationTimer = null)
        {
            if (!DataCheck.IsInBounds(cache.Get<IPreparedGeometry>("serverBounds"), OpenLocationCode.DecodeValid(plusCode)))
                return;
            
            PerformanceTracker pt = new PerformanceTracker("IncrementPlusCodeData");
            string lockKey = plusCode + key;
            locks.TryAdd(lockKey, new ReaderWriterLockSlim());
            var thisLock = locks[lockKey];
            thisLock.EnterWriteLock();
                var data = GenericData.GetAreaData(plusCode, key);
                double val = 0;
                Double.TryParse(data.ToString(), out val);
                val += changeAmount;
                GenericData.SetAreaData(plusCode, key, val.ToString(), expirationTimer);
            thisLock.ExitWriteLock();

            if (thisLock.WaitingWriteCount == 0)
                locks.TryRemove(lockKey, out thisLock);

            pt.Stop();
        }

        [HttpPut]
        [Route("/[controller]/IncrementElementData/{elementId}/{key}/{changeAmount}")]
        [Route("/[controller]/Element/Increment/{elementId}/{key}/{changeAmount}")]
        [Route("/[controller]/Place/Increment/{elementId}/{key}/{changeAmount}")]
        public void IncrementElementData(Guid elementId, string key, double changeAmount, double? expirationTimer = null)
        {
            PerformanceTracker pt = new PerformanceTracker("IncrementElementData");
            string lockKey = elementId.ToString() + key;
            locks.TryAdd(lockKey, new ReaderWriterLockSlim());
            var thisLock = locks[lockKey];
            thisLock.EnterWriteLock();
                var data = GenericData.GetPlaceData(elementId, key);
                double val = 0;
                Double.TryParse(data.ToString(), out val);
                val += changeAmount;
                GenericData.SetPlaceData(elementId, key, val.ToString(), expirationTimer);
            thisLock.ExitWriteLock();

            if (thisLock.WaitingWriteCount == 0)
                locks.TryRemove(lockKey, out thisLock);

            pt.Stop();
        }

        [HttpGet]
        [Route("/[controller]/GetPlusCodeTerrainData/{plusCode}")]
        [Route("/[controller]/Terrain/{plusCode}")]
        public string GetPlusCodeTerrainData(string plusCode)
        {
            //This function returns 1 line per Cell10, the smallest (and therefore highest priority) item intersecting that cell10.
            PerformanceTracker pt = new PerformanceTracker("GetPlusCodeTerrainData");
            GeoArea box = OpenLocationCode.DecodeValid(plusCode);
            if (!DataCheck.IsInBounds(cache.Get<IPreparedGeometry>("serverBounds"), box))
                return "";            
            var places = GetPlaces(box);
            places = places.Where(p => p.GameElementName != TagParser.defaultStyle.Name).ToList();

            StringBuilder sb = new StringBuilder();
            //pluscode|name|type|PrivacyID 

            var data = AreaTypeInfo.SearchArea(ref box, ref places);
            foreach (var d in data)
                sb.Append(d.Key).Append("|").Append(d.Value.Name).Append("|").Append(d.Value.areaType).Append("|").Append(d.Value.PrivacyId).Append("\r\n");
            var results = sb.ToString();
            pt.Stop(plusCode);
            return results;
        }

        [HttpGet]
        [Route("/[controller]/GetPlusCodeTerrainDataFull/{plusCode}")]
        [Route("/[controller]/Terrain/All/{plusCode}")]
        public string GetPlusCodeTerrainDataFull(string plusCode)
        {
            //This function returns 1 line per Cell10 per intersecting element. For an app that needs to know all things in all points.
            PerformanceTracker pt = new PerformanceTracker("GetPlusCodeTerrainDataFull");
            GeoArea box = OpenLocationCode.DecodeValid(plusCode);
            if (!DataCheck.IsInBounds(cache.Get<IPreparedGeometry>("serverBounds"), box))
                return "";
            var places = GetPlaces(box); //All the places in this Cell8
            places = places.Where(p => p.GameElementName != TagParser.defaultStyle.Name).ToList();

            StringBuilder sb = new StringBuilder();
            //pluscode|name|type|privacyID(named wrong but its the Guid)

            var data = AreaTypeInfo.SearchAreaFull(ref box, ref places);
            foreach (var d in data)
                foreach(var v in d.Value)
                    sb.Append(d.Key).Append("|").Append(v.Name).Append("|").Append(v.areaType).Append("|").Append(v.PrivacyId).Append("\r\n");
            var results = sb.ToString();
            pt.Stop(plusCode);
            return results;
        }

        [HttpGet]
        [Route("/[controller]/GetScoreForPlace/{elementId}")]
        [Route("/[controller]/Score/{elementId}")]
        public long GetScoreForPlace(Guid elementId)
        {
            return ScoreData.GetScoreForSinglePlace(elementId);
        }

        [HttpGet]
        [Route("/[controller]/GetDistanceToPlace/{elementId}/{lat}/{lon}")]
        [Route("/[controller]/Distance/{elementId}/{lat}/{lon}")]
        public double GetDistanceToPlace(Guid elementId, double lat, double lon)
        {
            var db = new PraxisContext();
            var place = db.Places.FirstOrDefault(e => e.PrivacyId == elementId);
            if (place == null) return 0;
            return place.ElementGeometry.Distance(new NetTopologySuite.Geometries.Point(lon, lat));
        }

        [HttpGet]
        [Route("/[controller]/GetCenterOfPlace/{elementId}")]
        [Route("/[controller]/Center/{elementId}")]
        public string GetCenterOfPlace(Guid elementId)
        {
            var db = new PraxisContext();
            var place = db.Places.FirstOrDefault(e => e.PrivacyId == elementId);
            if (place == null) return "0|0";
            var center = place.ElementGeometry.Centroid;
            return center.Y.ToString() + "|" + center.X.ToString();        
        }

        [HttpDelete]
        [Route("/[controller]/Player/{deviceId}")]
        public int DeleteUser(string deviceId)
        {
            //GDPR compliance requires this to exist and be available to the user. 
            //Custom games that attach players to locations may need additional logic to fully meet legal requirements.
            var db = new PraxisContext();
            var removing = db.PlayerData.Where(p => p.DeviceID == deviceId).ToArray();
            db.PlayerData.RemoveRange(removing);
            return db.SaveChanges();
        }
    }
}