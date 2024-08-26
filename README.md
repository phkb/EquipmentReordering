This commandline tool automates the process of recreating the plist files for the "Loadout By Category" OXPs (https://wiki.alioth.net/index.php/Loadout_by_Category), a mod for the game Oolite (https://www.oolite.space).

Steps to run

1. In line 61, ensure the path to the equipment-overrides.plist file is correct. Adjust as required. Make sure paths have double backslashes \\.
     Any changes to structure should be recorded on the header lines in the overrides file (lines with "//** EQ_REORDERING_...")
     The format of the headers is important. There must be spaces between components and colons between f3/f5 values
     ie. //** EQ_REORDERING_NAVIGATION F3:0 F5:0 -------------------------
2. Set up the destination folder. By default, this is C:\Temp. If you prefer a different folder change the location in line 62.
     If the folder doesn't exist at runtime, it will be created.
3. Ensure the most recent Expansion Index zip has been downloaded from https://github.com/OoliteProject/oolite-expansion-catalog/releases 
     and extracted to C:\Oolite\Maps\OoliteExpansionIndex
     If you have extracted the index to a different location, change the path in line 63.
4. Run the program

At completion, assuming no errors were encountered, 3 files will be generated:
1. C:\temp\f3_hdrequip.txt       This contains all the header equipment items to be pasted directly back into equipment.plist. 
                                 This will only be useful if any of the headers have actually changed.
2. C:\temp\f3_ordering.txt       This contains the new data which can be pasted directly back into equipment-overrides.plist
3. C:\temp\f3_missing.txt        This contains stubs for any equipment items that are in the Expansion Index but not in our data
                                 This will be useful if new equipment items are created. You can paste them as-is into the overrides file
                                 in the desired section (no changes required), and then rerun the steps above to create a new overrides 
                                 file with the new item automatically ordered.
Two addtional files will be generated for 1.90 compatibility
4. C:\temp\f3_hdrequip190.txt    This contains all the header equipment items to be pasted back to equipment.plist in the 190 version.
5. C:\temp\f3_ordering190.txt    This contains the new data for 1.90 compatibility which can be pasted directly back to equipment-overrides.plist in the 190 version
