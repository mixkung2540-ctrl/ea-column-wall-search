# EA Column and Wall Search

Windows desktop utility for preparing ETABS Column/Wall force data from Access databases for use in engineering calculation workflows.

Current release: **1.1**

Members show `Original [Source] (Output)`. Use **Sort Original**, **Sort Output**, or **Renamed** in Members and Output Names. In Members, click the first checkbox then Shift-click another to check or uncheck a visible range. Existing 1.0.5 features, including Excel paste into PT Override, remain available.

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
# Version 1.0.4 selection controls

- **None** still clears only visible items in its list.
- **Clear All** beside each Find box clears all selections in that list, including hidden items. It keeps the Find text and selections in the other list.
- **Edit > Clear All Selections (including hidden)** clears both Members and Stories.
- Imported data, output names and saved PT values are retained. Search again after selecting new items.


### Paste PT (1.0.5)

After Prepare PT, copy the moment cells in Excel, select the first destination cell in PT Override, then press Ctrl+V or click Paste PT. Check the Story order and click Append PT. Blank retains the original ETABS moment; zero is an explicit override.
