import cdsapi
import pandas as pd
import os
import argparse
 
# Initialize parser
parser = argparse.ArgumentParser()
 
# Adding optional argument
parser.add_argument("-s", help = "Start Date")
parser.add_argument("-e", help = "End Date")
 
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
    fname = d.strftime('era5_%Y%m%d.nc')
    c.retrieve(
        'reanalysis-era5-single-levels',
        {
            'product_type': 'reanalysis',
            'variable': [
                '100m_u_component_of_wind', '100m_v_component_of_wind', '10m_u_component_of_wind',
                '10m_v_component_of_wind', '2m_dewpoint_temperature', '2m_temperature',
                'boundary_layer_height', 'clear_sky_direct_solar_radiation_at_surface', 'convective_available_potential_energy',
                'convective_precipitation', 'convective_rain_rate', 'convective_snowfall',
                'convective_snowfall_rate_water_equivalent', 'downward_uv_radiation_at_the_surface', 'forecast_albedo',
                'forecast_logarithm_of_surface_roughness_for_heat', 'friction_velocity', 'geopotential',
                'high_cloud_cover', 'instantaneous_10m_wind_gust', 'instantaneous_surface_sensible_heat_flux',
                'large_scale_snowfall', 'large_scale_snowfall_rate_water_equivalent', 'low_cloud_cover',
                'mean_boundary_layer_dissipation', 'mean_convective_precipitation_rate', 'mean_convective_snowfall_rate',
                'mean_eastward_gravity_wave_surface_stress', 'mean_eastward_turbulent_surface_stress', 'mean_evaporation_rate',
                'mean_gravity_wave_dissipation', 'mean_large_scale_precipitation_fraction', 'mean_large_scale_precipitation_rate',
                'mean_large_scale_snowfall_rate', 'mean_northward_gravity_wave_surface_stress', 'mean_northward_turbulent_surface_stress',
                'mean_potential_evaporation_rate', 'mean_runoff_rate', 'mean_sea_level_pressure',
                'mean_snow_evaporation_rate', 'mean_snowfall_rate', 'mean_snowmelt_rate',
                'mean_sub_surface_runoff_rate', 'mean_surface_direct_short_wave_radiation_flux', 'mean_surface_direct_short_wave_radiation_flux_clear_sky',
                'mean_surface_downward_long_wave_radiation_flux', 'mean_surface_downward_long_wave_radiation_flux_clear_sky', 'mean_surface_downward_short_wave_radiation_flux',
                'mean_surface_downward_short_wave_radiation_flux_clear_sky', 'mean_surface_downward_uv_radiation_flux', 'mean_surface_latent_heat_flux',
                'mean_surface_net_long_wave_radiation_flux', 'mean_surface_net_long_wave_radiation_flux_clear_sky', 'mean_surface_net_short_wave_radiation_flux',
                'mean_surface_net_short_wave_radiation_flux_clear_sky', 'mean_surface_runoff_rate', 'mean_surface_sensible_heat_flux',
                'mean_top_downward_short_wave_radiation_flux', 'mean_top_net_long_wave_radiation_flux', 'mean_top_net_long_wave_radiation_flux_clear_sky',
                'mean_top_net_short_wave_radiation_flux', 'mean_top_net_short_wave_radiation_flux_clear_sky', 'mean_total_precipitation_rate',
                'mean_vertically_integrated_moisture_divergence', 'medium_cloud_cover', 'near_ir_albedo_for_diffuse_radiation',
                'near_ir_albedo_for_direct_radiation', 'potential_evaporation', 'precipitation_type',
                'snow_albedo', 'snow_density', 'snow_evaporation',
                'snowfall', 'snowmelt', 'soil_temperature_level_1',
                'soil_temperature_level_2', 'soil_temperature_level_3', 'soil_temperature_level_4',
                'soil_type', 'surface_latent_heat_flux', 'surface_net_solar_radiation',
                'surface_net_solar_radiation_clear_sky', 'surface_net_thermal_radiation', 'surface_net_thermal_radiation_clear_sky',
                'surface_pressure', 'surface_runoff', 'surface_sensible_heat_flux',
                'surface_solar_radiation_downward_clear_sky', 'surface_solar_radiation_downwards', 'surface_thermal_radiation_downward_clear_sky',
                'surface_thermal_radiation_downwards', 'temperature_of_snow_layer', 'toa_incident_solar_radiation',
                'top_net_solar_radiation', 'top_net_solar_radiation_clear_sky', 'top_net_thermal_radiation',
                'top_net_thermal_radiation_clear_sky', 'total_cloud_cover', 'total_column_cloud_liquid_water',
                'total_column_ozone', 'total_column_snow_water', 'total_precipitation',
                'total_sky_direct_solar_radiation_at_surface', 'uv_visible_albedo_for_diffuse_radiation', 'uv_visible_albedo_for_direct_radiation',
                'volumetric_soil_water_layer_1', 'volumetric_soil_water_layer_2', 'volumetric_soil_water_layer_3',
                'volumetric_soil_water_layer_4',
            ],
            'year': d.strftime('%Y'),
            'month': d.strftime('%m'),
            'day': d.strftime('%d'),
             'time': [
            '00:00', '01:00', '02:00',
            '03:00', '04:00', '05:00',
            '06:00', '07:00', '08:00',
            '09:00', '10:00', '11:00',
            '12:00', '13:00', '14:00',
            '15:00', '16:00', '17:00',
            '18:00', '19:00', '20:00',
            '21:00', '22:00', '23:00',],
            'area': [
                58.2, -130.54, 24.1,
                -62.25,
            ],
            'format': 'netcdf',
        },
        fname 
    )
    os.rename('./' + fname, dirname + fname) 
        