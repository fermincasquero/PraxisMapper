﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PraxisMapper.Controllers
{
    public class ScavengerHuntController : Controller
    {
        //Scavenger Hunt Mode
        //walk around to specific places, fill them in.
        //can toggle between 'go to places in order' and 'visit all places in any order'
        //This is obviously a work in progress, since this should be an API controller and needs some DB tables and etc.
        public IActionResult Index()
        {
            return View();
        }
    }
}
