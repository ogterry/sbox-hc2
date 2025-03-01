{
  "HeightRange": "2.00,50.00,1",
  "BiomeRange": "0.05,0.40,1",
  "SpawnsInGround": false,
  "Radius": 125,
  "Weight": 2,
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
          "Position": "1120,-80"
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
          "Position": "1840,32"
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
          "value": "prefabs/resources/node_tree_3.prefab",
          "T": "Sandbox.PrefabFile"
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
          "Position": "1360,416"
        }
      },
      {
        "Id": 38,
        "Type": "resource.ref",
        "Properties": {
          "value": "prefabs/resources/node_tree_5.prefab",
          "T": "Sandbox.PrefabFile"
        },
        "UserData": {
          "Position": "992,720"
        }
      },
      {
        "Id": 40,
        "Type": "random.chance",
        "UserData": {
          "Position": "1264,288"
        }
      },
      {
        "Id": 41,
        "Type": "op.conditional",
        "UserData": {
          "Position": "1568,320"
        }
      },
      {
        "Id": 42,
        "Type": "resource.ref",
        "Properties": {
          "value": "prefabs/resources/node_tree_4.prefab",
          "T": "Sandbox.PrefabFile"
        },
        "UserData": {
          "Position": "1168,368"
        }
      },
      {
        "Id": 44,
        "Type": "property",
        "ParentId": 0,
        "Properties": {
          "_name": "z",
          "_type": "Vector3"
        }
      },
      {
        "Id": 45,
        "Type": "op.subtract",
        "UserData": {
          "Position": "1328,96"
        }
      },
      {
        "Id": 46,
        "Type": "call",
        "Properties": {
          "_name": "WithZ",
          "_isStatic": false,
          "_type": "Vector3"
        },
        "UserData": {
          "Position": "1536,96"
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
        "SrcId": 46,
        "SrcName": "_result",
        "DstId": 27,
        "DstName": "position"
      },
      {
        "SrcId": 41,
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
      },
      {
        "Value": 0.4,
        "DstId": 40,
        "DstName": "probability"
      },
      {
        "SrcId": 42,
        "SrcName": "_result",
        "DstId": 41,
        "DstName": "a"
      },
      {
        "SrcId": 37,
        "SrcName": "_result",
        "DstId": 41,
        "DstName": "b"
      },
      {
        "SrcId": 40,
        "SrcName": "_result",
        "DstId": 41,
        "DstName": "x"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 44,
        "DstName": "_target"
      },
      {
        "SrcId": 44,
        "SrcName": "_result",
        "DstId": 45,
        "DstName": "a"
      },
      {
        "Value": {
          "$type": "Simple",
          "Type": "System.Single",
          "Value": 10
        },
        "DstId": 45,
        "DstName": "b"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 46,
        "DstName": "_target"
      },
      {
        "SrcId": 45,
        "SrcName": "_result",
        "DstId": 46,
        "DstName": "z"
      }
    ]
  },
  "__references": [],
  "__version": 0
}