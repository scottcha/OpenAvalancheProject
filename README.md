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
