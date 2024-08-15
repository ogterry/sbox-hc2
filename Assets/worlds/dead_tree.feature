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
          "value": "prefabs/resources/node_tree.prefab",
          "T": "Sandbox.PrefabFile"
        },
        "UserData": {
          "Position": "128,176"
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
          "Position": "768,-16"
        }
      },
      {
        "Id": 28,
        "Type": "random.chance",
        "UserData": {
          "Position": "240,96"
        }
      },
      {
        "Id": 29,
        "Type": "op.conditional",
        "UserData": {
          "Position": "656,192"
        }
      },
      {
        "Id": 31,
        "Type": "resource.ref",
        "Properties": {
          "value": "prefabs/resources/node_tree_2.prefab",
          "T": "Sandbox.PrefabFile"
        },
        "UserData": {
          "Position": "160,352"
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
        "SrcId": 29,
        "SrcName": "_result",
        "DstId": 27,
        "DstName": "prefab"
      },
      {
        "SrcId": 26,
        "SrcName": "_result",
        "DstId": 29,
        "DstName": "a"
      },
      {
        "SrcId": 31,
        "SrcName": "_result",
        "DstId": 29,
        "DstName": "b"
      },
      {
        "SrcId": 28,
        "SrcName": "_result",
        "DstId": 29,
        "DstName": "x"
      }
    ]
  },
  "__references": [],
  "__version": 0
}