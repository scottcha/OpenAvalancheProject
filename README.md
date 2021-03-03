# Open Avalanche Project



Open source project to bring data and ml to avalanche forecasting


Webpage is https://openavalancheproject.org
Docs are located at https://scottcha.github.io/OpenAvalancheProject/

We are starting to try and improve communications on the group here https://groups.google.com/g/openavalancheproject



Directories are organized as follows:
- Data

    Contains files associated with data inputs, such as geojson definitions of avalanche regions.  Training and label data are linked in the README there as they are too large to host in git.
- Environments

    Conda environment yml files
- ML

    In the current state this is empty but will host the production pipline files once the notebook effort demonstrates a sufficient architecture and accuracy
- WebApp is the bulk of the operational code

    - OpenAvalancheProjectWebApp Contains the code for the website
    - OpenAvalancheProjectWebApp.Tests Tests for the website (unfortunately empty at the moment)

## Tutorial 
### 1. Getting new input data

This aspect of the tutorial will cover how you can obtain new weather input data for a new date range or region.  This part assumes you have avalanche forecast labels for the dates and region (OAP currently has historical forecast labels for three avalanche centers in the US from the 15-16 season through the 19-20 season and is working on expanding that).

Due to the large size of the input GFS data and the fact that its already hosted by NCAR OAP isn't currently providing copies of this data.  If you want to start a data processing pipeline from the original data you can start with this process here.  If you aren't interested in the data processing steps and only in the ML steps you can download the labels here: https://oapstorageprod.blob.core.windows.net/oap-training-data/Data/V1.1FeaturesWithLabels2013-2020.zip and a subset of training data here: https://oapstorageprod.blob.core.windows.net/oap-training-data/Data/MLDataWashington.zip and skip to the fourth notebook 4.TimeseriesAi

The input data is derived from the .25 degree GFS model hosted by NCAR hosted at this site: https://rda.ucar.edu/datasets/ds084.1/
You'll need to create an account and once you are logged in you can visit the above link and then click on the Data Access tab.  One note is that I've found that chromium based browsers don't work well on this site so I recommend you use Firefox for at least downloading the data.

Due to the size of the files we are downloading I only recommend downloading one season and for a regional subset at a time.  In this example I'm going to download the data for Washington.  

![NCAR Get Data](DataPipelineNotebooks/images/NCAR_GetData.png)

Click on the "Get a Subset" link.

The next page allows us to select both the dates and parameters we are interested in.  Currently we read all parameters so leave the parameters unchecked.  For dates choose one winter season.  In the below screenshot I've selected dates Nov 1, 2015 thorough April 30, 2016 for the 15-16 season.  The models assume the season starts Nov 1 and ends April 30 (it wouldn't be too difficult to update the data pipeline for a southern hemisphere winter but its not something I've done yet).

![NCAR Date Selection](DataPipelineNotebooks/images/NCAR_DateSelection.png)

Click Continue and wait for it to validate your selections. 

The next page allows you to further subset your data.  There are a few important things here.  

    1. Verify that the dates are correct.  
    2. We want the output as grib (same as input) 
    3. Download all vertical levels.  
    4. Select only the 3-24 hour forecasts in the gridded products as currently OAP doesn't use more than this.  
    5. You can also then select the bounding box for the area you want to download. Once you have a bounding box you like write down the lat/lon values so its easier to input when we come back for other date ranges.

![NCAR Subset Selection](DataPipelineNotebooks/images/NCAR_Subset2.png)

Once the selections are correct and you can eventually click through to submit your request.  You should get a confirmation page of your selections and the system will start to retrieve your data.  This usually takes a few hours and you will get an email when its ready for download.  At this point if you want additional date/time ranges you can submit the requests and they will get queued and made avalable for download when they are ready.  In this example the downloaded files were 1.1 GB.

Extract and decompress all the files until you have a per forecast grib file and ensure all the files have been moved in to a single directory (per season per location). If you are using Linux this stackoverflow post may help https://askubuntu.com/questions/146634/shell-script-to-move-all-files-from-subfolders-to-parent-folder.

Once you have all the files as grib files in a single directory for that date and location (i.e., 15-16/Washington/) there are a couple final cleaning steps.  Due to the download process sometimes some files earlier than 11/1 are included.  You can just delete those files (the file date is)
    
_Its worth a brief interlude in to understanding how these files are encoded.  Here is a typical file name gfs.0p25.2015110100.f003.grib2.chamberlin455705.  Lets break that down gfs: is the model we are using.  0p25 I beleive is the resolution at .25 degress but I haven't seen this documented.  2015110100 is the encoded date of the model runtime.  You will see in your dataset that there are four models run per day: 00, 06, 12, 18.  Currently we are only using the 00 model (the first of the day).  The next component is .f003 which is the forecast for 3 hours from the model runtime.  grib2 is the input file format.  chamberlin455705 is the enocded download request. 

