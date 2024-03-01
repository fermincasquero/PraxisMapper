﻿using System.Collections.Generic;
using static PraxisCore.DbTables;

namespace PraxisCore.Styles
{
    /// <summary>
    /// A condensed version of suggestedGameplay, meant to only be high-quality areas to play games at. 
    /// </summary>
    public static class suggestedmini
    {
        //This 
        public static List<StyleEntry> style = new List<StyleEntry>()
        {
            new StyleEntry() { IsGameElement = true, MatchOrder = 1, Name ="park", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {

                    new StylePaint() { HtmlColorCode = "CC93FF61", FillOrStroke = "fill", LineWidthDegrees=0.0000625F, LinePattern= "solid", LayerId = 100 },
                    new StylePaint() { HtmlColorCode = "CCB2FF8F", FillOrStroke = "stroke", LineWidthDegrees=0.0001875F, LinePattern= "solid", LayerId = 99 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "leisure", Value = "park", MatchType = "or" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 2, Name ="university", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "CCFFFFE5", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 },
                    new StylePaint() { HtmlColorCode = "CCF5EED3", FillOrStroke = "stroke", LineWidthDegrees=0.0001875F, LinePattern= "solid", LayerId = 99 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "university|college", MatchType = "any" }}
            },
            new StyleEntry() { IsGameElement = true, MatchOrder = 3, Name ="natureReserve", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "CC124504", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 },
                    new StylePaint() { HtmlColorCode = "CC027021", FillOrStroke = "stroke", LineWidthDegrees=0.0001875F, LinePattern= "solid", LayerId = 99 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "leisure", Value = "nature_reserve", MatchType = "equals" }
            } },
            new StyleEntry() {IsGameElement = true, MatchOrder = 4, Name ="cemetery", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "CCAACBAF", FillOrStroke = "fill", FileName="Landuse-cemetery.png", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 },
                    new StylePaint() { HtmlColorCode = "CC404040", FillOrStroke = "stroke", LineWidthDegrees=0.0001875F, LinePattern= "solid", LayerId = 99 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "landuse", Value = "cemetery", MatchType = "or" },
                    new StyleMatchRule() {Key="amenity", Value="grave_yard", MatchType="or" } 
            } },
            new StyleEntry() { IsGameElement = true, MatchOrder = 5, Name ="historical", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "CCB3B3B3", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 },
                    new StylePaint() { HtmlColorCode = "CC9D9D9D", FillOrStroke = "stroke", LineWidthDegrees=0.0001875F, LinePattern= "solid", LayerId = 99 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "historic", Value = "*", MatchType = "equals" }}
            },
            new StyleEntry() { IsGameElement = true, MatchOrder = 6, Name ="theatre", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "theatre", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 7, Name ="concert hall", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "concert hall", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 8, Name ="arts centre", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "arts_centre", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 9, Name ="planetarium", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "planetarium", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 10, Name ="artsCulture", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "planetarium", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 11, Name ="library", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "library", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 12, Name ="public bookcase", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "public_bookcase", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 13, Name ="community centre", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "community_centre", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 14, Name ="conference centre", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "conference_centre", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 15, Name ="exhibition centre", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "exhibition_centre", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 16, Name ="events_venue", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "amenity", Value = "events_venue", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 17, Name ="aquarium", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "aquarium", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 18, Name ="artwork", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "artwork", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 19, Name ="attraction", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "attraction", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 20, Name ="gallery", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "gallery", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 21, Name ="museum", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "museum", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 22, Name ="theme_park", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "theme_park", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 23, Name ="viewpoint", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "viewpoint", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 24, Name ="zoo", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "3B3B3B", FillOrStroke = "fill", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "tourism", Value = "zoo", MatchType = "equal" },
            }},
            new StyleEntry() { IsGameElement = true, MatchOrder = 7000, Name ="serverGenerated", StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "CC76E3E1", FillOrStroke = "fill", LineWidthDegrees=0.0000625F, LinePattern= "solid", LayerId = 100 },
                    new StylePaint() { HtmlColorCode = "CC5CB5B4", FillOrStroke = "stroke", LineWidthDegrees=0.0001875F, LinePattern= "solid", LayerId = 99 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() {Key="suggstedmini", Value="generated", MatchType="equals"},
            }},
            //background is a mandatory style entry name, but its transparent here..
            new StyleEntry() { MatchOrder = 10000, Name ="background",  StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "00000000", FillOrStroke = "fill", LineWidthDegrees=0.00000625F, LinePattern= "solid", LayerId = 101 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() {Key = "bg", Value = "bg", MatchType = "equals"}, //this one only gets called by name anyways.
            }},
            //this name needs to exist because of the default style using this name.
            new StyleEntry() { MatchOrder = 10001, Name ="unmatched",  StyleSet = "suggestedmini",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "00000000", FillOrStroke = "fill", LineWidthDegrees=0.00000625F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() { Key = "a", Value = "s", MatchType = "equals" }}
            },
        };
    }
}
