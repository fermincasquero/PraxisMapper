﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PraxisCore;
using PraxisMapper.Classes;
using System;
using System.Buffers;
using System.Linq;
using static PraxisCore.DbTables;

namespace PraxisMapper.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        //For endpoints that relay information about the server itself. not game data.
        //
        private readonly IConfiguration Configuration;
        private static IMemoryCache cache;

        public ServerController(IConfiguration configuration, IMemoryCache memoryCacheSingleton)
        {
            Configuration = configuration;
            cache = memoryCacheSingleton;
        }

        [HttpGet]
        [Route("/[controller]/Test")]
        public string Test()
        {
            //Used for clients to test if server is alive. Returns OK normally, clients should check for non-OK results to display as a maintenance message.
            return "OK";
        }

        [HttpGet]
        [Route("/[controller]/ServerBounds")]
        public string GetServerBounds()
        {
            var bounds = cache.Get<ServerSetting>("settings");
            return bounds.SouthBound + "|" + bounds.WestBound + "|" + bounds.NorthBound + "|" + bounds.EastBound;
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

        [HttpGet]
        [Route("/[controller]/UseAntiCheat")]
        public bool UseAntiCheat()
        {
            //This may belong on a different endpoint? Possibly Admin? Or should I make a new Server endpoint for things like that?
            return Configuration.GetValue<bool>("enableAntiCheat");
        }

        [HttpGet]
        [Route("/[controller]/MOTD")]
        [Route("/[controller]/Message")]
        public string MessageOfTheDay()
        {
            var db = new PraxisContext(); //NOTE: not using the cached ServerSettings table, since this might change on the fly.
            var message = db.ServerSettings.First().MessageOfTheDay;
            return message;
        }

        [HttpPut]
        [Route("/[controller]/AntiCheat/{filename}")]
        public void AntiCheat(string filename)
        {
            var endData = GenericData.ReadBody(Request.BodyReader, (int)Request.ContentLength);

            var db = new PraxisContext();
            var IP = Request.HttpContext.Connection.RemoteIpAddress.ToString(); //NOTE: may become deviceID after testing if that's better.

            if (!Classes.PraxisAntiCheat.antiCheatStatus.ContainsKey(IP))
                Classes.PraxisAntiCheat.antiCheatStatus.TryAdd(IP, new Classes.AntiCheatInfo());

            if (db.AntiCheatEntries.Any(a => a.filename == filename && a.data == endData))
            {
                var entry = Classes.PraxisAntiCheat.antiCheatStatus[IP];
                if (!entry.entriesValidated.Contains(filename))
                    entry.entriesValidated.Add(filename);

                if (entry.entriesValidated.Count == Classes.PraxisAntiCheat.expectedCount)
                    entry.validUntil = DateTime.Now.AddHours(24);
            }
        }

        [HttpPut]
        [Route("/[controller]/EncryptUserPassword/{devicedId}/{password}")]
        [Route("/[controller]/Password/{devicedId}/{password}")]
        public bool EncryptUserPassword(string deviceId, string password)
        {
            Response.Headers.Add("X-noPerfTrack", "Server/Password/deviceId/password-PUT");
            return GenericData.EncryptPassword(deviceId, password, Configuration["PasswordRounds"].ToInt());
        }

        [HttpGet]
        [Route("/[controller]/CheckPassword/{deviceId}/{password}")]
        [Route("/[controller]/Password/{deviceId}/{password}")]
        public bool CheckPassword(string deviceId, string password)
        {
            Response.Headers.Add("X-noPerfTrack", "Server/Password/deviceId/password-GET");
            return GenericData.CheckPassword(deviceId, password);
        }


        [HttpGet]
        [Route("/[controller]/Login/{accountId}/{password}")]
        public AuthDataResponse Login(string accountId, string password)
        {
            Response.Headers.Add("X-noPerfTrack", "Server/Login/VARSREMOVED");
            var db = new PraxisContext();
            if (GenericData.CheckPassword(accountId, password))
            {
                Guid token = Guid.NewGuid();
                PraxisAuthentication.authTokens.TryRemove(accountId, out var ignore));
                PraxisAuthentication.authTokens.TryAdd(token.ToString(), new AuthData(accountId, token.ToString(), DateTime.UtcNow.AddSeconds(1800)));
                return new AuthDataResponse(token, 1800);
            }
            return null;
        }
    }
}

