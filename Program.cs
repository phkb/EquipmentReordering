using System.Text;
/*
 * Steps to run
 * 
 * 1. In line 61, ensure the path to the equipment-overrides.plist file is correct. Adjust as required. Make sure paths have double backslashes \\.
 *      Any changes to structure should be recorded on the header lines in the overrides file (lines with "//** EQ_REORDERING_...")
 *      The format of the headers is important. There must be spaces between components and colons between f3/f5 values
 *      ie. //** EQ_REORDERING_NAVIGATION F3:0 F5:0 -------------------------
 * 2. Set up the destination folder. By default, this is C:\Temp. If you prefer a difference folder change the location in line 62.
 *      If the folder doesn't exist at runtime, it will be created.
 * 3. Ensure the most recent Expansion Index zip has been downloaded from https://github.com/OoliteProject/oolite-expansion-catalog/releases 
 *      and extracted to C:\Oolite\Maps\OoliteExpansionIndex
 *      If you have extracted the index to a different location, change the path in line 63.
 * 4. Run the program
 * 
 * At completion, assuming no errors were encountered, 3 files will be generated:
 * 1. C:\temp\f3_hdrequip.txt       This contains all the header equipment items to be pasted directly back into equipment.plist. 
 *                                  This will only be useful if any of the headers have actually changed.
 * 2. C:\temp\f3_ordering.txt       This contains the new data which can be pasted directly back into equipment-overrides.plist
 * 3. C:\temp\f3_missing.txt        This contains stubs for any equipment items that are in the Expansion Index but not in our data
 *                                  This will be useful if new equipment items are created. You can paste them as-is into the overrides file
 *                                  in the desired section (no changes required), and then rerun the steps above to create a new overrides 
 *                                  file with the new item automatically ordered.
 * Two addtional files will be generated for 1.90 compatibility
 * 4. C:\temp\f3_hdrequip190.txt    This contains all the header equipment items to be pasted back to equipment.plist in the 190 version.
 * 5. C:\temp\f3_ordering190.txt    This contains the new data for 1.90 compatibility which can be pasted directly back to equipment-overrides.plist in the 190 version
*/

namespace EquipmentReordering;
class Program
{
    // dictionary to aid with creating the header equip items
    private static readonly Dictionary<string, Array> equipKeys = new()
    {
        { "EQ_REORDERING_REFUELING", new string[2] { "Fuel", "Various items related to the refuelling of your ship or Quirium fuel in general." } },
        { "EQ_REORDERING_GENERAL", new string[2] { "General/Miscellaneous", "General and miscellaneous items not categorised elsewhere." } },
        { "EQ_REORDERING_NAVIGATION", new string[2] { "Navigation", "Items relating to the movement of the ship." } },
        { "EQ_REORDERING_LASERS", new string[2] { "Laser Weapons", "Weapons that can be mounted on your ship's hardpoints." } },
        { "EQ_REORDERING_MISSILES", new string[2] { "Pylon Weapons", "Weapons that can be mounted in your ship's pylon mounts." } },
        { "EQ_REORDERING_ENERGY", new string[2] { "Energy", "Items related to your ship's energy banks." } },
        { "EQ_REORDERING_DEFENSIVE", new string[2] { "Defensive", "Items to help you stay alive." } },
        { "EQ_REORDERING_SHIELDS", new string[2] { "Shields", "Items relating to your ship's shields." } },
        { "EQ_REORDERING_TARGETING", new string[2] { "Targeting", "Items relating to your ship's targeting systems." } },
        { "EQ_REORDERING_ARMOUR", new string[2] { "Armour", "Items related to your ship's armour." } },
        { "EQ_REORDERING_SCANNERS", new string[2] { "Scanners", "canning equipment of various types." } },
        { "EQ_REORDERING_MFD", new string[2] { "M.F.D.s", "Multi-Function Display (MFD) units." } },
        { "EQ_REORDERING_CARGO", new string[2] { "Cargo/Mining", "Equipment items related to cargo handling or mining." } },
        { "EQ_REORDERING_LMSS", new string[2] { "Laser Mount Switching System", "Items related to the Laser Mount Switching System." } },
        { "EQ_REORDERING_TRACTORBEAM", new string[2] { "Towbar and Accessories", "Equipment related to use of the Towbar and associated tractor beam." } },
        { "EQ_REORDERING_REPAIRBOTS", new string[2] { "Self-Repair System", "Items related to the Self-Repair system." } },
        { "EQ_REORDERING_ESCORTS", new string[2] { "Escorts/Fighters", "Items relating to escorts and fighters." } },
        { "EQ_REORDERING_LICENSES", new string[2] { "Documentation", "Licenses, permits, insurance and subscriptions of various kinds." } },
        { "EQ_REORDERING_ILLICIT", new string[2] { "Illicit", "Items of a definite dubious legality." } },
        { "EQ_REORDERING_SALVAGE", new string[2] { "Second-Hand Equipment", "Second-hand equipment salvaged from derelict ships." } }
    };

