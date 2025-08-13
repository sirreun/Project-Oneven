using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using System.IO;
using UnityEngine.UIElements;
using System.Linq;
using static UnityEngine.Rendering.DebugUI.Table;
using System.Security.Cryptography;
using UnityEditor;
using System.Data;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Tilemaps;

public class ProceduralGeneration
{

    // Generates a grid given a list of rules and a grid size.
    // TODO: make a circleish shaped grid instead of a square. ( maybe not, just have the eedge generate in a circle ish? otherwise some of the calculations for the grid will be messed up?
    // TODO: use type tag constraint instead?
    // TODO: turn some entry logs into for debugging only
    private static bool isDebugging = true;
    private static bool isDebuggingLogic = false;
    private static int tileLimit = 20;
    public static void Generate(List<TagConstraint> ruleset, int height, int width)
    {
        OpenTextLogFile();
        /// Logging Statement
        AddEntryToLog("Generating Terrain with " + width + " columns and " + height + " rows.");

        List<TagConstraint.NameWeightPair>[,] probabilityGrid = InitializeProbabilityGrid(ruleset, width, height);

        string[,] generatedGrid = new string[width,height]; // TODO: unsure if needed
        int totalTiles = height * width;

        /// Pick a random tile in the grid to start with.
        (int, int) currentTile =  (Random.Range(0, width - 1), Random.Range(0, height - 1));
        int completedTileCount = 0;
        string selectedTag = "";
        int numberOfLoops = 0;


        // Loop:
        // Choose the tile given options and weights.
        // Recalculate the options for the rest of the grid as well as the entropy for each tile.
        // Decide the next tile location based on lowest entropy.
        while (completedTileCount < totalTiles)
        {
            numberOfLoops++;

            int column = currentTile.Item1;
            int row = currentTile.Item2;

            /// Logging Statement
            AddEntryToLog("\n-----------------------------------------------");
            AddEntryToLog("\n-----------------------------------------------\nTile selected: (" + column + ", " + row + ").");

            /// The distribution list of values to match the weights from all the options for the current tile.
            /// (ex. a list with all weights of one will give a cut point list of {1, 2, .. n} if there are
            /// n options.)

            //TODO: BUG: cut poitns incorrect, 10 madee for rulset that should have 4
            List<int> cutPoints = new List<int>();

            /// Get the list of weights associated with the current tile
            List<TagConstraint.NameWeightPair> currentOptionList = probabilityGrid[column, row];

            
            string message = "Options for the current tile:\n";
            
            foreach (TagConstraint.NameWeightPair option in currentOptionList)
            {
                message += "\t> " + option.ToString();
                if (cutPoints.Count == 0)
                {
                    cutPoints.Add(option.Weight);
                }
                else
                {
                    cutPoints.Add(cutPoints[cutPoints.Count - 1] + option.Weight);
                }
            }

            if (isDebuggingLogic)
            {
                AddEntryToLog(message);
            }
            
            
            /// Loggin Statement
            string cutPointList = "";
            foreach (int cutPoint in cutPoints) 
            {
                cutPointList += cutPoint.ToString();
                cutPointList += ", ";
            }

            /// Logging Statement
            AddEntryToLog("Cut Points Generated: " + cutPointList);
            /// Logging Statement end

            // Generate a random value in the range of the cut points
            int maxiumumWeight = cutPoints[cutPoints.Count - 1];
            int selectedTile = Random.Range(1, maxiumumWeight);

            /// Logging Statement
            message = "Selected value from 1 to " + maxiumumWeight + " is " + selectedTile;
            AddEntryToLog(message);

            /// Determine what tag goes with the associated tile number based on the cut point distribution.
            /// Cut points are always max values for a tag's range.
            for (int i = 0; i < cutPoints.Count; i++)
            {
                if (selectedTile <= cutPoints[i])
                {
                    /// Use i to find the associated tag
                    selectedTag = currentOptionList[i].Name;
                    break;
                }
            }


            if (selectedTag == "")
            {
                AddEntryToLog("\nERROR: Selected tile not found due to mathematical error.");
                Debug.LogError("ProceduralGeneration.cs: Generate(): selected tile not found in the bounds of the cutPoints, please fix mathematical error in code.");
                return;
            }
            else if (cutPoints[0] == 0)
            {
                AddEntryToLog("\nERROR: Current tile was already selected.");
                Debug.LogError("ProceduralGeneration.cs: Generate(): current tile has already been selected.");
            }

            /// Logging Statement
            AddEntryToLog("Selected " + selectedTag + " for tile (" + column + ", " + row + ").");

            generatedGrid[column, row] = selectedTag;

            // TODO: recalculate the options for the grid as well as the entropy ( need a list for this? or just keep the minimum indicies?)
            /// The value of zero indicates that this tile has already been decided
            List<TagConstraint.NameWeightPair> generatedValue = new List<TagConstraint.NameWeightPair>();
            generatedValue.Add(new TagConstraint.NameWeightPair(selectedTag, 0));
            probabilityGrid[column, row] = generatedValue;

            AddEntryToLog("\nRecalculating probability grid.");
            RecalculateProbabilityGrid(column, row, selectedTag, width, height, ruleset, probabilityGrid);


            // TODO: decide new current tile using entropy calculations, ignore tiles with a 0 in weight and one list item ( no actually these have lowest entropy)
            // NOTE: do we keep a list of tiles already decided? or recalculate each time
            currentTile = CalculateMinimumEntropy(probabilityGrid, width, height);

            completedTileCount++;

            if (isDebugging)
            {
                if (numberOfLoops >= tileLimit) 
                {
                    AddEntryToLog("Met Debugging limit, function stopped.");
                    return;
                }
                AddEntryToLog(PrintProbabilityGrid(probabilityGrid, width, height));
                AddEntryToLog("Loop number " + numberOfLoops + ":");
            }
            AddEntryToLog(completedTileCount + " of " + totalTiles + " completed.");
        }

        /// Logging Statement
        AddEntryToLog("\n FINAL GRID:");

        // TODO: export this to a csv file eventually
        AddEntryToLog(PrintProbabilityGrid(probabilityGrid, width, height));
    }

