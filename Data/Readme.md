Training data can be downloaded here:
https://oapstorageprod.blob.core.windows.net/oap-training-data/Data/CleanedForecastsNWAC_CAIC_UAC_CAC.V1.2013-2021.zip


1.	This file is an attempt to collect all the data from ever avy center I’ve worked on so far.  
2.	Not ever avy center published the same data and not ever forecast has the same data.  Scanning though there is a mix between no-data and null.  I’m not sure if its entirely consistent but null should mean that datacenter doesn’t publish that data while no-data means it wasn’t part of that forecast.
3.	Columns are in alphabetical order but the important ones are: 
a.	“Unified Region” which is the avy region name
b.	“PublishedDate” the date the forecast was published.
c.	“Day1Date” yyyyMMdd encoded date of the avy forecast for the first date available in the forecast (Day2Date would be the second forecast day).  
d.	“Day1DangerAboveTreeline” is the forecast at the highest elevation provided for the region
e.	“Day1DangerNearTreeline” is the forecast at the mid elevation provided for the region
f.	“Day1DangerBelowTreeline” is the forecast at the lower elevation provided for the region
g.	“ForecastUrl” the archived url where I pulled the forecast in case the data needs to be checked against the source.
4.	There are many other columns meant to encode avy-rose values, avy-rose avalanche problems and other elements of the forecast.  Most of the column names should be self explanatory.


Older datasets for prior updates are archived below.
https://oapstorageprod.blob.core.windows.net/oap-training-data/Data/V1.1FeaturesWithLabels2013-2020.zip

A small training set which has been processed for input in to an ML model (samples, features, timestep) can be downloaded here: https://oapstorageprod.blob.core.windows.net/oap-training-data/Data/MLDataWashington.zip The feature names are indexed according to the file https://oapstorageprod.blob.core.windows.net/oap-training-data/Data/FeatureLabels.csv
