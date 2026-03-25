@echo off

dotnet test tests/Domain.Tests --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory coverage/
dotnet test tests/Application.Tests --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory coverage/
dotnet test tests/Integration.Tests --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory coverage/

reportgenerator ^
  -reports:"coverage/**/coverage.cobertura.xml" ^
  -targetdir:"coveragereport" ^
  -reporttypes:"Html" ^
  -assemblyfilters:"+Domain;+Application;+Infrastructure;+Api" ^
  -filefilters:"-*Generated*"

start coveragereport\index.html