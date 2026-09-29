# EA Column and Wall Search

Windows desktop utility for preparing ETABS Column and Wall force data from Access databases.

Current version: **1.0.2**

## Download

Download [EA_Search_1.0.2_Portable.zip](release/EA_Search_1.0.2_Portable.zip), extract the ZIP, then run `EA_Search_1.0.2.exe`.

## Main features

- Import multiple ETABS Access files
- Column and Wall modes in one program
- ETABS 2020, 2016 and 9.7.4 schemas
- Multiple member and storey selection
- PT moment overrides for UDCON1 to UDCON4
- Output name editing
- Save and open `.easearch` projects
- CSV export and seven column tab separated Copy Data output

## Requirements

- Windows 64 bit
- .NET Framework 4.x
- Microsoft ACE OLE DB provider 64 bit for Access import

Run `build.ps1` to compile the source. Full Thai instructions are in `README_TH.txt`.

Engineering databases and project files are excluded from this repository. Users must verify units, signs, load cases, force mapping and results before engineering use.