    // TODO: to sacve tim emight want to only update this on game start and not every generation? (as in the tile lsit, not the init)
    private static List<TagConstraint.NameWeightPair>[,] InitializeProbabilityGrid(List<TagConstraint> ruleset, int totalColumns, int totalRows)
    {
        List<TagConstraint.NameWeightPair>[,] probabilityGrid = new List<TagConstraint.NameWeightPair>[totalRows, totalColumns];
        List<TagConstraint.NameWeightPair> allOptions = new List<TagConstraint.NameWeightPair>();
        int matchingIndex;

        foreach (TagConstraint constraint in ruleset)
        {
            foreach (TagConstraint.NameWeightPair option in constraint.Options)
            {
                TagConstraint.NameWeightPair newOption = new TagConstraint.NameWeightPair(option.Name, option.Weight);
                
                /// If already in allOptions, add the weight to the exisiting weight
                if (ListContainsTagName(allOptions, newOption, out matchingIndex))
                {
                    allOptions[matchingIndex].Weight += newOption.Weight;
                }
                else
                {
                    allOptions.Add(newOption);
                }
            }         
        }

        for (int i = 0; i < totalColumns; i++)
        {
            for (int j = 0; j < totalRows; j++)
            {
                /// For every tile in the grid:
                /// Add all the options
                probabilityGrid[i, j] = new List<TagConstraint.NameWeightPair>();
                probabilityGrid[i, j] = allOptions;
            }
        }


        /// Logging Statement
        string message = "Initialized probability grid to:\n";
        foreach (TagConstraint.NameWeightPair option in allOptions)
        {
            message += "\t> " + option.ToString();
        }
        AddEntryToLog(message);

        
        return probabilityGrid;
    }

