$targetNetFramework='net8.0'
#param([string]$targetNetFramework)

$projectName='Microsoft.Identity.Web.AotCompatibiliy.TestApp'
$rootDirectory = Split-Path $PSScriptRoot -Parent
$publishOutput = dotnet publish $rootDirectory/tests/$projectName/$projectName.csproj --self-contained -nodeReuse:false /p:UseSharedCompilation=false

$actualWarningCount = 0

foreach ($line in $($publishOutput -split "`r`n"))
{
    if (($line -like "*analysis warning IL*") -or ($line -like "*analysis error IL*"))
    {
        Write-Host $line
        $actualWarningCount += 1
    }
}

Write-Host "Actual warning count is: ", $actualWarningCount
$expectedWarningCount = 0

if ($LastExitCode -ne 0)
{
    Write-Host "There was an error while publishing AotCompatibility Test App. LastExitCode is:", $LastExitCode
    Write-Host $publishOutput
}

#$runtime = if ($IsWindows) {  "win-x64" } elseif ($IsMacOS) { "macos-x64"} else {"linux-x64"}
$app = './$projectName.exe'
#if ($IsWindows ) {"./Microsoft.IdentityModel.AotCompatibility.TestApp.exe" } else {"./Microsoft.IdentityModel.AotCompatibility.TestApp" }

Push-Location $rootDirectory/tests/$projectName/bin/Release/$targetNetFramework/win-x64

Write-Host "Executing test App..."
$app
Write-Host "Finished executing test App"

if ($LastExitCode -ne 0)
{
  Write-Host "There was an error while executing AotCompatibility Test App. LastExitCode is:", $LastExitCode
}

Pop-Location

$testPassed = 0
if ($actualWarningCount -ne $expectedWarningCount)
{
    $testPassed = 1
    Write-Host "Actual warning count:", actualWarningCount, "is not as expected. Expected warning count is:", $expectedWarningCount
}

Exit $testPassed






