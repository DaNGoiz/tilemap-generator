using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DemoAuto : MonoBehaviour
{
    public Tile[] groundTiles;
    private Tilemap groundTilemap;

    public Tilemap waterTilemap; // temp public, should have better solution
    private List<Vector3Int> waterTileWorldPos = new List<Vector3Int>();
    
    void Start()
    {
        groundTilemap = GetComponent<Tilemap>();
        waterTilemap = GetComponent<Tilemap>();

        GetAllWaterTilePos();
        


        //test
        // groundTilemap.SetTile(new Vector3Int(0,0,0),groundTiles[0]);
    }

    private void GetAllWaterTilePos()
    {
        //get all positions of water tiles
        Vector3Int waterOrg = waterTilemap.origin;
        Vector3Int waterSize = waterTilemap.size;
        for(int x = waterOrg.x; x < waterSize.x + waterOrg.x; x++)
        {
            for(int y = waterOrg.y; y < waterSize.y + waterOrg.y; y++)
            {
                waterTileWorldPos.Add(new Vector3Int(x,y,0));
            }
        }
    }

    void Update()
    {
        StartCoroutine(DemoAutoMap());
    }

    private IEnumerator DemoAutoMap()
    {
        foreach(Vector3Int v in waterTileWorldPos)
        {   
            groundTilemap.SetTile(v,groundTiles[0]);
            yield return new WaitForSeconds(0.3f);
        } 
    }
}
