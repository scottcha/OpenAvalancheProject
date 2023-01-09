from openavalancheproject.parse_gfs import *
seasons = ['18-19']
state = 'Canada'
interpolate = 1
n_jobs = 22
data_root = '/mnt/f/OAPMLData'
pgfs = ParseGFS('18-19', state, data_root, resample_length='3H')
results = pgfs.resample_local(jobs=n_jobs)