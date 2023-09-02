using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadRule : MonoBehaviour
{
    // Given a test asset, return a rule dictionary
    // structure: currentTileNo -> direction -> canConstructTileNo -> weight
    public static Dictionary<int, Dictionary<string, Dictionary<int, int>>> LoadDictionaryFromTextAsset(TextAsset tileRuleTextFile)
    {
        Dictionary<int, Dictionary<string, Dictionary<int, int>>> ruleDictionary = new Dictionary<int, Dictionary<string, Dictionary<int, int>>>();
        
        if (tileRuleTextFile != null)
        {
            string fileContent = tileRuleTextFile.text;
            string[] lines = fileContent.Split('\n');

            int currentTileNo = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
                if (lines[i].StartsWith("["))
                {
                    currentTileNo = int.Parse(lines[i].Substring(1, lines[i].Length - 2)); // maybe need to -1
                    ruleDictionary.Add(currentTileNo, new Dictionary<string, Dictionary<int, int>>());
                }
                else if (lines[i].StartsWith("up") || lines[i].StartsWith("down") || lines[i].StartsWith("left") || lines[i].StartsWith("right"))
                {
                    string[] directoinTiles = lines[i].Split(':');
                    string[] tiles = directoinTiles[1].Split(',');
                    for (int j = 0; j < tiles.Length; j++)
                    {
                        tiles[j] = tiles[j].Trim();
                        string[] tileNoAndWeight = tiles[j].Split('*');
                        int tileNo = int.Parse(tileNoAndWeight[0]);
                        int weight = int.Parse(tileNoAndWeight[1]);
                        
                        if (!ruleDictionary[currentTileNo].ContainsKey(directoinTiles[0])){ruleDictionary[currentTileNo].Add(directoinTiles[0], new Dictionary<int, int>());}
                        if (!ruleDictionary[currentTileNo][directoinTiles[0]].ContainsKey(tileNo)){ruleDictionary[currentTileNo][directoinTiles[0]].Add(tileNo, weight);}
                    }
                }
            }
        }

        return ruleDictionary;
    }
}