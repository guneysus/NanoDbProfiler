$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\\MSBuild\Current\Bin\amd64\MSBuild.exe"

function Init {
	# dotnet tool install -g nbgv
	dotnet tool install --tool-path .tools nbgv
	winget install --id Microsoft.NuGet
}

function Build {
  . "${msbuild}" /bl `
		.\src\NanoDbProfiler.AspNetCore\NanoDbProfiler.AspNetCore.csproj `
		-p:Configuration=Release `
		"/t:Clean;Build" `
		 -p:Deterministic=true
}

function Publish {
	# nbgv prepare-release
	dotnet restore src/NanoDbProfiler.sln
	# dotnet build src/NanoDbProfiler.sln --configuration Release
	Build
	dotnet pack src/NanoDbProfiler.sln -o dist/ --no-build --configuration Release
	dotnet nuget push `
		"dist/NanoDbProfiler.AspNetCore.$(nbgv get-version -v NuGetPackageVersion).nupkg" `
		--source https://www.myget.org/F/guneysu/api/v2/package --api-key=$env:NUGET_API_KEY
}

function Get-NugetVersion {
	$nugetVersion = $(nbgv get-version -v NuGetPackageVersion)
	Write-Host $nugetVersion
}