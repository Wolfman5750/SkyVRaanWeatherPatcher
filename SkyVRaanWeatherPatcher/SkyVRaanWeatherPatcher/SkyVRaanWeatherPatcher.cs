using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Collections.Generic;
using Noggog;

namespace SkyVRaanWaterWeatherPatcher
{
    public class SkyVRaanWeatherPatcher
    {
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                new UserPreferences()
                {
                    AddImplicitMasters = false,
                    //IncludeDisabledMods = true,
                    ActionsForEmptyArgs = new RunDefaultPatcher
                    {
                        
                        IdentifyingModKey = "SkyVRaanWeatherPatcher.esp",
                        TargetRelease = GameRelease.SkyrimSE
                    }
                }
            );
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var WTHRCounter = 0;

            //The Following factors control the brightness and saturation for each weather type at each time of day.
            //                             sunrise  day   sunset    night

            var BaseSatAdjust = new[]      { 1.0,   1.0,    1.0,     3.0 }; //0.8
            var BaseBrightAdjust = new[]   { 1.0,   1.0,    1.0,     0.5 }; //1.3

            var CloudySatAdjust = new[]    { 1.0,   1.0,    1.0,    1.0 };
            var CloudyBrightAdjust = new[] { 1.0,   1.0,    1.0,    0.5 };

            var RainySatAdjust = new[]     { 1.0,   1.0,    1.0,    1.0 };
            var RainyBrightAdjust = new[]  { 1.0,   1.0,    1.0,    0.5 };

            var SnowSatAdjust = new[]       { 1.0,  1.0,    1.0,    1.0 };
            var SnowBrightAdjust = new[]    { 1.0,  1.0,    1.0,    0.5 };


            //The following parameters allow for blending of different sky colors in different weather types at the different times of day.
            //These factors are meant to be a weighted average, and should add up to 1.0 for each time of day.
            //                             sunrise  day   sunset    night
            var SkyBlendLower = new[]      { 0.0,   0.5,    0.0,    0.0 }; //Weight of Lower Sky Color
            var SkyBlendUpper = new[]      { 0.0,   0.5,    0.0,    1.0 }; //Weight of Upper Sky Color
            var SkyBlendHoriz = new[]      { 1.0,   0.0,    1.0,    0.0 }; //Weight of Horizon Color
            var SkyBlendCloud28 = new[]    { 0.0,   0.0,    0.0,    0.0 }; //Weight of Cloud Layer 28 Color
            var SkyBlendExistMult = new[]  { 0.0,   0.0,    0.0,    0.0 }; //Weight of existing Water Multiplier

            //                                  sunrise  day   sunset    night
            var SkyBlendLowerRainy = new[]      { 0.0,   0.5,   0.0,     0.0 };
            var SkyBlendUpperRainy = new[]      { 0.0,   0.5,   0.0,     1.0 };
            var SkyBlendHorizRainy = new[]      { 1.0,   0.0,   1.0,     0.0 };
            var SkyBlendCloud28Rainy = new[]    { 0.0,   0.0,   0.0,     0.0 };
            var SkyBlendExistMultRainy = new[]  { 0.0,   0.0,   0.0,     0.0 };

            //                                  sunrise  day   sunset    night
            var SkyBlendLowerCloudy = new[]     { 0.0,   0.5,    0.0,    0.0 };
            var SkyBlendUpperCloudy = new[]     { 1.0,   0.5,    1.0,    1.0 };
            var SkyBlendHorizCloudy = new[]     { 0.0,   0.0,    0.0,    0.0 };
            var SkyBlendCloud28Cloudy = new[]   { 0.0,   0.0,    0.0,    0.0 };
            var SkyBlendExistMultCloudy = new[] { 0.0,   0.0,    0.0,    0.0 };

            //                                  sunrise  day   sunset    night
            var SkyBlendLowerSnow = new[]       { 0.0,   0.5,    0.0,    0.0 };
            var SkyBlendUpperSnow = new[]       { 1.0,   0.5,    1.0,    1.0 };
            var SkyBlendHorizSnow = new[]       { 0.0,   0.0,    0.0,    0.0 };
            var SkyBlendCloud28Snow = new[]     { 0.0,   0.0,    0.0,    0.0 };
            var SkyBlendExistMultSnow = new[]   { 0.0,   0.0,    0.0,    0.0 };

