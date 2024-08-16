{
  "HeightRange": "6.00,10.00,1",
  "BiomeRange": "0.00,0.30,1",
  "SpawnsInGround": false,
  "Radius": 500,
  "Weight": 0.1,
  "Spawn": {
    "__version": 7,
    "__guid": "550821d4-b9c4-40ef-998f-7276a82c0f3e",
    "UserData": {
      "Title": "Spawn",
      "ReferencedComponentTypes": []
    },
    "Variables": [],
    "Nodes": [
      {
        "Id": 0,
        "Type": "input",
        "UserData": {
          "Position": "368,-336"
        }
      },
      {
        "Id": 26,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "prefabs/world/stone_circle.prefab"
        },
        "UserData": {
          "Position": "864,400"
        }
      },
      {
        "Id": 27,
        "Type": "call",
        "Properties": {
          "_name": "SpawnPrefab",
          "_isStatic": false,
          "_type": "Voxel.Modifications.VoxelWorldGen"
        },
        "UserData": {
          "Position": "1840,32"
        }
      },
      {
        "Id": 28,
        "Type": "random.chance",
        "UserData": {
          "Position": "240,96"
        }
      }
    ],
    "Links": [
      {
        "SrcId": 0,
        "SrcName": "_signal",
        "DstId": 27,
        "DstName": "_signal"
      },
      {
        "SrcId": 0,
        "SrcName": "worldGen",
        "DstId": 27,
        "DstName": "_target"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 27,
        "DstName": "position"
      },
      {
        "SrcId": 26,
        "SrcName": "_result",
        "DstId": 27,
        "DstName": "prefab"
      },
      {
        "Value": 0.05,
        "DstId": 28,
        "DstName": "probability"
      }
    ]
  },
  "__references": [],
  "__version": 0
}