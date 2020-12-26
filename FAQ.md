When you are processing multiple nc files and get an error like:

    ValueError: 'HLCY_3000M0maboveground' is not present in all datasets.

Its due to the fact that currently Xarray doesn't enable concatenation of multiple files which have different variables.  The issue for these datasets has been fixed in a fork of Xarray here https://github.com/scottcha/xarray and branch:

    git checkout add-defaults-during-concat-508

but which has some outstanding issues with other cases and thus hasn't yet been merged in to the main Xarray project.  You can use this fork when doing the ParseGFS.resample_local process to work around this issue but otherwise I recommend using the main Xarray project.