            var ColorShift = new[]
            {
                new[]                      { 0,     0,      0,      0 }, //R
                new[]                      { 0,     0,      0,      0 }, //G
                new[]                      { 0,     0,      0,      0 }  //B
            };

            var WeatherBlacklist = new HashSet<FormKey>
            {
                //*********************************************************
                //** Formkeys included in this blacklist will be skipped **
                //*********************************************************
                Skyrim.Weather.DefaultWeather,
                Skyrim.Weather.FXMagicStormRain,
                Skyrim.Weather.FXSkyrimStormBlowingGrass,
                Skyrim.Weather.FXWthrCaveBluePaleLight,
                Skyrim.Weather.FXWthrCaveBlueSkylight,
                Skyrim.Weather.FXWthrInvertDayNight,
                Skyrim.Weather.FXWthrInvertDayNighWarm,
                Skyrim.Weather.FXWthrInvertLightMarkarth,
                Skyrim.Weather.FXWthrInvertLightsSolitude,
                Skyrim.Weather.FXWthrInvertLightsWhiterun,
                Skyrim.Weather.FXWthrInvertWindowsWhiterun,
                Skyrim.Weather.FXWthrInvertWindowsWindhelm,
                Skyrim.Weather.FXWthrInvertWindowsWindhelm2,
                Skyrim.Weather.FXWthrInvertWindowsWinterhold,
                Skyrim.Weather.FXWthrSunlight,
                Skyrim.Weather.FXWthrSunlightWhite,
                Skyrim.Weather.TESTCloudyRain,
                Skyrim.Weather.SkyrimMQ206weather,
                Skyrim.Weather.SkyrimDA02Weather,
                Skyrim.Weather.EditorCloudPreview,
                Skyrim.Weather.WorldMapWeather
            };

            Console.WriteLine($"Patching Weather ...");

