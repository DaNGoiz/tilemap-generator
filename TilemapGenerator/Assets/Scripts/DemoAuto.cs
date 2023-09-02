using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Tilemaps;

public class DemoAuto : MonoBehaviour
{
    // get all current level tiles (each level of tiles are named by natural number, i.e. start from 0)
    // get the tilemap of lower layer
    // generate the current layer with rules given

    // - cannot generate out of the lower layer area, unless special position area is defined

    public TextAsset tileRuleTextFile;
    private Dictionary<int, Dictionary<string, Dictionary<int, int>>> ruleDictionary;
    
    public Tile[] currentLayerTiles;
    private Tilemap currentLayerTilemap;

    public Tilemap lowLayerTilemap; // - temp public, should have better solution
    private List<Vector3Int> lowerLayerTileWorldPos = new List<Vector3Int>();
    
    private Dictionary<Vector3Int, int> existingTiles = new Dictionary<Vector3Int, int>();

    void Start()
    {
        currentLayerTilemap = GetComponent<Tilemap>();
        lowLayerTilemap = GetComponent<Tilemap>();

        GetAllLowerLayerTilePos();
        
        ruleDictionary = ReadRule.LoadDictionaryFromTextAsset(tileRuleTextFile);

        // test
        // existingTiles.Add(new Vector3Int(1,0,0), 0);
        // existingTiles.Add(new Vector3Int(0,-1,0), 0);
        // print(ChooseOneTile(new Vector3Int(0,0,0)));
        // currentLayerTilemap.SetTile(new Vector3Int(0,0,0),currentLayerTiles[ChooseOneTile(new Vector3Int(0,0,0))]);
        
        // debug
        // foreach (var tile in ruleDictionary)
        // {
        //     Debug.Log("Key: " + tile.Key);
        //     foreach (var direction in tile.Value)
        //     {
        //         Debug.Log("Direction: " + direction.Key);
        //         foreach (var tileNo in direction.Value)
        //         {
        //             Debug.Log("TileNo: " + tileNo.Key + ", Weight: " + tileNo.Value);
        //         }
        //     }
        // }
    }

    private void GetAllLowerLayerTilePos()
    {
        //get all positions of water tiles
        Vector3Int lowerLayerOrg = lowLayerTilemap.origin;
        Vector3Int lowerLayerSize = lowLayerTilemap.size;
        // print(lowerLayerOrg + " " + lowerLayerSize);
        for(int x = lowerLayerOrg.x; x < lowerLayerSize.x + lowerLayerOrg.x; x++)
        {
            for(int y = lowerLayerOrg.y; y < lowerLayerSize.y + lowerLayerOrg.y; y++)
            {
                lowerLayerTileWorldPos.Add(new Vector3Int(x,y,0));
            }
        }
    }

    void Update()
    {
        StartCoroutine(DemoAutoMap());
    }

    private IEnumerator DemoAutoMap()
    {
        if(!existingTiles.ContainsKey(lowerLayerTileWorldPos[0])){existingTiles.Add(lowerLayerTileWorldPos[0], 46);}
        foreach(Vector3Int vec in lowerLayerTileWorldPos)
        {   
            if(vec == lowerLayerTileWorldPos[0]){continue;}
            int tile = ChooseOneTile(vec);
            if(tile == -1){continue;}
            if(!existingTiles.ContainsKey(vec))
            {
                currentLayerTilemap.SetTile(vec,currentLayerTiles[tile]);
                if(!existingTiles.ContainsKey(vec)){existingTiles.Add(vec, tile);}
                else{existingTiles[vec] = tile;}
            }
            
            yield return new WaitForSeconds(0.01f);
        } 
    }

