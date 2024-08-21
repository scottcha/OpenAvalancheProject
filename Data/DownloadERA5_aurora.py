import cdsapi
import pandas as pd
import os
import argparse
 
# Initialize parser
parser = argparse.ArgumentParser()
 
# Adding optional argument
parser.add_argument('-s', help = 'Start Date')
parser.add_argument('-e', help = 'End Date')
 
# Read arguments from command line
args = parser.parse_args()

start_date = args.s
end_date = args.e

daterange = pd.date_range(start_date, end_date, freq='d') 

c = cdsapi.Client()
for d in daterange:
    print('On ' + str(d)) 
    dirname = d.strftime('./%Y/%m/') 
    os.makedirs(dirname, exist_ok=True)
    fname = d.strftime('era5_atmospheric_%Y%m%d.nc')
    c.retrieve(
        'reanalysis-era5-single-levels',
        {
            'product_type': 'reanalysis',
            'variable': [
                'temperature',
                'u_component_of_wind',
                'v_component_of_wind',
                'specific_humidity',
                'geopotential',
            ],
            'pressure_level': [
                '50',
                '100',
                '150',
                '200',
                '250',
                '300',
                '400',
                '500',
                '600',
                '700',
                '850',
                '925',
                '1000',
            ], 
            'year': d.strftime('%Y'),
            'month': d.strftime('%m'),
            'day': d.strftime('%d'),
            'time': [ '00:00', '06:00', '12:00', '18:00',]
            'area': [
                58.2, -130.54, 24.1,
                -62.25,
            ],
            'format': 'netcdf',
        },
        fname 
    )
    os.rename('./' + fname, dirname + fname) 
        