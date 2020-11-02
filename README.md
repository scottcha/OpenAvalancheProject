# OpenAvalancheProject
Open source project to bring data and ml to avalanche forecasting

Webpage is https://openavalancheproject.org

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
1. Getting new input data
    This aspect of the tutorial will cover how you can obtain new weather input data for a new date range or region.  This part assumes you have avalanche forecast labels for the dates and region (OAP currently has historical forecast labels for three avalanche centers in the US from the 15-16 season through the 19-20 season and is working on expanding that).

    Due to the large size of the input GFS data and the fact that its already hosted by NCAR OAP isn't currently providing copies of this data.  If you want to start a data processing pipeline from the original data you can star with this process here.

    The input data is derived from the .25 degree GFS model hosted by NCAR hosted at this site: https://rda.ucar.edu/datasets/ds084.1/
    You'll need to create an account and once you are logged in you can visit the above link and then click on the Data Access tab.  One note is that I've found that chromium based browsers don't work well on this site so I recommend you use Firefox for at least downloading the data.

    Due to the size of the files we are downloading I only recommend downloading one season and for a regional subset at a time.  In this example I'm going to download the data for Washington.  
    
    ![NCAR Get Data](Tutorial/NCAR_GetData.png?raw=true "NCAR Get Data")

    Click on the "Get a Subset" link.

    The next page allows us to select both the dates and parameters we are interested in.  Currently we read all parameters so leave the parameters unchecked.  For dates choose one winter season.  In the below screenshot I've selected dates Nov 1, 2015 thorough April 30, 2016 for the 15-16 season.  The models assume the season starts Nov 1 and ends April 30 (it wouldn't be too difficult to update the data pipeline for a southern hemisphere winter but its not something I've done yet).

    ![NCAR Date Selection](Tutorial/NCAR_DateSelection.png?raw=true "NCAR Date Selection")

    Click Continue and wait for it to validate your selections. 

    The next page allows you to further subset your data.  There are a few important things here.  

       1. Verify that the dates are correct.  
       2. We want the output as netCDF 
       3. Download all vertical levels.  
       4. Select only the 3-24 hour forecasts in the gridded products as currently OAP doesn't use more than this.  
       5. You can also then select the bounding box for the area you want to download. Once you have a bounding box you like write down the lat/lon values so its easier to input when we come back for other date ranges.
       6. Select a compression as the files are very large.
    
    ![NCAR Subset Selection](Tutorial/NCAR_Subset.png?raw=true "NCAR Subset")

    Once the selections are correct and you can eventually click through to submit your request.  You should get a confirmation page of your selections and the system will start to retrieve your data.  This usually takes a few hours and you will get an email when its ready for download.  At this point if you want additional date/time ranges you can submit the requests and they will get queued and made avalable for download when they are ready.





### Citations
National Centers for Environmental Prediction/National Weather Service/NOAA/U.S. Department of Commerce. 2015, updated daily. NCEP GFS 0.25 Degree Global Forecast Grids Historical Archive. Research Data Archive at the National Center for Atmospheric Research, Computational and Information Systems Laboratory. https://doi.org/10.5065/D65D8PWK. Accessed April, 2020