    private int ChooseOneTile(Vector3Int currentTilePos)
    {
        Dictionary<int, int> tileNumberAndWeight_up = new Dictionary<int, int>();
        Dictionary<int, int> tileNumberAndWeight_down = new Dictionary<int, int>();
        Dictionary<int, int> tileNumberAndWeight_left = new Dictionary<int, int>();
        Dictionary<int, int> tileNumberAndWeight_right = new Dictionary<int, int>();

        tileNumberAndWeight_up = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x, currentTilePos.y + 1, 0), "down");
        tileNumberAndWeight_down = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x, currentTilePos.y - 1, 0), "up");
        tileNumberAndWeight_left = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x - 1, currentTilePos.y, 0), "right");
        tileNumberAndWeight_right = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x + 1, currentTilePos.y, 0), "left");

        // combine the weights together
        // (intersection, if have same tile with different weight, choose the higher one)
        // if unavailable, debug and return a special tile number for emptiness

        List<Dictionary<int, int>> dictionaries = new List<Dictionary<int, int>>
        {
            tileNumberAndWeight_up,
            tileNumberAndWeight_down,
            tileNumberAndWeight_left,
            tileNumberAndWeight_right
        };

        dictionaries = dictionaries.Where(dict => dict != null).ToList();
        var commonKeys = dictionaries
            .SelectMany(dict => dict.Keys)
            .GroupBy(key => key)
            .Where(group => group.Count() == dictionaries.Count)
            .Select(group => group.Key);

        // if (commonKeys.Count() == 0)
        // {
        //     Debug.Log("No common key");
        // }

        Dictionary<int, int> commonTileNumberAndWeight = new Dictionary<int, int>();
        foreach (var dict in dictionaries)
        {
            foreach (int key in commonKeys)
            {
                if (dict.ContainsKey(key))
                {
                    if (!commonTileNumberAndWeight.ContainsKey(key))
                    {
                        commonTileNumberAndWeight.Add(key, dict[key]);
                    }
                    else
                    {
                        if (commonTileNumberAndWeight[key] < dict[key])
                        {
                            commonTileNumberAndWeight[key] = dict[key];
                        }
                    }
                }
            }
        }

        // foreach (var tileNo in commonTileNumberAndWeight)
        // {
        //     Debug.Log("TileNo: " + tileNo.Key + ", Weight: " + tileNo.Value);
        // }

        
        // generate a random number from total weight and refer it to a tile
        int totalWeight = 0;
        foreach (int weight in commonTileNumberAndWeight.Values)
        {
            totalWeight += weight;
        }
        int randomNumber = Random.Range(1, totalWeight + 1);
        int cumulativeWeight = 0;
        int keyTileNumber = -1;
        foreach (var kvp in commonTileNumberAndWeight)
        {
            cumulativeWeight += kvp.Value;
            if (randomNumber <= cumulativeWeight)
            {
                keyTileNumber = kvp.Key;
                // return the tile number i, currentLayerTiles[i] will be the next tile generated
                return keyTileNumber;
            }
        }
        return keyTileNumber;
    }

    private Dictionary<int, int> GetAvailableTileNumberAndWeightFromSurround(Vector3Int surroundTilePos, string targetDirection)
    {
        if(!lowerLayerTileWorldPos.Contains(surroundTilePos)){return null;}
        if(existingTiles.ContainsKey(surroundTilePos))
        {
            Dictionary<int, int> tileNumberAndWeight = new Dictionary<int, int>();
            int surroundTileNo = existingTiles[surroundTilePos];
            if(ruleDictionary[surroundTileNo].ContainsKey(targetDirection))
            {
                foreach (var tileNo in ruleDictionary[surroundTileNo][targetDirection])
                {
                    if (!tileNumberAndWeight.ContainsKey(tileNo.Key))
                    {
                        tileNumberAndWeight.Add(tileNo.Key, tileNo.Value);
                    }
                    else
                    {
                        if (tileNumberAndWeight[tileNo.Key] < tileNo.Value)
                        {
                            tileNumberAndWeight[tileNo.Key] = tileNo.Value;
                        }
                    }
                }
                return tileNumberAndWeight;
            }
            else{return null;}
        }
        else
        {
            // return new Dictionary<int, int>{{ -1, -1 }};
            return null;
        }
    }
}
