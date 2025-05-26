using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Board : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    [Header("Prefabs & Materiais")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;


    //LOGIC
    private ChessPiece[,] chessPieces;
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private GameObject previousHoveredTile;
    private Vector3 bounds;

    private void Awake() { 
    
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();
        PositionAllPieces();    
    }

    private void Update() {

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            //Get the indexes of the tile i've hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //if we're hovering a tile after not hovering any tiles
            if (currentHover != hitPosition)
            {
                // Se tinha tile anterior, volta pro material e layer originais
                if (previousHoveredTile != null)
                {
                    previousHoveredTile.GetComponent<MeshRenderer>().material = tileMaterial;
                    previousHoveredTile.layer = LayerMask.NameToLayer("Tile");
                }

                // Atualiza o tile atual (material + layer)
                previousHoveredTile = tiles[hitPosition.x, hitPosition.y];
                previousHoveredTile.GetComponent<MeshRenderer>().material = hoverMaterial;
                previousHoveredTile.layer = LayerMask.NameToLayer("Hover");

                currentHover = hitPosition;
            }

        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                if (previousHoveredTile != null)
                {
                    previousHoveredTile.GetComponent<MeshRenderer>().material = tileMaterial;
                    previousHoveredTile.layer = LayerMask.NameToLayer("Tile");
                }


                currentHover = -Vector2Int.one;
                previousHoveredTile = null;
            }

        }
    }

    //GERAÇÃO DO BOARD
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY){
    
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = (GameObject)GenerateSingleTile(tileSize, x, y);
    }
    private object GenerateSingleTile(float tileSize, int x, int y) { 
    
        GameObject tileObject = new GameObject(string.Format("x{0}, Y{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;



        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;


        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");

        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // SPAWN DAS PEÇAS
    private void SpawnAllPieces() {

        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int purpleTeam = 0;
        int orangeTeam = 1;

        //Time Roxo
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Tanque, purpleTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Dano, purpleTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Sup, purpleTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Flanco, purpleTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Rei, purpleTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Flanco, purpleTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Sup, purpleTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Dano, purpleTeam);
        // chessPieces[8, 0] = SpawnSinglePiece(ChessPieceType.Tanque, purpleTeam);

        //Peãos
        //for (int i = 0; i < TILE_COUNT_X; i++)
        //   chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn , purpleTeam);

        //Time Laranja
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Tanque,orangeTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.Rei, orangeTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
        // chessPieces[8, 0] = SpawnSinglePiece(ChessPieceType.Tanque, purpleTeam);

        //Peãos
        //for (int i = 0; i < TILE_COUNT_X; i++)
        //   chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn , purpleTeam);
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team) { 
    
        ChessPiece cp = Instantiate(prefabs[(int)type - 1 ], transform).GetComponent<ChessPiece>();
        
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    //POSIÇÃO DAS PEÇAS
    private void PositionAllPieces(){ 
    
        for (int x = 0; x < TILE_COUNT_X; x++) 
            for(int y = 0; y< TILE_COUNT_Y; y++)
                if (chessPieces[x,y] != null)
                    PositionSinglePiece(x,y, true);

    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].transform.position = GetTileCenter(x, y);
    }

    private Vector3 GetTileCenter(int x , int y) {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }
    

    //OPERAÇÕES
    private Vector2Int LookupTileIndex(GameObject hitInfo) { 
   
        for (int x = 0; x < TILE_COUNT_X; x++)
            for(int y = 0; y< TILE_COUNT_Y; y++)
                if (tiles[x,y] == hitInfo)
                    return new Vector2Int(x,y);

        return -Vector2Int.one; //Invalid
    }

}
