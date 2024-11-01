$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\\MSBuild\Current\Bin\amd64\MSBuild.exe"

function Init {
	# dotnet tool install -g nbgv
	dotnet tool install --tool-path .tools nbgv
	winget install --id Microsoft.NuGet
}

function Get-Version {
	dotnet restore .\src\NanoDbProfiler.AspNetCore\NanoDbProfiler.AspNetCore.csproj --use-lock-file | Out-Null

	# Define the path to the packages.lock.json file
	$filePath = ".\src\NanoDbProfiler.AspNetCore\packages.lock.json"

	# Check if the file exists
	if (Test-Path $filePath) {
		# Read and parse the JSON content
		$jsonContent = Get-Content -Path $filePath -Raw | ConvertFrom-Json

		# Navigate to the net8.0 dependencies and find Microsoft.EntityFrameworkCore.Relational
		$dependency = $jsonContent.dependencies."net8.0"."Microsoft.EntityFrameworkCore.Relational"

		if ($dependency -ne $null) {
			# Extract and display the resolved version
			$version = $dependency.resolved
			Write-Output $version
		}
		else {
			Write-Error "Microsoft.EntityFrameworkCore.Relational dependency not found in packages.lock.json"
		}
	}
	else {
		Write-Error "File not found: $filePath"
	}

}

function Patch-Version {
	dotnet nbgv set-version $(pake get-version)

}

function Build {


	# . "${msbuild}" /bl `
	# 	.\src\NanoDbProfiler.AspNetCore\NanoDbProfiler.AspNetCore.csproj `
	# 	-p:Configuration=Release `
	# 	"/t:Clean;Build" `
	# 	-p:Deterministic=true

	dotnet build .\src\NanoDbProfiler.AspNetCore\NanoDbProfiler.AspNetCore.csproj /p:PublicRelease=true
}

function Publish {
	# nbgv prepare-release
	dotnet restore src/NanoDbProfiler.sln
	# dotnet build src/NanoDbProfiler.sln --configuration Release
	Build
	dotnet pack src/NanoDbProfiler.sln -o dist/ --no-build --configuration Release
	dotnet nuget push `
		"dist/NanoDbProfiler.AspNetCore.$(nbgv get-version -v Version).nupkg" `
		--source https://www.myget.org/F/guneysu/api/v2/package --api-key=$env:MYGET_API_KEY
}

function Get-NugetVersion {
	$nugetVersion = $(nbgv get-version -v NuGetPackageVersion)
	Write-Host $nugetVersion
}