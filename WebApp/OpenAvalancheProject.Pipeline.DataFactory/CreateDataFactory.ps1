#
# CreateDataFactory.ps1
#
Import-Module AzureRm.DataFactoryV2 -MinimumVersion 0.4.1
Login-AzureRmAccount
Set-Location 'D:\src\GitHub\OpenAvalancheProject\WebApp\OpenAvalancheProject.Pipeline.DataFactory\Config\';
$DataFactoryName = "oapdatafactory";
$ResourceGroup = "oapresourcegroup";
Set-AzureRmDataFactoryV2LinkedService -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroup -Name "AzureStorageLinkedService" -DefinitionFile ".\AzureStorageLinkedService.json"
Set-AzureRmDataFactoryV2LinkedService -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroup -Name "AzureDataLakeLinkedService" -DefinitionFile ".\AzureDataLakeLinkedService.json"
$DFPipeLine = Set-AzureRmDataFactoryV2Pipeline -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroup -Name "CookForecastPipeline" -DefinitionFile ".\CookForecastPipeline.json"
Set-AzureRmDataFactoryV2Trigger -ResourceGroupName $ResourceGroup -DataFactoryName $DataFactoryName -Name "DailyTriggerCookForecastPipeline" -DefinitionFile ".\DailyTriggerCookForecastPipeline.json"
#Get Status of trigger
Get-AzureRmDataFactoryV2Trigger -ResourceGroupName $ResourceGroup -DataFactoryName $DataFactoryName -Name "DailyTriggerCookForecastPipeline"
#Start the trigger
Start-AzureRmDataFactoryV2Trigger -ResourceGroupName $ResourceGroup -DataFactoryName $DataFactoryName -Name "DailyTriggerCookForecastPipeline"
$RunId = Invoke-AzureRmDataFactoryV2Pipeline -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroup -PipelineName "CookForecastPipeline" -ParameterFile ".\PipelineInputParameters.json"
#monitor 
#https://oapdlanalyticsdevelop.azuredatalakeanalytics.net/Jobs/a63a38a2-27a8-4a37-898e-ce95100b967f?api-version=2015-10-01-preview
Stop-AzureRmDataFactoryV2Trigger -ResourceGroupName $ResourceGroup -DataFactoryName $DataFactoryName -Name "DailyTriggerCookForecastPipeline"
Remove-AzureRmDataFactoryV2Trigger -ResourceGroupName $ResourceGroup -DataFactoryName $DataFactoryName -Name "DailyTriggerCookForecastPipeline"
 