    /// <summary>
    /// Determines if a tagConstraint is in list based only on the name. Returns the index if found, otherwise returns the last index in the list.
    /// </summary>
    private static bool ListContainsTagName(List<TagConstraint.NameWeightPair> list, TagConstraint.NameWeightPair tagConstraint, out int index)
    {
        for(int i = 0; i < list.Count; i++)
        {
            TagConstraint.NameWeightPair pair = list[i];
            if (pair.Name == tagConstraint.Name)
            {
                index = i;
                return true;
            }
        }
        index = list.Count - 1;
        return false;
    }

    private static string PrintProbabilityGrid(List<TagConstraint.NameWeightPair>[,] probabilityGrid, int totalColumns, int totalRows)
    {
        string message = "";
        for (int i = 0; i < totalColumns; i++) 
        { 
            for (int j = 0; j < totalRows; j++)
            {
                if (probabilityGrid[i,j].First().Weight != 0)
                {
                    //Not yet selected, is variable
                    message += "~, ";
                }
                else
                {
                    message += probabilityGrid[i, j].First().Name + ", ";
                }
            }
            message += "\n";
        }

        return message;
    }

    /// <summary>
    /// https://en.wikipedia.org/wiki/Entropy_(information_theory)
    /// </summary>
    private static (int, int) CalculateMinimumEntropy(List<TagConstraint.NameWeightPair>[,] probabilityGrid, int width, int height)
    {
        int minimumWidth = 0;
        int minimumHeight = 0;
        double? minimumEntropy = null;

        List<(int, int)> tilesWithOneOption = new List<(int, int)> ();

        for (int i = 0; i < width; i++)
        { 
            for (int j = 0; j < height; j++)
            {
                List<TagConstraint.NameWeightPair> currentTile = probabilityGrid[i, j];
                int numberOfOptions = currentTile.Count;
                // check if value has been assigned yet, if not return, else skip
                if (numberOfOptions == 1)
                {
                    if (currentTile[0].Weight != 0)
                    {
                        tilesWithOneOption.Add((i, j));
                    }
                }
                else if (numberOfOptions == 0)
                {
                    Debug.LogError("No options for this tile, something has gone terribly wrong.");
                }
                else
                { 
                    List<double> surprise = new List<double>();
                    List<double> probabilities = new List<double>();
                    double weightDenominator = 0;

                    foreach (TagConstraint.NameWeightPair option in currentTile)
                    {
                         weightDenominator += option.Weight;
                    }

                    foreach (TagConstraint.NameWeightPair option in currentTile)
                    {
                        try
                        {
                            double probability = option.Weight / weightDenominator;
                            probabilities.Add(probability);
                            surprise.Add(System.Math.Log(probability, numberOfOptions));

                            //AddEntryToLog(" Probability " + probability + " and surpise " + surprise.Last().ToString() + " added.");
                        }
                        catch
                        {
                            Debug.LogError("CalculateMinimumEntropy: Dividing by zero, something has gone terribly wrong.");
                        }   
                    }

                    double entropy = 0;

                    for(int k = 0; k < numberOfOptions; k++)
                    {
                        //AddEntryToLog("Adding " + probabilities[k] * surprise[k] + " to entropy.");
                        entropy += (probabilities[k] * surprise[k]);
                    }

                    entropy *= -1;

                    Debug.Log("Entropy is " + entropy);

                    if (minimumEntropy != null)
                    {
                        if (entropy < minimumEntropy)
                        {
                            minimumEntropy = entropy;
                            minimumWidth = i;
                            minimumHeight = j;
                        }
                    }
                    else
                    {
                        minimumEntropy = entropy;
                        minimumWidth = i;
                        minimumHeight = j;
                    }
                }
            }
        }
        if (tilesWithOneOption.Count > 0)
        {
            /// Return a random tile from the list
            int index = Random.Range(0, tilesWithOneOption.Count);
            return tilesWithOneOption[index];
        }

        (int, int) minimumEntropyCoordinates = (minimumWidth, minimumHeight);

        return minimumEntropyCoordinates;
    }
    
