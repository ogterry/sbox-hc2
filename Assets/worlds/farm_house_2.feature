{
  "HeightRange": "6.00,10.00,1",
  "BiomeRange": "0.00,0.30,1",
  "WantsFlatGround": false,
  "SpawnsInGround": false,
  "MinCount": 0,
  "MaxCount": 0,
  "Radius": 256,
  "Weight": 0.02,
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
          "value": "scenes/prefabs/farmhouse_02.prefab"
        },
        "UserData": {
          "Position": "656,-64"
        }
      },
      {
        "Id": 27,
        "Type": "call",
        "Properties": {
          "_isStatic": false,
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_name": "SpawnPrefab"
        },
        "UserData": {
          "Position": "1184,-352"
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