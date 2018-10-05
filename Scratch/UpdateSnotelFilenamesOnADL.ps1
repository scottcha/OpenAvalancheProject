Connect-AzureRmAccount
Get-AzureRmSubscription
Set-AzureRmContext -Subscription "Azure Pay As You Go"
Register-AzureRmResourceProvider -ProviderNamespace "Microsoft.DataLakeStore"
$dataLakeStoreName = "oapdatalakestoredevelop"
$myrootdir = "/snotel-merged-csv-westus-v1.1"
$dir = Get-AzureRmDataLakeStoreChildItem -AccountName $dataLakeStoreName -Path $myrootdir
foreach($year in $dir) #year level
{
    $monthDir = Get-AzureRmDataLakeStoreChildItem -AccountName $dataLakeStoreName -Path $year.Path
    foreach($month in $monthDir)
    {
        $dayDir = Get-AzureRmDataLakeStoreChildItem -AccountName $dataLakeStoreName -Path $month.Path 
        foreach($day in $dayDir)
        {
            $hourFiles = Get-AzureRmDataLakeStoreChildItem -AccountName $dataLakeStoreName -Path $day.Path 
            foreach($file in $hourFiles)
            {
                if($file.Name -match "snotel")
                {
                    Write-Host -Message "Have $file.Name"
                    #name already correct
                    continue
                }
                else 
                {
                    $path = $file.Path.Substring(0, $file.Path.LastIndexOf('/'))
                    $fileParts = $file.Name.Split('.')
                    $destination = $path + "/" + $fileParts[0] + "." + $fileParts[1] + ".snotel.csv"
                    #Write-Host -Message "Take $file.Path and move to $destination"
                    Move-AzureRmDataLakeStoreItem -AccountName $dataLakeStoreName -Path $file.Path -Destination $destination
                }
            }
        }
    }
}