            foreach (var WTHRContext in state.LoadOrder.PriorityOrder.Weather().WinningContextOverrides(state.LinkCache))
            {
                if (!WeatherBlacklist.Contains(WTHRContext.Record.FormKey))
                {
                    var WTHROverride = WTHRContext.GetOrAddAsOverride(state.PatchMod);

                    WTHRCounter++;
                    foreach (var WhatTimeIsIt in EnumExt.GetValues<TimeOfDay>())
                    {

                        System.Drawing.Color LwrSkyCol = WTHRContext.Record.SkyLowerColor[WhatTimeIsIt];
                        System.Drawing.Color UpprSkyCol = WTHRContext.Record.SkyUpperColor[WhatTimeIsIt];
                        System.Drawing.Color HorizCol = WTHRContext.Record.HorizonColor[WhatTimeIsIt];
                        System.Drawing.Color WtrMultCol = WTHRContext.Record.WaterMultiplierColor[WhatTimeIsIt];
                        ICloudLayerGetter CldLyr28 = WTHRContext.Record.Clouds[28];

                        var CalcRGB = new[] { 0, 0, 0 }; //0=R 1=G 2=B

                        if (WTHRContext.Record.Flags.HasFlag(Weather.Flag.Rainy))
                        {

                            CalcRGB[0] = Convert.ToInt32(SkyBlendLowerRainy[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.R +
                                                         SkyBlendUpperRainy[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.R +
                                                         SkyBlendExistMultRainy[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.R +
                                                         SkyBlendHorizRainy[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.R +
                                                         SkyBlendCloud28Rainy[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].R +
                                                         ColorShift[0][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[1] = Convert.ToInt32(SkyBlendLowerRainy[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.G +
                                                        SkyBlendUpperRainy[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.G +
                                                        SkyBlendHorizRainy[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.G +
                                                        SkyBlendExistMultRainy[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.G +
                                                        SkyBlendCloud28Rainy[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].G +
                                                        ColorShift[1][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[2] = Convert.ToInt32(SkyBlendLowerRainy[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.B +
                                                         SkyBlendUpperRainy[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.B +
                                                         SkyBlendHorizRainy[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.B +
                                                         SkyBlendExistMultRainy[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.B +
                                                         SkyBlendCloud28Rainy[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].B +
                                                         ColorShift[2][Convert.ToInt32(WhatTimeIsIt)]);


                        }
                        else if (WTHRContext.Record.Flags.HasFlag(Weather.Flag.Cloudy))
                        {
                            CalcRGB[0] = Convert.ToInt32(SkyBlendLowerCloudy[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.R +
                                                         SkyBlendUpperCloudy[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.R +
                                                         SkyBlendHorizCloudy[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.R +
                                                         SkyBlendExistMultCloudy[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.R +
                                                         SkyBlendCloud28Cloudy[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].R +
                                                         ColorShift[0][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[1] = Convert.ToInt32(SkyBlendLowerCloudy[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.G +
                                                        SkyBlendUpperCloudy[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.G +
                                                        SkyBlendHorizCloudy[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.G +
                                                        SkyBlendExistMultCloudy[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.G +
                                                        SkyBlendCloud28Cloudy[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].G +
                                                        ColorShift[1][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[2] = Convert.ToInt32(SkyBlendLowerCloudy[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.B +
                                                         SkyBlendUpperCloudy[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.B +
                                                         SkyBlendHorizCloudy[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.B +
                                                         SkyBlendExistMultCloudy[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.B +
                                                         SkyBlendCloud28Cloudy[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].B +
                                                         ColorShift[2][Convert.ToInt32(WhatTimeIsIt)]);
                        }

                        else if (WTHRContext.Record.Flags.HasFlag(Weather.Flag.Snow))
                        {
                            CalcRGB[0] = Convert.ToInt32(SkyBlendLowerSnow[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.R +
                                                         SkyBlendUpperSnow[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.R +
                                                         SkyBlendHorizSnow[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.R +
                                                         SkyBlendExistMultSnow[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.R +
                                                         SkyBlendCloud28Snow[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].R +
                                                         ColorShift[0][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[1] = Convert.ToInt32(SkyBlendLowerSnow[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.G +
                                                        SkyBlendUpperSnow[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.G + 
                                                        SkyBlendHorizSnow[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.G +
                                                        SkyBlendExistMultSnow[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.G +
                                                        SkyBlendCloud28Snow[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].G +
                                                        ColorShift[1][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[2] = Convert.ToInt32(SkyBlendLowerSnow[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.B +
                                                         SkyBlendUpperSnow[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.B + 
                                                         SkyBlendHorizSnow[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.B +
                                                         SkyBlendExistMultSnow[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.B +
                                                         SkyBlendCloud28Snow[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].B +
                                                         ColorShift[2][Convert.ToInt32(WhatTimeIsIt)]);
                        }
                        else
                        {

                            CalcRGB[0] = Convert.ToInt32(SkyBlendLower[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.R +
                                                         SkyBlendUpper[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.R + 
                                                         SkyBlendHoriz[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.R +
                                                         SkyBlendExistMult[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.R +
                                                         SkyBlendCloud28[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].R +
                                                         ColorShift[0][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[1] = Convert.ToInt32(SkyBlendLower[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.G +
                                                        SkyBlendUpper[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.G + 
                                                        SkyBlendHoriz[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.G +
                                                        SkyBlendExistMult[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.G +
                                                        SkyBlendCloud28[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].G +
                                                        ColorShift[1][Convert.ToInt32(WhatTimeIsIt)]);

                            CalcRGB[2] = Convert.ToInt32(SkyBlendLower[Convert.ToInt32(WhatTimeIsIt)] * LwrSkyCol.B +
                                                         SkyBlendUpper[Convert.ToInt32(WhatTimeIsIt)] * UpprSkyCol.B + 
                                                         SkyBlendHoriz[Convert.ToInt32(WhatTimeIsIt)] * HorizCol.B +
                                                         SkyBlendExistMult[Convert.ToInt32(WhatTimeIsIt)] * WtrMultCol.B +
                                                         SkyBlendCloud28[Convert.ToInt32(WhatTimeIsIt)] * CldLyr28.Colors[WhatTimeIsIt].B +
                                                         ColorShift[2][Convert.ToInt32(WhatTimeIsIt)]);
                        }

                        if (CalcRGB[0] + CalcRGB[1] + CalcRGB[2] > 0)
                        {

                            //Adjust Saturation
                            var averageRGB = Convert.ToInt32((CalcRGB[0] + CalcRGB[1] + CalcRGB[2]) / 3);
                            var SatAdjust = BaseSatAdjust[Convert.ToInt32(WhatTimeIsIt)];
                            var BrightAdjust = BaseBrightAdjust[Convert.ToInt32(WhatTimeIsIt)];

                            if (WTHRContext.Record.Flags.HasFlag(Weather.Flag.Rainy))
                            {
                                SatAdjust = RainySatAdjust[Convert.ToInt32(WhatTimeIsIt)];
                                BrightAdjust = RainyBrightAdjust[Convert.ToInt32(WhatTimeIsIt)];
                            }
                            else if (WTHRContext.Record.Flags.HasFlag(Weather.Flag.Cloudy))
                            {
                                SatAdjust = CloudySatAdjust[Convert.ToInt32(WhatTimeIsIt)];
                                BrightAdjust = CloudyBrightAdjust[Convert.ToInt32(WhatTimeIsIt)];
                            }
                            else if (WTHRContext.Record.Flags.HasFlag(Weather.Flag.Snow))
                            {
                                SatAdjust = SnowSatAdjust[Convert.ToInt32(WhatTimeIsIt)];
                                BrightAdjust = SnowBrightAdjust[Convert.ToInt32(WhatTimeIsIt)];
                            }

                            //if (WTHROverride.EditorID == "SkyrimClear")
                            //{
                            //    Console.WriteLine("Clear");
                            //}

                            CalcRGB[0] = Math.Max(Convert.ToInt32(averageRGB + (CalcRGB[0] - averageRGB) * SatAdjust),1);
                            CalcRGB[1] = Math.Max(Convert.ToInt32(averageRGB + (CalcRGB[1] - averageRGB) * SatAdjust),1);
                            CalcRGB[2] = Math.Max(Convert.ToInt32(averageRGB + (CalcRGB[2] - averageRGB) * SatAdjust),1);

                            CalcRGB[0] = Convert.ToInt32(CalcRGB[0] * BrightAdjust);
                            CalcRGB[1] = Convert.ToInt32(CalcRGB[1] * BrightAdjust);
                            CalcRGB[2] = Convert.ToInt32(CalcRGB[2] * BrightAdjust);

                            if(CalcRGB[0] > 255| CalcRGB[0] > 255| CalcRGB[0] > 255)
                            {
                                var MAXRGB = MathExt.Max(Convert.ToInt32(CalcRGB[0]),
                                 Convert.ToInt32(CalcRGB[1]),
                                 Convert.ToInt32(CalcRGB[2]));

                                CalcRGB[0] = Convert.ToInt32(CalcRGB[0] * 255 / MAXRGB);
                                CalcRGB[1] = Convert.ToInt32(CalcRGB[1] * 255 / MAXRGB);
                                CalcRGB[2] = Convert.ToInt32(CalcRGB[2] * 255 / MAXRGB);

                            }

                            WTHROverride.WaterMultiplierColor[WhatTimeIsIt] = System.Drawing.Color.FromArgb(0, CalcRGB[0], CalcRGB[1], CalcRGB[2]);

                            Console.WriteLine($"Weather {WTHROverride.EditorID} Water Multiplier for {WhatTimeIsIt} changed from {WtrMultCol} to {WTHROverride.WaterMultiplierColor[WhatTimeIsIt]}");
                        }
                    };
                }
            }


            Console.WriteLine();
            Console.WriteLine($"Patched {WTHRCounter} Weather records");
        }
    

    
    }
}
