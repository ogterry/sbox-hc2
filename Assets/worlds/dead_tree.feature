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
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_name": "SpawnPrefab",
          "_isStatic": false
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
      },
      {
        "Id": 33,
        "Type": "random.chance",
        "UserData": {
          "Position": "576,432"
        }
      },
      {
        "Id": 34,
        "Type": "op.conditional",
        "UserData": {
          "Position": "992,528"
        }
      },
      {
        "Id": 35,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "prefabs/resources/node_tree_3.prefab"
        },
        "UserData": {
          "Position": "496,688"
        }
      },
      {
        "Id": 36,
        "Type": "random.chance",
        "UserData": {
          "Position": "1072,464"
        }
      },
      {
        "Id": 37,
        "Type": "op.conditional",
        "UserData": {
          "Position": "1488,560"
        }
      },
      {
        "Id": 38,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "prefabs/resources/node_tree_4.prefab"
        },
        "UserData": {
          "Position": "992,720"
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
        "SrcId": 37,
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
      },
      {
        "SrcId": 29,
        "SrcName": "_result",
        "DstId": 34,
        "DstName": "a"
      },
      {
        "SrcId": 35,
        "SrcName": "_result",
        "DstId": 34,
        "DstName": "b"
      },
      {
        "SrcId": 33,
        "SrcName": "_result",
        "DstId": 34,
        "DstName": "x"
      },
      {
        "SrcId": 34,
        "SrcName": "_result",
        "DstId": 37,
        "DstName": "a"
      },
      {
        "SrcId": 38,
        "SrcName": "_result",
        "DstId": 37,
        "DstName": "b"
      },
      {
        "SrcId": 36,
        "SrcName": "_result",
        "DstId": 37,
        "DstName": "x"
      }
    ]
  },
  "__references": [],
  "__version": 0
}