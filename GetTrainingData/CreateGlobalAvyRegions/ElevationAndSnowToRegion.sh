#!/bin/bash

#converts a list of elevation and snow data geotiff files to a merged avy region tif file
#on layer1 of the tif with a value of 0 indicating not an avy region and 100 indicating an avy region

#script assumes several things:
#1. all file extensions are .tif
#2. directories are organized as 
#   AsterElevation\
#   AvySlopes\
#   AvySlopesFiltered\
#   AvySlopesSieved\   
#   AvySlopesSieved2\
#   AvySlopesWarped\

#script also assumes gdal tools are installed and available on path

BASE_PATH=$1
REGION=$2
SNOW_LOCATION=$3
NUM_JOBS=$4

mkdir -p ${BASE_PATH}/AvySlopes/${REGION}/

ls ${BASE_PATH}/AsterElevation/${REGION}/*_dem.tif | parallel -j $NUM_JOBS gdaldem slope {} ${BASE_PATH}/AvySlopes/${REGION}/{/} -s 111120.0 -of GTiff

mkdir -p ${BASE_PATH}/AvySlopesFiltered/${REGION}/

ls ${BASE_PATH}/AvySlopes/${REGION}/*.tif | parallel -j $NUM_JOBS gdal_calc.py -S {} --outfile=${BASE_PATH}/AvySlopesFiltered/${REGION}/{/} --calc="'100*(S > 20)'" --NoDataValue=0

mkdir -p ${BASE_PATH}/AvySlopesSieved/${REGION}/

ls ${BASE_PATH}/AvySlopesFiltered/${REGION}/*.tif | parallel -j $NUM_JOBS gdal_sieve.py -st 1500 -8 -of GTiff {} ${BASE_PATH}/AvySlopesSieved/${REGION}/{/} 

mkdir -p ${BASE_PATH}/AvySlopesSieved2/${REGION}/

ls ${BASE_PATH}/AvySlopesSieved/${REGION}/*.tif | parallel -j $NUM_JOBS gdal_sieve.py -st 1500 -8 -of GTiff {} ${BASE_PATH}/AvySlopesSieved2/${REGION}/{/} 

mkdir -p ${BASE_PATH}/AvySlopesWarped/${REGION}/

ls ${BASE_PATH}/AvySlopesSieved2/${REGION}/*.tif | parallel -j $NUM_JOBS gdalwarp -overwrite -ts 8 0 -r max {} ${BASE_PATH}/AvySlopesWarped/${REGION}/{/} 

mkdir -p ${BASE_PATH}/AvySlopesMerged/

gdal_merge.py -of Float32 -ps .125 .125 -of GTiff -o ${BASE_PATH}/AvySlopesMerged/${REGION}_Merged.tif ${BASE_PATH}/AvySlopesWarped/${REGION}/*.tif

#final steps execute manually
#gdal_merge.py -ot Float32 -of GTiff -o ${BASE_PATH}/AvyRegions/avy_regions_full.tif --optfile /mergeInputFiles.txt
#gdal_sieve.py -st 10 -4 -of GTiff ${BASE_PATH}/AvyRegions/avy_regions_full.tif ${BASE_PATH}/avy_regions_full_sieved.tif