using Microsoft.Analytics.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XGBoost;




namespace OpenAvalancheProject.Pipeline.Usql.Udos
{
    [SqlUserDefinedReducer]
    public class PredictReducer : IReducer
    {
        //public static string DownloadModelToTemp(string modelName)
        //{
        //    var localFileName = Path.GetTempPath() + modelName;
        //    using (var fileStream = File.Create(localFileName))
        //    {
        //        myBlob.Seek(0, SeekOrigin.Begin);
        //        myBlob.CopyTo(fileStream);
        //    }

        //    return localFileName;
        //}

        List<List<float>> cache = new List<List<float>>();

        public override IEnumerable<IRow> Reduce(IRowset input, IUpdatableRow output)
        {
            foreach (var row in input.Rows)
            {
                var rowList = new List<float>();
                rowList.Add(row.Get<float>("n_f_APCPsurface1HourForecast"));

                for(int i = 1; i < 341; i++)
                {
                    rowList.Add(0);
                }
                cache.Add(rowList);
            }

            //convert to array of floats
            float[][] values = new float[cache.Count][];
            for(int i = 0; i < cache.Count; i++)
            {
                values[i] = cache[i].ToArray();
            }
            
            //deploy resource command should place is in working dir
            string modelLocation = "ModelAboveV1.bin";
            var xgbc = BaseXgbModel.LoadClassifierFromFile(modelLocation);
            var predictions = xgbc.Predict(values);
           
            for(int i = 0; i < predictions.Length; i++)
            {
                output.Set<float>("prediction", predictions[i]);
                yield return output.AsReadOnly();
            }


            /*
            __fileDate AS Date,
        Lat,
        Lon,
        n_f_APCPsurface1HourForecast,
        n_f_WindSpeed10m1HourForecast AS n_f_10mWindSpeed1HourForecast,  
        n_f_APCPsurface2HourForecast,
        n_f_WindSpeed10m2HourForecast AS n_f_10mWindSpeed2HourForecast,
        n_f_APCPsurface3HourForecast,
        n_f_WindSpeed10m3HourForecast AS n_f_10mWindSpeed3HourForecast, 
        n_f_APCPsurface4HourForecast,
        n_f_WindSpeed10m4HourForecast AS n_f_10mWindSpeed4HourForecast,
        n_f_APCPsurface5HourForecast,
        n_f_WindSpeed10m5HourForecast AS n_f_10mWindSpeed5HourForecast,
        n_f_APCPsurface6HourForecast,
        n_f_WindSpeed10m6HourForecast AS n_f_10mWindSpeed6HourForecast,
        n_f_APCPsurface7HourForecast,
        n_f_WindSpeed10m7HourForecast AS n_f_10mWindSpeed7HourForecast,
        n_f_APCPsurface8HourForecast,
        n_f_WindSpeed10m8HourForecast AS n_f_10mWindSpeed8HourForecast,
        n_f_APCPsurface9HourForecast,
        n_f_WindSpeed10m9HourForecast AS n_f_10mWindSpeed9HourForecast,
        n_f_APCPsurface10HourForecast,
        n_f_WindSpeed10m10HourForecast AS n_f_10mWindSpeed10HourForecast,
        n_f_APCPsurface11HourForecast,
        n_f_WindSpeed10m11HourForecast AS n_f_10mWindSpeed11HourForecast,
        n_f_APCPsurface12HourForecast,
        n_f_WindSpeed10m12HourForecast AS n_f_10mWindSpeed12HourForecast,
        n_f_APCPsurface13HourForecast,
        n_f_WindSpeed10m13HourForecast AS n_f_10mWindSpeed13HourForecast,  
        n_f_APCPsurface14HourForecast,
        n_f_WindSpeed10m14HourForecast AS n_f_10mWindSpeed14HourForecast,
        n_f_APCPsurface15HourForecast,
        n_f_WindSpeed10m15HourForecast AS n_f_10mWindSpeed15HourForecast, 
        n_f_APCPsurface16HourForecast,
        n_f_WindSpeed10m16HourForecast AS n_f_10mWindSpeed16HourForecast,
        n_f_APCPsurface17HourForecast,
        n_f_WindSpeed10m17HourForecast AS n_f_10mWindSpeed17HourForecast,
        n_f_APCPsurface18HourForecast,
        n_f_WindSpeed10m18HourForecast AS n_f_10mWindSpeed18HourForecast,
        n_f_APCPsurface19HourForecast,
        n_f_WindSpeed10m19HourForecast AS n_f_10mWindSpeed19HourForecast,
        n_f_APCPsurface20HourForecast,
        n_f_WindSpeed10m20HourForecast AS n_f_10mWindSpeed20HourForecast,
        n_f_APCPsurface21HourForecast,
        n_f_WindSpeed10m21HourForecast AS n_f_10mWindSpeed21HourForecast,
        n_f_APCPsurface22HourForecast,
        n_f_WindSpeed10m22HourForecast AS n_f_10mWindSpeed22HourForecast,
        n_f_APCPsurface23HourForecast,
        n_f_WindSpeed10m23HourForecast AS n_f_10mWindSpeed23HourForecast,
        n_f_MaxTempSurfaceF AS n_f_tempMaxF,
        n_f_MaxWindSpeed10m AS n_f_10mWindSpeedMax,
        n_r_SnowDepthIn AS n_r_snowDepthIn,
        n_f_MinTempSurfaceF AS n_f_tempMinF,
        n_f_AvgTempSurfaceF AS n_f_tempAveF,
        n_f_WindSpeed10m0HourForecast AS n_f_10mWindSpeed,
        n_f_APCPsurface0HourForecast AS n_f_APCPsurface,
        n_r_PrecipincrementSnowIn AS n_r_precipIncrementSnowIn,
        n_r_Prev3DaySnowAccumulation AS n_r_Prev3daySnowAccumulation,
        [n_r_Prev7DaySnowAccumulation]
        AS n_r_Prev7daySnowAccumulation,
        [n_r_Prev3DayMaxTemp] AS n_r_Prev3dayMaxTemp,
        [n_r_Prev3DayMaxWindSpeed10m] AS n_r_Prev3DayMax10mWind,
        [n_r_Prev3DayMinTemp] AS n_r_Prev3dayMinTemp,
        [n_r_Prev7DayMaxTemp] AS n_r_Prev7dayMaxTemp,
        [n_r_Prev7DayMaxWindSpeed10m] AS n_r_Prev7DayMax10mWind,

        [n_r_Prev7DayMinTemp] AS n_r_Prev7dayMinTemp,

        [n_r_Prev1DayMaxTemp] AS n_r_Prev1dayMaxTemp,

        [n_r_Prev1DayMaxWindSpeed10m] AS n_r_Prev1DayMax10mWind,

        [n_r_Prev1DayMinTemp] AS n_r_Prev1dayMinTemp,

        [n_r_Prev1DayPrecip] AS n_r_Prev1DayPrecip,
        c_r_Prev3DayFreezeThawLikeliness,
        c_r_Prev7DayFreezeThawLikeliness,

        [c_r_Prev3DayWindSlabLikeliness] AS c_r_Prev3DayWindSlabLikeliness,

        [c_r_Prev7DayWindSlabLikeliness] AS c_r_Prev7DayWindSlabLikeliness,
        n_f_Next24HourChangeInTempFromPrev3DayMax,
        n_f_Next24HourChangeInTempFromPrev1DayMax AS n_f_Next24HoursChangeInTempFromPrev1DayMax,
        c_r_LongTermColdTemps AS c_f_LongTermColdTemps,
        n_r_Prev24HoursPrecipAsRainTotalIn,
        n_r_SNOWDAS_SnowDepth_mm,
        n_r_SNOWDAS_SWE_mm,
        n_r_SNOWDAS_SnowmeltRunoff_micromm,
        n_r_SNOWDAS_Sublimation_micromm,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem,
        n_r_SNOWDAS_SnowpackAveTemp_k,
        n_r_SnowDepthIn1InPast AS n_r_snowDepthIn1InPast,
        n_r_PrecipincrementSnowIn1InPast AS n_r_precipIncrementSnowIn1InPast,
        n_r_Prev3DaySnowAccumulation1InPast AS n_r_Prev3daySnowAccumulation1InPast,
        [n_r_Prev7DaySnowAccumulation1InPast] AS n_r_Prev7daySnowAccumulation1InPast,
        [n_r_Prev3DayMaxTemp1InPast] AS n_r_Prev3dayMaxTemp1InPast,
        [n_r_Prev3DayMaxWindSpeed10m1InPast] AS n_r_Prev3DayMax10mWind1InPast,
        [n_r_Prev3DayMinTemp1InPast] AS n_r_Prev3dayMinTemp1InPast,
        [n_r_Prev7DayMaxTemp1InPast] AS n_r_Prev7dayMaxTemp1InPast,
        [n_r_Prev7DayMaxWindSpeed10m1InPast] AS n_r_Prev7DayMax10mWind1InPast,

        [n_r_Prev7DayMinTemp1InPast] AS n_r_Prev7dayMinTemp1InPast,

        [n_r_Prev1DayMaxTemp1InPast] AS n_r_Prev1dayMaxTemp1InPast,

        [n_r_Prev1DayMaxWindSpeed10m1InPast] AS n_r_Prev1DayMax10mWind1InPast,

        [n_r_Prev1DayMinTemp1InPast] AS n_r_Prev1dayMinTemp1InPast,

        [n_r_Prev1DayPrecip1InPast] AS n_r_Prev1DayPrecip1InPast,
        c_r_Prev3DayFreezeThawLikeliness1InPast,
        c_r_Prev7DayFreezeThawLikeliness1InPast,

        [c_r_Prev3DayWindSlabLikeliness1InPast] AS c_r_Prev3DayWindSlabLikeliness1InPast,

        [c_r_Prev7DayWindSlabLikeliness1InPast] AS c_r_Prev7DayWindSlabLikeliness1InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn1InPast,
        n_r_SNOWDAS_SnowDepth_mm1InPast,
        n_r_SNOWDAS_SWE_mm1InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm1InPast,
        n_r_SNOWDAS_Sublimation_micromm1InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem1InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem1InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k1InPast,
        n_r_SnowDepthIn2InPast AS n_r_snowDepthIn2InPast,
        n_r_PrecipincrementSnowIn2InPast AS n_r_precipIncrementSnowIn2InPast,
        n_r_Prev3DaySnowAccumulation2InPast AS n_r_Prev3daySnowAccumulation2InPast,
        [n_r_Prev7DaySnowAccumulation2InPast] AS n_r_Prev7daySnowAccumulation2InPast,
        [n_r_Prev3DayMaxTemp2InPast] AS n_r_Prev3dayMaxTemp2InPast,
        [n_r_Prev3DayMaxWindSpeed10m2InPast] AS n_r_Prev3DayMax10mWind2InPast,
        [n_r_Prev3DayMinTemp2InPast] AS n_r_Prev3dayMinTemp2InPast,
        [n_r_Prev7DayMaxTemp2InPast] AS n_r_Prev7dayMaxTemp2InPast,
        [n_r_Prev7DayMaxWindSpeed10m2InPast] AS n_r_Prev7DayMax10mWind2InPast,

        [n_r_Prev7DayMinTemp2InPast] AS n_r_Prev7dayMinTemp2InPast,

        [n_r_Prev1DayMaxTemp2InPast] AS n_r_Prev1dayMaxTemp2InPast,

        [n_r_Prev1DayMaxWindSpeed10m2InPast] AS n_r_Prev1DayMax10mWind2InPast,

        [n_r_Prev1DayMinTemp2InPast] AS n_r_Prev1dayMinTemp2InPast,

        [n_r_Prev1DayPrecip2InPast] AS n_r_Prev1DayPrecip2InPast,
        c_r_Prev3DayFreezeThawLikeliness2InPast,
        c_r_Prev7DayFreezeThawLikeliness2InPast,

        [c_r_Prev3DayWindSlabLikeliness2InPast] AS c_r_Prev3DayWindSlabLikeliness2InPast,

        [c_r_Prev7DayWindSlabLikeliness2InPast] AS c_r_Prev7DayWindSlabLikeliness2InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn2InPast,
        n_r_SNOWDAS_SnowDepth_mm2InPast,
        n_r_SNOWDAS_SWE_mm2InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm2InPast,
        n_r_SNOWDAS_Sublimation_micromm2InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem2InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem2InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k2InPast,
        n_r_SnowDepthIn3InPast AS n_r_snowDepthIn3InPast,
        n_r_PrecipincrementSnowIn3InPast AS n_r_precipIncrementSnowIn3InPast,
        n_r_Prev3DaySnowAccumulation3InPast AS n_r_Prev3daySnowAccumulation3InPast,
        [n_r_Prev7DaySnowAccumulation3InPast] AS n_r_Prev7daySnowAccumulation3InPast,
        [n_r_Prev3DayMaxTemp3InPast] AS n_r_Prev3dayMaxTemp3InPast,
        [n_r_Prev3DayMaxWindSpeed10m3InPast] AS n_r_Prev3DayMax10mWind3InPast,
        [n_r_Prev3DayMinTemp3InPast] AS n_r_Prev3dayMinTemp3InPast,
        [n_r_Prev7DayMaxTemp3InPast] AS n_r_Prev7dayMaxTemp3InPast,
        [n_r_Prev7DayMaxWindSpeed10m3InPast] AS n_r_Prev7DayMax10mWind3InPast,

        [n_r_Prev7DayMinTemp3InPast] AS n_r_Prev7dayMinTemp3InPast,

        [n_r_Prev1DayMaxTemp3InPast] AS n_r_Prev1dayMaxTemp3InPast,

        [n_r_Prev1DayMaxWindSpeed10m3InPast] AS n_r_Prev1DayMax10mWind3InPast,

        [n_r_Prev1DayMinTemp3InPast] AS n_r_Prev1dayMinTemp3InPast,

        [n_r_Prev1DayPrecip3InPast] AS n_r_Prev1DayPrecip3InPast,
        c_r_Prev3DayFreezeThawLikeliness3InPast,
        c_r_Prev7DayFreezeThawLikeliness3InPast,

        [c_r_Prev3DayWindSlabLikeliness3InPast] AS c_r_Prev3DayWindSlabLikeliness3InPast,

        [c_r_Prev7DayWindSlabLikeliness3InPast] AS c_r_Prev7DayWindSlabLikeliness3InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn3InPast,
        n_r_SNOWDAS_SnowDepth_mm3InPast,
        n_r_SNOWDAS_SWE_mm3InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm3InPast,
        n_r_SNOWDAS_Sublimation_micromm3InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem3InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem3InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k3InPast,
        n_r_SnowDepthIn4InPast AS n_r_snowDepthIn4InPast,
        n_r_PrecipincrementSnowIn4InPast AS n_r_precipIncrementSnowIn4InPast,
        n_r_Prev3DaySnowAccumulation4InPast AS n_r_Prev3daySnowAccumulation4InPast,
        [n_r_Prev7DaySnowAccumulation4InPast] AS n_r_Prev7daySnowAccumulation4InPast,
        [n_r_Prev3DayMaxTemp4InPast] AS n_r_Prev3dayMaxTemp4InPast,
        [n_r_Prev3DayMaxWindSpeed10m4InPast] AS n_r_Prev3DayMax10mWind4InPast,
        [n_r_Prev3DayMinTemp4InPast] AS n_r_Prev3dayMinTemp4InPast,
        [n_r_Prev7DayMaxTemp4InPast] AS n_r_Prev7dayMaxTemp4InPast,
        [n_r_Prev7DayMaxWindSpeed10m4InPast] AS n_r_Prev7DayMax10mWind4InPast,

        [n_r_Prev7DayMinTemp4InPast] AS n_r_Prev7dayMinTemp4InPast,

        [n_r_Prev1DayMaxTemp4InPast] AS n_r_Prev1dayMaxTemp4InPast,

        [n_r_Prev1DayMaxWindSpeed10m4InPast] AS n_r_Prev1DayMax10mWind4InPast,

        [n_r_Prev1DayMinTemp4InPast] AS n_r_Prev1dayMinTemp4InPast,

        [n_r_Prev1DayPrecip4InPast] AS n_r_Prev1DayPrecip4InPast,
        c_r_Prev3DayFreezeThawLikeliness4InPast,
        c_r_Prev7DayFreezeThawLikeliness4InPast,

        [c_r_Prev3DayWindSlabLikeliness4InPast] AS c_r_Prev3DayWindSlabLikeliness4InPast,

        [c_r_Prev7DayWindSlabLikeliness4InPast] AS c_r_Prev7DayWindSlabLikeliness4InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn4InPast,
        n_r_SNOWDAS_SnowDepth_mm4InPast,
        n_r_SNOWDAS_SWE_mm4InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm4InPast,
        n_r_SNOWDAS_Sublimation_micromm4InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem4InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem4InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k4InPast,
        n_r_SnowDepthIn5InPast AS n_r_snowDepthIn5InPast,
        n_r_PrecipincrementSnowIn5InPast AS n_r_precipIncrementSnowIn5InPast,
        n_r_Prev3DaySnowAccumulation5InPast AS n_r_Prev3daySnowAccumulation5InPast,
        [n_r_Prev7DaySnowAccumulation5InPast] AS n_r_Prev7daySnowAccumulation5InPast,
        [n_r_Prev3DayMaxTemp5InPast] AS n_r_Prev3dayMaxTemp5InPast,
        [n_r_Prev3DayMaxWindSpeed10m5InPast] AS n_r_Prev3DayMax10mWind5InPast,
        [n_r_Prev3DayMinTemp5InPast] AS n_r_Prev3dayMinTemp5InPast,
        [n_r_Prev7DayMaxTemp5InPast] AS n_r_Prev7dayMaxTemp5InPast,
        [n_r_Prev7DayMaxWindSpeed10m5InPast] AS n_r_Prev7DayMax10mWind5InPast,

        [n_r_Prev7DayMinTemp5InPast] AS n_r_Prev7dayMinTemp5InPast,

        [n_r_Prev1DayMaxTemp5InPast] AS n_r_Prev1dayMaxTemp5InPast,

        [n_r_Prev1DayMaxWindSpeed10m5InPast] AS n_r_Prev1DayMax10mWind5InPast,

        [n_r_Prev1DayMinTemp5InPast] AS n_r_Prev1dayMinTemp5InPast,

        [n_r_Prev1DayPrecip5InPast] AS n_r_Prev1DayPrecip5InPast,
        c_r_Prev3DayFreezeThawLikeliness5InPast,
        c_r_Prev7DayFreezeThawLikeliness5InPast,

        [c_r_Prev3DayWindSlabLikeliness5InPast] AS c_r_Prev3DayWindSlabLikeliness5InPast,

        [c_r_Prev7DayWindSlabLikeliness5InPast] AS c_r_Prev7DayWindSlabLikeliness5InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn5InPast,
        n_r_SNOWDAS_SnowDepth_mm5InPast,
        n_r_SNOWDAS_SWE_mm5InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm5InPast,
        n_r_SNOWDAS_Sublimation_micromm5InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem5InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem5InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k5InPast,
        n_r_SnowDepthIn6InPast AS n_r_snowDepthIn6InPast,
        n_r_PrecipincrementSnowIn6InPast AS n_r_precipIncrementSnowIn6InPast,
        n_r_Prev3DaySnowAccumulation6InPast AS n_r_Prev3daySnowAccumulation6InPast,
        [n_r_Prev7DaySnowAccumulation6InPast] AS n_r_Prev7daySnowAccumulation6InPast,
        [n_r_Prev3DayMaxTemp6InPast] AS n_r_Prev3dayMaxTemp6InPast,
        [n_r_Prev3DayMaxWindSpeed10m6InPast] AS n_r_Prev3DayMax10mWind6InPast,
        [n_r_Prev3DayMinTemp6InPast] AS n_r_Prev3dayMinTemp6InPast,
        [n_r_Prev7DayMaxTemp6InPast] AS n_r_Prev7dayMaxTemp6InPast,
        [n_r_Prev7DayMaxWindSpeed10m6InPast] AS n_r_Prev7DayMax10mWind6InPast,

        [n_r_Prev7DayMinTemp6InPast] AS n_r_Prev7dayMinTemp6InPast,

        [n_r_Prev1DayMaxTemp6InPast] AS n_r_Prev1dayMaxTemp6InPast,

        [n_r_Prev1DayMaxWindSpeed10m6InPast] AS n_r_Prev1DayMax10mWind6InPast,

        [n_r_Prev1DayMinTemp6InPast] AS n_r_Prev1dayMinTemp6InPast,

        [n_r_Prev1DayPrecip6InPast] AS n_r_Prev1DayPrecip6InPast,
        c_r_Prev3DayFreezeThawLikeliness6InPast,
        c_r_Prev7DayFreezeThawLikeliness6InPast,

        [c_r_Prev3DayWindSlabLikeliness6InPast] AS c_r_Prev3DayWindSlabLikeliness6InPast,

        [c_r_Prev7DayWindSlabLikeliness6InPast] AS c_r_Prev7DayWindSlabLikeliness6InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn6InPast,
        n_r_SNOWDAS_SnowDepth_mm6InPast,
        n_r_SNOWDAS_SWE_mm6InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm6InPast,
        n_r_SNOWDAS_Sublimation_micromm6InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem6InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem6InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k6InPast,
        n_r_SnowDepthIn7InPast AS n_r_snowDepthIn7InPast,
        n_r_PrecipincrementSnowIn7InPast AS n_r_precipIncrementSnowIn7InPast,
        n_r_Prev3DaySnowAccumulation7InPast AS n_r_Prev3daySnowAccumulation7InPast,
        [n_r_Prev7DaySnowAccumulation7InPast] AS n_r_Prev7daySnowAccumulation7InPast,
        [n_r_Prev3DayMaxTemp7InPast] AS n_r_Prev3dayMaxTemp7InPast,
        [n_r_Prev3DayMaxWindSpeed10m7InPast] AS n_r_Prev3DayMax10mWind7InPast,
        [n_r_Prev3DayMinTemp7InPast] AS n_r_Prev3dayMinTemp7InPast,
        [n_r_Prev7DayMaxTemp7InPast] AS n_r_Prev7dayMaxTemp7InPast,
        [n_r_Prev7DayMaxWindSpeed10m7InPast] AS n_r_Prev7DayMax10mWind7InPast,

        [n_r_Prev7DayMinTemp7InPast] AS n_r_Prev7dayMinTemp7InPast,

        [n_r_Prev1DayMaxTemp7InPast] AS n_r_Prev1dayMaxTemp7InPast,

        [n_r_Prev1DayMaxWindSpeed10m7InPast] AS n_r_Prev1DayMax10mWind7InPast,

        [n_r_Prev1DayMinTemp7InPast] AS n_r_Prev1dayMinTemp7InPast,

        [n_r_Prev1DayPrecip7InPast] AS n_r_Prev1DayPrecip7InPast,
        c_r_Prev3DayFreezeThawLikeliness7InPast,
        c_r_Prev7DayFreezeThawLikeliness7InPast,

        [c_r_Prev3DayWindSlabLikeliness7InPast] AS c_r_Prev3DayWindSlabLikeliness7InPast,

        [c_r_Prev7DayWindSlabLikeliness7InPast] AS c_r_Prev7DayWindSlabLikeliness7InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn7InPast,
        n_r_SNOWDAS_SnowDepth_mm7InPast,
        n_r_SNOWDAS_SWE_mm7InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm7InPast,
        n_r_SNOWDAS_Sublimation_micromm7InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem7InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem7InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k7InPast,
        n_r_SnowDepthIn8InPast AS n_r_snowDepthIn8InPast,
        n_r_PrecipincrementSnowIn8InPast AS n_r_precipIncrementSnowIn8InPast,
        n_r_Prev3DaySnowAccumulation8InPast AS n_r_Prev3daySnowAccumulation8InPast,
        [n_r_Prev7DaySnowAccumulation8InPast] AS n_r_Prev7daySnowAccumulation8InPast,
        [n_r_Prev3DayMaxTemp8InPast] AS n_r_Prev3dayMaxTemp8InPast,
        [n_r_Prev3DayMaxWindSpeed10m8InPast] AS n_r_Prev3DayMax10mWind8InPast,
        [n_r_Prev3DayMinTemp8InPast] AS n_r_Prev3dayMinTemp8InPast,
        [n_r_Prev7DayMaxTemp8InPast] AS n_r_Prev7dayMaxTemp8InPast,
        [n_r_Prev7DayMaxWindSpeed10m8InPast] AS n_r_Prev7DayMax10mWind8InPast,

        [n_r_Prev7DayMinTemp8InPast] AS n_r_Prev7dayMinTemp8InPast,

        [n_r_Prev1DayMaxTemp8InPast] AS n_r_Prev1dayMaxTemp8InPast,

        [n_r_Prev1DayMaxWindSpeed10m8InPast] AS n_r_Prev1DayMax10mWind8InPast,

        [n_r_Prev1DayMinTemp8InPast] AS n_r_Prev1dayMinTemp8InPast,

        [n_r_Prev1DayPrecip8InPast] AS n_r_Prev1DayPrecip8InPast,
        c_r_Prev3DayFreezeThawLikeliness8InPast,
        c_r_Prev7DayFreezeThawLikeliness8InPast,

        [c_r_Prev3DayWindSlabLikeliness8InPast] AS c_r_Prev3DayWindSlabLikeliness8InPast,

        [c_r_Prev7DayWindSlabLikeliness8InPast] AS c_r_Prev7DayWindSlabLikeliness8InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn8InPast,
        n_r_SNOWDAS_SnowDepth_mm8InPast,
        n_r_SNOWDAS_SWE_mm8InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm8InPast,
        n_r_SNOWDAS_Sublimation_micromm8InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem8InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem8InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k8InPast,
        n_r_SnowDepthIn9InPast AS n_r_snowDepthIn9InPast,
        n_r_PrecipincrementSnowIn9InPast AS n_r_precipIncrementSnowIn9InPast,
        n_r_Prev3DaySnowAccumulation9InPast AS n_r_Prev3daySnowAccumulation9InPast,
        [n_r_Prev7DaySnowAccumulation9InPast] AS n_r_Prev7daySnowAccumulation9InPast,
        [n_r_Prev3DayMaxTemp9InPast] AS n_r_Prev3dayMaxTemp9InPast,
        [n_r_Prev3DayMaxWindSpeed10m9InPast] AS n_r_Prev3DayMax10mWind9InPast,
        [n_r_Prev3DayMinTemp9InPast] AS n_r_Prev3dayMinTemp9InPast,
        [n_r_Prev7DayMaxTemp9InPast] AS n_r_Prev7dayMaxTemp9InPast,
        [n_r_Prev7DayMaxWindSpeed10m9InPast] AS n_r_Prev7DayMax10mWind9InPast,

        [n_r_Prev7DayMinTemp9InPast] AS n_r_Prev7dayMinTemp9InPast,

        [n_r_Prev1DayMaxTemp9InPast] AS n_r_Prev1dayMaxTemp9InPast,

        [n_r_Prev1DayMaxWindSpeed10m9InPast] AS n_r_Prev1DayMax10mWind9InPast,

        [n_r_Prev1DayMinTemp9InPast] AS n_r_Prev1dayMinTemp9InPast,

        [n_r_Prev1DayPrecip9InPast] AS n_r_Prev1DayPrecip9InPast,
        c_r_Prev3DayFreezeThawLikeliness9InPast,
        c_r_Prev7DayFreezeThawLikeliness9InPast,

        [c_r_Prev3DayWindSlabLikeliness9InPast] AS c_r_Prev3DayWindSlabLikeliness9InPast,

        [c_r_Prev7DayWindSlabLikeliness9InPast] AS c_r_Prev7DayWindSlabLikeliness9InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn9InPast,
        n_r_SNOWDAS_SnowDepth_mm9InPast,
        n_r_SNOWDAS_SWE_mm9InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm9InPast,
        n_r_SNOWDAS_Sublimation_micromm9InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem9InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem9InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k9InPast,
        n_r_SnowDepthIn10InPast AS n_r_snowDepthIn10InPast,
        n_r_PrecipincrementSnowIn10InPast AS n_r_precipIncrementSnowIn10InPast,
        n_r_Prev3DaySnowAccumulation10InPast AS n_r_Prev3daySnowAccumulation10InPast,
        [n_r_Prev7DaySnowAccumulation10InPast] AS n_r_Prev7daySnowAccumulation10InPast,
        [n_r_Prev3DayMaxTemp10InPast] AS n_r_Prev3dayMaxTemp10InPast,
        [n_r_Prev3DayMaxWindSpeed10m10InPast] AS n_r_Prev3DayMax10mWind10InPast,
        [n_r_Prev3DayMinTemp10InPast] AS n_r_Prev3dayMinTemp10InPast,
        [n_r_Prev7DayMaxTemp10InPast] AS n_r_Prev7dayMaxTemp10InPast,
        [n_r_Prev7DayMaxWindSpeed10m10InPast] AS n_r_Prev7DayMax10mWind10InPast,

        [n_r_Prev7DayMinTemp10InPast] AS n_r_Prev7dayMinTemp10InPast,

        [n_r_Prev1DayMaxTemp10InPast] AS n_r_Prev1dayMaxTemp10InPast,

        [n_r_Prev1DayMaxWindSpeed10m10InPast] AS n_r_Prev1DayMax10mWind10InPast,

        [n_r_Prev1DayMinTemp10InPast] AS n_r_Prev1dayMinTemp10InPast,

        [n_r_Prev1DayPrecip10InPast] AS n_r_Prev1DayPrecip10InPast,
        c_r_Prev3DayFreezeThawLikeliness10InPast,
        c_r_Prev7DayFreezeThawLikeliness10InPast,

        [c_r_Prev3DayWindSlabLikeliness10InPast] AS c_r_Prev3DayWindSlabLikeliness10InPast,

        [c_r_Prev7DayWindSlabLikeliness10InPast] AS c_r_Prev7DayWindSlabLikeliness10InPast,
        n_r_Prev24HoursPrecipAsRainTotalIn10InPast,
        n_r_SNOWDAS_SnowDepth_mm10InPast,
        n_r_SNOWDAS_SWE_mm10InPast,
        n_r_SNOWDAS_SnowmeltRunoff_micromm10InPast,
        n_r_SNOWDAS_Sublimation_micromm10InPast,
        n_r_SNOWDAS_SolidPrecip_kgpersquarem10InPast,
        n_r_SNOWDAS_LiquidPrecip_kgpersquarem10InPast,
        n_r_SNOWDAS_SnowpackAveTemp_k10InPast
            */

        }

    }
    //internal class PredictionRow
    //{
    //    public List<double> Fields { get; set; }
    //}
}
