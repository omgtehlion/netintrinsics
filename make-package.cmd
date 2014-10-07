setlocal enableextensions enabledelayedexpansion

rm -rf nuget

pushd NetIntrinsics
rm -f NetIntrinsics-net*
for %%s in (net20/v2.0 net35/v3.5 net40/v4.0 net451/v4.5.1) do (
	for /F "tokens=1 delims=/" %%a in ("%%s") do set fn=%%a
	for /F "tokens=2 delims=/" %%a in ("%%s") do set ver=%%a
	set csproj=NetIntrinsics-!fn!.csproj
	cat NetIntrinsics.csproj | sed s/OutputPath^>bin\\Release/OutputPath^>..\\nuget\\lib\\!fn!/ | sed s/TargetFrameworkVersion^>.*^</TargetFrameworkVersion^>!ver!^</ > !csproj!
	"C:\Program Files (x86)\MSBuild\12.0\Bin\msbuild" /p:Configuration=Release !csproj!
	rm -f !csproj!
)
popd

rm -f nuget/lib/*/*.pdb
..\nuget pack -BasePath nuget
rm -rf nuget
