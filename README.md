# Master

[![Build status](https://ci.appveyor.com/api/projects/status/of9xwqr2d1rs4xj9?svg=true)](https://ci.appveyor.com/project/KP0H/master)

Orationi.Master is core of system. Master should implement IOrationiMasterService interface to communicate with his slaves. By default connection should be initialized by node-side (orationi.slave). Orationi.Master manage slave services, database of modules, system health etc.
