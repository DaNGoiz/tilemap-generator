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

    [Header("Tile Rule")]
    [Tooltip("If write rules for upper layer, the tile number should represent currentLayerTiles of upper layer.")]
    public TextAsset tileRuleTextFile;
    public int[] startingTiles;
    private Dictionary<int, Dictionary<string, Dictionary<int, int>>> ruleDictionary;
    
    [Header("Current Layer Tiles")]
    public Tile[] currentLayerTiles;
    private Tilemap currentLayerTilemap;

    [Header("Lower Layer Tiles")]
    public Tilemap lowLayerTilemap;
    private List<Vector3Int> lowerLayerTileWorldPos = new List<Vector3Int>();
    
    private Dictionary<Vector3Int, int> existingTiles = new Dictionary<Vector3Int, int>();

    [Header("Set Lower Layer Dependence")]
    public bool needToTestLowerLayer = false;
    public GameObject lowerLayerTilemapObject;
    [Tooltip("The name of the current layer tilemap in lower layer rule, should be the same as the name in xxx of above_xxx:1*1")]
    public string currentLayerNameInLowerLayerRule;
    // - still manual. can max num be random generated? or generate due to amount of lower layer tiles?
    // - can be for all layers
    public int currentLayerObjectNumberMaximum = 0;
    private Dictionary<Vector3Int, int> lowerLayerExistingTiles;

    [Header("Set Upper Layer Dependence")]
    public bool needToSetUpperLayer = false;
    public GameObject[] upperLayerTilemapObjects;


    void Awake()
    {
        if(needToSetUpperLayer && upperLayerTilemapObjects != null)
        {
            foreach(var obj in upperLayerTilemapObjects){obj.SetActive(false);}
        }
    }

    void Start()
    {
        gameObject.GetComponent<Tilemap>().ClearAllTiles();

        currentLayerTilemap = GetComponent<Tilemap>();

        GetAllLowerLayerTilePos();
        
        ruleDictionary = ReadRule.LoadDictionaryFromTextAsset(tileRuleTextFile);
        
        if(needToTestLowerLayer)
        {
            lowerLayerExistingTiles = lowerLayerTilemapObject.GetComponent<DemoAuto>().GetExistingTiles();
        }
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

    public Dictionary<Vector3Int, int> GetExistingTiles(){return existingTiles;}

    private bool allTilesCorrect = false;
    private bool hasSetUpperLayer = false;
    void Update()
    {
        if(!allTilesCorrect){StartCoroutine(DemoAutoMap());}
        else if(needToSetUpperLayer)
        {
            if(!hasSetUpperLayer && upperLayerTilemapObjects != null)
            {
                hasSetUpperLayer = true;
                foreach(var obj in upperLayerTilemapObjects){obj.SetActive(true);}
            }
        }
    }

    private bool singleCorountineStarted = false;
    private int currentLayerObjectNumberCount = 0;
    private IEnumerator DemoAutoMap()
    {
        foreach(Vector3Int vec in lowerLayerTileWorldPos)
        {   
            int tile = ChooseOneTile(vec);

            // if is upper layer then can generate many
            if(needToTestLowerLayer && currentLayerObjectNumberCount < currentLayerObjectNumberMaximum)
            {
                tile = ChooseOneTileStartFromNull(vec);
                if(tile != -1){currentLayerObjectNumberCount++;}
            }
            else
            {
                if(!singleCorountineStarted)
                {
                    tile = ChooseOneTileStartFromNull(vec);
                    if(tile != -1){singleCorountineStarted = true;}
                }
            }

            if(tile == -1){continue;} // if no tile available
            if(!existingTiles.ContainsKey(vec)) // if not generated before
            {
                currentLayerTilemap.SetTile(vec,currentLayerTiles[tile]);
                if(!existingTiles.ContainsKey(vec)){existingTiles.Add(vec, tile);}
                else{existingTiles[vec] = tile;}
            }
            
            yield return new WaitForSeconds(0.01f);
        }

        allTilesCorrect = CheckCorrestness();
    }

    private int ChooseOneTileStartFromNull(Vector3Int vec)
    {
        // just a temp function to satisfy the generation from left down corner
        if(!lowerLayerTileWorldPos.Contains(vec)){return -1;}
        if(needToTestLowerLayer)
        {
            if(!lowerLayerExistingTiles.ContainsKey(vec)){return -1;}
        }
        int randomNumber = Random.Range(0, 20); // temp ratio
        if(randomNumber == 0){
            if(startingTiles.Length > 0)
            {
                int startingTile = startingTiles[Random.Range(0, startingTiles.Length)];
                if(needToTestLowerLayer)
                {
                    Dictionary<int, Dictionary<string, Dictionary<int, int>>> lowerLayerRuleDictionary = ReadRule.LoadDictionaryFromTextAsset(
                        lowerLayerTilemapObject.GetComponent<DemoAuto>().tileRuleTextFile,
                        currentLayerNameInLowerLayerRule
                    );
                    int lowerLayerTileNo = lowerLayerExistingTiles[vec];
                    if(lowerLayerRuleDictionary[lowerLayerTileNo].ContainsKey("above"))
                    {
                        if(lowerLayerRuleDictionary[lowerLayerTileNo]["above"].ContainsKey(startingTile))
                        {
                            return startingTile;
                        }
                        else{return -1;}
                    }
                    else{return -1;}
                }
                else{return startingTile;}
            }
            else{return -1;}
        }
        else{return -1;}
    }

    private int ChooseOneTile(Vector3Int currentTilePos)
    {
        Dictionary<int, int> commonTileNumberAndWeight = GetAllAvailableTileNumberAndWeightFromSurround(currentTilePos);
        
        // generate a random number from total weight and refer it to a tile
        int totalWeight = 0;
        foreach (int weight in commonTileNumberAndWeight.Values)
        {
            totalWeight += weight;
        }
        int randomNumber = Random.Range(1, totalWeight + 1);
        int cumulativeWeight = 0;
        int keyTileNumber = -1; // default value
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

    private Dictionary<int, int> GetAllAvailableTileNumberAndWeightFromSurround(Vector3Int currentTilePos)
    {
        Dictionary<int, int> tileNumberAndWeight_up = new Dictionary<int, int>();
        Dictionary<int, int> tileNumberAndWeight_down = new Dictionary<int, int>();
        Dictionary<int, int> tileNumberAndWeight_left = new Dictionary<int, int>();
        Dictionary<int, int> tileNumberAndWeight_right = new Dictionary<int, int>();
        Dictionary<int, int> tileNumberAndWeight_lower = new Dictionary<int, int>();

        tileNumberAndWeight_up = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x, currentTilePos.y + 1, 0), "down");
        tileNumberAndWeight_down = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x, currentTilePos.y - 1, 0), "up");
        tileNumberAndWeight_left = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x - 1, currentTilePos.y, 0), "right");
        tileNumberAndWeight_right = GetAvailableTileNumberAndWeightFromSurround(new Vector3Int(currentTilePos.x + 1, currentTilePos.y, 0), "left");
        if(needToTestLowerLayer)
        {
            if(lowerLayerExistingTiles.ContainsKey(currentTilePos))
            {
                tileNumberAndWeight_lower = GetAvailableTileNumberAndWeightFromLowerLayer(currentTilePos);
            }
        }

        // combine the weights together
        // (intersection, if have same tile with different weight, take the higher one)
        // if unavailable, return -1

        List<Dictionary<int, int>> dictionaries = new List<Dictionary<int, int>>
        {
            tileNumberAndWeight_up,
            tileNumberAndWeight_down,
            tileNumberAndWeight_left,
            tileNumberAndWeight_right,
        };

        // print all things in the dictionaries
        // if(needToTestLowerLayer)
        // {
        //     Debug.Log("===== New =====");
        //     foreach (var dict in dictionaries)
        //     {
        //         if(dict != null)
        //         {
        //             foreach (var tileNo in dict)
        //             {
        //                 Debug.Log("TileNo: " + tileNo.Key + ", Weight: " + tileNo.Value);
        //             }
        //         }
        //     }
        // }

        dictionaries = dictionaries.Where(dict => dict != null).ToList();
        var commonKeys = dictionaries
            .SelectMany(dict => dict.Keys)
            .GroupBy(key => key)
            .Where(group => group.Count() == dictionaries.Count)
            .Select(group => group.Key);

        // if (commonKeys.Count() == 0){Debug.Log("No common key");}

        // only add the common keys that lower layer has
        if(needToTestLowerLayer){
            if(tileNumberAndWeight_lower != null)
            {
                List<int> commonKeysInLowerLayer = new List<int>();
                foreach(var key in commonKeys)
                {
                    if(tileNumberAndWeight_lower.ContainsKey(key))
                    {
                        commonKeysInLowerLayer.Add(key);
                    }
                }
                commonKeys = commonKeysInLowerLayer;
            }
        }

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

        // foreach (var tileNo in commonTileNumberAndWeight){Debug.Log("TileNo: " + tileNo.Key + ", Weight: " + tileNo.Value);}
        if(needToTestLowerLayer)
        {
            // Debug.Log("Current tile: " + currentTilePos);
            // foreach(var tileNo in commonTileNumberAndWeight){Debug.Log("TileNo: " + tileNo.Key + ", Weight: " + tileNo.Value);}
        }
        return commonTileNumberAndWeight;
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
        else{return null;}
    }

    private Dictionary<int, int> GetAvailableTileNumberAndWeightFromLowerLayer(Vector3Int tilePos){
        int lowerLayerTileNo = lowerLayerExistingTiles[tilePos];
        Dictionary<int, Dictionary<string, Dictionary<int, int>>> lowerLayerRuleDictionary =
        ReadRule.LoadDictionaryFromTextAsset(
            lowerLayerTilemapObject.GetComponent<DemoAuto>().tileRuleTextFile,
            currentLayerNameInLowerLayerRule
        );
        if(lowerLayerRuleDictionary[lowerLayerTileNo].ContainsKey("above"))
        {
            return lowerLayerRuleDictionary[lowerLayerTileNo]["above"];
        }
        else{return null;}
    }

    private bool CheckCorrestness()
    {
        int count = 0;
        Dictionary<Vector3Int, int> wrongTiles = new Dictionary<Vector3Int, int>();
        foreach(var tile in existingTiles)
        {
            Dictionary<int, int> commonTileNumberAndWeight = GetAllAvailableTileNumberAndWeightFromSurround(tile.Key);
            if((tile.Value != -1 && !commonTileNumberAndWeight.ContainsKey(tile.Value)) || tile.Value == -1)
            {
                wrongTiles.Add(tile.Key, tile.Value);
                count++;
                
                Debug.Log("Wrong tile at: " + tile.Key);
            }
        }

        foreach(var tile in wrongTiles)
        {
            existingTiles.Remove(tile.Key);
        }

        if(count == 0){return true;}
        else{return false;}
    }
}
