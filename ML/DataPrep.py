import numpy as np
import pandas as pd
import re
import sys
import datetime
from imblearn.over_sampling import RandomOverSampler
import fiona
import shapely
import shapely.geometry

class DataPrep:

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
            if(row['Lat'] == cp[0] and row['Lon'] == cp[1]):
                return True
        return False

    def day_of_season(self, row):
        if(row['Date'].month < 6):
            #month is in first part of following year
            return pd.Timedelta(row['Date'] - datetime.datetime(row['Date'].year-1, 11, 1)).days
        else:
            return pd.Timedelta(row['Date'] - datetime.datetime(row['Date'].year, 11, 1)).days

    def prep_day1_danger_train_test(self, 
                                    input_file, 
                                    ignore_extreme = False,
                                    only_labels = [],
                                    interpolate_missing = True, 
                                    normalize = False, 
                                    split_by_season = True, 
                                    test_is_most_recent_season = False,
                                    oversample = False,
                                    label_critical_points = True,
                                    only_critical_points = False,
                                    only_precise_points = True,
                                    coastal_or_contenental = 'both'):

        prediction_trend_data = []
        input_numeric_columns = []
        input_categorical_columns = []
        output_columns = []

        df = pd.read_csv(input_file, parse_dates=['Date'], low_memory=False)

        all_columns = list(df)
        numeric_regex = re.compile("n_*")
        input_numeric_columns = list(filter(numeric_regex.match, all_columns))
        categorical_regex = re.compile("c_*")
        input_categorical_columns = list(filter(categorical_regex.match, all_columns))
        output_regex = re.compile("o_*")
        output_columns = list(filter(output_regex.match, all_columns))
        metadata_columns = ["Lat", "Lon", "UnifiedRegion", "Date"]

        df = df[input_numeric_columns+input_categorical_columns+output_columns+prediction_trend_data+metadata_columns]

        if coastal_or_contenental  == 'coastal':
            df = df[df['c_IsCoastalSnowpack'] == 1]
        elif coastal_or_contenental  == 'contenental':
            df = df[df['c_IsContenentalSnowpack'] == 1]

        #filter rows with missing labels
        df = df[df['o_Day1DangerAboveTreeline']!='no-data']
        df = df[df['o_Day1DangerNearTreeline']!='no-data']
        df = df[df['o_Day1DangerBelowTreeline']!='no-data']

        date_columns = ['DayOfYear', 'DayOfSeason']
        df['DayOfYear'] = df['Date'].dt.dayofyear

        #number of days since nov 1
        df['DayOfSeason'] = df.apply(self.day_of_season, axis=1)

        #clean mising values
        mappingMissingValues = {-9999:np.nan}
        if interpolate_missing:
            df = df.replace(mappingMissingValues)
            df = df.interpolate(method='linear', axis=0).ffill().bfill()

        if ignore_extreme:
            df = df[df['o_Day1DangerAboveTreeline']!='Extreme']
            df = df[df['o_Day1DangerNearTreeline']!='Extreme']
            df = df[df['o_Day1DangerBelowTreeline']!='Extreme']
        
        #filter to label set provided
        if len(only_labels) > 0:
            df = df[df['o_Day1DangerAboveTreeline'].isin(only_labels)]
            df = df[df['o_Day1DangerNearTreeline'].isin(only_labels)]
            df = df[df['o_Day1DangerBelowTreeline'].isin(only_labels)]

        if label_critical_points:
            critical_points = pd.read_csv('../Data/CriticalPointsToForecast.csv')
            critical_forecast_points = critical_points.apply(self.find_nearest_forecast_point, axis=1)
            df.loc[:,'IsCriticalPoint'] =  df.apply(lambda x: self.is_critical_point(x, critical_forecast_points), axis=1)
            metadata_columns.append('IsCriticalPoint')

        if only_precise_points:
            df.loc[:, 'IsPrecisePoint'] = df.apply(self.label_precice_training, axis=1)
            df = df[df['IsPrecisePoint']==True]
        
        if only_critical_points:
            if('IsCriticalPoint' not in df.columns):
                print("Must set label_critical_point = True if using only_critical_points = True")
                return
            df = df[df['IsCriticalPoint'] == True] 

        splitBySeason = True
        testAsMostRecent = False
        yColumns = ['o_Day1DangerAboveTreeline']
        yNearColumn = ['o_Day1DangerNearTreeline']
        yBelowColumn = ['o_Day1DangerBelowTreeline']

        if(splitBySeason):
            if(testAsMostRecent):
                df_train = df[df['Date'] < datetime.datetime(2017, 5, 1)] 
                df_test = df[df['Date'] > datetime.datetime(2017, 5, 1)]
            else:
                df_train = df[(df['Date'] < datetime.datetime(2016, 5, 1)) | (df['Date'] > datetime.datetime(2017, 5, 1))] 
                df_test = df[(df['Date'] > datetime.datetime(2016, 5, 1)) & (df['Date'] < datetime.datetime(2017, 5, 1))]
            
            y_Above_train = df_train[yColumns + metadata_columns]
            y_Near_train = df_train[yNearColumn + metadata_columns]
            y_Below_train = df_train[yBelowColumn + metadata_columns]
            
            y_Above_test = df_test[yColumns + metadata_columns]
            y_Near_test = df_test[yNearColumn + metadata_columns]
            y_Below_test = df_test[yBelowColumn + metadata_columns]
            
            #same x input trained to get a different output
            X_Above_train = X_Near_train = X_Below_train = df_train[input_numeric_columns+input_categorical_columns+date_columns]
            X_Above_test = X_Near_test = X_Below_test = df_test[input_numeric_columns+input_categorical_columns+date_columns]
            
        else:
            #random stratified split
            X = df[input_numeric_columns+input_categorical_columns]
            
            yAbove=df[yColumns + metadata_columns]
            X_Above_train, X_Above_test, y_Above_train, y_Above_test = train_test_split(X, yAbove, stratify=yAbove["o_Day1DangerAboveTreeline"], test_size=0.20, random_state=1)
            
            yNear=df[yNearColumn+metadata_columns]
            X_Near_train, X_Near_test, y_Near_train, y_Near_test = train_test_split(X, yNear, stratify=yNear['o_Day1DangerNearTreeline'], test_size=0.20, random_state=1)
            
            yBelow=df[yBelowColumn+metadata_columns]
            X_Below_train, X_Below_test, y_Below_train, y_Below_test = train_test_split(X, yBelow, stratify=yBelow['o_Day1DangerBelowTreeline'], test_size=0.20, random_state=1)

        if oversample:
            ros = RandomOverSampler(random_state=42)
            X_Above_train_res, y_Above_train_res = ros.fit_resample(X_Above_train, y_Above_train[yColumns].values.ravel())
            X_Above_train = X_Above_train_res
            y_Above_train = y_Above_train_res



        return X_Above_test, X_Above_train, y_Above_test, y_Above_train, X_Near_test, X_Near_train, y_Near_test, y_Near_train, X_Below_test, X_Below_train, y_Below_test, y_Below_train