    // TODO: determine a better way to store the probability grid? maybe just pass a pointer?? just unsure if this keeps the modifications of  probability grid/../ i think it does
    private static void RecalculateProbabilityGrid(int column, int row, string selectedTag, int totalColumns, int totalRows, List<TagConstraint> ruleset, List<TagConstraint.NameWeightPair>[,] probabilityGrid)
    {
        int numberOfAdjacentTiles;
        List<(int, int)> coordinates = GetAdjacentCoordinates(column, row, totalColumns, totalRows, probabilityGrid, out numberOfAdjacentTiles);
        List<(int, int)> coordinatesAlreadyVisited = new List<(int, int)>();
        coordinatesAlreadyVisited.Add((column, row));

        if (numberOfAdjacentTiles == 0)
        {
            Debug.LogWarning("ProceduralGeneration.cs: RecalculateProbabilityGrid: no adjacent tiles, either one size grid or something went wrong.");
        }

        int numberOfLoops = 0;


        while (coordinates.Count > 0)
        {
            numberOfLoops++;
            AddEntryToLog("Loop number " + numberOfLoops + ":");
            if (isDebugging)
            {
                if (numberOfLoops >= tileLimit) 
                {
                    AddEntryToLog("\nWARNING: number of loops exceed, returned early\n");
                    return;
                }
            }

            List <(int, int)> coordinatesToRecalculateIntersected = new List<(int, int)> ();

            /// Recalculate surrounding tiles for all adjacent tiles
            foreach ((int, int) coordinate in coordinates)
            {
                if (TileIsSelected(coordinate.Item1, coordinate.Item2, probabilityGrid))
                {
                    /// If the tile has already been selected, it can't be updated.
                    if (isDebuggingLogic)
                    {
                        AddEntryToLog("Tile already selected, can't be updated.");
                    }
                    
                    continue;
                }

                if (isDebuggingLogic)
                {
                    AddEntryToLog("Recalculating options for tile (" + coordinate.Item1 + ", " + coordinate.Item2 + ")...");
                }
                
                List<(int, int)> coordinatesToRecalculate = RecalculateAdjacentTile(coordinate.Item1, coordinate.Item2, totalColumns, totalRows, probabilityGrid, ruleset);
                coordinatesAlreadyVisited.Add(coordinate);

                // Merge coordinates into list to get rid of recurring coordinates

                for (int i = 0; i < coordinatesToRecalculate.Count; i++)
                {
                    (int, int) newCoordinate = coordinatesToRecalculate[i];
                    if (coordinatesToRecalculateIntersected.Contains(newCoordinate))
                    {
                        continue;
                    }
                    else
                    {
                        if (coordinatesAlreadyVisited.Contains(newCoordinate))
                        {
                            continue;
                        }
                        else
                        {
                            coordinatesToRecalculateIntersected.Add(newCoordinate);
                        }
                        
                    }
                }
            }

            if (isDebuggingLogic)
            {
                string message = "\nCoordinates that need to be updated next:\n";
                foreach ((int, int) coordinate in coordinatesToRecalculateIntersected)
                {
                    message += "\t> (" + coordinate.Item1 + ", " + coordinate.Item2 + ")\n";
                }
                AddEntryToLog(message);

            }
            


            AddEntryToLog("\n-----------------------------------------------\n");

            coordinates = coordinatesToRecalculateIntersected;
        }
    }
    
