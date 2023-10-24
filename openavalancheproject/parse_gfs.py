# AUTOGENERATED! DO NOT EDIT! File to edit: ../DataPipelineNotebooks/1.ParseGFS.ipynb.

# %% auto 0
__all__ = ['ParseGFS']

# %% ../DataPipelineNotebooks/1.ParseGFS.ipynb 6
import xarray as xr
import matplotlib.pyplot as plt
import pandas as pd
import salem
import numpy as np
import geopandas as gpd
from joblib import Parallel, delayed
import os

# %% ../DataPipelineNotebooks/1.ParseGFS.ipynb 12
class ParseGFS:
    """Class which provides the basic utilities and processing to transform a set of GFS hourly weather file 
       to a set of filtered, aggregated and optionally interpolated netCDF files organized.
    """
    @staticmethod
    def season_to_snow_start_date(season):
        if season == '15-16':
            return '2015-11-01'
        elif season == '16-17':
            return '2016-11-01'
        elif season == '17-18':
            return '2017-11-01'
        elif season == '18-19':
            return '2018-11-01'
        elif season == '19-20':
            return '2019-11-01'
        elif season == '20-21':
            return '2020-11-01'
    
    def __init__(self, season, state, input_data_root, output_data_root=None, interpolate=1, resample_length='1d'):
        """Initialize the class
        
        Keyword arguments:
        season: the season code (e.g., 15-16, 16-17) for the season you are processing
        state: the name of the state or country we are processing
        data_root: the root path of the data folders which contains the 1.RawWeatherData folder
        interpolate: the degree of interpolation (1x and 4x have been tested, 1x is default)   
        resample_length: the timeframe to resample to
        """
        self.season = season
        self.snow_start_date = ParseGFS.season_to_snow_start_date(season)
        self.state = state
        self.interpolate = interpolate
        assert(input_data_root is not None)
        self.input_data_root = input_data_root
        if(output_data_root is None):
            self.output_data_root = input_data_root
        self.state_path = None
        self.resample_length = resample_length
        
        #make sure these are correct, but these generally don't need to change
        #if state == 'Washington' or state == 'Canada':
        self.state_path = state
        #else:
        #    self.state_path = 'Colorado' #the nc file which contains the weather data contains both utah and colorado


        #Path to USAvalancheRegions.geojson in this repo
        self.region_path = '../Data' 
        #path to the gfs netcdf files for input
        self.dataset_path = input_data_root + '/1.RawWeatherData/gfs/' + season + '/' + self.state_path + '/'
        self.precip_dataset_path = input_data_root + '/1.RawWeatherData/gfs/' + season + '/' + self.state_path + 'AccumulationGrib/'

        self.filtered_path = output_data_root + '2.GFSFiltered' + str(interpolate) + 'xInterpolation' + resample_length + '/' + season + '/'

        #input file pattern as we'll read a full winter season in one pass
        self.file_pattern2 = '00.f0[0-2]*.nc'

        p = 181
        if season in ['15-16', '19-20']:
            p = 182 #leap years

        self.date_values_pd = pd.date_range(self.snow_start_date, periods=p, freq="D")
        if self.state == 'ColoradoSmall':
            #truncate dates to just first n days
            self.date_values_pd = self.date_values_pd[0:24]
        
        print(self.dataset_path + ' Is Input Directory')
        print(self.filtered_path + ' Is output directory of filtering')
        
        #check dates end on April 30 which is the last day we support
        #assert(self.date_values_pd[-1].month == 4)
        #assert(self.date_values_pd[-1].day == 30)
        
        if not os.path.exists(self.filtered_path):
            os.makedirs(self.filtered_path)
        
        #Read in all avy region shapes and metadata
        regions_df = None
        if self.state == 'Canada':
            regions_df = gpd.read_file(self.region_path + '/CAAvalancheRegions.geojson')
        else:
            regions_df = gpd.read_file(self.region_path + '/USAvalancheRegions.geojson')
        #filter to just the ones where we have lables for training
        self.training_regions_df = regions_df[regions_df['is_training']==True].copy()

        #TODO: this needs to not rely on a code change to add a region
        if self.state == 'Washington':
            self.training_regions_df = self.training_regions_df[self.training_regions_df['center']=='Northwest Avalanche Center']
        elif self.state == 'Utah':
            self.training_regions_df = self.training_regions_df[self.training_regions_df['center']=='Utah Avalanche Center']
        elif self.state == 'Colorado':
            self.training_regions_df = self.training_regions_df[self.training_regions_df['center']=='Colorado Avalanche Information Center']
        elif self.state == 'ColoradoSmall':
            #used in debugging/profiling
            self.training_regions_df = self.training_regions_df[self.training_regions_df['center']=='Colorado Avalanche Information Center']
            #just get the first region
            self.training_regions_df = self.training_regions_df.iloc[0:1]
        elif self.state == 'Canada':
            #include everything
            pass
        
        
        self.training_regions_df.reset_index(drop=True, inplace=True)
    
        
    def interpolate_and_write(self, tmp_ds):
        import salem #required for multiprocessing--new behavior
        """
        interpolate and filter each day 
        don't use dask for this, much faster to process the files in parallel 
        
        Keyword arguments:
        tmp_ds: the dataframe to process
        """
        errors = []
        redo_date = []
        date = tmp_ds.time.dt.strftime('%Y%m%d').values[0]
        for _, row in self.training_regions_df.iterrows():
            print("Calculating region: " + row['name'])
            f = self.filtered_path + 'Region_' + row['name'] + '_' + date + '.nc'
            try:
                if self.interpolate == 1:                    
                    #subset the xarray dataset to the region defined by a geojson polygon in the row; don't use salem for interpolation
                    tmp_subset = tmp_ds.salem.subset(geometry=row['geometry'])     
                else:
                    #TODO: I've noticed that this might set a few vars, like snowdepth, to nan even when 
                    #interpolate == 1; need to investigate before using.
                    new_lon = np.linspace(tmp_ds.longitude[0], tmp_ds.longitude[-1], tmp_ds.dims['longitude'] * self.interpolate)
                    new_lat = np.linspace(tmp_ds.latitude[0], tmp_ds.latitude[-1], tmp_ds.dims['latitude'] * self.interpolate)
                    interpolated_ds = tmp_ds.interp(latitude=new_lat, longitude=new_lon)
                    tmp_subset = interpolated_ds.salem.subset(geometry=row['geometry']).salem.roi(geometry=row['geometry'])
            except ValueError:            
                errors.append('Value Error: Ensure the correct training regions have been provided')
                del tmp_subset
                continue
            except Exception as err:
                print('Salem subset exception ' + format(err))
                continue

            try:
                comp = dict(zlib=True, complevel=7)
                encoding = {var: comp for var in tmp_subset.data_vars}                                     
                tmp_subset.load().to_netcdf(f, encoding=encoding )
            except Exception as err:
                print('output exception')
                os.remove(f)
                errors.append(f + ' -- ' + format(err))
                redo_date.append(date)
                del tmp_subset
                continue
            
        
        return (errors, redo_date)
    
    def resample(self, t):
        """
        Convert netcdf files which already pivot across levels
        from a full forcecast file which covers many days
        to one which only covers one day in the future
        also changes the data from hourly to daily min, avg, and max values
        
        Keyword arguments:
        t: the pandas datetime to process
       
        """
        
        print('On time: ' + str(t) + '\n')
        precip_ds = None
        pd_t = pd.to_datetime(t)
        with xr.open_mfdataset(self.precip_dataset_path + 'gfs.0p25.' + t + self.file_pattern2, combine='nested', concat_dim='time', parallel=False) as precip_ds:
            #3-hour Accumulation (initial+0 to initial+3)
            #6-hour Accumulation (initial+0 to initial+6)
            #3-hour Accumulation (initial+6 to initial+9)
            #6-hour Accumulation (initial+6 to initial+12)
            #3-hour Accumulation (initial+12 to initial+15)
            #6-hour Accumulation (initial+12 to initial+18)
            #3-hour Accumulation (initial+18 to initial+21)
            #6-hour Accumulation (initial+18 to initial+24)
            #correct the values to all be 3 hour accumulations
            corrected_dses = []
            for i in range(0,len(precip_ds.time.values)):
                if i % 2 == 0:
                    corrected_dses.append(precip_ds.isel(time=i))
                else:
                    tmp_ds = precip_ds.isel(time=i) - precip_ds.isel(time=i-1)         
                    corrected_dses.append(precip_ds.isel(time=i).assign(tmp_ds))
            precip_ds = xr.concat(corrected_dses, dim='time').persist()
            
            #resample
            total_name_dict = {}
            for k in precip_ds.data_vars.keys():
                total_name_dict[k] = k + '_sum'
            resampled_precip_ds = precip_ds.resample(time=self.resample_length)
            sum_resample = resampled_precip_ds.sum().rename(total_name_dict)   

        ret_value = None
        try:
            with xr.open_mfdataset(self.dataset_path + 'gfs.0p25.' + t + self.file_pattern2, combine='nested', concat_dim='time', parallel=False) as ds: 

                #make sure we are just getting the first 24 hours                
                min_name_dict = {}
                max_name_dict = {}
                avg_name_dict = {}
                for k in ds.data_vars.keys():
                    min_name_dict[k] = k + '_min'
                    max_name_dict[k] = k + '_max'
                    avg_name_dict[k] = k + '_avg'

                resampled_ds = ds.resample(time=self.resample_length)
                min_resample = resampled_ds.min().rename(min_name_dict)
                max_resample = resampled_ds.max().rename(max_name_dict)
                avg_resample = resampled_ds.mean().rename(avg_name_dict)

                merged_ds = xr.merge([min_resample, max_resample, avg_resample, sum_resample]).persist()
                try:
                    ret_value = self.interpolate_and_write(merged_ds)
              
                except Exception as err:
                    return self.input_data_root + self.state_path + '_' + t + '.nc' + ' -- ' + format(err)
                    ds.close()
                    merged_ds.close()
                del merged_ds
        except OSError as err:
            print('Missing files for time: ' + t)
        return ret_value
            
    
    def check_resample(self, dates):
        """
        method to check if there are any file open issues with the newly output files
        
        Keyword arguments:
        date: pandas dates to check
        """
        dates_with_errors = []
        for t in dates.strftime('%Y%m%d'):
            try:
                with xr.open_dataset(self.input_data_root + self.state_path + '_' + t + '.nc') as file:
                    file.close()
                continue                
            except Exception as e: 
                #print(format(e))
                dates_with_errors.append(t)
        return dates_with_errors
    
    def resample_local(self, jobs=4):
        """
        Executes the resample process on the local machine.
        All-Nan Slice and Divide warnings can be ignored
            
        Keyword arguments:
        jobs: number of parallel processs to use (default = 4)
        """
        results = Parallel(n_jobs=jobs, backend='loky')(map(delayed(self.resample), self.date_values_pd.strftime('%Y%m%d')))
        #the new code seems to largely prevent corruption, truncating for now
        return results
        


   
