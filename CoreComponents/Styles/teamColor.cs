﻿using System.Collections.Generic;
using static PraxisCore.DbTables;

namespace PraxisCore.Styles
{
    /// <summary>
    /// Style with 3 default team colors for use in team-based games. Use GetPaintOpsForPlacesData to load team info from PlaceGameData instead of Tags.
    /// </summary>
    public static class teamColor
    {
        //teamColor: 3 predefined styles to allow for Red(1), Green(2) and Blue(3) teams in a game. Set a tag to the color's ID and then call a DrawCustomX function with this style.
        public static List<StyleEntry> style = new List<StyleEntry>()
        {           
            new StyleEntry() { MatchOrder = 1, Name ="1",  StyleSet = "teamColor",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "88FF0000", FillOrStroke = "fill", LineWidthDegrees=0.00000625F, LinePattern= "solid", LayerId = 99 },
                    new StylePaint() { HtmlColorCode = "FF0000", FillOrStroke = "stroke", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() {Key = "team", Value = "red", MatchType = "equals"},
            }},
            new StyleEntry() { MatchOrder = 2, Name ="2",   StyleSet = "teamColor",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "8800FF00", FillOrStroke = "fill", LineWidthDegrees=0.00000625F, LinePattern= "solid", LayerId = 99 },
                    new StylePaint() { HtmlColorCode = "00FF00", FillOrStroke = "stroke", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() {Key = "team", Value = "green", MatchType = "equals"},
            }},
            new StyleEntry() { MatchOrder = 3, Name ="3",  StyleSet = "teamColor",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "880000FF", FillOrStroke = "fill", LineWidthDegrees=0.00000625F, LinePattern= "solid", LayerId = 99 },
                    new StylePaint() { HtmlColorCode = "0000FF", FillOrStroke = "stroke", LineWidthDegrees=0.0000125F, LinePattern= "solid", LayerId = 100 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() {Key = "team", Value = "blue", MatchType = "equals"},
            }},

            //background is a mandatory style entry name, but its transparent here..
            new StyleEntry() { MatchOrder = 10000, Name ="background",  StyleSet = "teamColor",
                PaintOperations = new List<StylePaint>() {
                    new StylePaint() { HtmlColorCode = "00000000", FillOrStroke = "fill", LineWidthDegrees=0.00000625F, LinePattern= "solid", LayerId = 101 }
                },
                StyleMatchRules = new List<StyleMatchRule>() {
                    new StyleMatchRule() {Key = "*", Value = "*", MatchType = "default"},
            }},
        };
    }
}
