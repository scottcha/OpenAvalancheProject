import numpy as np
import pandas as pd
import re
import sys
import math
import datetime
import fiona
import shapely
import shapely.geometry
from keras.preprocessing.sequence import pad_sequences
from sklearn.preprocessing import MinMaxScaler, Normalizer

class continue_loop(Exception):
    pass

class DataPrep:
    date_label = 'Date' # for labels, __fileDate for the series

    lat_lon_cache = None
    
    def label_precice_training(self, row):
        with fiona.open('../Data/AvyTrainingRegionsPrecise/AvyTrainingRegionsPrecise.shp') as avy_regions:        
            for region in avy_regions:
                shape = shapely.geometry.asShape(region['geometry'])
                point = shapely.geometry.Point(row['Lon'], row['Lat'])
                
                if shape.contains(point):
                    return True
            return False

    def find_nearest_forecast_point(self, row):
        if(self.lat_lon_cache is None):
            self.lat_lon_cache = pd.read_csv('../Data/LatLonCache.csv')

        tmp_lat_lon_cache = self.lat_lon_cache.copy()
    
        #TODO: currently a simple esitmate for converting lon to miles, 53 degrees per mile--can do better
        tmp_lat_lon_cache['distance'] = np.sqrt(np.square(69.0*(tmp_lat_lon_cache['Lat']-row['Lat'])) + np.square(53.0*(tmp_lat_lon_cache['Lon']-row['Lon'])))
    
        nearest_row = tmp_lat_lon_cache.sort_values(by=['distance']).iloc[0, :]
        
        #nearest point needs to be within 20 miles
        if nearest_row['distance'] > 20.0:
            #should never happen since our grid is smaller but just in case
            return -1, -1
        else:
            return nearest_row['Lat'], nearest_row['Lon']
    
    def is_critical_point(self, row, critical_points):
        for cp in critical_points:
            if(round(row['Lat'], 3) == round(cp[0], 3) and round(row['Lon'], 3) == round(cp[1], 3)):
                return True
        return False

    def day_of_season(self, row, date_column_name):
        if(row[date_column_name].month < 6):
            #month is in first part of following year
            return pd.Timedelta(row[date_column_name] - datetime.datetime(row[date_column_name].year-1, 11, 1)).days
        else:
            return pd.Timedelta(row[date_column_name] - datetime.datetime(row[date_column_name].year, 11, 1)).days

    def identify_season(self, row):
        year = row[self.date_label].year
        month = row[self.date_label].month
        if year == 2013 or (year == 2014 and month < 5):
            return '13-14'
        elif (year == 2014 and month > 10) or (year == 2015 and month < 5):
            return '14-15'
        elif (year == 2015 and month > 10) or (year == 2016 and month < 5):
            return '15-16'
        elif (year == 2016 and month > 10) or (year == 2017 and month < 5):
            return '16-17'
        elif (year == 2017 and month > 10) or (year == 2018 and month < 5):
            return '17-18'
        else:
            return 'unknown season'

    def filter_snowpack_type(self, snowpack_type, df):
        if snowpack_type == 'coastal':
            df = df[df['c_IsCoastalSnowpack'] == 1]
        elif snowpack_type == 'contenental':
            df = df[df['c_IsContenentalSnowpack'] == 1]
        return df

    def create_date_features(self, name_of_date_col, df):
        df['DayOfYear'] = df[name_of_date_col].dt.dayofyear
        #number of days since nov 1
        df['DayOfSeason'] = df.apply(self.day_of_season, args=(name_of_date_col,), axis=1)
        return df
    
    def clean_missing_values(self, interpolate_missing, df, fill_na_after_interpolate = False, inplace=False):
        #clean mising values
        mappingMissingValues = {-9999:np.nan}
        if interpolate_missing:
            df = df.replace(mappingMissingValues)
            filter = df[['Season', 'Lat', 'Lon']].drop_duplicates()

            listOfFrames = []
            for f in filter.iterrows():
                listOfFrames.append(df[(df['Season'] == f[1]['Season']) & (df['Lat'] == f[1]['Lat']) & (df['Lon'] == f[1]['Lon'])].interpolate(method='linear', axis=0).ffill().bfill())

            interpolated = pd.concat(listOfFrames)

            #fill in any remaining nan with 0 as that datapoint was probably missing for a long
            #period of time (all of the sesason for the lat/lon)
            if fill_na_after_interpolate:
                interpolated.fillna(0, inplace=True) 

            if(inplace):
                df = interpolated
                return df
            else:
                return interpolated
        else:
            return df

    def ignore_extreme(self, ignore_extreme, df):
        if ignore_extreme:
            df = df[df['o_Day1DangerAboveTreeline']!='Extreme']
            df = df[df['o_Day1DangerNearTreeline']!='Extreme']
            df = df[df['o_Day1DangerBelowTreeline']!='Extreme']
        return df
    
    def remove_nodata(self, df):
        df = df[df['o_Day1DangerAboveTreeline']!='no-data']
        df = df[df['o_Day1DangerNearTreeline']!='no-data']
        df = df[df['o_Day1DangerBelowTreeline']!='no-data']
        return df

    def filter_labels(self, only_label_list, df):
        #filter to label set provided
        if len(only_label_list) > 0:
            df = df[df['o_Day1DangerAboveTreeline'].isin(only_label_list)]
            df = df[df['o_Day1DangerNearTreeline'].isin(only_label_list)]
            df = df[df['o_Day1DangerBelowTreeline'].isin(only_label_list)]
        return df

    def label_critical_points(self, label_critical_points, metadata_columns, df):
        if label_critical_points:
            critical_points = pd.read_csv('../Data/CriticalPointsToForecast.csv')
            critical_forecast_points = critical_points.apply(self.find_nearest_forecast_point, axis=1)
            df.loc[:,'IsCriticalPoint'] =  df.apply(lambda x: self.is_critical_point(x, critical_forecast_points), axis=1)
            metadata_columns.append('IsCriticalPoint')
        return df
    
    def only_precise_points(self, only_precise_points, df):
        if only_precise_points:
            df.loc[:, 'IsPrecisePoint'] = df.apply(self.label_precice_training, axis=1)
            df = df[df['IsPrecisePoint']==True]
        return df

    def only_critical_points(self, only_critical_points, df):
        if only_critical_points:
            if('IsCriticalPoint' not in df.columns):
                print("Must set label_critical_point = True if using only_critical_points = True")
                return
            df = df[df['IsCriticalPoint'] == True] 
        return df



    def create_keras_input(self, number_of_days_in_sample, input_cols, output_cols, metadata_cols, df):
        dfGrouped = df.groupby(['Season', 'Lat', 'Lon'])
        listOfDf = list(dfGrouped)
        sampleList = list()
        outputList = list()
        for l in listOfDf:
            for i in range (1, len(l[1])):
                start_of_range = i - number_of_days_in_sample
                if start_of_range < 0:
                    start_of_range = 0 #pad sequences will prefill anything shorter with 0

                #skip of there is no output label for this date
                if l[1][output_cols].iloc[i-1].isnull().values.any():
                    continue
                try:
                    for o in range(len(output_cols)):
                        if l[1][output_cols[o]].iloc[i-1] == 'no-data':
                            raise continue_loop()  #continue the outerloop
                except continue_loop:
                    continue

                sampleList.append(l[1][start_of_range:i][input_cols])
                outputList.append(l[1][output_cols+metadata_cols].iloc[i-1])

        paddedSampleList = pad_sequences(sampleList, maxlen=number_of_days_in_sample, dtype='float32', padding='pre', truncating='pre', value=0.0)
        if len(outputList)>0:
            y = pd.concat(outputList, axis=1).T
        else:
            y = None
        return paddedSampleList, y 

    def prep_timeseries_train_test(self,
                                   input_file,
                                   nrows = None,
                                   ignore_extreme = False,
                                   only_labels = [],
                                   interpolate_missing = True, 
                                   test_is_most_recent_season = False,
                                   normalize = True,
                                   oversample = False,
                                   label_critical_points = True,
                                   only_critical_points = False,
                                   only_precise_points = True,
                                   coastal_or_contenental = 'both',
                                   date_features = True,
                                   region_features = True,
                                   print_df = False):

        prediction_trend_data = []
        input_numeric_columns = []
        input_categorical_columns = []
        output_columns = []

        df = pd.read_csv(input_file, parse_dates=[self.date_label], low_memory=False, nrows=nrows)

        df['Season'] = df.apply(self.identify_season, axis=1)

        input_cols = ['APCPSurface', 'MaxTempSurfaceF',
                    'MinTempSurfaceF', 'AvgTempSurfaceF', 'MaxTemp2mAboveGroundF',
                    'MinTemp2mAboveGroundF', 'AvgTemp2mAboveGroundF',
                    'MaxTemp80mAboveGroundF', 'MinTemp80mAboveGroundF',
                    'AvgTemp80mAboveGroundF', 'MaxTempTropF', 'MinTempTropF',
                    'AvgTempTropF', 'AvgRH2mAboveGround', 'AvgWindDirection10m',
                    'AvgWindDirection80m', 'AvgWindDirectionTrop', 'AvgWindSpeed10m',
                    'MaxWindSpeed10m', 'AvgWindSpeed80m', 'MaxWindSpeed80m',
                    'AvgWindSpeedTrop', 'MaxWindSpeedTrop',
                    # 'APCPSurface_Day1',
                    # 'MaxTempSurfaceF_Day1', 'MinTempSurfaceF_Day1',
                    # 'AvgTempSurfaceF_Day1', 'MaxTemp2mAboveGroundF_Day1',
                    # 'MinTemp2mAboveGroundF_Day1', 'AvgTemp2mAboveGroundF_Day1',
                    # 'MaxTemp80mAboveGroundF_Day1', 'MinTemp80mAboveGroundF_Day1',
                    # 'AvgTemp80mAboveGroundF_Day1', 'MaxTempTropF_Day1',
                    # 'MinTempTropF_Day1', 'AvgTempTropF_Day1', 'AvgRH2mAboveGround_Day1',
                    # 'AvgWindDirection10m_Day1', 'AvgWindDirection80m_Day1',
                    # 'AvgWindDirectionTrop_Day1', 'AvgWindSpeed10m_Day1',
                    # 'MaxWindSpeed10m_Day1', 'AvgWindSpeed80m_Day1',
                    # 'MaxWindSpeed80m_Day1', 'AvgWindSpeedTrop_Day1',
                    # 'MaxWindSpeedTrop_Day1', 'APCPSurface_Day2', 'MaxTempSurfaceF_Day2',
                    # 'MinTempSurfaceF_Day2', 'AvgTempSurfaceF_Day2',
                    # 'MaxTemp2mAboveGroundF_Day2', 'MinTemp2mAboveGroundF_Day2',
                    # 'AvgTemp2mAboveGroundF_Day2', 'MaxTemp80mAboveGroundF_Day2',
                    # 'MinTemp80mAboveGroundF_Day2', 'AvgTemp80mAboveGroundF_Day2',
                    # 'MaxTempTropF_Day2', 'MinTempTropF_Day2', 'AvgTempTropF_Day2',
                    # 'AvgRH2mAboveGround_Day2', 'AvgWindDirection10m_Day2',
                    # 'AvgWindDirection80m_Day2', 'AvgWindDirectionTrop_Day2',
                    # 'AvgWindSpeed10m_Day2', 'MaxWindSpeed10m_Day2',
                    # 'AvgWindSpeed80m_Day2', 'MaxWindSpeed80m_Day2',
                    # 'AvgWindSpeedTrop_Day2', 'MaxWindSpeedTrop_Day2',
                    # 'APCPSurface_Day3', 'MaxTempSurfaceF_Day3', 'MinTempSurfaceF_Day3',
                    # 'AvgTempSurfaceF_Day3', 'MaxTemp2mAboveGroundF_Day3',
                    # 'MinTemp2mAboveGroundF_Day3', 'AvgTemp2mAboveGroundF_Day3',
                    # 'MaxTemp80mAboveGroundF_Day3', 'MinTemp80mAboveGroundF_Day3',
                    # 'AvgTemp80mAboveGroundF_Day3', 'MaxTempTropF_Day3',
                    # 'MinTempTropF_Day3', 'AvgTempTropF_Day3', 'AvgRH2mAboveGround_Day3',
                    # 'AvgWindDirection10m_Day3', 'AvgWindDirection80m_Day3',
                    # 'AvgWindDirectionTrop_Day3', 'AvgWindSpeed10m_Day3',
                    # 'MaxWindSpeed10m_Day3', 'AvgWindSpeed80m_Day3',
                    # 'MaxWindSpeed80m_Day3', 'AvgWindSpeedTrop_Day3',
                    # 'MaxWindSpeedTrop_Day3', 
                    'SnowWaterEquivalentIn', 'PrecipIncrementSnowIn',
                    'PrecipitationAccumulation', 'SnowDepthIn', 'TempMinF', 'TempMaxF',
                    'TempAveF', 'SNOWDAS_SnowDepth_mm', 'SNOWDAS_SWE_mm',
                    'SNOWDAS_SnowmeltRunoff_micromm', 'SNOWDAS_Sublimation_micromm',
                    'SNOWDAS_SublimationBlowing_micromm',
                    'SNOWDAS_SolidPrecip_kgpersquarem',
                    'SNOWDAS_LiquidPrecip_kgpersquarem', 'SNOWDAS_SnowpackAveTemp_k',
                    'c_IsCoastalSnowpack', 'c_IsContenentalSnowpack']

        output_cols = ['o_Day1DangerAboveTreeline', 'o_Day1DangerNearTreeline', 'o_Day1DangerBelowTreeline']
        output_cols_full = ['o_Day1DangerAboveTreeline',
                'o_Day1DangerNearTreeline', 'o_Day1DangerBelowTreeline',
                'o_Day1DetailedForecast', 'o_Day1Warning', 'o_Day1WarningEnd',
                'o_Day2DangerAboveTreeline', 'o_Day2DangerNearTreeline',
                'o_Day2DangerBelowTreeline', 'o_Day2DetailedForecast',
                'o_Day2Warning', 'o_Day2WarningEnd', 'o_Cornices_Likelihood',
                'o_Cornices_MaximumSize', 'o_Cornices_MinimumSize',
                'o_Cornices_OctagonAboveTreelineEast',
                'o_Cornices_OctagonAboveTreelineNorth',
                'o_Cornices_OctagonAboveTreelineNorthEast',
                'o_Cornices_OctagonAboveTreelineNorthWest',
                'o_Cornices_OctagonAboveTreelineSouth',
                'o_Cornices_OctagonAboveTreelineSouthEast',
                'o_Cornices_OctagonAboveTreelineSouthWest',
                'o_Cornices_OctagonAboveTreelineWest',
                'o_Cornices_OctagonNearTreelineEast',
                'o_Cornices_OctagonNearTreelineNorth',
                'o_Cornices_OctagonNearTreelineNorthEast',
                'o_Cornices_OctagonNearTreelineNorthWest',
                'o_Cornices_OctagonNearTreelineSouth',
                'o_Cornices_OctagonNearTreelineSouthEast',
                'o_Cornices_OctagonNearTreelineSouthWest',
                'o_Cornices_OctagonNearTreelineWest',
                'o_Cornices_OctagonBelowTreelineEast',
                'o_Cornices_OctagonBelowTreelineNorth',
                'o_Cornices_OctagonBelowTreelineNorthEast',
                'o_Cornices_OctagonBelowTreelineNorthWest',
                'o_Cornices_OctagonBelowTreelineSouth',
                'o_Cornices_OctagonBelowTreelineSouthEast',
                'o_Cornices_OctagonBelowTreelineSouthWest',
                'o_Cornices_OctagonBelowTreelineWest', 'o_Glide_Likelihood',
                'o_Glide_MaximumSize', 'o_Glide_MinimumSize',
                'o_Glide_OctagonAboveTreelineEast',
                'o_Glide_OctagonAboveTreelineNorth',
                'o_Glide_OctagonAboveTreelineNorthEast',
                'o_Glide_OctagonAboveTreelineNorthWest',
                'o_Glide_OctagonAboveTreelineSouth',
                'o_Glide_OctagonAboveTreelineSouthEast',
                'o_Glide_OctagonAboveTreelineSouthWest',
                'o_Glide_OctagonAboveTreelineWest',
                'o_Glide_OctagonNearTreelineEast',
                'o_Glide_OctagonNearTreelineNorth',
                'o_Glide_OctagonNearTreelineNorthEast',
                'o_Glide_OctagonNearTreelineNorthWest',
                'o_Glide_OctagonNearTreelineSouth',
                'o_Glide_OctagonNearTreelineSouthEast',
                'o_Glide_OctagonNearTreelineSouthWest',
                'o_Glide_OctagonNearTreelineWest',
                'o_Glide_OctagonBelowTreelineEast',
                'o_Glide_OctagonBelowTreelineNorth',
                'o_Glide_OctagonBelowTreelineNorthEast',
                'o_Glide_OctagonBelowTreelineNorthWest',
                'o_Glide_OctagonBelowTreelineSouth',
                'o_Glide_OctagonBelowTreelineSouthEast',
                'o_Glide_OctagonBelowTreelineSouthWest',
                'o_Glide_OctagonBelowTreelineWest', 'o_LooseDry_Likelihood',
                'o_LooseDry_MaximumSize', 'o_LooseDry_MinimumSize',
                'o_LooseDry_OctagonAboveTreelineEast',
                'o_LooseDry_OctagonAboveTreelineNorth',
                'o_LooseDry_OctagonAboveTreelineNorthEast',
                'o_LooseDry_OctagonAboveTreelineNorthWest',
                'o_LooseDry_OctagonAboveTreelineSouth',
                'o_LooseDry_OctagonAboveTreelineSouthEast',
                'o_LooseDry_OctagonAboveTreelineSouthWest',
                'o_LooseDry_OctagonAboveTreelineWest',
                'o_LooseDry_OctagonNearTreelineEast',
                'o_LooseDry_OctagonNearTreelineNorth',
                'o_LooseDry_OctagonNearTreelineNorthEast',
                'o_LooseDry_OctagonNearTreelineNorthWest',
                'o_LooseDry_OctagonNearTreelineSouth',
                'o_LooseDry_OctagonNearTreelineSouthEast',
                'o_LooseDry_OctagonNearTreelineSouthWest',
                'o_LooseDry_OctagonNearTreelineWest',
                'o_LooseDry_OctagonBelowTreelineEast',
                'o_LooseDry_OctagonBelowTreelineNorth',
                'o_LooseDry_OctagonBelowTreelineNorthEast',
                'o_LooseDry_OctagonBelowTreelineNorthWest',
                'o_LooseDry_OctagonBelowTreelineSouth',
                'o_LooseDry_OctagonBelowTreelineSouthEast',
                'o_LooseDry_OctagonBelowTreelineSouthWest',
                'o_LooseDry_OctagonBelowTreelineWest', 'o_LooseWet_Likelihood',
                'o_LooseWet_MaximumSize', 'o_LooseWet_MinimumSize',
                'o_LooseWet_OctagonAboveTreelineEast',
                'o_LooseWet_OctagonAboveTreelineNorth',
                'o_LooseWet_OctagonAboveTreelineNorthEast',
                'o_LooseWet_OctagonAboveTreelineNorthWest',
                'o_LooseWet_OctagonAboveTreelineSouth',
                'o_LooseWet_OctagonAboveTreelineSouthEast',
                'o_LooseWet_OctagonAboveTreelineSouthWest',
                'o_LooseWet_OctagonAboveTreelineWest',
                'o_LooseWet_OctagonNearTreelineEast',
                'o_LooseWet_OctagonNearTreelineNorth',
                'o_LooseWet_OctagonNearTreelineNorthEast',
                'o_LooseWet_OctagonNearTreelineNorthWest',
                'o_LooseWet_OctagonNearTreelineSouth',
                'o_LooseWet_OctagonNearTreelineSouthEast',
                'o_LooseWet_OctagonNearTreelineSouthWest',
                'o_LooseWet_OctagonNearTreelineWest',
                'o_LooseWet_OctagonBelowTreelineEast',
                'o_LooseWet_OctagonBelowTreelineNorth',
                'o_LooseWet_OctagonBelowTreelineNorthEast',
                'o_LooseWet_OctagonBelowTreelineNorthWest',
                'o_LooseWet_OctagonBelowTreelineSouth',
                'o_LooseWet_OctagonBelowTreelineSouthEast',
                'o_LooseWet_OctagonBelowTreelineSouthWest',
                'o_LooseWet_OctagonBelowTreelineWest',
                'o_PersistentSlab_Likelihood', 'o_PersistentSlab_MaximumSize',
                'o_PersistentSlab_MinimumSize',
                'o_PersistentSlab_OctagonAboveTreelineEast',
                'o_PersistentSlab_OctagonAboveTreelineNorth',
                'o_PersistentSlab_OctagonAboveTreelineNorthEast',
                'o_PersistentSlab_OctagonAboveTreelineNorthWest',
                'o_PersistentSlab_OctagonAboveTreelineSouth',
                'o_PersistentSlab_OctagonAboveTreelineSouthEast',
                'o_PersistentSlab_OctagonAboveTreelineSouthWest',
                'o_PersistentSlab_OctagonAboveTreelineWest',
                'o_PersistentSlab_OctagonNearTreelineEast',
                'o_PersistentSlab_OctagonNearTreelineNorth',
                'o_PersistentSlab_OctagonNearTreelineNorthEast',
                'o_PersistentSlab_OctagonNearTreelineNorthWest',
                'o_PersistentSlab_OctagonNearTreelineSouth',
                'o_PersistentSlab_OctagonNearTreelineSouthEast',
                'o_PersistentSlab_OctagonNearTreelineSouthWest',
                'o_PersistentSlab_OctagonNearTreelineWest',
                'o_PersistentSlab_OctagonBelowTreelineEast',
                'o_PersistentSlab_OctagonBelowTreelineNorth',
                'o_PersistentSlab_OctagonBelowTreelineNorthEast',
                'o_PersistentSlab_OctagonBelowTreelineNorthWest',
                'o_PersistentSlab_OctagonBelowTreelineSouth',
                'o_PersistentSlab_OctagonBelowTreelineSouthEast',
                'o_PersistentSlab_OctagonBelowTreelineSouthWest',
                'o_PersistentSlab_OctagonBelowTreelineWest',
                'o_DeepPersistentSlab_Likelihood',
                'o_DeepPersistentSlab_MaximumSize',
                'o_DeepPersistentSlab_MinimumSize',
                'o_DeepPersistentSlab_OctagonAboveTreelineEast',
                'o_DeepPersistentSlab_OctagonAboveTreelineNorth',
                'o_DeepPersistentSlab_OctagonAboveTreelineNorthEast',
                'o_DeepPersistentSlab_OctagonAboveTreelineNorthWest',
                'o_DeepPersistentSlab_OctagonAboveTreelineSouth',
                'o_DeepPersistentSlab_OctagonAboveTreelineSouthEast',
                'o_DeepPersistentSlab_OctagonAboveTreelineSouthWest',
                'o_DeepPersistentSlab_OctagonAboveTreelineWest',
                'o_DeepPersistentSlab_OctagonNearTreelineEast',
                'o_DeepPersistentSlab_OctagonNearTreelineNorth',
                'o_DeepPersistentSlab_OctagonNearTreelineNorthEast',
                'o_DeepPersistentSlab_OctagonNearTreelineNorthWest',
                'o_DeepPersistentSlab_OctagonNearTreelineSouth',
                'o_DeepPersistentSlab_OctagonNearTreelineSouthEast',
                'o_DeepPersistentSlab_OctagonNearTreelineSouthWest',
                'o_DeepPersistentSlab_OctagonNearTreelineWest',
                'o_DeepPersistentSlab_OctagonBelowTreelineEast',
                'o_DeepPersistentSlab_OctagonBelowTreelineNorth',
                'o_DeepPersistentSlab_OctagonBelowTreelineNorthEast',
                'o_DeepPersistentSlab_OctagonBelowTreelineNorthWest',
                'o_DeepPersistentSlab_OctagonBelowTreelineSouth',
                'o_DeepPersistentSlab_OctagonBelowTreelineSouthEast',
                'o_DeepPersistentSlab_OctagonBelowTreelineSouthWest',
                'o_DeepPersistentSlab_OctagonBelowTreelineWest',
                'o_StormSlabs_Likelihood', 'o_StormSlabs_MaximumSize',
                'o_StormSlabs_MinimumSize',
                'o_StormSlabs_OctagonAboveTreelineEast',
                'o_StormSlabs_OctagonAboveTreelineNorth',
                'o_StormSlabs_OctagonAboveTreelineNorthEast',
                'o_StormSlabs_OctagonAboveTreelineNorthWest',
                'o_StormSlabs_OctagonAboveTreelineSouth',
                'o_StormSlabs_OctagonAboveTreelineSouthEast',
                'o_StormSlabs_OctagonAboveTreelineSouthWest',
                'o_StormSlabs_OctagonAboveTreelineWest',
                'o_StormSlabs_OctagonNearTreelineEast',
                'o_StormSlabs_OctagonNearTreelineNorth',
                'o_StormSlabs_OctagonNearTreelineNorthEast',
                'o_StormSlabs_OctagonNearTreelineNorthWest',
                'o_StormSlabs_OctagonNearTreelineSouth',
                'o_StormSlabs_OctagonNearTreelineSouthEast',
                'o_StormSlabs_OctagonNearTreelineSouthWest',
                'o_StormSlabs_OctagonNearTreelineWest',
                'o_StormSlabs_OctagonBelowTreelineEast',
                'o_StormSlabs_OctagonBelowTreelineNorth',
                'o_StormSlabs_OctagonBelowTreelineNorthEast',
                'o_StormSlabs_OctagonBelowTreelineNorthWest',
                'o_StormSlabs_OctagonBelowTreelineSouth',
                'o_StormSlabs_OctagonBelowTreelineSouthEast',
                'o_StormSlabs_OctagonBelowTreelineSouthWest',
                'o_StormSlabs_OctagonBelowTreelineWest', 'o_WetSlabs_Likelihood',
                'o_WetSlabs_MaximumSize', 'o_WetSlabs_MinimumSize',
                'o_WetSlabs_OctagonAboveTreelineEast',
                'o_WetSlabs_OctagonAboveTreelineNorth',
                'o_WetSlabs_OctagonAboveTreelineNorthEast',
                'o_WetSlabs_OctagonAboveTreelineNorthWest',
                'o_WetSlabs_OctagonAboveTreelineSouth',
                'o_WetSlabs_OctagonAboveTreelineSouthEast',
                'o_WetSlabs_OctagonAboveTreelineSouthWest',
                'o_WetSlabs_OctagonAboveTreelineWest',
                'o_WetSlabs_OctagonNearTreelineEast',
                'o_WetSlabs_OctagonNearTreelineNorth',
                'o_WetSlabs_OctagonNearTreelineNorthEast',
                'o_WetSlabs_OctagonNearTreelineNorthWest',
                'o_WetSlabs_OctagonNearTreelineSouth',
                'o_WetSlabs_OctagonNearTreelineSouthEast',
                'o_WetSlabs_OctagonNearTreelineSouthWest',
                'o_WetSlabs_OctagonNearTreelineWest',
                'o_WetSlabs_OctagonBelowTreelineEast',
                'o_WetSlabs_OctagonBelowTreelineNorth',
                'o_WetSlabs_OctagonBelowTreelineNorthEast',
                'o_WetSlabs_OctagonBelowTreelineNorthWest',
                'o_WetSlabs_OctagonBelowTreelineSouth',
                'o_WetSlabs_OctagonBelowTreelineSouthEast',
                'o_WetSlabs_OctagonBelowTreelineSouthWest',
                'o_WetSlabs_OctagonBelowTreelineWest', 'o_WindSlab_Likelihood',
                'o_WindSlab_MaximumSize', 'o_WindSlab_MinimumSize',
                'o_WindSlab_OctagonAboveTreelineEast',
                'o_WindSlab_OctagonAboveTreelineNorth',
                'o_WindSlab_OctagonAboveTreelineNorthEast',
                'o_WindSlab_OctagonAboveTreelineNorthWest',
                'o_WindSlab_OctagonAboveTreelineSouth',
                'o_WindSlab_OctagonAboveTreelineSouthEast',
                'o_WindSlab_OctagonAboveTreelineSouthWest',
                'o_WindSlab_OctagonAboveTreelineWest',
                'o_WindSlab_OctagonNearTreelineEast',
                'o_WindSlab_OctagonNearTreelineNorth',
                'o_WindSlab_OctagonNearTreelineNorthEast',
                'o_WindSlab_OctagonNearTreelineNorthWest',
                'o_WindSlab_OctagonNearTreelineSouth',
                'o_WindSlab_OctagonNearTreelineSouthEast',
                'o_WindSlab_OctagonNearTreelineSouthWest',
                'o_WindSlab_OctagonNearTreelineWest',
                'o_WindSlab_OctagonBelowTreelineEast',
                'o_WindSlab_OctagonBelowTreelineNorth',
                'o_WindSlab_OctagonBelowTreelineNorthEast',
                'o_WindSlab_OctagonBelowTreelineNorthWest',
                'o_WindSlab_OctagonBelowTreelineSouth',
                'o_WindSlab_OctagonBelowTreelineSouthEast',
                'o_WindSlab_OctagonBelowTreelineSouthWest',
                'o_WindSlab_OctagonBelowTreelineWest']

                    
        metadata_cols = ['Lat', 'Lon', self.date_label, 'Season', 'c_IsCoastalSnowpack', 'c_IsContenentalSnowpack', 'UnifiedRegion']

        df = self.filter_snowpack_type(coastal_or_contenental, df)

        input_cols += ['DayOfYear', 'DayOfSeason']
        categorical_cols = ['c_IsCoastalSnowpack', 'c_IsContenentalSnowpack', 'DayOfYear', 'DayOfSeason']

        df = self.create_date_features(self.date_label, df)

        df = self.clean_missing_values(interpolate_missing, df, fill_na_after_interpolate=True, inplace=True)

        df = self.ignore_extreme(ignore_extreme, df)

        df = self.filter_labels(only_labels, df)

        df = self.label_critical_points(label_critical_points, metadata_cols, df)

        if(print_df):
            df.to_csv("critical_points1.csv")

        df = self.only_precise_points(only_precise_points, df)

        df = self.only_critical_points(only_critical_points, df)
        
        if(print_df):
            df.to_csv("critical_points2.csv")

        df = df[list(set().union(input_cols,output_cols,metadata_cols))]

        if(print_df):
            df.to_csv("traintestdf.csv")

        if(date_features == False):
            df.drop(labels=['DayOfYear', 'DayOfSeason'], axis=1, inplace=True)
            input_cols.remove('DayOfYear')
            input_cols.remove('DayOfSeason')

        if(region_features == False):
            df.drop(labels=['c_IsCoastalSnowpack', 'c_IsContenentalSnowpack'], axis=1, inplace=True)
            input_cols.remove('c_IsCoastalSnowpack')
            input_cols.remove('c_IsContenentalSnowpack')
            metadata_cols.remove('c_IsCoastalSnowpack')
            metadata_cols.remove('c_IsContenentalSnowpack')

        if(test_is_most_recent_season):
            df_train = df[df[self.date_label] < datetime.datetime(2017, 5, 1)] 
            df_test = df[df[self.date_label] > datetime.datetime(2017, 5, 1)]
        else:
            df_train = df[(df[self.date_label] < datetime.datetime(2016, 5, 1)) | (df[self.date_label] > datetime.datetime(2017, 5, 1))] 
            df_test = df[(df[self.date_label] > datetime.datetime(2016, 5, 1)) & (df[self.date_label] < datetime.datetime(2017, 5, 1))]

        print("test shape: " + str(df_test.shape))
        #normalize data
        if(normalize):
            scaler = Normalizer()
            dontNormalize = metadata_cols + output_cols + categorical_cols 
            n = scaler.fit(df_train.loc[:, ~df_train.columns.isin(dontNormalize)])
            df_train.loc[:, ~df_train.columns.isin(dontNormalize)] = n.transform(df_train.loc[:, ~df_train.columns.isin(dontNormalize)])
            df_test.loc[:, ~df_test.columns.isin(dontNormalize)] = n.transform(df_test.loc[:, ~df_test.columns.isin(dontNormalize)])

        # if oversample:
        #     ros = RandomOverSampler(random_state=42)
        #     df_train_res, df_test_res = ros.fit_resample(df_train, df_test[output_cols].values.ravel())

        #TODO: randomly remove some of the test test for validation set
        X_train, y_train = self.create_keras_input(150, input_cols, output_cols, metadata_cols, df_train)
        X_test, y_test = self.create_keras_input(150, input_cols, output_cols, metadata_cols, df_test)

        #make sure all values are not null
        np.nan_to_num(X_train, copy=False)
        np.nan_to_num(X_test, copy=False)
        
        mapping = {'Low':0, 'Moderate':1, 'Considerable':2, 'High':3, 'Extreme':4}

        if y_train is not None:
            y_train = y_train.replace({'o_Day1DangerAboveTreeline': mapping, 'o_Day1DangerNearTreeline': mapping, 'o_Day1DangerBelowTreeline': mapping})
        if y_test is not None:
            y_test = y_test.replace({'o_Day1DangerAboveTreeline': mapping, 'o_Day1DangerNearTreeline': mapping, 'o_Day1DangerBelowTreeline': mapping})
        
      
        return X_test, X_train, y_test, y_train

    def prep_data(self, 
                  input_file,
                  nrows = None, 
                  ignore_extreme = False,
                  only_labels = [],
                  interpolate_missing = True, 
                  label_critical_points = True,
                  only_critical_points = False,
                  only_precise_points = True,
                  coastal_or_contenental = 'both', 
                  metadata_columns = None):
        prediction_trend_data = []
        output_columns = []

        df = pd.read_csv(input_file, parse_dates=[self.date_label], low_memory=False, nrows=nrows)
        df['Season'] = df.apply(self.identify_season, axis=1)
        all_columns = list(df)
        numeric_regex = re.compile("n_*")
        input_numeric_columns = list(filter(numeric_regex.match, all_columns))
        categorical_regex = re.compile("c_*")
        input_categorical_columns = list(filter(categorical_regex.match, all_columns))
        output_regex = re.compile("o_*")
        output_columns = list(filter(output_regex.match, all_columns))

        df = df[input_numeric_columns+input_categorical_columns+output_columns+prediction_trend_data+metadata_columns]

        df = self.filter_snowpack_type(coastal_or_contenental, df)

        input_numeric_columns += ['DayOfYear', 'DayOfSeason']

        df = self.create_date_features(self.date_label, df)

        df = self.clean_missing_values(interpolate_missing, df, fill_na_after_interpolate=True, inplace=True)

        df = self.ignore_extreme(ignore_extreme, df)

        df = self.remove_nodata(df)

        df = self.filter_labels(only_labels, df)

        df = self.label_critical_points(label_critical_points, metadata_columns, df)

        df = self.only_precise_points(only_precise_points, df)

        df = self.only_critical_points(only_critical_points, df)

        return df, input_numeric_columns, input_categorical_columns

      

    def prep_day1_danger_train_test(self, 
                                    input_file,
                                    nrows = None, 
                                    ignore_extreme = False,
                                    only_labels = [],
                                    interpolate_missing = True, 
                                    split_by_season = True, 
                                    test_is_most_recent_season = False,
                                    oversample = False,
                                    label_critical_points = True,
                                    only_critical_points = False,
                                    only_precise_points = True,
                                    coastal_or_contenental = 'both'):

        input_numeric_columns = []
        input_categorical_columns = []
        metadata_columns = ['Lat', 'Lon', 'UnifiedRegion', self.date_label, 'Season']

        df, input_numeric_columns, input_categorical_columns = self.prep_data(input_file, 
                            nrows,
                            ignore_extreme,
                            only_labels,
                            interpolate_missing,
                            label_critical_points,
                            only_critical_points,
                            only_precise_points,
                            coastal_or_contenental, 
                            metadata_columns)
                            

        yColumns = ['o_Day1DangerAboveTreeline',
                    'o_Cornices_Likelihood',
                    'o_Glide_Likelihood',
                    'o_LooseDry_Likelihood',
                    'o_LooseWet_Likelihood',
                    'o_PersistentSlab_Likelihood',
                    'o_DeepPersistentSlab_Likelihood',
                    'o_StormSlabs_Likelihood',
                    'o_WetSlabs_Likelihood',
                    'o_WindSlab_Likelihood'
                    ]

        yNearColumn = ['o_Day1DangerNearTreeline']
        yBelowColumn = ['o_Day1DangerBelowTreeline']
        if(split_by_season):
            if(test_is_most_recent_season):
                df_train = df[df[self.date_label] < datetime.datetime(2017, 5, 1)] 
                df_test = df[df[self.date_label] > datetime.datetime(2017, 5, 1)]
            else:
                df_train = df[(df[self.date_label] < datetime.datetime(2016, 5, 1)) | (df[self.date_label] > datetime.datetime(2017, 5, 1))] 
                df_test = df[(df[self.date_label] > datetime.datetime(2016, 5, 1)) & (df[self.date_label] < datetime.datetime(2017, 5, 1))]
            
            y_Above_train = df_train[yColumns + metadata_columns]
            y_Near_train = df_train[yNearColumn + metadata_columns]
            y_Below_train = df_train[yBelowColumn + metadata_columns]
            
            y_Above_test = df_test[yColumns + metadata_columns]
            y_Near_test = df_test[yNearColumn + metadata_columns]
            y_Below_test = df_test[yBelowColumn + metadata_columns]
            
            #same x input trained to get a different output
            X_Above_train = X_Near_train = X_Below_train = df_train[input_numeric_columns+input_categorical_columns]
            X_Above_test = X_Near_test = X_Below_test = df_test[input_numeric_columns+input_categorical_columns]
            
        else:
            #random stratified split
            X = df[input_numeric_columns+input_categorical_columns]
            
            yAbove=df[yColumns + metadata_columns]
            X_Above_train, X_Above_test, y_Above_train, y_Above_test = train_test_split(X, yAbove, stratify=yAbove["o_Day1DangerAboveTreeline"], test_size=0.20, random_state=1)
            
            yNear=df[yNearColumn+metadata_columns]
            X_Near_train, X_Near_test, y_Near_train, y_Near_test = train_test_split(X, yNear, stratify=yNear['o_Day1DangerNearTreeline'], test_size=0.20, random_state=1)
            
            yBelow=df[yBelowColumn+metadata_columns]
            X_Below_train, X_Below_test, y_Below_train, y_Below_test = train_test_split(X, yBelow, stratify=yBelow['o_Day1DangerBelowTreeline'], test_size=0.20, random_state=1)

        mapping = {'Low':0, 'Moderate':1, 'Considerable':2, 'High':3, 'Extreme':4}

        y_Above_train = y_Above_train.replace({'o_Day1DangerAboveTreeline': mapping})
        y_Near_train = y_Near_train.replace({'o_Day1DangerNearTreeline': mapping})
        y_Below_train = y_Below_train.replace({'o_Day1DangerBelowTreeline': mapping})

        y_Above_test = y_Above_test.replace({'o_Day1DangerAboveTreeline': mapping})
        y_Near_test = y_Near_test.replace({'o_Day1DangerNearTreeline': mapping})
        y_Below_test = y_Below_test.replace({'o_Day1DangerBelowTreeline': mapping})
        
        if oversample:
            ros = RandomOverSampler(random_state=42)
            X_Above_train_res, y_Above_train_res = ros.fit_resample(X_Above_train, y_Above_train[yColumns].values.ravel())
            X_Above_train = X_Above_train_res
            y_Above_train = y_Above_train_res

        return X_Above_test, X_Above_train, y_Above_test, y_Above_train, X_Near_test, X_Near_train, y_Near_test, y_Near_train, X_Below_test, X_Below_train, y_Below_test, y_Below_train