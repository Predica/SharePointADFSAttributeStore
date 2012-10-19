"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\x64\gacutil.exe" /i .\bin\debug\Predica.Tools.SharePoint.SharePointAttributeStore.dll
copy .\bin\debug\Predica.Tools.SharePoint.SharePointAttributeStore.pdb c:\Windows\assembly\GAC_MSIL\Predica.Tools.SharePoint.SharePointAttributeStore\1.0.0.0__8e7c7c1f18b74e88\
%windir%\system32\inetsrv\appcmd.exe recycle apppool /apppool.name:"ADFSAppPool"
net stop adfssrv
net start adfssrv