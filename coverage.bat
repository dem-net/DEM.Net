rem .\packages\OpenCover.4.6.519\tools\opencover.console.exe -register:user -target:"%VS140COMNTOOLS%\..\IDE\MSTest.exe" -targetargs:"/testcontainer:.\DEM.Net.Test\bin\Debug\DEM.Net.Test.dll" -output:".\Dem.Net_coverage.xml"

.\packages\Codecov.1.1.0\tools\Codecov.exe -f ".\Dem.Net_coverage.xml"
pause

