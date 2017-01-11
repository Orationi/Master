New-Service -Name "OrationiMasterService" -BinaryPathName "Orationi.Master.exe -k netsvcs"
Start-Service -Name "OrationiMasterService"