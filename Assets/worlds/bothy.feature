{
  "HeightRange": "6.00,13.00,1",
  "BiomeRange": "0.00,0.15,1",
  "WantsFlatGround": true,
  "SpawnsInGround": false,
  "MinCount": 0,
  "MaxCount": 0,
  "Radius": 256,
  "Weight": 0.07,
  "Spawn": {
    "__version": 7,
    "__guid": "ecd9ebea-0048-4250-bb28-0dd1f908d26e",
    "UserData": {
      "Title": "Spawn",
      "ReferencedComponentTypes": []
    },
    "Variables": [
      {
        "Name": "height_offset_boffy",
        "Type": "System.Object"
      }
    ],
    "Nodes": [
      {
        "Id": 0,
        "Type": "input",
        "UserData": {
          "Position": "48,-352"
        }
      },
      {
        "Id": 10,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "scenes/prefabs/bothy_01.prefab"
        },
        "UserData": {
          "Position": "96,-176"
        }
      },
      {
        "Id": 11,
        "Type": "call",
        "Properties": {
          "_name": "SpawnPrefab",
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false
        },
        "UserData": {
          "Position": "416,-320"
        }
      }
    ],
    "Links": [
      {
        "SrcId": 0,
        "SrcName": "_signal",
        "DstId": 11,
        "DstName": "_signal"
      },
      {
        "SrcId": 0,
        "SrcName": "worldGen",
        "DstId": 11,
        "DstName": "_target"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 11,
        "DstName": "position"
      },
      {
        "SrcId": 10,
        "SrcName": "_result",
        "DstId": 11,
        "DstName": "prefab"
      }
    ]
  },
  "__references": [],
  "__version": 0
}