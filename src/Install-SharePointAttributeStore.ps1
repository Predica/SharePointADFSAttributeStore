$AdfsSnapinName = "Microsoft.Adfs.Powershell"
If ( (Get-PSSnapin -Name $AdfsSnapinName -EA SilentlyContinue) -eq $Null ) {
	Write-Host ( "Loading snapin: {0}" -F $AdfsSnapinName )
	Add-PSSnapin -Name $AdfsSnapinName -EA Stop | Out-Null
}

$AdfsSharePointStoreName = "SharePoint Site"
$AdfsSharePointStoreClassName = "Predica.Tools.SharePoint.SharePointAttributeStore.SharePointListAttributeStore, Predica.Tools.SharePoint.SharePointAttributeStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=8e7c7c1f18b74e88"
$ADFSSharePointAttributeStoreConfig = @{
	"SiteUrl"="https://portal.qsdev.local"
	"ListName"="Persons"
}

Write-Host ( "Adding custom attribute Store`n{0}" -F $AdfsSharePointStoreName )
If ( Get-ADFSAttributeStore -Name $AdfsSharePointStoreName -EA SilentlyContinue ) {
	Remove-ADFSAttributeStore -TargetName $AdfsSharePointStoreName
}

Add-ADFSAttributeStore -TypeQualifiedName $AdfsSharePointStoreClassName -Configuration $ADFSSharePointAttributeStoreConfig -Name $AdfsSharePointStoreName