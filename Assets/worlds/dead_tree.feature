{
  "HeightRange": "6.00,10.00,1",
  "BiomeRange": "0.00,0.30,1",
  "Radius": 128,
  "Weight": 1,
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
        "Id": 26,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "prefabs/resources/node_tree.prefab"
        },
        "UserData": {
          "Position": "352,128"
        }
      },
      {
        "Id": 27,
        "Type": "call",
        "Properties": {
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false,
          "_name": "SpawnPrefab"
        },
        "UserData": {
          "Position": "768,-16"
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
      }
    ]
  },
  "__references": [],
  "__version": 0
}