Next delete all files which have a model run hour other than 00 (i.e., 06, 12, 18).  Check that you have 1456 files at this point (8 files per day for 182 days, my download is missing the last 4 files which isn't a big deal as a subsequent steps averages these hourly forecasts to daily forecasts).  The total size of the input files at this point is ~900MB.

Now remove the download request label in the filename which is easily accomplished using the rename command.

    rename 's/(.*)\.grib2.chamberlin455705/$1.grib2/g' *

![File List Example](DataPipelineNotebooks/images/files_example2.png)

The final step is to ensure the input data is in the correct folder structure.  All data for this project will sit off a path you define as the base path.  The GFS input data then needs to be in subfolders of that path delineated by season and state (or country).
For example if our past path in this example is:

    /media/scottcha/E1/Data/OAPMLData/

The place this data in 

    /media/scottcha/E1/Data/OAPMLData/1.RawWeatherData/15-16/Washington/

Notes:

* There is an option to covert the file to NetCDF in the NCAR/UCAR UI.  Don't use this as it will result in a .nc file which isn't in the same format as the one we are going to use.

### 2. Transform and Filtering the Data

Now that we have the input file set we can start to go through the initial data pipeline steps to transform and filter the data. Today this is done in a series of Jupyter notebooks.  This format makes it easy to incrementally process and check the outputs while the project is in a development phase (once we have a model which seems to have a resonable output these steps will be encoded in a set of python modules and implemented as a processing pipeline).

Assuming you have Anaconda and Jupyter installed first change directory to the Environments directory at the root level of the repo.  This contains two conda envrionment definitions, one for the processing steps, pangeo_small.yml, and one for the deep learning step, timeseriesai.yml.

    conda env create -f pangeo_small.yml

_This environment file was adopted from the (Pangeo project)[https://pangeo.io/] but has been slimmed down a bit._

Once the environment has been created you can activate it with

    conda activate pangeo_small

There is one step we need to take before going through the notebooks and that is converting the grib2 files to NetCDF.  We do this for a couple of reasons but primarily that using this tool efficiently collapses the vertical dimensions (called level) in to the variable definitions so we can more easily get it to the ML format we need.  The utility to do this is called wgrib2 and should have been installed in the pangeo_small environment.

Using a terminal prompt change directory to the folder where you downloaded and unpacked the weather model files.  

    /media/scottcha/E1/Data/OAPMLData/1.RawWeatherData/15-16/Washington/

In that directory you can execute this command to iterate through all the files and tranform them:

    for i in *.grib2; do wgrib2 $i -netcdf $i.nc; done

Rexecute a rename command to remove the grib extension:

    rename 's/(.*)\.grib2/$1.nc/g' *

_There are ways of improving the efficiency by doing this in parallel so feel free to improve on this._

To start a new notebook launch jupyter

    jupyter notebook

### 3. ParseGFS 
#### Parsing and filtering the input files

Completing these next few steps bascially takes the raw input weather data and leaves us with data slightly transformed but filtered to only the coordinates in the avalanche regions for that location.  For example here is what a regional view of one of the parameters (U component of wind vector) looks like when both interpolated 4x and viewed across the entire Washington region:

![Washington Wind Component](DataPipelineNotebooks/images/Wind_Example.png)

We've used this geojson definition of the avalanche regions to subset that view in to much smaller views focused on the avalanche forecast regions.  Here are all the US regions.

![US Avalanche Regions](DataPipelineNotebooks/images/US_Avy_Regions.png)

And then this is what it looks like when filtered to only the Olympics avalanche region (the small one in the top left of the US regions):

![Olympics Wind Component](DataPipelineNotebooks/images/Wind_Region_Example.png)





### Building the project
To build and install the project first clone locally.
Then you can build the project by executing

    nbdev_build_lib
    
In the project root.  To install the modules in to your current conda environment you can then execute

    pip install -e .
    

```python
#test_ignore
from openavalancheproject.parse_gfs import ParseGFS
from openavalancheproject.convert_to_zarr import ConvertToZarr
from openavalancheproject.prep_ml import PrepML
```

# Files on disk structure

OAPMLData\
    CleanedForecastsNWAC_CAIC_UAC.V1.2013-2020.csv #labels downloaded from https://oapstorageprod.blob.core.windows.net/oap-training-data/Data/V1.1FeaturesWithLabels2013-2020.zip
    

    1.RawWeatherData/
        gfs/
            <season>/
                /<state or country>/
    2.GFSDaily(x)Interpolation/
    3.GFSFiltered(x)Interpolation/
    4.GFSFiltered(x)InterpolationZarr/
    5.MLData

## These parameters need to be set

```python
#test_ignore
season = '17-18'
state = 'Washington'

interpolate = 1 #interpolation factor: whether we can to augment the data through lat/lon interpolation; 1 no interpolation, 4 is 4x interpolation

data_root = '/media/scottcha/E1/Data/OAPMLData/'

n_jobs = 4 #number of parallel processes, this processing is IO bound so don't set this too high
```

```python
#test_ignore
pgfs = ParseGFS(season, state, data_root)
```

    /media/scottcha/E1/Data/OAPMLData//1.RawWeatherData/gfs/17-18/Washington/ Is Input Directory
    /media/scottcha/E1/Data/OAPMLData/2.GFSDaily1xInterpolation/17-18/ Is output directory and input to filtering
    /media/scottcha/E1/Data/OAPMLData/3.GFSFiltered1xInterpolation/17-18/ Is output directory of filtering


### The first step is to resample the GFS files 

```python
#test_ignore
#limiting this to 4 jobs as fileio is the bottleneck
#n_jobs=4
#CPU times: user 1.11 s, sys: 551 ms, total: 1.66 s
#Wall time: 12min 22s
%time results = pgfs.resample_local()
```

    On time: 20171101
    On time: 20171102
    
    On time: 20171103
    
    
    On time: 20171104
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171105
    
    On time: 20171106
    
    On time: 20171107
    
    On time: 20171108
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171109
    
    On time: 20171110
    
    On time: 20171111
    
    On time: 20171112
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171113
    
    On time: 20171114
    
    On time: 20171115
    
    On time: 20171116
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171117
    
    On time: 20171118
    
    On time: 20171119
    
    On time: 20171120
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171121
    
    On time: 20171122
    
    On time: 20171123
    
    On time: 20171124
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171125
    
    On time: 20171126
    
    On time: 20171127
    
    On time: 20171128
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171129
    
    On time: 20171130
    
    On time: 20171201
    
    On time: 20171202
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171203
    
    On time: 20171204
    
    On time: 20171205
    
    On time: 20171206
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171207
    
    On time: 20171208
    
    On time: 20171209
    
    On time: 20171210
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171211
    
    On time: 20171212
    
    On time: 20171213
    
    On time: 20171214
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171215
    
    On time: 20171216
    
    On time: 20171217
    
    On time: 20171218
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171219
    
    On time: 20171220
    
    On time: 20171221
    
    On time: 20171222
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171223
    
    On time: 20171224
    
    On time: 20171225
    
    On time: 20171226
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171227
    
    On time: 20171228
    
    On time: 20171229
    
    On time: 20171230
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20171231
    
    On time: 20180101
    
    On time: 20180102
    
    On time: 20180103
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180104
    
    On time: 20180105
    
    On time: 20180106
    
    On time: 20180107
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180108
    
    On time: 20180109
    
    On time: 20180110
    
    On time: 20180111
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180112
    
    On time: 20180113
    
    On time: 20180114
    
    On time: 20180115
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180116
    
    On time: 20180117
    
    On time: 20180118
    
    On time: 20180119
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180120
    
    On time: 20180121
    
    On time: 20180122
    
    On time: 20180123
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180124
    
    On time: 20180125
    
    On time: 20180126
    
    On time: 20180127
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180128
    
    On time: 20180129
    
    On time: 20180130
    
    On time: 20180131
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180201
    
    On time: 20180202
    
    On time: 20180203
    
    On time: 20180204
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180205
    
    On time: 20180206
    
    On time: 20180207
    
    On time: 20180208
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180209
    
    On time: 20180210
    
    On time: 20180211
    
    On time: 20180212
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180213
    
    On time: 20180214
    
    On time: 20180215
    
    On time: 20180216
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180217
    
    On time: 20180218
    
    On time: 20180219
    
    On time: 20180220
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180221
    
    On time: 20180222
    
    On time: 20180223
    
    On time: 20180224
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180225
    
    On time: 20180226
    
    On time: 20180227
    
    On time: 20180228
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180301
    
    On time: 20180302
    
    On time: 20180303
    
    On time: 20180304
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180305
    
    On time: 20180306
    
    On time: 20180307
    
    On time: 20180308
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180309
    
    On time: 20180310
    
    On time: 20180311
    
    On time: 20180312
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180313
    
    On time: 20180314
    
    On time: 20180315
    
    On time: 20180316
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180317
    
    On time: 20180318
    
    On time: 20180319
    
    On time: 20180320
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180321
    
    On time: 20180322
    
    On time: 20180323
    
    On time: 20180324
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180325
    
    On time: 20180326
    
    On time: 20180327
    
    On time: 20180328
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180329
    
    On time: 20180330
    
    On time: 20180331
    
    On time: 20180401
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180402
    
    On time: 20180403
    
    On time: 20180404
    
    On time: 20180405
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180406
    
    On time: 20180407
    
    On time: 20180408
    
    On time: 20180409
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180410
    
    On time: 20180411
    
    On time: 20180412
    
    On time: 20180413
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180414
    
    On time: 20180415
    
    On time: 20180416
    
    On time: 20180417
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180418
    
    On time: 20180419
    
    On time: 20180420
    
    On time: 20180421
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180422
    
    On time: 20180423
    
    On time: 20180424
    
    On time: 20180425
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180426
    
    On time: 20180427
    
    On time: 20180428
    
    On time: 20180429
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    On time: 20180430
    


    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/utils.py:31: RuntimeWarning: All-NaN slice encountered
      return func(*args, **kwargs)
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/core.py:121: RuntimeWarning: All-NaN slice encountered
      return func(*(_execute_task(a, cache) for a in args))
    /home/scottcha/miniconda3/envs/pangeo_small3/lib/python3.7/site-packages/dask/array/numpy_compat.py:40: RuntimeWarning: invalid value encountered in true_divide
      x = np.divide(x1, x2, out)


    No Errors
    CPU times: user 1.25 s, sys: 581 ms, total: 1.83 s
    Wall time: 14min 47s


### Then interpolate and filter those files

```python
#test_ignore
results
```




    [None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None,
     None]



```python
#test_ignore
#it seems that n_jobs > 8 introdces a lot of errors in to the netcdf write
#n_jobs = 6
#CPU times: user 1.83 s, sys: 830 ms, total: 2.66 s
#Wall time: 45min 18s
%time results = pgfs.interpolate_and_write_local()
```

    On time: 20171104
    On time: 20171101
    On time: 20171103On time: 20171102
    
    On time: 20171106
    On time: 20171105
    On time: 20171107
    On time: 20171108
    On time: 20171109
    On time: 20171110
    On time: 20171111
    On time: 20171112
    On time: 20171113
    On time: 20171114
    On time: 20171115
    On time: 20171116
    On time: 20171117
    On time: 20171118
    On time: 20171119
    On time: 20171120
    On time: 20171122
    On time: 20171121
    On time: 20171123
    On time: 20171124
    On time: 20171125
    On time: 20171126
    On time: 20171127
    On time: 20171128
    On time: 20171129
    On time: 20171130
    On time: 20171201
    On time: 20171202
    On time: 20171203
    On time: 20171204
    On time: 20171205
    On time: 20171206
    On time: 20171207
    On time: 20171208
    On time: 20171209
    On time: 20171210
    On time: 20171211
    On time: 20171212
    On time: 20171213
    On time: 20171214
    On time: 20171215
    On time: 20171216
    On time: 20171217
    On time: 20171218
    On time: 20171219
    On time: 20171220
    On time: 20171221
    On time: 20171222
    On time: 20171223
    On time: 20171224
    On time: 20171225
    On time: 20171226
    On time: 20171227
    On time: 20171228
    On time: 20171229
    On time: 20171230
    On time: 20171231
    On time: 20180101
    On time: 20180102
    On time: 20180103
    On time: 20180104
    On time: 20180105
    On time: 20180106
    On time: 20180107
    On time: 20180108
    On time: 20180109
    On time: 20180110
    On time: 20180111
    On time: 20180112
    On time: 20180113
    On time: 20180114
    On time: 20180115
    On time: 20180116
    On time: 20180117
    On time: 20180118
    On time: 20180119
    On time: 20180120
    On time: 20180121
    On time: 20180122
    On time: 20180123
    On time: 20180124
    On time: 20180125
    On time: 20180127
    On time: 20180126
    On time: 20180128
    On time: 20180129
    On time: 20180130
    On time: 20180131
    On time: 20180201
    On time: 20180202
    On time: 20180203
    On time: 20180204
    On time: 20180205
    On time: 20180206
    On time: 20180207
    On time: 20180208
    On time: 20180209
    On time: 20180210
    On time: 20180211
    On time: 20180212
    On time: 20180213
    On time: 20180214
    On time: 20180215
    On time: 20180216
    On time: 20180217
    On time: 20180218
    On time: 20180219
    On time: 20180220
    On time: 20180221
    On time: 20180222
    On time: 20180223
    On time: 20180224
    On time: 20180225
    On time: 20180226
    On time: 20180227
    On time: 20180228
    On time: 20180301
    On time: 20180302
    On time: 20180303
    On time: 20180304
    On time: 20180305
    On time: 20180306
    On time: 20180307
    On time: 20180308
    On time: 20180309
    On time: 20180310
    On time: 20180311
    On time: 20180312
    On time: 20180313
    On time: 20180314
    On time: 20180315
    On time: 20180316
    On time: 20180317
    On time: 20180318
    On time: 20180319
    On time: 20180320
    On time: 20180321
    On time: 20180322
    On time: 20180323
    On time: 20180324
    On time: 20180325
    On time: 20180326
    On time: 20180327
    On time: 20180328
    On time: 20180329
    On time: 20180330
    On time: 20180331
    On time: 20180401
    On time: 20180402
    On time: 20180403
    On time: 20180404
    On time: 20180405
    On time: 20180406
    On time: 20180407
    On time: 20180408
    On time: 20180409
    On time: 20180410
    On time: 20180411
    On time: 20180412
    On time: 20180413
    On time: 20180414
    On time: 20180415
    On time: 20180416
    On time: 20180417
    On time: 20180418
    On time: 20180419
    On time: 20180420
    On time: 20180421
    On time: 20180422
    On time: 20180423
    On time: 20180424
    On time: 20180425
    On time: 20180426
    On time: 20180427
    On time: 20180428
    On time: 20180429
    On time: 20180430
    No Errors, go to ConvertToZarr
    CPU times: user 2.04 s, sys: 679 ms, total: 2.72 s
    Wall time: 42min 30s


### If interpolate and write returns errors you can retry them individually like:

```python
#test_ignore
#individually fix any potenital file write errors
redo = ['20151103', '20151105']
#fix any errors
redo2 = []
for r in redo:
    print('on ' + r)
    a, b = interpolate_and_write(r)
    if len(b) > 0:
        redo2.append(b)
```

### Once the converstion is complete for a set of seasons and states we need to convert the batch to Zarr

```python
#test_ignore
#currently only have Washington regions and one season specified for the tutorial
#uncomment regions and seasons if doing a larger transform
regions = {
           'Washington': ['Mt Hood', 'Olympics', 'Snoqualmie Pass', 'Stevens Pass',
           'WA Cascades East, Central', 'WA Cascades East, North', 'WA Cascades East, South',
           'WA Cascades West, Central', 'WA Cascades West, Mt Baker', 'WA Cascades West, South']
           }
seasons = ['17-18']
```

### 4. ConvertToZarr
#### Reformat data in to efficient Zarr format
The next step in our data transformation pipeline is to transform the NetCDF files to Zarr files which are indexed in such a way to make access to specific dates and lat/lon pairs as efficient as possible. This process can be run entirely end to end once you are sure the parameters are set correctly.  It does take about 6 hours on my workstation using all cores.  The imporant item about this notebook is that we are essentially indexing the data to be accessed efficiently when we create our ML datasets. 


```python
#test_ignore
ctz = ConvertToZarr(seasons, regions, data_root)
```

```python
#test_ignore
ctz.convert_local()
```

    On Region_Snoqualmie Pass_20171101.ncOn Region_WA Cascades East, Central_20171101.ncOn Region_Mt Hood_20171101.ncOn Region_WA Cascades East, North_20171101.ncOn Region_Stevens Pass_20171101.ncOn Region_Olympics_20171101.ncOn Region_WA Cascades West, Central_20171101.nc
    On Region_WA Cascades East, South_20171101.nc
    
    
    
    
    
    
    On Region_WA Cascades West, South_20171101.nc
    On Region_WA Cascades West, Mt Baker_20171101.nc
    On Region_Stevens Pass_20171102.nc
    On Region_Mt Hood_20171102.nc
    On Region_Snoqualmie Pass_20171102.nc
    On Region_Stevens Pass_20171103.nc
    On Region_Mt Hood_20171103.nc
    On Region_Stevens Pass_20171104.nc
    On Region_Mt Hood_20171104.nc
    On Region_Snoqualmie Pass_20171103.nc
    On Region_Stevens Pass_20171105.nc
    On Region_Mt Hood_20171105.nc
    On Region_Olympics_20171102.nc
    On Region_WA Cascades East, South_20171102.nc
    On Region_Snoqualmie Pass_20171104.nc
    On Region_Stevens Pass_20171106.nc
    On Region_Mt Hood_20171106.nc
    On Region_WA Cascades West, Mt Baker_20171102.nc
    On Region_Stevens Pass_20171107.nc
    On Region_Mt Hood_20171107.nc
    On Region_WA Cascades West, Central_20171102.nc
    On Region_Snoqualmie Pass_20171105.nc
    On Region_Stevens Pass_20171108.nc
    On Region_Mt Hood_20171108.nc
    On Region_WA Cascades West, South_20171102.nc
    On Region_WA Cascades East, Central_20171102.nc
    On Region_Stevens Pass_20171109.nc
    On Region_Mt Hood_20171109.nc
    On Region_Snoqualmie Pass_20171106.nc
    On Region_WA Cascades East, North_20171102.nc
    On Region_Olympics_20171103.nc
    On Region_Mt Hood_20171110.nc
    On Region_Stevens Pass_20171110.nc
    On Region_Snoqualmie Pass_20171107.nc
    On Region_WA Cascades East, South_20171103.nc
    On Region_Mt Hood_20171111.nc
    On Region_Stevens Pass_20171111.nc
    On Region_WA Cascades West, Mt Baker_20171103.nc
    On Region_Mt Hood_20171112.nc
    On Region_Stevens Pass_20171112.nc
    On Region_Snoqualmie Pass_20171108.nc
    On Region_Mt Hood_20171113.nc
    On Region_Stevens Pass_20171113.nc
    On Region_Olympics_20171104.nc
    On Region_Snoqualmie Pass_20171109.nc
    On Region_Mt Hood_20171114.nc
    On Region_Stevens Pass_20171114.nc
    On Region_WA Cascades West, Central_20171103.nc
    On Region_Mt Hood_20171115.nc
    On Region_Stevens Pass_20171115.nc
    On Region_Snoqualmie Pass_20171110.nc
    On Region_WA Cascades East, South_20171104.nc
    On Region_Mt Hood_20171116.nc
    On Region_Stevens Pass_20171116.nc
    On Region_WA Cascades East, Central_20171103.nc
    On Region_WA Cascades West, South_20171103.nc
    On Region_WA Cascades West, Mt Baker_20171104.nc
    On Region_Mt Hood_20171117.nc
    On Region_Stevens Pass_20171117.nc
    On Region_Snoqualmie Pass_20171111.nc
    On Region_WA Cascades East, North_20171103.nc
    On Region_Olympics_20171105.nc
    On Region_Mt Hood_20171118.nc
    On Region_Stevens Pass_20171118.nc
    On Region_Snoqualmie Pass_20171112.nc
    On Region_Mt Hood_20171119.nc
    On Region_Stevens Pass_20171119.nc
    On Region_Mt Hood_20171120.nc
    On Region_Stevens Pass_20171120.nc
    On Region_Snoqualmie Pass_20171113.nc
    On Region_WA Cascades West, Central_20171104.nc
    On Region_WA Cascades East, South_20171105.nc
    On Region_Stevens Pass_20171121.nc
    On Region_Mt Hood_20171121.nc
    On Region_Olympics_20171106.nc
    On Region_WA Cascades West, Mt Baker_20171105.nc
    On Region_Stevens Pass_20171122.nc
    On Region_Mt Hood_20171122.nc
    On Region_Snoqualmie Pass_20171114.nc
    On Region_Mt Hood_20171123.nc
    On Region_Stevens Pass_20171123.nc
    On Region_Snoqualmie Pass_20171115.nc
    On Region_Mt Hood_20171124.nc
    On Region_Stevens Pass_20171124.nc
    On Region_WA Cascades East, Central_20171104.nc
    On Region_WA Cascades West, South_20171104.nc
    On Region_Mt Hood_20171125.nc
    On Region_Stevens Pass_20171125.nc
    On Region_Snoqualmie Pass_20171116.nc
    On Region_Olympics_20171107.nc
    On Region_WA Cascades East, North_20171104.nc
    On Region_Mt Hood_20171126.nc
    On Region_Stevens Pass_20171126.nc
    On Region_WA Cascades East, South_20171106.nc
    On Region_WA Cascades West, Mt Baker_20171106.nc
    On Region_Snoqualmie Pass_20171117.nc
    On Region_Stevens Pass_20171127.nc
    On Region_Mt Hood_20171127.nc
    On Region_WA Cascades West, Central_20171105.nc
    On Region_Stevens Pass_20171128.nc
    On Region_Mt Hood_20171128.nc
    On Region_Snoqualmie Pass_20171118.nc
    On Region_Stevens Pass_20171129.nc
    On Region_Mt Hood_20171129.nc
    On Region_Olympics_20171108.nc
    On Region_Stevens Pass_20171130.nc
    On Region_Snoqualmie Pass_20171119.nc
    On Region_Mt Hood_20171130.nc
    On Region_Stevens Pass_20171201.nc
    On Region_Mt Hood_20171201.nc
    On Region_WA Cascades East, South_20171107.nc
    On Region_Snoqualmie Pass_20171120.nc
    On Region_WA Cascades West, Mt Baker_20171107.nc
    On Region_WA Cascades West, South_20171105.nc
    On Region_Stevens Pass_20171202.nc
    On Region_WA Cascades East, Central_20171105.nc
    On Region_Mt Hood_20171202.nc
    On Region_Stevens Pass_20171203.nc
    On Region_Mt Hood_20171203.nc
    On Region_Snoqualmie Pass_20171121.nc
    On Region_WA Cascades East, North_20171105.nc
    On Region_Olympics_20171109.nc
    On Region_Stevens Pass_20171204.nc
    On Region_Mt Hood_20171204.nc
    On Region_WA Cascades West, Central_20171106.nc
    On Region_Snoqualmie Pass_20171122.nc
    On Region_Stevens Pass_20171205.nc
    On Region_Mt Hood_20171205.nc
    On Region_Stevens Pass_20171206.nc
    On Region_WA Cascades East, South_20171108.nc
    On Region_Mt Hood_20171206.nc
    On Region_Snoqualmie Pass_20171123.nc
    On Region_WA Cascades West, Mt Baker_20171108.nc
    On Region_Stevens Pass_20171207.nc
    On Region_Mt Hood_20171207.nc
    On Region_Olympics_20171110.nc
    On Region_Snoqualmie Pass_20171124.nc
    On Region_Stevens Pass_20171208.nc
    On Region_Mt Hood_20171208.nc
    On Region_Stevens Pass_20171209.nc
    On Region_Mt Hood_20171209.nc
    On Region_WA Cascades West, South_20171106.nc
    On Region_Snoqualmie Pass_20171125.nc
    On Region_WA Cascades East, Central_20171106.nc
    On Region_Stevens Pass_20171210.nc
    On Region_Mt Hood_20171210.nc
    On Region_WA Cascades West, Central_20171107.nc
    On Region_Stevens Pass_20171211.nc
    On Region_WA Cascades East, South_20171109.nc
    On Region_Mt Hood_20171211.nc
    On Region_Snoqualmie Pass_20171126.nc
    On Region_WA Cascades East, North_20171106.nc
    On Region_Olympics_20171111.nc
    On Region_WA Cascades West, Mt Baker_20171109.nc
    On Region_Stevens Pass_20171212.nc
    On Region_Mt Hood_20171212.nc
    On Region_Snoqualmie Pass_20171127.nc
    On Region_Stevens Pass_20171213.nc
    On Region_Mt Hood_20171213.nc
    On Region_Stevens Pass_20171214.nc
    On Region_Mt Hood_20171214.nc
    On Region_Snoqualmie Pass_20171128.nc
    On Region_Stevens Pass_20171215.nc
    On Region_Mt Hood_20171215.nc
    On Region_WA Cascades East, South_20171110.nc
    On Region_Snoqualmie Pass_20171129.nc
    On Region_Stevens Pass_20171216.nc
    On Region_Olympics_20171112.nc
    On Region_Mt Hood_20171216.nc
    On Region_WA Cascades West, Mt Baker_20171110.nc
    On Region_Stevens Pass_20171217.nc
    On Region_Mt Hood_20171217.nc
    On Region_WA Cascades West, South_20171107.nc
    On Region_WA Cascades West, Central_20171108.nc
    On Region_WA Cascades East, Central_20171107.nc
    On Region_Snoqualmie Pass_20171130.nc
    On Region_Stevens Pass_20171218.nc
    On Region_Mt Hood_20171218.nc
    On Region_Stevens Pass_20171219.nc
    On Region_Mt Hood_20171219.nc
    On Region_Snoqualmie Pass_20171201.nc
    On Region_Stevens Pass_20171220.nc
    On Region_Mt Hood_20171220.nc
    On Region_WA Cascades East, North_20171107.nc
    On Region_Olympics_20171113.nc
    On Region_Snoqualmie Pass_20171202.nc
    On Region_Stevens Pass_20171221.nc
    On Region_WA Cascades East, South_20171111.nc
    On Region_Mt Hood_20171221.nc
    On Region_WA Cascades West, Mt Baker_20171111.nc
    On Region_Stevens Pass_20171222.nc
    On Region_Mt Hood_20171222.nc
    On Region_Snoqualmie Pass_20171203.nc
    On Region_Stevens Pass_20171223.nc
    On Region_Mt Hood_20171223.nc
    On Region_WA Cascades West, Central_20171109.nc
    On Region_Snoqualmie Pass_20171204.nc
    On Region_Stevens Pass_20171224.nc
    On Region_Olympics_20171114.nc
    On Region_Mt Hood_20171224.nc
    On Region_WA Cascades West, South_20171108.nc
    On Region_Stevens Pass_20171225.nc
    On Region_Mt Hood_20171225.nc
    On Region_WA Cascades East, Central_20171108.nc
    On Region_Snoqualmie Pass_20171205.nc
    On Region_Stevens Pass_20171226.nc
    On Region_WA Cascades East, South_20171112.nc
    On Region_Mt Hood_20171226.nc
    On Region_WA Cascades West, Mt Baker_20171112.nc
    On Region_Stevens Pass_20171227.nc
    On Region_Snoqualmie Pass_20171206.nc
    On Region_Mt Hood_20171227.nc
    On Region_Stevens Pass_20171228.nc
    On Region_Olympics_20171115.nc
    On Region_Mt Hood_20171228.nc
    On Region_WA Cascades East, North_20171108.nc
    On Region_Snoqualmie Pass_20171207.nc
    On Region_Stevens Pass_20171229.nc
    On Region_Mt Hood_20171229.nc
    On Region_Stevens Pass_20171230.nc
    On Region_Mt Hood_20171230.nc
    On Region_Snoqualmie Pass_20171208.nc
    On Region_WA Cascades West, Central_20171110.nc
    On Region_Stevens Pass_20171231.nc
    On Region_WA Cascades East, South_20171113.nc
    On Region_Mt Hood_20171231.nc
    On Region_WA Cascades West, Mt Baker_20171113.nc
    On Region_Snoqualmie Pass_20171209.nc
    On Region_Stevens Pass_20180101.nc
    On Region_Mt Hood_20180101.nc
    On Region_Olympics_20171116.nc
    On Region_WA Cascades West, South_20171109.nc
    On Region_WA Cascades East, Central_20171109.nc
    On Region_Stevens Pass_20180102.nc
    On Region_Mt Hood_20180102.nc
    On Region_Snoqualmie Pass_20171210.nc
    On Region_Stevens Pass_20180103.nc
    On Region_Mt Hood_20180103.nc
    On Region_Snoqualmie Pass_20171211.nc
    On Region_Stevens Pass_20180104.nc
    On Region_Mt Hood_20180104.nc
    On Region_Stevens Pass_20180105.nc
    On Region_WA Cascades East, South_20171114.nc
    On Region_Olympics_20171117.nc
    On Region_Mt Hood_20180105.nc
    On Region_WA Cascades East, North_20171109.nc
    On Region_Snoqualmie Pass_20171212.nc
    On Region_WA Cascades West, Mt Baker_20171114.nc
    On Region_WA Cascades West, Central_20171111.nc
    On Region_Stevens Pass_20180106.nc
    On Region_Mt Hood_20180106.nc
    On Region_Stevens Pass_20180107.nc
    On Region_Snoqualmie Pass_20171213.nc
    On Region_Mt Hood_20180107.nc
    On Region_Stevens Pass_20180108.nc
    On Region_Mt Hood_20180108.nc
    On Region_Snoqualmie Pass_20171214.nc
    On Region_Stevens Pass_20180109.nc
    On Region_Mt Hood_20180109.nc
    On Region_Olympics_20171118.nc
    On Region_WA Cascades East, Central_20171110.nc
    On Region_WA Cascades West, South_20171110.nc
    On Region_Stevens Pass_20180110.nc
    On Region_WA Cascades East, South_20171115.nc
    On Region_Mt Hood_20180110.nc
    On Region_Snoqualmie Pass_20171215.nc
    On Region_WA Cascades West, Mt Baker_20171115.nc
    On Region_Stevens Pass_20180111.nc
    On Region_Mt Hood_20180111.nc
    On Region_Snoqualmie Pass_20171216.nc
    On Region_Stevens Pass_20180112.nc
    On Region_WA Cascades West, Central_20171112.nc
    On Region_Mt Hood_20180112.nc
    On Region_Stevens Pass_20180113.nc
    On Region_Mt Hood_20180113.nc
    On Region_WA Cascades East, North_20171110.nc
    On Region_Olympics_20171119.nc
    On Region_Snoqualmie Pass_20171217.nc
    On Region_Stevens Pass_20180114.nc
    On Region_Mt Hood_20180114.nc
    On Region_Stevens Pass_20180115.nc
    On Region_Snoqualmie Pass_20171218.nc
    On Region_WA Cascades East, South_20171116.nc
    On Region_Mt Hood_20180115.nc
    On Region_WA Cascades West, Mt Baker_20171116.nc
    On Region_Stevens Pass_20180116.nc
    On Region_Mt Hood_20180116.nc
    On Region_Snoqualmie Pass_20171219.nc
    On Region_Stevens Pass_20180117.nc
    On Region_WA Cascades East, Central_20171111.nc
    On Region_WA Cascades West, South_20171111.nc
    On Region_Mt Hood_20180117.nc
    On Region_Olympics_20171120.nc
    On Region_Stevens Pass_20180118.nc
    On Region_Snoqualmie Pass_20171220.nc
    On Region_Mt Hood_20180118.nc
    On Region_WA Cascades West, Central_20171113.nc
    On Region_Stevens Pass_20180119.nc
    On Region_Mt Hood_20180119.nc
    On Region_Snoqualmie Pass_20171221.nc
    On Region_Stevens Pass_20180120.nc
    On Region_WA Cascades East, South_20171117.nc
    On Region_Mt Hood_20180120.nc
    On Region_WA Cascades West, Mt Baker_20171117.nc
    On Region_Stevens Pass_20180121.nc
    On Region_WA Cascades East, North_20171111.nc
    On Region_Mt Hood_20180121.nc
    On Region_Snoqualmie Pass_20171222.nc
    On Region_Olympics_20171121.nc
    On Region_Stevens Pass_20180122.nc
    On Region_Mt Hood_20180122.nc
    On Region_Stevens Pass_20180123.nc
    On Region_Snoqualmie Pass_20171223.nc
    On Region_Mt Hood_20180123.nc
    On Region_Stevens Pass_20180124.nc
    On Region_Mt Hood_20180124.nc
    On Region_Snoqualmie Pass_20171224.nc
    On Region_WA Cascades East, Central_20171112.nc
    On Region_WA Cascades West, South_20171112.nc
    On Region_Stevens Pass_20180125.nc
    On Region_WA Cascades West, Central_20171114.nc
    On Region_WA Cascades East, South_20171118.nc
    On Region_WA Cascades West, Mt Baker_20171118.nc
    On Region_Mt Hood_20180125.nc
    On Region_Olympics_20171122.nc
    On Region_Stevens Pass_20180126.nc
    On Region_Snoqualmie Pass_20171225.nc
    On Region_Mt Hood_20180126.nc
    On Region_Stevens Pass_20180127.nc
    On Region_Mt Hood_20180127.nc
    On Region_Snoqualmie Pass_20171226.nc
    On Region_Stevens Pass_20180128.nc
    On Region_Mt Hood_20180128.nc
    On Region_Stevens Pass_20180129.nc
    On Region_Mt Hood_20180129.nc
    On Region_WA Cascades East, North_20171112.nc
    On Region_Snoqualmie Pass_20171227.nc
    On Region_Olympics_20171123.nc
    On Region_WA Cascades East, South_20171119.nc
    On Region_Stevens Pass_20180130.nc
    On Region_WA Cascades West, Mt Baker_20171119.nc
    On Region_Mt Hood_20180130.nc
    On Region_Snoqualmie Pass_20171228.nc
    On Region_Stevens Pass_20180131.nc
    On Region_Mt Hood_20180131.nc
    On Region_WA Cascades West, Central_20171115.nc
    On Region_Stevens Pass_20180201.nc
    On Region_Mt Hood_20180201.nc
    On Region_WA Cascades West, South_20171113.nc
    On Region_WA Cascades East, Central_20171113.nc
    On Region_Snoqualmie Pass_20171229.nc
    On Region_Stevens Pass_20180202.nc
    On Region_Mt Hood_20180202.nc
    On Region_Olympics_20171124.nc
    On Region_Stevens Pass_20180203.nc
    On Region_Mt Hood_20180203.nc
    On Region_Snoqualmie Pass_20171230.nc
    On Region_WA Cascades East, South_20171120.nc
    On Region_WA Cascades West, Mt Baker_20171120.nc
    On Region_Stevens Pass_20180204.nc
    On Region_Mt Hood_20180204.nc
    On Region_Snoqualmie Pass_20171231.nc
    On Region_Stevens Pass_20180205.nc
    On Region_Mt Hood_20180205.nc
    On Region_Stevens Pass_20180206.nc
    On Region_Mt Hood_20180206.nc
    On Region_Snoqualmie Pass_20180101.nc
    On Region_WA Cascades East, North_20171113.nc
    On Region_Olympics_20171125.nc
    On Region_WA Cascades West, Central_20171116.nc
    On Region_Stevens Pass_20180207.nc
    On Region_Mt Hood_20180207.nc
    On Region_Snoqualmie Pass_20180102.nc
    On Region_Stevens Pass_20180208.nc
    On Region_Mt Hood_20180208.nc
    On Region_WA Cascades East, South_20171121.nc
    On Region_WA Cascades West, Mt Baker_20171121.nc
    On Region_Stevens Pass_20180209.nc
    On Region_WA Cascades East, Central_20171114.nc
    On Region_Mt Hood_20180209.nc
    On Region_WA Cascades West, South_20171114.nc
    On Region_Snoqualmie Pass_20180103.nc
    On Region_Stevens Pass_20180210.nc
    On Region_Mt Hood_20180210.nc
    On Region_Olympics_20171126.nc
    On Region_Snoqualmie Pass_20180104.nc
    On Region_Stevens Pass_20180211.nc
    On Region_Mt Hood_20180211.nc
    On Region_Stevens Pass_20180212.nc
    On Region_Mt Hood_20180212.nc
    On Region_Snoqualmie Pass_20180105.nc
    On Region_Stevens Pass_20180213.nc
    On Region_Mt Hood_20180213.nc
    On Region_WA Cascades West, Central_20171117.nc
    On Region_WA Cascades East, South_20171122.nc
    On Region_WA Cascades West, Mt Baker_20171122.nc
    On Region_Snoqualmie Pass_20180106.nc
    On Region_Mt Hood_20180214.nc
    On Region_Stevens Pass_20180214.nc
    On Region_Olympics_20171127.nc
    On Region_WA Cascades East, North_20171114.nc
    On Region_Mt Hood_20180215.nc
    On Region_Stevens Pass_20180215.nc
    On Region_Snoqualmie Pass_20180107.nc
    On Region_Mt Hood_20180216.nc
    On Region_Stevens Pass_20180216.nc
    On Region_WA Cascades East, Central_20171115.nc
    On Region_WA Cascades West, South_20171115.nc
    On Region_Mt Hood_20180217.nc
    On Region_Stevens Pass_20180217.nc
    On Region_Snoqualmie Pass_20180108.nc
    On Region_Mt Hood_20180218.nc
    On Region_Stevens Pass_20180218.nc
    On Region_Olympics_20171128.nc
    On Region_WA Cascades East, South_20171123.nc
    On Region_Snoqualmie Pass_20180109.nc
    On Region_WA Cascades West, Mt Baker_20171123.nc
    On Region_Mt Hood_20180219.nc
    On Region_Stevens Pass_20180219.nc
    On Region_WA Cascades West, Central_20171118.nc
    On Region_Mt Hood_20180220.nc
    On Region_Stevens Pass_20180220.nc
    On Region_Snoqualmie Pass_20180110.nc
    On Region_Mt Hood_20180221.nc
    On Region_Stevens Pass_20180221.nc
    On Region_Snoqualmie Pass_20180111.nc
    On Region_Mt Hood_20180222.nc
    On Region_Stevens Pass_20180222.nc
    On Region_Olympics_20171129.nc
    On Region_WA Cascades East, North_20171115.nc
    On Region_Mt Hood_20180223.nc
    On Region_Stevens Pass_20180223.nc
    On Region_Snoqualmie Pass_20180112.nc
    On Region_WA Cascades East, South_20171124.nc
    On Region_WA Cascades West, Mt Baker_20171124.nc
    On Region_Mt Hood_20180224.nc
    On Region_Stevens Pass_20180224.nc
    On Region_WA Cascades East, Central_20171116.nc
    On Region_WA Cascades West, South_20171116.nc
    On Region_Stevens Pass_20180225.nc
    On Region_Mt Hood_20180225.nc
    On Region_Snoqualmie Pass_20180113.nc
    On Region_Stevens Pass_20180226.nc
    On Region_Mt Hood_20180226.nc
    On Region_WA Cascades West, Central_20171119.nc
    On Region_Olympics_20171130.nc
    On Region_Snoqualmie Pass_20180114.nc
    On Region_Stevens Pass_20180227.nc
    On Region_Mt Hood_20180227.nc
    On Region_Stevens Pass_20180228.nc
    On Region_Mt Hood_20180228.nc
    On Region_Snoqualmie Pass_20180115.nc
    On Region_WA Cascades East, South_20171125.nc
    On Region_WA Cascades West, Mt Baker_20171125.nc
    On Region_Stevens Pass_20180301.nc
    On Region_Mt Hood_20180301.nc
    On Region_Snoqualmie Pass_20180116.nc
    On Region_Stevens Pass_20180302.nc
    On Region_Mt Hood_20180302.nc
    On Region_Olympics_20171201.nc
    On Region_WA Cascades East, North_20171116.nc
    On Region_Stevens Pass_20180303.nc
    On Region_Mt Hood_20180303.nc
    On Region_Snoqualmie Pass_20180117.nc
    On Region_WA Cascades East, Central_20171117.nc
    On Region_Stevens Pass_20180304.nc
    On Region_Mt Hood_20180304.nc
    On Region_WA Cascades West, South_20171117.nc
    On Region_WA Cascades West, Central_20171120.nc
    On Region_Snoqualmie Pass_20180118.nc
    On Region_Stevens Pass_20180305.nc
    On Region_Mt Hood_20180305.nc
    On Region_WA Cascades East, South_20171126.nc
    On Region_WA Cascades West, Mt Baker_20171126.nc
    On Region_Stevens Pass_20180306.nc
    On Region_Mt Hood_20180306.nc
    On Region_Olympics_20171202.nc
    On Region_Snoqualmie Pass_20180119.nc
    On Region_Stevens Pass_20180307.nc
    On Region_Mt Hood_20180307.nc
    On Region_Snoqualmie Pass_20180120.nc
    On Region_Stevens Pass_20180308.nc
    On Region_Mt Hood_20180308.nc
    On Region_Stevens Pass_20180309.nc
    On Region_Mt Hood_20180309.nc
    On Region_Snoqualmie Pass_20180121.nc
    On Region_Stevens Pass_20180310.nc
    On Region_Mt Hood_20180310.nc
    On Region_WA Cascades East, South_20171127.nc
    On Region_WA Cascades West, Mt Baker_20171127.nc
    On Region_Olympics_20171203.nc
    On Region_WA Cascades East, North_20171117.nc
    On Region_WA Cascades West, Central_20171121.nc
    On Region_Stevens Pass_20180311.nc
    On Region_Mt Hood_20180311.nc
    On Region_Snoqualmie Pass_20180122.nc
    On Region_WA Cascades East, Central_20171118.nc
    On Region_WA Cascades West, South_20171118.nc
    On Region_Stevens Pass_20180312.nc
    On Region_Mt Hood_20180312.nc
    On Region_Snoqualmie Pass_20180123.nc
    On Region_Stevens Pass_20180313.nc
    On Region_Mt Hood_20180313.nc
    On Region_Stevens Pass_20180314.nc
    On Region_Mt Hood_20180314.nc
    On Region_Snoqualmie Pass_20180124.nc
    On Region_Olympics_20171204.nc
    On Region_WA Cascades East, South_20171128.nc
    On Region_Stevens Pass_20180315.nc
    On Region_Mt Hood_20180315.nc
    On Region_WA Cascades West, Mt Baker_20171128.nc
    On Region_Snoqualmie Pass_20180125.nc
    On Region_Stevens Pass_20180316.nc
    On Region_Mt Hood_20180316.nc
    On Region_Stevens Pass_20180317.nc
    On Region_Mt Hood_20180317.nc
    On Region_WA Cascades West, Central_20171122.nc
    On Region_Snoqualmie Pass_20180126.nc
    On Region_Stevens Pass_20180318.nc
    On Region_Mt Hood_20180318.nc
    On Region_Olympics_20171205.nc
    On Region_WA Cascades East, North_20171118.nc
    On Region_WA Cascades East, Central_20171119.nc
    On Region_Snoqualmie Pass_20180127.nc
    On Region_Stevens Pass_20180319.nc
    On Region_Mt Hood_20180319.nc
    On Region_WA Cascades West, South_20171119.nc
    On Region_WA Cascades East, South_20171129.nc
    On Region_WA Cascades West, Mt Baker_20171129.nc
    On Region_Stevens Pass_20180320.nc
    On Region_Mt Hood_20180320.nc
    On Region_Snoqualmie Pass_20180128.nc
    On Region_Stevens Pass_20180321.nc
    On Region_Mt Hood_20180321.nc
    On Region_Stevens Pass_20180322.nc
    On Region_Mt Hood_20180322.nc
    On Region_Snoqualmie Pass_20180129.nc
    On Region_Olympics_20171206.nc
    On Region_Stevens Pass_20180323.nc
    On Region_Mt Hood_20180323.nc
    On Region_WA Cascades West, Central_20171123.nc
    On Region_Snoqualmie Pass_20180130.nc
    On Region_Stevens Pass_20180324.nc
    On Region_Mt Hood_20180324.nc
    On Region_WA Cascades East, South_20171130.nc
    On Region_WA Cascades West, Mt Baker_20171130.nc
    On Region_Stevens Pass_20180325.nc
    On Region_Mt Hood_20180325.nc
    On Region_Snoqualmie Pass_20180131.nc
    On Region_WA Cascades East, North_20171119.nc
    On Region_WA Cascades East, Central_20171120.nc
    On Region_Stevens Pass_20180326.nc
    On Region_Mt Hood_20180326.nc
    On Region_Olympics_20171207.nc
    On Region_WA Cascades West, South_20171120.nc
    On Region_Snoqualmie Pass_20180201.nc
    On Region_Mt Hood_20180327.nc
    On Region_Stevens Pass_20180327.nc
    On Region_Mt Hood_20180328.nc
    On Region_Stevens Pass_20180328.nc
    On Region_Snoqualmie Pass_20180202.nc
    On Region_Mt Hood_20180329.nc
    On Region_Stevens Pass_20180329.nc
    On Region_WA Cascades West, Mt Baker_20171201.nc
    On Region_WA Cascades East, South_20171201.nc
    On Region_Snoqualmie Pass_20180203.nc
    On Region_Mt Hood_20180330.nc
    On Region_WA Cascades West, Central_20171124.nc
    On Region_Stevens Pass_20180330.nc
    On Region_Olympics_20171208.nc
    On Region_Mt Hood_20180331.nc
    On Region_Stevens Pass_20180331.nc
    On Region_Snoqualmie Pass_20180204.nc
    On Region_Mt Hood_20180401.nc
    On Region_Stevens Pass_20180401.nc
    On Region_Mt Hood_20180402.nc
    On Region_Snoqualmie Pass_20180205.nc
    On Region_Stevens Pass_20180402.nc
    On Region_WA Cascades East, Central_20171121.nc
    On Region_WA Cascades East, North_20171120.nc
    On Region_Mt Hood_20180403.nc
    On Region_Stevens Pass_20180403.nc
    On Region_WA Cascades West, Mt Baker_20171202.nc
    On Region_Olympics_20171209.nc
    On Region_WA Cascades West, South_20171121.nc
    On Region_WA Cascades East, South_20171202.nc
    On Region_Snoqualmie Pass_20180206.nc
    On Region_Mt Hood_20180404.nc
    On Region_Stevens Pass_20180404.nc
    On Region_Mt Hood_20180405.nc
    On Region_Snoqualmie Pass_20180207.nc
    On Region_Stevens Pass_20180405.nc
    On Region_WA Cascades West, Central_20171125.nc
    On Region_Mt Hood_20180406.nc
    On Region_Stevens Pass_20180406.nc
    On Region_Snoqualmie Pass_20180208.nc
    On Region_Olympics_20171210.nc
    On Region_Mt Hood_20180407.nc
    On Region_Stevens Pass_20180407.nc
    On Region_WA Cascades West, Mt Baker_20171203.nc
    On Region_Mt Hood_20180408.nc
    On Region_Snoqualmie Pass_20180209.nc
    On Region_Stevens Pass_20180408.nc
    On Region_WA Cascades East, South_20171203.nc
    On Region_Mt Hood_20180409.nc
    On Region_Stevens Pass_20180409.nc
    On Region_Snoqualmie Pass_20180210.nc
    On Region_Mt Hood_20180410.nc
    On Region_WA Cascades East, Central_20171122.nc
    On Region_Stevens Pass_20180410.nc
    On Region_WA Cascades East, North_20171121.nc
    On Region_WA Cascades West, South_20171122.nc
    On Region_Olympics_20171211.nc
    On Region_Mt Hood_20180411.nc
    On Region_Stevens Pass_20180411.nc
    On Region_Snoqualmie Pass_20180211.nc
    On Region_WA Cascades West, Central_20171126.nc
    On Region_Mt Hood_20180412.nc
    On Region_Stevens Pass_20180412.nc
    On Region_WA Cascades West, Mt Baker_20171204.nc
    On Region_Snoqualmie Pass_20180212.nc
    On Region_Mt Hood_20180413.nc
    On Region_Stevens Pass_20180413.nc
    On Region_WA Cascades East, South_20171204.nc
    On Region_Mt Hood_20180414.nc
    On Region_Stevens Pass_20180414.nc
    On Region_Snoqualmie Pass_20180213.nc
    On Region_Olympics_20171212.nc
    On Region_Mt Hood_20180415.nc
    On Region_Stevens Pass_20180415.nc
    On Region_Snoqualmie Pass_20180214.nc
    On Region_Mt Hood_20180416.nc
    On Region_Stevens Pass_20180416.nc
    On Region_Mt Hood_20180417.nc
    On Region_Stevens Pass_20180417.nc
    On Region_WA Cascades East, Central_20171123.nc
    On Region_WA Cascades West, Mt Baker_20171205.nc
    On Region_WA Cascades West, Central_20171127.nc
    On Region_Snoqualmie Pass_20180215.nc
    On Region_WA Cascades West, South_20171123.nc
    On Region_WA Cascades East, North_20171122.nc
    On Region_WA Cascades East, South_20171205.nc
    On Region_Mt Hood_20180418.nc
    On Region_Stevens Pass_20180418.nc
    On Region_Olympics_20171213.nc
    On Region_Mt Hood_20180419.nc
    On Region_Snoqualmie Pass_20180216.nc
    On Region_Stevens Pass_20180419.nc
    On Region_Mt Hood_20180420.nc
    On Region_Stevens Pass_20180420.nc
    On Region_Snoqualmie Pass_20180217.nc
    On Region_Mt Hood_20180421.nc
    On Region_Stevens Pass_20180421.nc
    On Region_Mt Hood_20180422.nc
    On Region_WA Cascades West, Mt Baker_20171206.nc
    On Region_Stevens Pass_20180422.nc
    On Region_Snoqualmie Pass_20180218.nc
    On Region_WA Cascades East, South_20171206.nc
    On Region_Olympics_20171214.nc
    On Region_Mt Hood_20180423.nc
    On Region_Stevens Pass_20180423.nc
    On Region_WA Cascades West, Central_20171128.nc
    On Region_Snoqualmie Pass_20180219.nc
    On Region_Mt Hood_20180424.nc
    On Region_Stevens Pass_20180424.nc
    On Region_WA Cascades East, Central_20171124.nc
    On Region_WA Cascades West, South_20171124.nc
    On Region_Mt Hood_20180425.nc
    On Region_Stevens Pass_20180425.nc
    On Region_WA Cascades East, North_20171123.nc
    On Region_Snoqualmie Pass_20180220.nc
    On Region_Mt Hood_20180426.nc
    On Region_Stevens Pass_20180426.nc
    On Region_Olympics_20171215.nc
    On Region_Mt Hood_20180427.nc
    On Region_WA Cascades West, Mt Baker_20171207.nc
    On Region_Snoqualmie Pass_20180221.nc
    On Region_Stevens Pass_20180427.nc
    On Region_WA Cascades East, South_20171207.nc
    On Region_Mt Hood_20180428.nc
    On Region_Stevens Pass_20180428.nc
    On Region_Snoqualmie Pass_20180222.nc
    On Region_Mt Hood_20180429.nc
    On Region_Stevens Pass_20180429.nc
    On Region_Mt Hood_20180430.nc
    On Region_WA Cascades West, Central_20171129.nc
    On Region_Snoqualmie Pass_20180223.nc
    On Region_Stevens Pass_20180430.nc
    On Region_Olympics_20171216.nc
    On Region_Snoqualmie Pass_20180224.nc
    On Region_WA Cascades West, Mt Baker_20171208.nc
    On Region_WA Cascades East, Central_20171125.nc
    On Region_WA Cascades East, South_20171208.nc
    On Region_WA Cascades West, South_20171125.nc
    On Region_WA Cascades East, North_20171124.nc
    On Region_Snoqualmie Pass_20180225.nc
    On Region_Olympics_20171217.nc
    On Region_Snoqualmie Pass_20180226.nc
    On Region_Snoqualmie Pass_20180227.nc
    On Region_WA Cascades West, Central_20171130.nc
    On Region_WA Cascades West, Mt Baker_20171209.nc
    On Region_WA Cascades East, South_20171209.nc
    On Region_Snoqualmie Pass_20180228.nc
    On Region_Olympics_20171218.nc
    On Region_WA Cascades East, Central_20171126.nc
    On Region_Snoqualmie Pass_20180301.nc
    On Region_WA Cascades West, South_20171126.nc
    On Region_WA Cascades East, North_20171125.nc
    On Region_Snoqualmie Pass_20180302.nc
    On Region_WA Cascades West, Mt Baker_20171210.nc
    On Region_WA Cascades East, South_20171210.nc
    On Region_Snoqualmie Pass_20180303.nc
    On Region_WA Cascades West, Central_20171201.nc
    On Region_Olympics_20171219.nc
    On Region_Snoqualmie Pass_20180304.nc
    On Region_Snoqualmie Pass_20180305.nc
    On Region_WA Cascades West, Mt Baker_20171211.nc
    On Region_WA Cascades East, South_20171211.nc
    On Region_WA Cascades East, Central_20171127.nc
    On Region_Olympics_20171220.nc
    On Region_Snoqualmie Pass_20180306.nc
    On Region_WA Cascades West, South_20171127.nc
    On Region_WA Cascades East, North_20171126.nc
    On Region_Snoqualmie Pass_20180307.nc
    On Region_WA Cascades West, Central_20171202.nc
    On Region_Snoqualmie Pass_20180308.nc
    On Region_Olympics_20171221.nc
    On Region_WA Cascades West, Mt Baker_20171212.nc
    On Region_WA Cascades East, South_20171212.nc
    On Region_Snoqualmie Pass_20180309.nc
    On Region_Snoqualmie Pass_20180310.nc
    On Region_WA Cascades East, Central_20171128.nc
    On Region_Olympics_20171222.nc
    On Region_WA Cascades West, South_20171128.nc
    On Region_Snoqualmie Pass_20180311.nc
    On Region_WA Cascades West, Central_20171203.nc
    On Region_WA Cascades West, Mt Baker_20171213.nc
    On Region_WA Cascades East, South_20171213.nc
    On Region_WA Cascades East, North_20171127.nc
    On Region_Snoqualmie Pass_20180312.nc
    On Region_Snoqualmie Pass_20180313.nc
    On Region_Olympics_20171223.nc
    On Region_Snoqualmie Pass_20180314.nc
    On Region_WA Cascades West, Mt Baker_20171214.nc
    On Region_WA Cascades East, South_20171214.nc
    On Region_WA Cascades West, Central_20171204.nc
    On Region_Snoqualmie Pass_20180315.nc
    On Region_WA Cascades East, Central_20171129.nc
    On Region_WA Cascades West, South_20171129.nc
    On Region_Olympics_20171224.nc
    On Region_Snoqualmie Pass_20180316.nc
    On Region_WA Cascades East, North_20171128.nc
    On Region_Snoqualmie Pass_20180317.nc
    On Region_WA Cascades West, Mt Baker_20171215.nc
    On Region_WA Cascades East, South_20171215.nc
    On Region_Snoqualmie Pass_20180318.nc
    On Region_Olympics_20171225.nc
    On Region_Snoqualmie Pass_20180319.nc
    On Region_WA Cascades West, Central_20171205.nc
    On Region_WA Cascades East, Central_20171130.nc
    On Region_Snoqualmie Pass_20180320.nc
    On Region_WA Cascades West, Mt Baker_20171216.nc
    On Region_WA Cascades West, South_20171130.nc
    On Region_WA Cascades East, South_20171216.nc
    On Region_Olympics_20171226.nc
    On Region_Snoqualmie Pass_20180321.nc
    On Region_Snoqualmie Pass_20180322.nc
    On Region_WA Cascades East, North_20171129.nc
    On Region_Snoqualmie Pass_20180323.nc
    On Region_Olympics_20171227.nc
    On Region_WA Cascades West, Central_20171206.nc
    On Region_WA Cascades West, Mt Baker_20171217.nc
    On Region_WA Cascades East, South_20171217.nc
    On Region_Snoqualmie Pass_20180324.nc
    On Region_WA Cascades East, Central_20171201.nc
    On Region_Snoqualmie Pass_20180325.nc
    On Region_WA Cascades West, South_20171201.nc
    On Region_Olympics_20171228.nc
    On Region_Snoqualmie Pass_20180326.nc
    On Region_WA Cascades West, Mt Baker_20171218.nc
    On Region_WA Cascades East, South_20171218.nc
    On Region_Snoqualmie Pass_20180327.nc
    On Region_WA Cascades East, North_20171130.nc
    On Region_WA Cascades West, Central_20171207.nc
    On Region_Snoqualmie Pass_20180328.nc
    On Region_Olympics_20171229.nc
    On Region_Snoqualmie Pass_20180329.nc
    On Region_WA Cascades East, Central_20171202.nc
    On Region_WA Cascades West, Mt Baker_20171219.nc
    On Region_WA Cascades East, South_20171219.nc
    On Region_Snoqualmie Pass_20180330.nc
    On Region_WA Cascades West, South_20171202.nc
    On Region_Olympics_20171230.nc
    On Region_Snoqualmie Pass_20180331.nc
    On Region_WA Cascades West, Central_20171208.nc
    On Region_Snoqualmie Pass_20180401.nc
    On Region_WA Cascades East, North_20171201.nc
    On Region_WA Cascades West, Mt Baker_20171220.nc
    On Region_WA Cascades East, South_20171220.nc
    On Region_Snoqualmie Pass_20180402.nc
    On Region_Olympics_20171231.nc
    On Region_Snoqualmie Pass_20180403.nc
    On Region_WA Cascades East, Central_20171203.nc
    On Region_WA Cascades West, South_20171203.nc
    On Region_Snoqualmie Pass_20180404.nc
    On Region_WA Cascades West, Central_20171209.nc
    On Region_WA Cascades West, Mt Baker_20171221.nc
    On Region_WA Cascades East, South_20171221.nc
    On Region_Snoqualmie Pass_20180405.nc
    On Region_Olympics_20180101.nc
    On Region_Snoqualmie Pass_20180406.nc
    On Region_WA Cascades East, North_20171202.nc
    On Region_Snoqualmie Pass_20180407.nc
    On Region_Olympics_20180102.nc
    On Region_WA Cascades West, Mt Baker_20171222.nc
    On Region_WA Cascades East, South_20171222.nc
    On Region_Snoqualmie Pass_20180408.nc
    On Region_WA Cascades East, Central_20171204.nc
    On Region_WA Cascades West, Central_20171210.nc
    On Region_WA Cascades West, South_20171204.nc
    On Region_Snoqualmie Pass_20180409.nc
    On Region_Snoqualmie Pass_20180410.nc
    On Region_Olympics_20180103.nc
    On Region_WA Cascades West, Mt Baker_20171223.nc
    On Region_Snoqualmie Pass_20180411.nc
    On Region_WA Cascades East, South_20171223.nc
    On Region_WA Cascades East, North_20171203.nc
    On Region_Snoqualmie Pass_20180412.nc
    On Region_WA Cascades West, Central_20171211.nc
    On Region_Olympics_20180104.nc
    On Region_WA Cascades East, Central_20171205.nc
    On Region_Snoqualmie Pass_20180413.nc
    On Region_WA Cascades West, South_20171205.nc
    On Region_Snoqualmie Pass_20180414.nc
    On Region_WA Cascades West, Mt Baker_20171224.nc
    On Region_WA Cascades East, South_20171224.nc
    On Region_Snoqualmie Pass_20180415.nc
    On Region_Olympics_20180105.nc
    On Region_Snoqualmie Pass_20180416.nc
    On Region_WA Cascades West, Central_20171212.nc
    On Region_WA Cascades East, North_20171204.nc
    On Region_Snoqualmie Pass_20180417.nc
    On Region_WA Cascades West, Mt Baker_20171225.nc
    On Region_WA Cascades East, South_20171225.nc
    On Region_WA Cascades East, Central_20171206.nc
    On Region_Olympics_20180106.nc
    On Region_Snoqualmie Pass_20180418.nc
    On Region_WA Cascades West, South_20171206.nc
    On Region_Snoqualmie Pass_20180419.nc
    On Region_Snoqualmie Pass_20180420.nc
    On Region_WA Cascades West, Mt Baker_20171226.nc
    On Region_WA Cascades East, South_20171226.nc
    On Region_Olympics_20180107.nc
    On Region_WA Cascades West, Central_20171213.nc
    On Region_Snoqualmie Pass_20180421.nc
    On Region_WA Cascades East, North_20171205.nc
    On Region_Snoqualmie Pass_20180422.nc
    On Region_WA Cascades East, Central_20171207.nc
    On Region_Olympics_20180108.nc
    On Region_Snoqualmie Pass_20180423.nc
    On Region_WA Cascades West, South_20171207.nc
    On Region_WA Cascades West, Mt Baker_20171227.nc
    On Region_WA Cascades East, South_20171227.nc
    On Region_Snoqualmie Pass_20180424.nc
    On Region_WA Cascades West, Central_20171214.nc
    On Region_Snoqualmie Pass_20180425.nc
    On Region_Olympics_20180109.nc
    On Region_Snoqualmie Pass_20180426.nc
    On Region_WA Cascades West, Mt Baker_20171228.nc
    On Region_WA Cascades East, South_20171228.nc
    On Region_WA Cascades East, North_20171206.nc
    On Region_Snoqualmie Pass_20180427.nc
    On Region_WA Cascades East, Central_20171208.nc
    On Region_WA Cascades West, South_20171208.nc
    On Region_Olympics_20180110.nc
    On Region_Snoqualmie Pass_20180428.nc
    On Region_WA Cascades West, Central_20171215.nc
    On Region_Snoqualmie Pass_20180429.nc
    On Region_WA Cascades West, Mt Baker_20171229.nc
    On Region_WA Cascades East, South_20171229.nc
    On Region_Snoqualmie Pass_20180430.nc
    On Region_Olympics_20180111.nc
    On Region_WA Cascades East, Central_20171209.nc
    On Region_WA Cascades East, North_20171207.nc
    On Region_WA Cascades West, Mt Baker_20171230.nc
    On Region_WA Cascades East, South_20171230.nc
    On Region_WA Cascades West, South_20171209.nc
    On Region_WA Cascades West, Central_20171216.nc
    On Region_Olympics_20180112.nc
    On Region_WA Cascades West, Mt Baker_20171231.nc
    On Region_Olympics_20180113.nc
    On Region_WA Cascades East, South_20171231.nc
    On Region_WA Cascades East, Central_20171210.nc
    On Region_WA Cascades West, Central_20171217.nc
    On Region_WA Cascades East, North_20171208.nc
    On Region_WA Cascades West, South_20171210.nc
    On Region_Olympics_20180114.nc
    On Region_WA Cascades West, Mt Baker_20180101.nc
    On Region_WA Cascades East, South_20180101.nc
    On Region_Olympics_20180115.nc
    On Region_WA Cascades West, Central_20171218.nc
    On Region_WA Cascades East, Central_20171211.nc
    On Region_WA Cascades West, Mt Baker_20180102.nc
    On Region_WA Cascades East, South_20180102.nc
    On Region_WA Cascades East, North_20171209.nc
    On Region_WA Cascades West, South_20171211.nc
    On Region_Olympics_20180116.nc
    On Region_WA Cascades West, Mt Baker_20180103.nc
    On Region_WA Cascades East, South_20180103.nc
    On Region_WA Cascades West, Central_20171219.nc
    On Region_Olympics_20180117.nc
    On Region_WA Cascades East, Central_20171212.nc
    On Region_WA Cascades East, North_20171210.nc
    On Region_WA Cascades West, South_20171212.nc
    On Region_WA Cascades West, Mt Baker_20180104.nc
    On Region_WA Cascades East, South_20180104.nc
    On Region_Olympics_20180118.nc
    On Region_WA Cascades West, Central_20171220.nc
    On Region_WA Cascades East, South_20180105.nc
    On Region_WA Cascades East, Central_20171213.nc
    On Region_WA Cascades West, Mt Baker_20180105.nc
    On Region_Olympics_20180119.nc
    On Region_WA Cascades West, South_20171213.nc
    On Region_WA Cascades East, North_20171211.nc
    On Region_Olympics_20180120.nc
    On Region_WA Cascades West, Central_20171221.nc
    On Region_WA Cascades East, South_20180106.nc
    On Region_WA Cascades West, Mt Baker_20180106.nc
    On Region_WA Cascades East, Central_20171214.nc
    On Region_Olympics_20180121.nc
    On Region_WA Cascades West, South_20171214.nc
    On Region_WA Cascades East, South_20180107.nc
    On Region_WA Cascades West, Mt Baker_20180107.nc
    On Region_WA Cascades East, North_20171212.nc
    On Region_WA Cascades West, Central_20171222.nc
    On Region_Olympics_20180122.nc
    On Region_WA Cascades East, South_20180108.nc
    On Region_WA Cascades West, Mt Baker_20180108.nc
    On Region_WA Cascades East, Central_20171215.nc
    On Region_Olympics_20180123.nc
    On Region_WA Cascades West, South_20171215.nc
    On Region_WA Cascades West, Central_20171223.nc
    On Region_WA Cascades East, North_20171213.nc
    On Region_WA Cascades East, South_20180109.nc
    On Region_WA Cascades West, Mt Baker_20180109.nc
    On Region_Olympics_20180124.nc
    On Region_WA Cascades East, Central_20171216.nc
    On Region_WA Cascades West, Central_20171224.nc
    On Region_WA Cascades East, South_20180110.nc
    On Region_WA Cascades West, Mt Baker_20180110.nc
    On Region_WA Cascades West, South_20171216.nc
    On Region_Olympics_20180125.nc
    On Region_WA Cascades East, North_20171214.nc
    On Region_Olympics_20180126.nc
    On Region_WA Cascades East, South_20180111.nc
    On Region_WA Cascades West, Mt Baker_20180111.nc
    On Region_WA Cascades West, Central_20171225.nc
    On Region_WA Cascades East, Central_20171217.nc
    On Region_WA Cascades West, South_20171217.nc
    On Region_Olympics_20180127.nc
    On Region_WA Cascades East, South_20180112.nc
    On Region_WA Cascades West, Mt Baker_20180112.nc
    On Region_WA Cascades East, North_20171215.nc
    On Region_Olympics_20180128.nc
    On Region_WA Cascades West, Central_20171226.nc
    On Region_WA Cascades East, South_20180113.nc
    On Region_WA Cascades East, Central_20171218.nc
    On Region_WA Cascades West, Mt Baker_20180113.nc
    On Region_WA Cascades West, South_20171218.nc
    On Region_Olympics_20180129.nc
    On Region_WA Cascades East, North_20171216.nc
    On Region_WA Cascades West, Central_20171227.nc
    On Region_WA Cascades East, South_20180114.nc
    On Region_WA Cascades West, Mt Baker_20180114.nc
    On Region_Olympics_20180130.nc
    On Region_WA Cascades East, Central_20171219.nc
    On Region_WA Cascades West, South_20171219.nc
    On Region_WA Cascades East, South_20180115.nc
    On Region_Olympics_20180131.nc
    On Region_WA Cascades West, Mt Baker_20180115.nc
    On Region_WA Cascades West, Central_20171228.nc
    On Region_WA Cascades East, North_20171217.nc
    On Region_Olympics_20180201.nc
    On Region_WA Cascades East, South_20180116.nc
    On Region_WA Cascades West, Mt Baker_20180116.nc
    On Region_WA Cascades East, Central_20171220.nc
    On Region_WA Cascades West, South_20171220.nc
    On Region_WA Cascades West, Central_20171229.nc
    On Region_Olympics_20180202.nc
    On Region_WA Cascades East, South_20180117.nc
    On Region_WA Cascades West, Mt Baker_20180117.nc
    On Region_WA Cascades East, North_20171218.nc
    On Region_Olympics_20180203.nc
    On Region_WA Cascades West, Central_20171230.nc
    On Region_WA Cascades East, Central_20171221.nc
    On Region_WA Cascades West, South_20171221.nc
    On Region_WA Cascades East, South_20180118.nc
    On Region_WA Cascades West, Mt Baker_20180118.nc
    On Region_Olympics_20180204.nc
    On Region_WA Cascades East, North_20171219.nc
    On Region_WA Cascades East, South_20180119.nc
    On Region_WA Cascades West, Mt Baker_20180119.nc
    On Region_WA Cascades West, Central_20171231.nc
    On Region_Olympics_20180205.nc
    On Region_WA Cascades East, Central_20171222.nc
    On Region_WA Cascades West, South_20171222.nc
    On Region_WA Cascades East, South_20180120.nc
    On Region_WA Cascades West, Mt Baker_20180120.nc
    On Region_Olympics_20180206.nc
    On Region_WA Cascades East, North_20171220.nc
    On Region_WA Cascades West, Central_20180101.nc
    On Region_Olympics_20180207.nc
    On Region_WA Cascades East, South_20180121.nc
    On Region_WA Cascades East, Central_20171223.nc
    On Region_WA Cascades West, Mt Baker_20180121.nc
    On Region_WA Cascades West, South_20171223.nc
    On Region_Olympics_20180208.nc
    On Region_WA Cascades West, Central_20180102.nc
    On Region_WA Cascades East, South_20180122.nc
    On Region_WA Cascades West, Mt Baker_20180122.nc
    On Region_WA Cascades East, North_20171221.nc
    On Region_WA Cascades East, Central_20171224.nc
    On Region_Olympics_20180209.nc
    On Region_WA Cascades West, South_20171224.nc
    On Region_WA Cascades East, South_20180123.nc
    On Region_WA Cascades West, Mt Baker_20180123.nc
    On Region_WA Cascades West, Central_20180103.nc
    On Region_Olympics_20180210.nc
    On Region_WA Cascades East, North_20171222.nc
    On Region_WA Cascades East, South_20180124.nc
    On Region_WA Cascades West, Mt Baker_20180124.nc
    On Region_WA Cascades East, Central_20171225.nc
    On Region_WA Cascades West, South_20171225.nc
    On Region_Olympics_20180211.nc
    On Region_WA Cascades West, Central_20180104.nc
    On Region_WA Cascades East, South_20180125.nc
    On Region_WA Cascades West, Mt Baker_20180125.nc
    On Region_Olympics_20180212.nc
    On Region_WA Cascades East, North_20171223.nc
    On Region_WA Cascades East, Central_20171226.nc
    On Region_WA Cascades West, Central_20180105.nc
    On Region_WA Cascades West, South_20171226.nc
    On Region_WA Cascades East, South_20180126.nc
    On Region_Olympics_20180213.nc
    On Region_WA Cascades West, Mt Baker_20180126.nc
    On Region_Olympics_20180214.nc
    On Region_WA Cascades East, South_20180127.nc
    On Region_WA Cascades West, Mt Baker_20180127.nc
    On Region_WA Cascades West, Central_20180106.nc
    On Region_WA Cascades East, North_20171224.nc
    On Region_WA Cascades East, Central_20171227.nc
    On Region_WA Cascades West, South_20171227.nc
    On Region_Olympics_20180215.nc
    On Region_WA Cascades East, South_20180128.nc
    On Region_WA Cascades West, Mt Baker_20180128.nc
    On Region_WA Cascades West, Central_20180107.nc
    On Region_Olympics_20180216.nc
    On Region_WA Cascades East, Central_20171228.nc
    On Region_WA Cascades East, North_20171225.nc
    On Region_WA Cascades West, South_20171228.nc
    On Region_WA Cascades East, South_20180129.nc
    On Region_WA Cascades West, Mt Baker_20180129.nc
    On Region_Olympics_20180217.nc
    On Region_WA Cascades West, Central_20180108.nc
    On Region_WA Cascades West, Mt Baker_20180130.nc
    On Region_WA Cascades East, South_20180130.nc
    On Region_WA Cascades East, Central_20171229.nc
    On Region_Olympics_20180218.nc
    On Region_WA Cascades East, North_20171226.nc
    On Region_WA Cascades West, South_20171229.nc
    On Region_WA Cascades West, Mt Baker_20180131.nc
    On Region_WA Cascades East, South_20180131.nc
    On Region_Olympics_20180219.nc
    On Region_WA Cascades West, Central_20180109.nc
    On Region_WA Cascades East, Central_20171230.nc
    On Region_Olympics_20180220.nc
    On Region_WA Cascades West, Mt Baker_20180201.nc
    On Region_WA Cascades West, South_20171230.nc
    On Region_WA Cascades East, North_20171227.nc
    On Region_WA Cascades East, South_20180201.nc
    On Region_WA Cascades West, Central_20180110.nc
    On Region_Olympics_20180221.nc
    On Region_WA Cascades West, Mt Baker_20180202.nc
    On Region_WA Cascades East, South_20180202.nc
    On Region_WA Cascades East, Central_20171231.nc
    On Region_WA Cascades West, South_20171231.nc
    On Region_Olympics_20180222.nc
    On Region_WA Cascades East, North_20171228.nc
    On Region_WA Cascades West, Central_20180111.nc
    On Region_WA Cascades West, Mt Baker_20180203.nc
    On Region_WA Cascades East, South_20180203.nc
    On Region_Olympics_20180223.nc
    On Region_WA Cascades West, Mt Baker_20180204.nc
    On Region_WA Cascades East, Central_20180101.nc
    On Region_WA Cascades East, South_20180204.nc
    On Region_WA Cascades West, South_20180101.nc
    On Region_WA Cascades West, Central_20180112.nc
    On Region_Olympics_20180224.nc
    On Region_WA Cascades East, North_20171229.nc
    On Region_WA Cascades West, Mt Baker_20180205.nc
    On Region_WA Cascades East, South_20180205.nc
    On Region_Olympics_20180225.nc
    On Region_WA Cascades West, Central_20180113.nc
    On Region_WA Cascades East, Central_20180102.nc
    On Region_WA Cascades West, South_20180102.nc
    On Region_Olympics_20180226.nc
    On Region_WA Cascades East, North_20171230.nc
    On Region_WA Cascades West, Mt Baker_20180206.nc
    On Region_WA Cascades East, South_20180206.nc
    On Region_Olympics_20180227.nc
    On Region_WA Cascades West, Central_20180114.nc
    On Region_WA Cascades West, Mt Baker_20180207.nc
    On Region_WA Cascades East, South_20180207.nc
    On Region_WA Cascades East, Central_20180103.nc
    On Region_WA Cascades West, South_20180103.nc
    On Region_Olympics_20180228.nc
    On Region_WA Cascades East, North_20171231.nc
    On Region_WA Cascades West, Mt Baker_20180208.nc
    On Region_WA Cascades East, South_20180208.nc
    On Region_WA Cascades West, Central_20180115.nc
    On Region_Olympics_20180301.nc
    On Region_WA Cascades East, Central_20180104.nc
    On Region_WA Cascades West, South_20180104.nc
    On Region_WA Cascades West, Mt Baker_20180209.nc
    On Region_WA Cascades East, South_20180209.nc
    On Region_WA Cascades East, North_20180101.nc
    On Region_Olympics_20180302.nc
    On Region_WA Cascades West, Central_20180116.nc
    On Region_WA Cascades West, Mt Baker_20180210.nc
    On Region_WA Cascades East, South_20180210.nc
    On Region_Olympics_20180303.nc
    On Region_WA Cascades East, Central_20180105.nc
    On Region_WA Cascades West, South_20180105.nc
    On Region_WA Cascades West, Central_20180117.nc
    On Region_WA Cascades East, North_20180102.nc
    On Region_Olympics_20180304.nc
    On Region_WA Cascades West, Mt Baker_20180211.nc
    On Region_WA Cascades East, South_20180211.nc
    On Region_Olympics_20180305.nc
    On Region_WA Cascades East, Central_20180106.nc
    On Region_WA Cascades East, South_20180212.nc
    On Region_WA Cascades West, Mt Baker_20180212.nc
    On Region_WA Cascades West, South_20180106.nc
    On Region_WA Cascades West, Central_20180118.nc
    On Region_WA Cascades East, North_20180103.nc
    On Region_Olympics_20180306.nc
    On Region_WA Cascades East, South_20180213.nc
    On Region_WA Cascades West, Mt Baker_20180213.nc
    On Region_Olympics_20180307.nc
    On Region_WA Cascades East, Central_20180107.nc
    On Region_WA Cascades West, Central_20180119.nc
    On Region_WA Cascades West, South_20180107.nc
    On Region_WA Cascades East, South_20180214.nc
    On Region_WA Cascades West, Mt Baker_20180214.nc
    On Region_WA Cascades East, North_20180104.nc
    On Region_Olympics_20180308.nc
    On Region_WA Cascades West, Central_20180120.nc
    On Region_WA Cascades East, Central_20180108.nc
    On Region_WA Cascades East, South_20180215.nc
    On Region_WA Cascades West, Mt Baker_20180215.nc
    On Region_Olympics_20180309.nc
    On Region_WA Cascades West, South_20180108.nc
    On Region_Olympics_20180310.nc
    On Region_WA Cascades East, North_20180105.nc
    On Region_WA Cascades West, Mt Baker_20180216.nc
    On Region_WA Cascades East, South_20180216.nc
    On Region_WA Cascades West, Central_20180121.nc
    On Region_WA Cascades East, Central_20180109.nc
    On Region_Olympics_20180311.nc
    On Region_WA Cascades West, South_20180109.nc
    On Region_WA Cascades West, Mt Baker_20180217.nc
    On Region_WA Cascades East, South_20180217.nc
    On Region_WA Cascades East, North_20180106.nc
    On Region_Olympics_20180312.nc
    On Region_WA Cascades West, Central_20180122.nc
    On Region_WA Cascades West, Mt Baker_20180218.nc
    On Region_WA Cascades East, South_20180218.nc
    On Region_WA Cascades East, Central_20180110.nc
    On Region_Olympics_20180313.nc
    On Region_WA Cascades West, South_20180110.nc
    On Region_WA Cascades West, Central_20180123.nc
    On Region_WA Cascades East, South_20180219.nc
    On Region_WA Cascades West, Mt Baker_20180219.nc
    On Region_WA Cascades East, North_20180107.nc
    On Region_Olympics_20180314.nc
    On Region_WA Cascades East, Central_20180111.nc
    On Region_Olympics_20180315.nc
    On Region_WA Cascades West, Mt Baker_20180220.nc
    On Region_WA Cascades East, South_20180220.nc
    On Region_WA Cascades West, South_20180111.nc
    On Region_WA Cascades West, Central_20180124.nc
    On Region_Olympics_20180316.nc
    On Region_WA Cascades East, North_20180108.nc
    On Region_WA Cascades West, Mt Baker_20180221.nc
    On Region_WA Cascades East, South_20180221.nc
    On Region_WA Cascades East, Central_20180112.nc
    On Region_Olympics_20180317.nc
    On Region_WA Cascades West, Central_20180125.nc
    On Region_WA Cascades West, South_20180112.nc
    On Region_WA Cascades West, Mt Baker_20180222.nc
    On Region_WA Cascades East, South_20180222.nc
    On Region_Olympics_20180318.nc
    On Region_WA Cascades East, North_20180109.nc
    On Region_WA Cascades East, Central_20180113.nc
    On Region_WA Cascades West, Central_20180126.nc
    On Region_WA Cascades West, Mt Baker_20180223.nc
    On Region_WA Cascades East, South_20180223.nc
    On Region_Olympics_20180319.nc
    On Region_WA Cascades West, South_20180113.nc
    On Region_WA Cascades West, Mt Baker_20180224.nc
    On Region_Olympics_20180320.nc
    On Region_WA Cascades East, South_20180224.nc
    On Region_WA Cascades East, North_20180110.nc
    On Region_WA Cascades West, Central_20180127.nc
    On Region_WA Cascades East, Central_20180114.nc
    On Region_Olympics_20180321.nc
    On Region_WA Cascades West, South_20180114.nc
    On Region_WA Cascades West, Mt Baker_20180225.nc
    On Region_WA Cascades East, South_20180225.nc
    On Region_WA Cascades West, Central_20180128.nc
    On Region_WA Cascades East, North_20180111.nc
    On Region_Olympics_20180322.nc
    On Region_WA Cascades West, Mt Baker_20180226.nc
    On Region_WA Cascades East, South_20180226.nc
    On Region_WA Cascades East, Central_20180115.nc
    On Region_WA Cascades West, South_20180115.nc
    On Region_Olympics_20180323.nc
    On Region_WA Cascades West, Central_20180129.nc
    On Region_WA Cascades East, South_20180227.nc
    On Region_WA Cascades West, Mt Baker_20180227.nc
    On Region_WA Cascades East, North_20180112.nc
    On Region_Olympics_20180324.nc
    On Region_WA Cascades East, Central_20180116.nc
    On Region_WA Cascades East, South_20180228.nc
    On Region_WA Cascades West, Mt Baker_20180228.nc
    On Region_WA Cascades West, South_20180116.nc
    On Region_WA Cascades West, Central_20180130.nc
    On Region_Olympics_20180325.nc
    On Region_WA Cascades West, Mt Baker_20180301.nc
    On Region_WA Cascades East, South_20180301.nc
    On Region_Olympics_20180326.nc
    On Region_WA Cascades East, North_20180113.nc
    On Region_WA Cascades East, Central_20180117.nc
    On Region_WA Cascades West, Central_20180131.nc
    On Region_WA Cascades West, South_20180117.nc
    On Region_Olympics_20180327.nc
    On Region_WA Cascades East, South_20180302.nc
    On Region_WA Cascades West, Mt Baker_20180302.nc
    On Region_Olympics_20180328.nc
    On Region_WA Cascades East, North_20180114.nc
    On Region_WA Cascades West, Central_20180201.nc
    On Region_WA Cascades East, Central_20180118.nc
    On Region_WA Cascades East, South_20180303.nc
    On Region_WA Cascades West, Mt Baker_20180303.nc
    On Region_WA Cascades West, South_20180118.nc
    On Region_Olympics_20180329.nc
    On Region_WA Cascades East, South_20180304.nc
    On Region_WA Cascades West, Mt Baker_20180304.nc
    On Region_WA Cascades West, Central_20180202.nc
    On Region_Olympics_20180330.nc
    On Region_WA Cascades East, North_20180115.nc
    On Region_WA Cascades East, Central_20180119.nc
    On Region_WA Cascades West, South_20180119.nc
    On Region_WA Cascades East, South_20180305.nc
    On Region_WA Cascades West, Mt Baker_20180305.nc
    On Region_Olympics_20180331.nc
    On Region_WA Cascades West, Central_20180203.nc
    On Region_WA Cascades East, South_20180306.nc
    On Region_WA Cascades East, North_20180116.nc
    On Region_WA Cascades West, Mt Baker_20180306.nc
    On Region_Olympics_20180401.nc
    On Region_WA Cascades East, Central_20180120.nc
    On Region_WA Cascades West, South_20180120.nc
    On Region_Olympics_20180402.nc
    On Region_WA Cascades West, Central_20180204.nc
    On Region_WA Cascades East, South_20180307.nc
    On Region_WA Cascades West, Mt Baker_20180307.nc
    On Region_WA Cascades East, North_20180117.nc
    On Region_WA Cascades East, Central_20180121.nc
    On Region_Olympics_20180403.nc
    On Region_WA Cascades East, South_20180308.nc
    On Region_WA Cascades West, South_20180121.nc
    On Region_WA Cascades West, Mt Baker_20180308.nc
    On Region_WA Cascades West, Central_20180205.nc
    On Region_Olympics_20180404.nc
    On Region_WA Cascades East, South_20180309.nc
    On Region_WA Cascades West, Mt Baker_20180309.nc
    On Region_WA Cascades East, Central_20180122.nc
    On Region_WA Cascades East, North_20180118.nc
    On Region_Olympics_20180405.nc
    On Region_WA Cascades West, South_20180122.nc
    On Region_WA Cascades West, Central_20180206.nc
    On Region_WA Cascades East, South_20180310.nc
    On Region_WA Cascades West, Mt Baker_20180310.nc
    On Region_Olympics_20180406.nc
    On Region_WA Cascades East, Central_20180123.nc
    On Region_WA Cascades East, North_20180119.nc
    On Region_WA Cascades West, Central_20180207.nc
    On Region_WA Cascades East, South_20180311.nc
    On Region_Olympics_20180407.nc
    On Region_WA Cascades West, Mt Baker_20180311.nc
    On Region_WA Cascades West, South_20180123.nc
    On Region_Olympics_20180408.nc
    On Region_WA Cascades East, South_20180312.nc
    On Region_WA Cascades West, Mt Baker_20180312.nc
    On Region_WA Cascades West, Central_20180208.nc
    On Region_WA Cascades East, Central_20180124.nc
    On Region_WA Cascades East, North_20180120.nc
    On Region_Olympics_20180409.nc
    On Region_WA Cascades West, South_20180124.nc
    On Region_WA Cascades East, South_20180313.nc
    On Region_WA Cascades West, Mt Baker_20180313.nc
    On Region_Olympics_20180410.nc
    On Region_WA Cascades West, Central_20180209.nc
    On Region_WA Cascades East, South_20180314.nc
    On Region_WA Cascades East, Central_20180125.nc
    On Region_WA Cascades West, Mt Baker_20180314.nc
    On Region_WA Cascades East, North_20180121.nc
    On Region_Olympics_20180411.nc
    On Region_WA Cascades West, South_20180125.nc
    On Region_WA Cascades West, Central_20180210.nc
    On Region_WA Cascades East, South_20180315.nc
    On Region_WA Cascades West, Mt Baker_20180315.nc
    On Region_Olympics_20180412.nc
    On Region_WA Cascades East, Central_20180126.nc
    On Region_WA Cascades East, North_20180122.nc
    On Region_Olympics_20180413.nc
    On Region_WA Cascades East, South_20180316.nc
    On Region_WA Cascades West, Mt Baker_20180316.nc
    On Region_WA Cascades West, South_20180126.nc
    On Region_WA Cascades West, Central_20180211.nc
    On Region_Olympics_20180414.nc
    On Region_WA Cascades East, South_20180317.nc
    On Region_WA Cascades West, Mt Baker_20180317.nc
    On Region_WA Cascades East, Central_20180127.nc
    On Region_WA Cascades East, North_20180123.nc
    On Region_Olympics_20180415.nc
    On Region_WA Cascades West, Central_20180212.nc
    On Region_WA Cascades West, South_20180127.nc
    On Region_WA Cascades East, South_20180318.nc
    On Region_WA Cascades West, Mt Baker_20180318.nc
    On Region_Olympics_20180416.nc
    On Region_WA Cascades East, Central_20180128.nc
    On Region_WA Cascades West, Central_20180213.nc
    On Region_WA Cascades East, North_20180124.nc
    On Region_WA Cascades East, South_20180319.nc
    On Region_WA Cascades West, Mt Baker_20180319.nc
    On Region_Olympics_20180417.nc
    On Region_WA Cascades West, South_20180128.nc
    On Region_Olympics_20180418.nc
    On Region_WA Cascades East, South_20180320.nc
    On Region_WA Cascades West, Mt Baker_20180320.nc
    On Region_WA Cascades West, Central_20180214.nc
    On Region_WA Cascades East, Central_20180129.nc
    On Region_WA Cascades East, North_20180125.nc
    On Region_WA Cascades West, South_20180129.nc
    On Region_Olympics_20180419.nc
    On Region_WA Cascades East, South_20180321.nc
    On Region_WA Cascades West, Mt Baker_20180321.nc
    On Region_WA Cascades West, Central_20180215.nc
    On Region_Olympics_20180420.nc
    On Region_WA Cascades East, Central_20180130.nc
    On Region_WA Cascades East, South_20180322.nc
    On Region_WA Cascades West, Mt Baker_20180322.nc
    On Region_WA Cascades East, North_20180126.nc
    On Region_WA Cascades West, South_20180130.nc
    On Region_Olympics_20180421.nc
    On Region_WA Cascades West, Central_20180216.nc
    On Region_WA Cascades East, South_20180323.nc
    On Region_WA Cascades West, Mt Baker_20180323.nc
    On Region_Olympics_20180422.nc
    On Region_WA Cascades East, Central_20180131.nc
    On Region_WA Cascades East, North_20180127.nc
    On Region_WA Cascades West, South_20180131.nc
    On Region_WA Cascades East, South_20180324.nc
    On Region_Olympics_20180423.nc
    On Region_WA Cascades West, Central_20180217.nc
    On Region_WA Cascades West, Mt Baker_20180324.nc
    On Region_Olympics_20180424.nc
    On Region_WA Cascades East, South_20180325.nc
    On Region_WA Cascades East, Central_20180201.nc
    On Region_WA Cascades West, Mt Baker_20180325.nc
    On Region_WA Cascades East, North_20180128.nc
    On Region_WA Cascades West, South_20180201.nc
    On Region_WA Cascades West, Central_20180218.nc
    On Region_Olympics_20180425.nc
    On Region_WA Cascades East, South_20180326.nc
    On Region_WA Cascades West, Mt Baker_20180326.nc
    On Region_Olympics_20180426.nc
    On Region_WA Cascades East, Central_20180202.nc
    On Region_WA Cascades West, Central_20180219.nc
    On Region_WA Cascades West, South_20180202.nc
    On Region_WA Cascades East, North_20180129.nc
    On Region_WA Cascades East, South_20180327.nc
    On Region_WA Cascades West, Mt Baker_20180327.nc
    On Region_Olympics_20180427.nc
    On Region_WA Cascades East, South_20180328.nc
    On Region_WA Cascades West, Central_20180220.nc
    On Region_WA Cascades West, Mt Baker_20180328.nc
    On Region_Olympics_20180428.nc
    On Region_WA Cascades East, Central_20180203.nc
    On Region_WA Cascades West, South_20180203.nc
    On Region_WA Cascades East, North_20180130.nc
    On Region_Olympics_20180429.nc
    On Region_WA Cascades East, South_20180329.nc
    On Region_WA Cascades West, Mt Baker_20180329.nc
    On Region_WA Cascades West, Central_20180221.nc
    On Region_Olympics_20180430.nc
    On Region_WA Cascades East, Central_20180204.nc
    On Region_WA Cascades West, South_20180204.nc
    On Region_WA Cascades East, South_20180330.nc
    On Region_WA Cascades West, Mt Baker_20180330.nc
    On Region_WA Cascades East, North_20180131.nc
    On Region_WA Cascades West, Central_20180222.nc
    On Region_WA Cascades East, South_20180331.nc
    On Region_WA Cascades West, Mt Baker_20180331.nc
    On Region_WA Cascades East, Central_20180205.nc
    On Region_WA Cascades West, South_20180205.nc
    On Region_WA Cascades East, North_20180201.nc
    On Region_WA Cascades West, Central_20180223.nc
    On Region_WA Cascades East, South_20180401.nc
    On Region_WA Cascades West, Mt Baker_20180401.nc
    On Region_WA Cascades East, Central_20180206.nc
    On Region_WA Cascades East, South_20180402.nc
    On Region_WA Cascades West, South_20180206.nc
    On Region_WA Cascades West, Mt Baker_20180402.nc
    On Region_WA Cascades West, Central_20180224.nc
    On Region_WA Cascades East, North_20180202.nc
    On Region_WA Cascades East, South_20180403.nc
    On Region_WA Cascades West, Mt Baker_20180403.nc
    On Region_WA Cascades East, Central_20180207.nc
    On Region_WA Cascades West, Central_20180225.nc
    On Region_WA Cascades West, South_20180207.nc
    On Region_WA Cascades East, South_20180404.nc
    On Region_WA Cascades East, North_20180203.nc
    On Region_WA Cascades West, Mt Baker_20180404.nc
    On Region_WA Cascades West, Central_20180226.nc
    On Region_WA Cascades East, Central_20180208.nc
    On Region_WA Cascades East, South_20180405.nc
    On Region_WA Cascades West, South_20180208.nc
    On Region_WA Cascades West, Mt Baker_20180405.nc
    On Region_WA Cascades East, North_20180204.nc
    On Region_WA Cascades East, South_20180406.nc
    On Region_WA Cascades West, Mt Baker_20180406.nc
    On Region_WA Cascades West, Central_20180227.nc
    On Region_WA Cascades East, Central_20180209.nc
    On Region_WA Cascades West, South_20180209.nc
    On Region_WA Cascades East, South_20180407.nc
    On Region_WA Cascades West, Mt Baker_20180407.nc
    On Region_WA Cascades East, North_20180205.nc
    On Region_WA Cascades West, Central_20180228.nc
    On Region_WA Cascades East, South_20180408.nc
    On Region_WA Cascades West, Mt Baker_20180408.nc
    On Region_WA Cascades East, Central_20180210.nc
    On Region_WA Cascades West, South_20180210.nc
    On Region_WA Cascades West, Central_20180301.nc
    On Region_WA Cascades East, North_20180206.nc
    On Region_WA Cascades East, South_20180409.nc
    On Region_WA Cascades West, Mt Baker_20180409.nc
    On Region_WA Cascades East, Central_20180211.nc
    On Region_WA Cascades West, South_20180211.nc
    On Region_WA Cascades East, South_20180410.nc
    On Region_WA Cascades West, Central_20180302.nc
    On Region_WA Cascades West, Mt Baker_20180410.nc
    On Region_WA Cascades East, North_20180207.nc
    On Region_WA Cascades East, South_20180411.nc
    On Region_WA Cascades West, Mt Baker_20180411.nc
    On Region_WA Cascades East, Central_20180212.nc
    On Region_WA Cascades West, South_20180212.nc
    On Region_WA Cascades West, Central_20180303.nc
    On Region_WA Cascades East, South_20180412.nc
    On Region_WA Cascades West, Mt Baker_20180412.nc
    On Region_WA Cascades East, North_20180208.nc
    On Region_WA Cascades West, Central_20180304.nc
    On Region_WA Cascades East, Central_20180213.nc
    On Region_WA Cascades West, South_20180213.nc
    On Region_WA Cascades East, South_20180413.nc
    On Region_WA Cascades West, Mt Baker_20180413.nc
    On Region_WA Cascades East, North_20180209.nc
    On Region_WA Cascades West, Central_20180305.nc
    On Region_WA Cascades East, South_20180414.nc
    On Region_WA Cascades West, Mt Baker_20180414.nc
    On Region_WA Cascades East, Central_20180214.nc
    On Region_WA Cascades West, South_20180214.nc
    On Region_WA Cascades East, South_20180415.nc
    On Region_WA Cascades West, Mt Baker_20180415.nc
    On Region_WA Cascades West, Central_20180306.nc
    On Region_WA Cascades East, North_20180210.nc
    On Region_WA Cascades East, Central_20180215.nc
    On Region_WA Cascades West, Mt Baker_20180416.nc
    On Region_WA Cascades East, South_20180416.nc
    On Region_WA Cascades West, South_20180215.nc
    On Region_WA Cascades West, Central_20180307.nc
    On Region_WA Cascades West, Mt Baker_20180417.nc
    On Region_WA Cascades East, South_20180417.nc
    On Region_WA Cascades East, North_20180211.nc
    On Region_WA Cascades East, Central_20180216.nc
    On Region_WA Cascades West, South_20180216.nc
    On Region_WA Cascades West, Central_20180308.nc
    On Region_WA Cascades West, Mt Baker_20180418.nc
    On Region_WA Cascades East, South_20180418.nc
    On Region_WA Cascades East, North_20180212.nc
    On Region_WA Cascades West, Mt Baker_20180419.nc
    On Region_WA Cascades East, South_20180419.nc
    On Region_WA Cascades East, Central_20180217.nc
    On Region_WA Cascades West, South_20180217.nc
    On Region_WA Cascades West, Central_20180309.nc
    On Region_WA Cascades West, Mt Baker_20180420.nc
    On Region_WA Cascades East, South_20180420.nc
    On Region_WA Cascades East, North_20180213.nc
    On Region_WA Cascades East, Central_20180218.nc
    On Region_WA Cascades West, South_20180218.nc
    On Region_WA Cascades West, Central_20180310.nc
    On Region_WA Cascades West, Mt Baker_20180421.nc
    On Region_WA Cascades East, South_20180421.nc
    On Region_WA Cascades West, Mt Baker_20180422.nc
    On Region_WA Cascades West, Central_20180311.nc
    On Region_WA Cascades East, South_20180422.nc
    On Region_WA Cascades East, North_20180214.nc
    On Region_WA Cascades East, Central_20180219.nc
    On Region_WA Cascades West, South_20180219.nc
    On Region_WA Cascades West, Mt Baker_20180423.nc
    On Region_WA Cascades East, South_20180423.nc
    On Region_WA Cascades West, Central_20180312.nc
    On Region_WA Cascades East, North_20180215.nc
    On Region_WA Cascades East, Central_20180220.nc
    On Region_WA Cascades West, South_20180220.nc
    On Region_WA Cascades West, Mt Baker_20180424.nc
    On Region_WA Cascades East, South_20180424.nc
    On Region_WA Cascades West, Central_20180313.nc
    On Region_WA Cascades West, Mt Baker_20180425.nc
    On Region_WA Cascades East, South_20180425.nc
    On Region_WA Cascades East, Central_20180221.nc
    On Region_WA Cascades West, South_20180221.nc
    On Region_WA Cascades East, North_20180216.nc
    On Region_WA Cascades West, Central_20180314.nc
    On Region_WA Cascades West, Mt Baker_20180426.nc
    On Region_WA Cascades East, South_20180426.nc
    On Region_WA Cascades East, Central_20180222.nc
    On Region_WA Cascades West, South_20180222.nc
    On Region_WA Cascades East, North_20180217.nc
    On Region_WA Cascades West, Mt Baker_20180427.nc
    On Region_WA Cascades East, South_20180427.nc
    On Region_WA Cascades West, Central_20180315.nc
    On Region_WA Cascades West, Mt Baker_20180428.nc
    On Region_WA Cascades East, South_20180428.nc
    On Region_WA Cascades East, Central_20180223.nc
    On Region_WA Cascades West, South_20180223.nc
    On Region_WA Cascades East, North_20180218.nc
    On Region_WA Cascades West, Central_20180316.nc
    On Region_WA Cascades West, Mt Baker_20180429.nc
    On Region_WA Cascades East, South_20180429.nc
    On Region_WA Cascades East, Central_20180224.nc
    On Region_WA Cascades West, Central_20180317.nc
    On Region_WA Cascades West, South_20180224.nc
    On Region_WA Cascades West, Mt Baker_20180430.nc
    On Region_WA Cascades East, North_20180219.nc
    On Region_WA Cascades East, South_20180430.nc
    On Region_WA Cascades West, Central_20180318.nc
    On Region_WA Cascades East, Central_20180225.nc
    On Region_WA Cascades West, South_20180225.nc
    On Region_WA Cascades East, North_20180220.nc
    On Region_WA Cascades West, Central_20180319.nc
    On Region_WA Cascades East, Central_20180226.nc
    On Region_WA Cascades West, South_20180226.nc
    On Region_WA Cascades East, North_20180221.nc
    On Region_WA Cascades West, Central_20180320.nc
    On Region_WA Cascades East, Central_20180227.nc
    On Region_WA Cascades West, South_20180227.nc
    On Region_WA Cascades East, North_20180222.nc
    On Region_WA Cascades West, Central_20180321.nc
    On Region_WA Cascades East, Central_20180228.nc
    On Region_WA Cascades West, South_20180228.nc
    On Region_WA Cascades West, Central_20180322.nc
    On Region_WA Cascades East, North_20180223.nc
    On Region_WA Cascades East, Central_20180301.nc
    On Region_WA Cascades West, South_20180301.nc
    On Region_WA Cascades West, Central_20180323.nc
    On Region_WA Cascades East, North_20180224.nc
    On Region_WA Cascades West, Central_20180324.nc
    On Region_WA Cascades East, Central_20180302.nc
    On Region_WA Cascades West, South_20180302.nc
    On Region_WA Cascades East, North_20180225.nc
    On Region_WA Cascades West, Central_20180325.nc
    On Region_WA Cascades East, Central_20180303.nc
    On Region_WA Cascades West, South_20180303.nc
    On Region_WA Cascades East, North_20180226.nc
    On Region_WA Cascades West, Central_20180326.nc
    On Region_WA Cascades East, Central_20180304.nc
    On Region_WA Cascades West, South_20180304.nc
    On Region_WA Cascades West, Central_20180327.nc
    On Region_WA Cascades East, North_20180227.nc
    On Region_WA Cascades East, Central_20180305.nc
    On Region_WA Cascades West, South_20180305.nc
    On Region_WA Cascades West, Central_20180328.nc
    On Region_WA Cascades East, North_20180228.nc
    On Region_WA Cascades West, South_20180306.nc
    On Region_WA Cascades East, Central_20180306.nc
    On Region_WA Cascades West, Central_20180329.nc
    On Region_WA Cascades East, North_20180301.nc
    On Region_WA Cascades West, Central_20180330.nc
    On Region_WA Cascades West, South_20180307.nc
    On Region_WA Cascades East, Central_20180307.nc
    On Region_WA Cascades East, North_20180302.nc
    On Region_WA Cascades West, Central_20180331.nc
    On Region_WA Cascades East, Central_20180308.nc
    On Region_WA Cascades West, South_20180308.nc
    On Region_WA Cascades West, Central_20180401.nc
    On Region_WA Cascades East, North_20180303.nc
    On Region_WA Cascades West, South_20180309.nc
    On Region_WA Cascades East, Central_20180309.nc
    On Region_WA Cascades West, Central_20180402.nc
    On Region_WA Cascades East, North_20180304.nc
    On Region_WA Cascades East, Central_20180310.nc
    On Region_WA Cascades West, South_20180310.nc
    On Region_WA Cascades West, Central_20180403.nc
    On Region_WA Cascades East, North_20180305.nc
    On Region_WA Cascades East, Central_20180311.nc
    On Region_WA Cascades West, South_20180311.nc
    On Region_WA Cascades West, Central_20180404.nc
    On Region_WA Cascades East, North_20180306.nc
    On Region_WA Cascades West, South_20180312.nc
    On Region_WA Cascades East, Central_20180312.nc
    On Region_WA Cascades West, Central_20180405.nc
    On Region_WA Cascades West, Central_20180406.nc
    On Region_WA Cascades East, North_20180307.nc
    On Region_WA Cascades East, Central_20180313.nc
    On Region_WA Cascades West, South_20180313.nc
    On Region_WA Cascades West, Central_20180407.nc
    On Region_WA Cascades East, North_20180308.nc
    On Region_WA Cascades West, South_20180314.nc
    On Region_WA Cascades East, Central_20180314.nc
    On Region_WA Cascades West, Central_20180408.nc
    On Region_WA Cascades East, North_20180309.nc
    On Region_WA Cascades West, South_20180315.nc
    On Region_WA Cascades East, Central_20180315.nc
    On Region_WA Cascades West, Central_20180409.nc
    On Region_WA Cascades West, South_20180316.nc
    On Region_WA Cascades East, Central_20180316.nc
    On Region_WA Cascades East, North_20180310.nc
    On Region_WA Cascades West, Central_20180410.nc
    On Region_WA Cascades East, Central_20180317.nc
    On Region_WA Cascades West, South_20180317.nc
    On Region_WA Cascades East, North_20180311.nc
    On Region_WA Cascades West, Central_20180411.nc
    On Region_WA Cascades West, Central_20180412.nc
    On Region_WA Cascades East, Central_20180318.nc
    On Region_WA Cascades West, South_20180318.nc
    On Region_WA Cascades East, North_20180312.nc
    On Region_WA Cascades West, Central_20180413.nc
    On Region_WA Cascades East, Central_20180319.nc
    On Region_WA Cascades West, South_20180319.nc
    On Region_WA Cascades East, North_20180313.nc
    On Region_WA Cascades West, Central_20180414.nc
    On Region_WA Cascades East, Central_20180320.nc
    On Region_WA Cascades West, South_20180320.nc
    On Region_WA Cascades East, North_20180314.nc
    On Region_WA Cascades West, Central_20180415.nc
    On Region_WA Cascades East, Central_20180321.nc
    On Region_WA Cascades West, South_20180321.nc
    On Region_WA Cascades East, North_20180315.nc
    On Region_WA Cascades West, Central_20180416.nc
    On Region_WA Cascades East, Central_20180322.nc
    On Region_WA Cascades West, South_20180322.nc
    On Region_WA Cascades West, Central_20180417.nc
    On Region_WA Cascades East, North_20180316.nc
    On Region_WA Cascades West, Central_20180418.nc
    On Region_WA Cascades East, Central_20180323.nc
    On Region_WA Cascades West, South_20180323.nc
    On Region_WA Cascades East, North_20180317.nc
    On Region_WA Cascades West, Central_20180419.nc
    On Region_WA Cascades East, Central_20180324.nc
    On Region_WA Cascades West, South_20180324.nc
    On Region_WA Cascades East, North_20180318.nc
    On Region_WA Cascades West, Central_20180420.nc
    On Region_WA Cascades East, Central_20180325.nc
    On Region_WA Cascades West, South_20180325.nc
    On Region_WA Cascades East, North_20180319.nc
    On Region_WA Cascades West, Central_20180421.nc
    On Region_WA Cascades East, Central_20180326.nc
    On Region_WA Cascades West, South_20180326.nc
    On Region_WA Cascades West, Central_20180422.nc
    On Region_WA Cascades East, North_20180320.nc
    On Region_WA Cascades West, South_20180327.nc
    On Region_WA Cascades East, Central_20180327.nc
    On Region_WA Cascades West, Central_20180423.nc
    On Region_WA Cascades East, North_20180321.nc
    On Region_WA Cascades West, Central_20180424.nc
    On Region_WA Cascades West, South_20180328.nc
    On Region_WA Cascades East, Central_20180328.nc
    On Region_WA Cascades East, North_20180322.nc
    On Region_WA Cascades West, Central_20180425.nc
    On Region_WA Cascades West, South_20180329.nc
    On Region_WA Cascades East, Central_20180329.nc
    On Region_WA Cascades East, North_20180323.nc
    On Region_WA Cascades West, Central_20180426.nc
    On Region_WA Cascades West, South_20180330.nc
    On Region_WA Cascades East, Central_20180330.nc
    On Region_WA Cascades West, Central_20180427.nc
    On Region_WA Cascades East, North_20180324.nc
    On Region_WA Cascades West, South_20180331.nc
    On Region_WA Cascades East, Central_20180331.nc
    On Region_WA Cascades West, Central_20180428.nc
    On Region_WA Cascades East, North_20180325.nc
    On Region_WA Cascades West, South_20180401.nc
    On Region_WA Cascades East, Central_20180401.nc
    On Region_WA Cascades West, Central_20180429.nc
    On Region_WA Cascades East, North_20180326.nc
    On Region_WA Cascades West, Central_20180430.nc
    On Region_WA Cascades West, South_20180402.nc
    On Region_WA Cascades East, Central_20180402.nc
    On Region_WA Cascades East, North_20180327.nc
    On Region_WA Cascades East, Central_20180403.nc
    On Region_WA Cascades West, South_20180403.nc
    On Region_WA Cascades East, North_20180328.nc
    On Region_WA Cascades West, South_20180404.nc
    On Region_WA Cascades East, Central_20180404.nc
    On Region_WA Cascades East, North_20180329.nc
    On Region_WA Cascades East, Central_20180405.nc
    On Region_WA Cascades West, South_20180405.nc
    On Region_WA Cascades East, North_20180330.nc
    On Region_WA Cascades West, South_20180406.nc
    On Region_WA Cascades East, Central_20180406.nc
    On Region_WA Cascades East, North_20180331.nc
    On Region_WA Cascades West, South_20180407.nc
    On Region_WA Cascades East, Central_20180407.nc
    On Region_WA Cascades East, North_20180401.nc
    On Region_WA Cascades West, South_20180408.nc
    On Region_WA Cascades East, Central_20180408.nc
    On Region_WA Cascades East, North_20180402.nc
    On Region_WA Cascades West, South_20180409.nc
    On Region_WA Cascades East, Central_20180409.nc
    On Region_WA Cascades East, North_20180403.nc
    On Region_WA Cascades East, Central_20180410.nc
    On Region_WA Cascades West, South_20180410.nc
    On Region_WA Cascades West, South_20180411.nc
    On Region_WA Cascades East, Central_20180411.nc
    On Region_WA Cascades East, North_20180404.nc
    On Region_WA Cascades West, South_20180412.nc
    On Region_WA Cascades East, Central_20180412.nc
    On Region_WA Cascades East, North_20180405.nc
    On Region_WA Cascades West, South_20180413.nc
    On Region_WA Cascades East, Central_20180413.nc
    On Region_WA Cascades East, North_20180406.nc
    On Region_WA Cascades West, South_20180414.nc
    On Region_WA Cascades East, Central_20180414.nc
    On Region_WA Cascades East, North_20180407.nc
    On Region_WA Cascades West, South_20180415.nc
    On Region_WA Cascades East, Central_20180415.nc
    On Region_WA Cascades East, North_20180408.nc
    On Region_WA Cascades West, South_20180416.nc
    On Region_WA Cascades East, Central_20180416.nc
    On Region_WA Cascades East, North_20180409.nc
    On Region_WA Cascades West, South_20180417.nc
    On Region_WA Cascades East, Central_20180417.nc
    On Region_WA Cascades East, North_20180410.nc
    On Region_WA Cascades West, South_20180418.nc
    On Region_WA Cascades East, Central_20180418.nc
    On Region_WA Cascades East, North_20180411.nc
    On Region_WA Cascades West, South_20180419.nc
    On Region_WA Cascades East, Central_20180419.nc
    On Region_WA Cascades East, North_20180412.nc
    On Region_WA Cascades West, South_20180420.nc
    On Region_WA Cascades East, Central_20180420.nc
    On Region_WA Cascades East, North_20180413.nc
    On Region_WA Cascades West, South_20180421.nc
    On Region_WA Cascades East, Central_20180421.nc
    On Region_WA Cascades East, North_20180414.nc
    On Region_WA Cascades West, South_20180422.nc
    On Region_WA Cascades East, Central_20180422.nc
    On Region_WA Cascades East, North_20180415.nc
    On Region_WA Cascades West, South_20180423.nc
    On Region_WA Cascades East, Central_20180423.nc
    On Region_WA Cascades East, North_20180416.nc
    On Region_WA Cascades West, South_20180424.nc
    On Region_WA Cascades East, Central_20180424.nc
    On Region_WA Cascades East, North_20180417.nc
    On Region_WA Cascades West, South_20180425.nc
    On Region_WA Cascades East, Central_20180425.nc
    On Region_WA Cascades East, North_20180418.nc
    On Region_WA Cascades West, South_20180426.nc
    On Region_WA Cascades East, Central_20180426.nc
    On Region_WA Cascades East, North_20180419.nc
    On Region_WA Cascades West, South_20180427.nc
    On Region_WA Cascades East, Central_20180427.nc
    On Region_WA Cascades East, North_20180420.nc
    On Region_WA Cascades West, South_20180428.nc
    On Region_WA Cascades East, Central_20180428.nc
    On Region_WA Cascades East, North_20180421.nc
    On Region_WA Cascades West, South_20180429.nc
    On Region_WA Cascades East, Central_20180429.nc
    On Region_WA Cascades East, North_20180422.nc
    On Region_WA Cascades West, South_20180430.nc
    On Region_WA Cascades East, Central_20180430.nc
    On Region_WA Cascades East, North_20180423.nc
    On Region_WA Cascades East, North_20180424.nc
    On Region_WA Cascades East, North_20180425.nc
    On Region_WA Cascades East, North_20180426.nc
    On Region_WA Cascades East, North_20180427.nc
    On Region_WA Cascades East, North_20180428.nc
    On Region_WA Cascades East, North_20180429.nc
    On Region_WA Cascades East, North_20180430.nc



### 5. PrepMLData
#### Converting the data in to a memmapped numpy timeseries (samples, feature, timestep)
This step needs to be run once to create a dataset to be used in a subsequent ML step.  The way to think about these methods is that we use the set of valid labels + the valid lat/lon pairs as an index in to the data.  Its important to understand the regions are geographically large and usually cover many lat/lon pairs in our gridded dataset while the labels apply to an entire region (multiple lat/lon pairs).  For example the _WA Cascades East, Central_ region coveres 24 lat/lon pairs so if on Jan 1 there was a label we wanted to predict our dataset would have 24 lat/lon pairs in that region associated with that label.  There are pros and cons for this approach.

Pros:
1. Reasonable data augmentation approach
2. Aligns with how we utltimatly want to provide predictions--more granular, not restricted to established regions

Cons:
1. Could be contributing to overfitting
2. The data becomes very large

That being said the methods will calculate this index for every label/lat/lon point and then we'll split this in to train and test sets.  Its important to ensure that the train test split is done in time (i.e., I usually use 15-16 through 18-19 as the training set and then 19-20 as the test set) as if you don't there will be data leakage.  

Once the train test split is done on the labels there is a process to build up the dataset.  This is still a slow process even when doing it in parallel and agains the indexed Zarr data.  I've spent a lot of time trying various ways of optimizing this but I'm sure this could use more work.  The primary method internal method for doign this is called _get_xr_batch_ and takes several parameters:

1. labels: list of the train or test set labels
2. lookback_days: the number of previous days to include in the dataset.  For example if the label is for Jan 1, then a lookback_days of 14 will also include the previous 14 days.  I've been typically using 180 days as lookback (if a lookback extends prior to Nov 1 then we just fill in NaN as the data is likly irrelevant) but its possible that a lower value might give better results.
3. batch_size: the size of the batch you want returned
4. y_column: the label you want to use
5. label_values: the possible values of the label from y_column.  We include this as the method can implement oversampling to adjust for the imbalanced data.
6. oversample: dict which indicates which labels should be oversampled.  
7. random_state: random variable initilizer
8. n_jobs: number of processes to use

In the tutorial the notebook produced one train batche of 1,000 rows and one test batch of 500 rows and then concats them in a single memapped file.



### At this point we can generate a train and test dataset from the Zarr data

```python
#test_ignore
pml = PrepML(data_root, interpolate,  date_start='2015-11-01', date_end='2020-04-30', date_train_test_cutoff='2019-11-01')
```

```python
#test_ignore
pml.regions = {            
            'Washington': ['Mt Hood', 'Olympics', 'Snoqualmie Pass', 'Stevens Pass',
            'WA Cascades East, Central', 'WA Cascades East, North', 'WA Cascades East, South',
            'WA Cascades West, Central', 'WA Cascades West, Mt Baker', 'WA Cascades West, South'
            ]}
```

```python
#test_ignore
%time train_labels, test_labels = pml.prep_labels()
```

    Mt Hood
    Olympics
    Snoqualmie Pass
    Stevens Pass
    WA Cascades East, Central
    WA Cascades East, North
    WA Cascades East, South
    WA Cascades West, Central
    WA Cascades West, Mt Baker
    WA Cascades West, South
    CPU times: user 19.6 s, sys: 531 ms, total: 20.2 s
    Wall time: 20.4 s


```python
#test_ignore
train_labels = train_labels[train_labels['UnifiedRegion'].isin(['Mt Hood', 
                                                              'Olympics', 
                                                              'Snoqualmie Pass',
                                                              'Stevens Pass',
                                                              'WA Cascades East, Central',
                                                              'WA Cascades East, North',
                                                              'WA Cascades East, South',
                                                              'WA Cascades West, Central',
                                                              'WA Cascades West, Mt Baker',
                                                              'WA Cascades West, South'])]
```

```python
#test_ignore
test_labels = test_labels[test_labels['UnifiedRegion'].isin(['Mt Hood', 
                                                              'Olympics', 
                                                              'Snoqualmie Pass',
                                                              'Stevens Pass',
                                                              'WA Cascades East, Central',
                                                              'WA Cascades East, North',
                                                              'WA Cascades East, South',
                                                              'WA Cascades West, Central',
                                                              'WA Cascades West, Mt Baker',
                                                              'WA Cascades West, South'])]
```

```python
#test_ignore
train_labels.head()
```




<div>
<style scoped>
    .dataframe tbody tr th:only-of-type {
        vertical-align: middle;
    }

    .dataframe tbody tr th {
        vertical-align: top;
    }

    .dataframe thead th {
        text-align: right;
    }
</style>
<table border="1" class="dataframe">
  <thead>
    <tr style="text-align: right;">
      <th></th>
      <th>UnifiedRegion</th>
      <th>latitude</th>
      <th>longitude</th>
      <th>UnifiedRegionleft</th>
      <th>Cornices_Likelihood</th>
      <th>Cornices_MaximumSize</th>
      <th>Cornices_MinimumSize</th>
      <th>Cornices_OctagonAboveTreelineEast</th>
      <th>Cornices_OctagonAboveTreelineNorth</th>
      <th>Cornices_OctagonAboveTreelineNorthEast</th>
      <th>...</th>
      <th>image_types</th>
      <th>image_urls</th>
      <th>rose_url</th>
      <th>BottomLineSummary</th>
      <th>Day1WarningText</th>
      <th>Day2WarningText</th>
      <th>parsed_date</th>
      <th>season</th>
      <th>Day1DangerAboveTreelineValue</th>
      <th>Day1DangerAboveTreelineWithTrend</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <th>0</th>
      <td>Mt Hood</td>
      <td>45.25</td>
      <td>-121.75</td>
      <td>Mt Hood</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>...</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>2015-12-05</td>
      <td>15-16</td>
      <td>1.0</td>
      <td>Moderate_Initial</td>
    </tr>
    <tr>
      <th>1</th>
      <td>Mt Hood</td>
      <td>45.25</td>
      <td>-121.75</td>
      <td>Mt Hood</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>...</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>2015-12-06</td>
      <td>15-16</td>
      <td>1.0</td>
      <td>Moderate_Flat</td>
    </tr>
    <tr>
      <th>2</th>
      <td>Mt Hood</td>
      <td>45.25</td>
      <td>-121.75</td>
      <td>Mt Hood</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>...</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>2015-12-07</td>
      <td>15-16</td>
      <td>2.0</td>
      <td>Considerable_Rising</td>
    </tr>
    <tr>
      <th>3</th>
      <td>Mt Hood</td>
      <td>45.25</td>
      <td>-121.75</td>
      <td>Mt Hood</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>...</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>2015-12-08</td>
      <td>15-16</td>
      <td>2.0</td>
      <td>Considerable_Flat</td>
    </tr>
    <tr>
      <th>4</th>
      <td>Mt Hood</td>
      <td>45.25</td>
      <td>-121.75</td>
      <td>Mt Hood</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>...</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>no-data</td>
      <td>2015-12-09</td>
      <td>15-16</td>
      <td>1.0</td>
      <td>Moderate_Falling</td>
    </tr>
  </tbody>
</table>
<p>5 rows × 302 columns</p>
</div>



### Note the class imbalance and the test set not having all classes.  This isn't a good set for ML (one should use the entire 2015-2020 dataset but you need to ensure you have all the data from those dates available)

```python
#test_ignore
train_labels['Day1DangerAboveTreeline'].value_counts()
```




    Moderate        27982
    Considerable    25588
    High             5715
    Low              2289
    no-data          1272
    Extreme            59
    Name: Day1DangerAboveTreeline, dtype: int64



```python
#test_ignore
test_labels['Day1DangerAboveTreeline'].value_counts()
```




    Series([], Name: Day1DangerAboveTreeline, dtype: int64)



### This will generate local files sampling from the datasets (parameters can specify exactly the amount of data to store) in the ML folder which can be used for the next ML process

Modifying the parameters so you don't run out of memory is important as its designed to append to the on disk files so as to stay within memory contraits: num_train_rows_per_file maxes out at around 50000 on my 48gb local machine.  If you want more data than then then use num_train_files parameter which will create multiple files num_train_rows_per_file and will append them in to one file at the end of the process. 

```python
#test_ignore
%time train_labels_remaining, test_labels_remaining = pml.generate_train_test_local(train_labels, test_labels, num_train_rows_per_file=1000, num_test_rows_per_file=500, num_variables=978)
```

### 6.TimeseriesAI
#### Demonstrate using the data as the input to a deep learning training process
Now that our data is in the right format we can try and do some machine learning on it.  The 4.TimeseriesAi notebook in the ML folder is only to demonstrate the process to do this as today the results are a proof of concept and not sophisticated at all.  This area has only had minimal investment to date and is where focus is now being applied.  The current issue is overfitting and that will need to be addresssed before both exapnding the dataset size or training for additional epochs.

The Notebook 4.TimeseriesAI leverages the Timeseries Deep Learning library https://github.com/timeseriesAI/tsai based on FastAI https://github.com/fastai/fastai and it realitvely straightforward to understand especially if you are familiar with FastAI.  As progress is made here this notbook will be updated to reflect the current state.

This notebook also depends on a different conda environment in the _Environments_ folder.  Create and activate the environment from the timeseriesai.yml file to use this notebook.



## Citations
National Centers for Environmental Prediction/National Weather Service/NOAA/U.S. Department of Commerce. 2015, updated daily. NCEP GFS 0.25 Degree Global Forecast Grids Historical Archive. Research Data Archive at the National Center for Atmospheric Research, Computational and Information Systems Laboratory. https://doi.org/10.5065/D65D8PWK. Accessed April, 2020