    static void Main(string[] args)
    {
        Console.WriteLine("Starting process");

        string plistFile = "C:\\Oolite\\AddOns\\Miscellaneous.oxp\\LoadoutByCategory.oxp\\Config\\equipment-overrides.plist";
        string outputFolder = "C:\\Temp";
        string indexFolder = "C:\\Oolite\\Maps\\OoliteExpansionIndex";

        // make sure our destination exists.
        if (!Path.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // look for a double OoliteExpansionIndex folder, and adjust if required
        if (Path.Exists(indexFolder + "\\OoliteExpansionIndex"))
        {
            indexFolder += "\\OoliteExpansionIndex";
        }

        // check to see if there is a "_192" version file - if so, switch to it.
        if (File.Exists(plistFile.Replace(".plist", "_192.plist"))) 
        {
            plistFile = plistFile.Replace(".plist", "_192.plist");
        }
        string orig = File.ReadAllText(plistFile);
        // tweak the text a bit before processing
        orig = orig.Replace("\"", "'") + "\n//**";

        // split the original text up into individual lines
        string[] list = orig.Split(['\n']);

        int soF5 = 0; // the sort order index value for the F5 page
        int soF3 = 0; // the purchase_sort_order value for the F3 page
        int inc = 4; // the increment value (ie how much to increase/decrease the indexes by with each line of data)
        int sectionCount = 0; // counts which section we are up to
        int totalSections = 0; // the total number of sections found in the input file
        StringBuilder sbData = new(); // holds the updated overrides data
        StringBuilder sbData190 = new(); // holds the updated overrides data for v1.90
        StringBuilder sbEquip = new(); // holds the header equip item data
        StringBuilder sbEquip190 = new(); // holds the header equip item data for v1.90
        List<string> section = []; // temporary holding spot for data, used for sorting
        List<string> foundFiles = []; // holds all the Expansion index files we found while processing

        // template equip items for the headers
        // 0 = header text
        // 1 = key
        // 2 = desc
        // 3 = sort_order
        // 4 = purchase_sort_order
        string equip = "\t(\r\n\t\t0,\r\n\t\t0,\r\n\t\t\"•• {0} ••\",\r\n\t\t\"{1}\",\r\n\t\t\"{2}\",\r\n\t\t{{\r\n\t\t\tavailable_to_all = yes;" + 
            "\r\n\t\t\tavailable_to_NPCs = yes;\r\n\t\t\tavailable_to_player = yes;\r\n\t\t\tinstallation_time = 1.0;" + 
            "\r\n\t\t\tcondition_script = \"loadout_categories_conditions.js\";\r\n\t\t\tsort_order = {3};\r\n\t\t\tpurchase_sort_order = {4};" + 
            "\r\n\t\t\thide_values = yes;\r\n\t\t\tdisplay_color = \"cyanColor\";\r\n\t\t\tscript_info = {{sortOrder = {3};}};\r\n\t\t}}\r\n\t),";
        string equipf5 = "\t(\r\n\t\t0,\r\n\t\t0,\r\n\t\t\"•• {0} ••\",\r\n\t\t\"{1}_F5\",\r\n\t\t\"{2}\",\r\n\t\t{{\r\n\t\t\tavailable_to_all = yes;" + 
            "\r\n\t\t\tavailable_to_NPCs = yes;\r\n\t\t\tavailable_to_player = yes;\r\n\t\t\tinstallation_time = 1.0;" + 
            "\r\n\t\t\tcondition_script = \"loadout_categories_conditions.js\";\r\n\t\t\tsort_order = {3};\r\n\t\t\tpurchase_sort_order = {4};" + 
            "\r\n\t\t\thide_values = yes;\r\n\t\t\tdisplay_color = \"cyanColor\";\r\n\t\t\tscript_info = {{sortOrder = {3};}};\r\n\t\t}}\r\n\t),";

        // these are items we want to force to the top of any group they are in
        string[] force_top = ["'EQ_FUEL'",
            "'EQ_FUEL_ALT1'",
            "'EQ_FUEL_ALT2'",
            "'EQ_FUEL_ALT3'",
            "'EQ_FUEL_ALT4'",
            "'EQ_FUEL_ALT5'",
            "'EQ_FUEL_ALT6'",
            "'EQ_FUEL_ALT7'",
            "'EQ_PIRATE_FUEL'",
            "'EQ_HOLY_FUEL'",
            "'EQ_RRS_FUEL'",
            "'EQ_WEAPON_NONE'",
            "'EQ_MISSILE_REMOVAL'",
            "'EQ_IND_MISSILE_REMOVAL'"];
        // these items we want to force the sort_order to match the purchase_sort_order
        string[] fuel = ["'EQ_FUEL'",
            "'EQ_FUEL_ALT1'",
            "'EQ_FUEL_ALT2'",
            "'EQ_FUEL_ALT3'",
            "'EQ_FUEL_ALT4'",
            "'EQ_FUEL_ALT5'",
            "'EQ_FUEL_ALT6'",
            "'EQ_FUEL_ALT7'",
            "'EQ_PIRATE_FUEL'",
            "'EQ_HOLY_FUEL'",
            "'EQ_RRS_FUEL'"];

        Console.WriteLine("Calculating number of sections required...");
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].Trim().StartsWith("//**") && list[i].Contains("EQ_")) totalSections += 1;
        }

        Console.WriteLine("Reading data...");
        // so lets go through our list, line by line
        for (int i = 0; i < list.Length; i++)
        {
            string item = list[i];
            // if the line is blank, don't do anything
            if (item.Trim() == "" || item.Trim() == "{" || item.Trim() == "}") continue;
            // if the line starts with comments, that's the beginning of a new section, so we can process what's in the section list
            if (item.Trim().StartsWith("//**"))
            {
                // do we have anything in our section list to process?
                if (section.Count > 0)
                {
                    // if so, first lets go through an look for any items we want to force to the top of the list after the sorting
                    for (int j = 0; j < section.Count; j++)
                    {
                        bool found = false;
                        // loop through our list of forced keys
                        for (int k = 0; k < force_top.Length; k++)
                        {
                            if (section[j].Contains(force_top[k]))
                            {
                                found = true;
                                break;
                            }
                        }
                        // if our line of data contains one of those keys, put a "0" at the front of the name, so a normal sort will put it at the top
                        if (found)
                        {
                            section[j] = "0" + section[j];
                        }
                    }

                    // sort our list alphabetically
                    section.Sort();

                    // go through the sorted list one more time
                    for (int j = 0; j < section.Count; j++)
                    {
                        // first, remove any instances where we added a "0" to the name
                        if (section[j].StartsWith('0'))
                        {
                            section[j] = section[j][1..];
                        }

                        // next, we need to insert the ordering number into the "sort_order"
                        // first split the line using the characters we defined
                        string[] items = section[j].Split("::");
                        // the line for the overrides file will be the second element of this array
                        string newitem = items[1];
                        // find the position where the first number (sort_order) should be inserted at
                        int pos1start = newitem.IndexOf("sort_order") + 13;
                        // find the end position for that number (basically, where the ";" character is)
                        int pos1end = newitem.IndexOf(';');
                        // find the position where the second number (purchase_sort_order) should be inserted at
                        int pos2start = newitem.IndexOf("purchase_sort_order") + 22;
                        // find the end position for that number
                        int pos2end = newitem.IndexOf(';', pos2start);
                        // find the position where the third number should be inserted at
                        int pos3start = newitem.IndexOf("sortOrder") + 12;
                        // find the end position for that number
                        int pos3end = newitem.IndexOf(';', pos3start);
                        // finally, find the last code character of that line, which should be a ";" character
                        int pos4end = newitem.LastIndexOf(';');
                        // build up the new string

                        // check to see if this is one of the fuel items
                        bool fuelFound = false;
                        for (int k = 0; k < fuel.Length; k++)
                        {
                            if (newitem.Contains(fuel[k])) fuelFound = true;
                        }

                        var item190 = newitem;
                        newitem = newitem[..pos1start] + (fuelFound ? soF3.ToString() : soF5.ToString()) + // grab everything from the start of the line to our first position, and add in the index
                            newitem[pos1end..pos2start] + soF3.ToString() + // grab everything from the end of the first position, to the start of the second, and add in the alt index 
                            newitem[pos2end..pos3start] + (fuelFound ? soF3.ToString() : soF5.ToString()) + // grab everything from the end of the second position, to the start of the third, and add in the index again
                            newitem[pos3end..(pos4end + 1)] + " // " + items[0].Trim(); // grab everything from the end of the third position, to the end of the code, and then add in the comment marker and the actual name of the item
                        // add this line to our output data
                        sbData.AppendLine(newitem);
                        // build the 1.90 version
                        item190 = item190[..pos1start] + soF3.ToString() + // grab everything from the start of the line to our first position, and add in the index
                            item190[pos1end..pos2start] + soF3.ToString() + // grab everything from the end of the first position, to the start of the second, and add in the alt index 
                            item190[pos2end..pos3start] + soF3.ToString() + // grab everything from the end of the second position, to the start of the third, and add in the index again
                            item190[pos3end..(pos4end + 1)] + " // " + items[0].Trim(); // grab everything from the end of the third position, to the end of the code, and then add in the comment marker and the actual name of the item
                        // add this line to our 1.90 output data
                        sbData190.AppendLine(item190);

                        // increment/decrement the two indexes by our inc value 
                        soF5 -= inc;
                        soF3 += inc;
                    }
                    // once we've finished outputting the data, clear the section list so we can go again
                    section.Clear();
                }

                // if the line we're working with isn't a comment line...
                if (item.Trim() != "//**")
                {
                    // update the header output with the name of the header key and the starting point for its items, being 1 less than the starting point
                    // ie for the ground that starts at 1000, the header will be set to 999
                    string[] itemList = item.Split(" ");
                    // pull out the header equip item and section indexes from the header line
                    int f3 = 0;
                    int f5 = 0;
                    for (int j = 0; j < itemList.Length; j++)
                    {
                        if (itemList[j].StartsWith("EQ_")) item = itemList[j];
                        if (itemList[j].StartsWith("F3:")) f3 = int.Parse(itemList[j].Split(":")[1]);
                        if (itemList[j].StartsWith("F5:")) f5 = int.Parse(itemList[j].Split(":")[1]);
                    }
                    // calculate the starting point for our two indexes
                    // for the f3 page
                    soF3 = 1000 * (f3 - 1);
                    if (soF3 == 0) soF3 = 10;
                    // for the f5 page
                    soF5 = 1000 * ((totalSections + 1) - (f5 - 1)) - 10;

                    // put a blank line between sections
                    if (sectionCount > 0)
                    {
                        sbData.AppendLine("");
                        sbData190.AppendLine("");
                    }

                    // grab the extra info for the header equip item
                    Array ar = equipKeys[item];
                    string? name = "";
                    string? descr = "";
                    if (ar is not null)
                    {
                        if (ar.GetValue(0) is not null) name = ar.GetValue(0).ToString();
                        if (ar.GetValue(1) is not null) descr = ar.GetValue(1).ToString();
                    }

                    // build header equip file
                    sbEquip.AppendLine(String.Format(equip, name, item, descr, soF3.ToString(), soF3.ToString()));
                    sbEquip.AppendLine(String.Format(equipf5, name, item, descr, (soF5 + 5).ToString(), soF3.ToString()));

                    sbEquip190.AppendLine(String.Format(equip, name, item, descr, soF3.ToString(), soF3.ToString()));
                    sbEquip190.AppendLine(String.Format(equipf5, name, item, descr, ((soF3 == 10 ? 0 : soF3) + 1000 - 10).ToString(), ((soF3 == 10 ? 0 : soF3) + 1000 - 10).ToString()));

                    // special case for navigation
                    // duplicate with astrogation instead
                    if (item.Contains("_NAVIGATION"))
                    {
                        sbEquip.AppendLine(String.Format(equip, "Astrogation", "EQ_REORDERING_ASTROGATION", descr, soF3.ToString(), soF3.ToString()));
                        sbEquip.AppendLine(String.Format(equipf5, "Astrogation", "EQ_REORDERING_ASTROGATION", descr, soF5.ToString(), soF3.ToString()));
                        
                        sbEquip190.AppendLine(String.Format(equip, "Astrogation", "EQ_REORDERING_ASTROGATION", descr, soF3.ToString(), soF3.ToString()));
                        sbEquip190.AppendLine(String.Format(equipf5, "Astrogation", "EQ_REORDERING_ASTROGATION", descr, ((soF3 == 10 ? 0 : soF3) + 1000 - 10).ToString(), ((soF3 == 10 ? 0 : soF3) + 1000 - 10).ToString()));
                    }

                    // add a comment line into our main data file, which acts as a section separator
                    sbData.AppendLine("    //** " + item + " F3:" + f3.ToString() + " F5:" + f5.ToString() + " --------------------------------------------------------------------------------");
                    // add a comment line into our v1.90 data file, which acts as a section separator
                    sbData190.AppendLine("    //** " + item + " --------------------------------------------------------------------------------");

                }
                // increment our section counter
                sectionCount += 1;
            }
            else
            {
                // here, were looking at a normal data line
                // first, split the line using the ' (single quote) character
                // because all keys are enclosed in single quotes, the 2nd element of this array will be the equipment key
                string[] data = item.Split("'");
                string eqkey = data[1];
                // now lets try to find the name of this equipment item
                string name = "";
                // put the key into a filename string
                string filename = indexFolder + "\\equipment\\" + eqkey + ".html";
                // add this filename to our list of found files
                foundFiles.Add(filename);
                try
                {
                    // try reading all the text from that HTML file
                    string html = File.ReadAllText(filename);
                    // if this completes successfully, split the text into lines
                    string[] lines = html.Split(['\n']);
                    // loop through the lines of html text
                    for (int j = 0; j < lines.Length; j++)
                    {
                        // look for the "Name" table row
                        if (lines[j].Contains("<tr><td>Name</td><td>"))
                        {
                            // when found, extract the name from the line of data
                            name = lines[j].Replace("<tr><td>Name</td><td>", "").Replace("</td></tr>", "").Trim();
                        }
                    }
                }
                // if we couldn't find a file (ie could be from an OXP that isn't in the manager), get whatever text has been placed in the comments of the line
                catch { name = item.Split("// ")[1]; }
                // add the name and data line into our section list, ready for sorting
                section.Add((name + "                                                                                                                    ").ToString()[..100] + "::" + item);
            }
        }

        sbEquip.AppendLine(String.Format(equipf5, "Weaponry", "EQ_REORDERING_WEAPONONLY", "", "1", "1"));
        sbEquip.AppendLine(String.Format(equipf5, "Berths/Weaponry", "EQ_REORDERING_BERTHWEAPON", "", "1", "1"));

        sbEquip190.AppendLine(String.Format(equipf5, "Weaponry", "EQ_REORDERING_WEAPONONLY", "", "1", "1"));
        sbEquip190.AppendLine(String.Format(equipf5, "Berths/Weaponry", "EQ_REORDERING_BERTHWEAPON", "", "1", "1"));

        Console.WriteLine("Creating f3_hdrequip.txt");
        // write all the header info to the header file
        File.WriteAllText(outputFolder + "\\f3_hdrequip.txt", "(\n" + sbEquip.ToString() + "\n)");
        File.WriteAllText(outputFolder + "\\f3_hdrequip190.txt", "(\n" + sbEquip190.ToString() + "\n)");

        Console.WriteLine("Creating f3_ordering.txt");
        // write all the reordering info to the ordering file
        File.WriteAllText(outputFolder + "\\f3_ordering.txt", "{\n" + sbData.ToString().Replace("'", "\"").Replace("&#39;", "'").Replace("&amp;", "&").Replace("&quot;", "\"") + "\n}");
        File.WriteAllText(outputFolder + "\\f3_ordering190.txt", "{\n" + sbData190.ToString().Replace("'", "\"").Replace("&#39;", "'").Replace("&amp;", "&").Replace("&quot;", "\"") + "\n}");

        Console.WriteLine("Checking expansion index for missing equipment items");
        // now we're going to check all the files we didn't look at in the first second, and list any equipment items that might need an entry
        // first, get a list of all the HTML files in the equipment folder
        string[] dir = Directory.GetFiles(indexFolder + "\\equipment\\", "*.html");
        // create a new stringbuilder to hold our missing records
        StringBuilder sbMissing = new();

        // go through the file list one by one
        for (int i = 0; i < dir.Length; i++)
        {
            // have we considered this file in the first part of the code?
            if (!foundFiles.Contains(dir[i]))
            {
                // if not, let's check it out..
                string name = "";
                string id = "";
                // get all the text from the file
                string html = File.ReadAllText(dir[i]);

                // eliminate some files that don't have visible equipment items, based on what we know, or have already extracted
                if (html.Contains("available_to_player</td><td>false") || html.Contains("available_to_player</td><td>no", StringComparison.CurrentCultureIgnoreCase)) continue;
                if (html.Contains("visible</td><td>false") || html.Contains("visible</td><td>no", StringComparison.CurrentCultureIgnoreCase)) continue;

                if (html.Contains("NumericHUD")) continue;
                if (html.Contains("EQ_KINGCOBRA")) continue;
                if (html.Contains("EQ_DANGEROUSHUD")) continue;
                if (html.Contains("EQ_XENONHUD")) continue;
                if (html.Contains("EQ_VIMANAX")) continue;
                if (html.Contains("EQ_OXPCONFIG")) continue;
                if (html.Contains("EQ_ALMANAC")) continue;
                if (html.Contains("EQ_MMS_NPC")) continue;
                if (html.Contains("EQ_NPC")) continue;
                if (html.Contains("EQ_NSHIELDS_NPC")) continue;
                if (html.Contains("EQ_TCAT")) continue;
                if (html.Contains("EQ_VECTOR")) continue;
                if (html.Contains("EQ_UNIT")) continue;
                if (html.Contains("EQ_ADVANCED_MINE")) continue;
                if (html.Contains("EQ_HOLD_MINE")) continue;
                if (html.Contains("EQ_ENGAGE_MINE")) continue;
                if (html.Contains("EQ_SWEEP_MINE")) continue;
                if (html.Contains("EQ_REGROUP_MINE")) continue;
                if (html.Contains("EQ_ADVANCE_MINE")) continue;
                if (html.Contains("EQ_UNDO_MINE")) continue;
                if (html.Contains("EQ_CONTROLBUTTON_MINE")) continue;
                if (html.Contains("EQ_GW_CONTROL_PANEL")) continue;
                if (html.Contains("EQ_MISSILESUMMARY")) continue;
                if (html.Contains("EQ_REORDERING")) continue;

                // if we get to this point, we should output the data for consideration
                // split up the HTML by lines
                string[] lines = html.Split(['\n']);
                // loop throught the lines one by one
                for (int j = 0; j < lines.Length; j++)
                {
                    // look for the "Identifier" keyword
                    if (lines[j].Contains("<tr><td>Identifier</td><td>"))
                    {
                        // extract the equipment key from the line of HTML text
                        id = lines[j].Replace("<tr><td>Identifier</td><td>", "").Replace("</td></tr>", "").Trim();
                    }
                    // look for the "Name" keyword
                    if (lines[j].Contains("<tr><td>Name</td><td>"))
                    {
                        // extract the equipment name from the line of HTML text
                        name = lines[j].Replace("<tr><td>Name</td><td>", "").Replace("</td></tr>", "").Trim();
                    }
                }
                // output a line into our stringbuilder with the correct format and "xx" for the sort_order
                sbMissing.AppendLine("    " + ("\"" + id + "\"                                                     ").ToString()[..44] + " = {sort_order = xx; purchase_sort_order = xx; script_info = {sortOrder = xx;};}; // " + name);
            }
        }

        Console.WriteLine("Creating f3_missing.txt...");
        // write all the missing records to the missing file
        File.WriteAllText(outputFolder + "\\f3_missing.txt", sbMissing.ToString().Replace("&#39;", "'").Replace("&amp;", "&").Replace("&quot;", "\""));

        // and we're done!
        Console.WriteLine("Complete");
    }
}