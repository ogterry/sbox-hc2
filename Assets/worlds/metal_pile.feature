{
  "HeightRange": "3.00,10.00,1",
  "BiomeRange": "0.20,0.50,1",
  "WantsFlatGround": false,
  "SpawnsInGround": false,
  "MinCount": 0,
  "MaxCount": 0,
  "Radius": 100,
  "Weight": 0.05,
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
          "Position": "384,-16"
        }
      },
      {
        "Id": 27,
        "Type": "call",
        "Properties": {
          "_isStatic": false,
          "_name": "SpawnPrefab",
          "_type": "Voxel.Modifications.VoxelWorldGen"
        },
        "UserData": {
          "Position": "768,-16"
        }
      },
      {
        "Id": 40,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "prefabs/resources/node_metal.prefab"
        },
        "UserData": {
          "Position": "368,80"
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
        "SrcId": 40,
        "SrcName": "_result",
        "DstId": 27,
        "DstName": "prefab"
      }
    ]
  },
  "__references": [],
  "__version": 0
}