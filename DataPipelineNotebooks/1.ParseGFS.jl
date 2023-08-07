using NCDatasets
using Glob
using GeoJSON
using DataFrames
using ArchGDAL
using Plots
using BenchmarkTools

data_root = "F:/OAPMLData/"

files = glob(string(data_root,"/1.RawWeatherData/gfs/15-16/Canada/gfs.0p25.201*.grib2.nc"))
datasets = []
for f in files
    push!(datasets, NCDataset(f))
end
#regions_df2 = DataFrames.DataFrame(ArchGDAL.getlayer(ArchGDAL.read("C:/Users/scott/source/repos/OpenAvalancheProject/Data/CAAvalancheRegions.geojson"), 0))
#load data in to memory once so its easier/faster to work with
latitude_dim = []
longitude_dim = []
time_dim = []
append!(latitude_dim, Array[datasets[1]["latitude"]][1])
append!(longitude_dim, Array[datasets[1]["longitude"]][1])

var_names = []
for (varname, var) in datasets[1] 
    println(varname)
    if varname in ["latitude", "longitude", "time"]
        continue
    else
        push!(var_names, varname)
    end
end

var_exclude_list = ["HLCY_3000M0maboveground", "PRES_tropopause", 
                    "ICAHT_tropopause", "PRMSL_meansealevel", 
                    "ICEC_surface", "LAND_surface", "VGRD_tropopause", 
                    "UGRD_tropopause", "TMP_tropopause", "HGT_tropopause"]

deleteat!(var_names, findall(x->x in var_exclude_list || 
                             startswith(x, "var0") || 
                             contains(x, "M2147D48") || 
                             contains(x, "2eM06") ||
                             contains(x, "255M0mb") || 
                             contains(x, "0D") ||
                             contains(x, "180M0") ||
                             contains(x, "30M0") ||
                             contains(x, "type2040") ||
                             contains(x, "0Cisotherm") || 
                             contains(x, "3658m") ||
                             contains(x, "2743m") || 
                             contains(x, "1829m") || 
                             contains(x, "100maboveground") ||
                             contains(x, "80maboveground") ||
                             contains(x, "maxwind") ||
                             contains(x, "localleveltype"), var_names))

function load_data(datasets, var_names)
    var_dict = Dict()
    #allocate the memory for the Dict
    for varname in var_names
        d = Array{Union{Missing, Float32}, 3}(undef, length(longitude_dim), length(latitude_dim), length(datasets))
        var_dict[varname] = d
    end
    Threads.@threads for i in eachindex(datasets)
        println("    On dataset ", i)
        for varname in var_names
            var_dict[varname][:,:,i] = datasets[i][varname][:,:,:] 
        end
    end
    return var_dict
end

var_dict = load_data(datasets, var_names)