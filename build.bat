msbuild /t:Clean,Build /p:Configuration=Release
cd Library
nuget pack -properties Configuration="Release"
cd ..