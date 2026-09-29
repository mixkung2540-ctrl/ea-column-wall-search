# EA Column and Wall Search

Windows desktop utility for preparing ETABS Column/Wall force data from Access databases for use in engineering calculation workflows.

Current release: **1.0.3**

## Features

- Imports multiple ETABS Access files for Column or Wall data
- Supports ETABS 2020, 2016 and 9.7.4 schemas
- Filters multiple members and storeys
- Applies PT moment overrides to UDCON1–4
- Renames output members without changing original matching keys
- Saves and opens `.easearch` project files
- Exports CSV or copies seven tab-separated data columns without headers
- Checks GitHub Releases for updates only when the user clicks **Check Update**

## Build

Run `build.ps1` on 64-bit Windows with .NET Framework 4.x. Runtime Access import also requires the 64-bit Microsoft ACE OLE DB provider.

The repository contains source code only. Engineering databases, `.easearch` projects, test data and generated executables are excluded. Download the portable build from [GitHub Releases](https://github.com/mixkung2540-ctrl/ea-column-wall-search/releases).

## Important

This program assists with data preparation. Users must verify units, signs, load cases, force mapping and results before engineering use. See `README_TH.txt` for full Thai instructions.