    private static bool TileIsSelected(int column, int row, List<TagConstraint.NameWeightPair>[,] probabilityGrid)
    {
        if (probabilityGrid[column, row].First().Weight == 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns all adjacent coordinates that exist and haven't already been selected
    /// </summary>
    private static List<(int,int)> GetAdjacentCoordinates(int column, int row, int totalColumns, int totalRows, List<TagConstraint.NameWeightPair>[,] probabilityGrid, out int numberOfAdjacentTiles)
    {
        List<(int, int)> output = new List<(int, int)>();
        numberOfAdjacentTiles = 0;

        if ((row - 1) >= 0)
        {
            /// There exists a North Tile
            output.Add((column, row - 1));
            numberOfAdjacentTiles++;
        }

        if ((column + 1) < totalColumns)
        {
            /// There exists a East Tile
            output.Add((column + 1, row));
            numberOfAdjacentTiles++;

        }

        if ((row + 1) < totalRows)
        {
            /// There exists a South Tile
            output.Add((column, row + 1));
            numberOfAdjacentTiles++;
        }

        if ((column - 1) >= 0)
        {
            /// There exists a West Tile
            output.Add((column - 1, row));
            numberOfAdjacentTiles++;
        }

        if (isDebuggingLogic)
        {
            string message = "Tiles adjacent to (" + column + ", " + row + "):\n";
            foreach ((int, int) coordinate in output)
            {
                message += "\t> (" + coordinate.Item1 + ", " + coordinate.Item2 + ")\n";
            }
            AddEntryToLog(message);

        }

        return output;
    }   
    
    private static List<(int, int)> RecalculateAdjacentTile(int column, int row, int totalColumns, int totalRows, List<TagConstraint.NameWeightPair>[,] probabilityGrid, List<TagConstraint> ruleset)
    {
        /// Create list of options that the NESW tiles can have by getting the ruleset for each option in the tile.
        /// This should be an intersection of all the lists for the adjacent NESW tiles.
        List<TagConstraint.NameWeightPair> options = new List<TagConstraint.NameWeightPair>();
        List<(int, int)> output = new List<(int, int)>();

        /// Check for adjacent tiles in NESW order
        int numberOfAdjacentTiles;
        List<(int, int)> adjacentCoordinates = GetAdjacentCoordinates(column, row, totalColumns, totalRows, probabilityGrid, out numberOfAdjacentTiles);

        if (numberOfAdjacentTiles == 0)
        {
            Debug.LogWarning("ProceduralGeneration.cs: RecalculateProbabilityGrid: no adjacent tiles, either one size grid or something went wrong.");
        }

        /// Get the updated options list for the adjacent tiles.
        for(int i = 0; i < adjacentCoordinates.Count; i++ )
        {
            
            /// NOTE: not sure how this will play out when all other options are done... also... what if list empty?
            bool isSelected;
            (int, int) coordinate = adjacentCoordinates[i];

            if (options.Count == 0)
            {
                options = DifferentiateOptionsFromSelectedTiles(probabilityGrid[coordinate.Item1, coordinate.Item2], ruleset, out isSelected);
            }
            else
            {
                options = IntersectOptions(options, DifferentiateOptionsFromSelectedTiles(probabilityGrid[coordinate.Item1, coordinate.Item2], ruleset, out isSelected));
            }


            if (!isSelected)
            {

                output.Add(coordinate);
            }
            else
            {


                AddEntryToLog("Tile (" + coordinate.Item1 + ", " + coordinate.Item2 + ") has already been selected.");
            }

        }

        if (options == probabilityGrid[column, row])
        {
            /// Tile probabilites not updated, end of recursion
            AddEntryToLog("Tile already updated.");
            Debug.Log("Tile not updated");
            return output;

        }

        if (options.Count == 0)
        {
            AddEntryToLog("\nERROR: Unable to find an option for tile, generation failed.");
            Debug.LogError("GenerateTerrain.cs: Unable to find an option for tile, generation failed.");
            // TODO: deal with this in grids to check this is not just a small chance fail rather than
            // a full ruleset issue
        }

        /// Update current tile with options
        probabilityGrid[column, row] = options;

        /// Logging Statement
        string message = "Changed tile (" + column + ", " + row + ") to:\n";
        foreach (TagConstraint.NameWeightPair option in options)
        {
            message += "\t> " + option.Name + " with weight " + option.Weight.ToString() + "\n";
        }
        AddEntryToLog(message);

        

        return output;

    }
    
    /// <summary>
    /// Given a tile's list of options and the ruleset determine if the tile has already been selected.
    /// If selected, return the options for that selected tile type, otherwise return the input.
    /// </summary>
    private static List<TagConstraint.NameWeightPair> DifferentiateOptionsFromSelectedTiles(List<TagConstraint.NameWeightPair> input, List<TagConstraint> ruleset, out bool isSelected)
    {
        isSelected = false;
        List<TagConstraint.NameWeightPair> output = new List<TagConstraint.NameWeightPair>();

        foreach (TagConstraint.NameWeightPair option in input)
        {
            if (option.Weight == 0)
            {
                /// Is a selected Tile
                if (input.Count > 1) 
                {
                    Debug.LogWarning("Selected tile, this list should only have one item: count = " + input.Count + ".\nThis means a zero has been misplaced.");
                }
                /// Doesn't check for later zeros, but in this format has the option
                isSelected = true;

                /// Log
                AddEntryToLog("Current tile has already been selected, use the options for the selected tile type.");
                return output = FindTagConstraint(option.Name, ruleset).Options;   
            }
            else
            {
                AddEntryToLog("Tile has not yet been selected, use the orignal list of options.");
                return input;
            }
        }
        /// Log
        AddEntryToLog("WARNING: No options found from adjacent coordinate.");
        return output;
    }

    private static List<TagConstraint.NameWeightPair> IntersectOptions(List<TagConstraint.NameWeightPair> options, List<TagConstraint.NameWeightPair> newTile)
    {
        List <TagConstraint.NameWeightPair> output = new List<TagConstraint.NameWeightPair>();

        foreach (TagConstraint.NameWeightPair optionOne in options)
        {
            foreach (TagConstraint.NameWeightPair optionTwo in newTile)
            {
                if (optionOne.Name == optionTwo.Name)
                {
                    // TODO:Confirm, but thinking about this this still might be correct?
                    int combinedWeight = optionOne.Weight + optionTwo.Weight;

                    output.Add(new TagConstraint.NameWeightPair(optionOne.Name, combinedWeight));
                }
            }
        }

        List<int> allWeights = new List<int>();
        foreach (TagConstraint.NameWeightPair pair in output)
        {
            allWeights.Add(pair.Weight);
        }

        return output;
    }

    private static TagConstraint FindTagConstraint(string tag, List<TagConstraint> ruleset)
    {
        foreach(TagConstraint tagConstraint in ruleset)
        {
            if (tagConstraint.Name == tag)
            {
                return tagConstraint;
            }
        }

        Debug.LogWarning("ProceduralGeneration.cs: FindTagConstraint: Error, given tag not found in ruleset");
        return null;
    }

    private static void OpenTextLogFile()
    {
        /// Overwrites the previous logfile if it exists
        string path = "Assets/TerrainOutput/logfile.txt";
        StreamWriter writer = new StreamWriter(path, false);
        writer.Close();
    }

    private static void AddEntryToLog(string message)
    {
        string path = "Assets/TerrainOutput/logfile.txt";
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(message);
        writer.Close();

    }
 
}

public class Module
{
    public string name;
    public int id; // Note: unsure if needed
    public string tag;
    public GameObject prefab;

    public Module(string name, string tag, GameObject prefab)
    {
        this.name = name;
        this.id = 0; // TODO: fix this later
        this.tag = tag;
        this.prefab = prefab;
    }
}



