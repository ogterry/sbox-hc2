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
          "Position": "368,-336"
        }
      },
      {
        "Id": 26,
        "Type": "resource.ref",
        "Properties": {
          "value": "prefabs/world/stone_circle.prefab",
          "T": "Sandbox.PrefabFile"
        },
        "UserData": {
          "Position": "144,224"
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
          "Position": "1344,272"
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
        "Value": 0.05,
        "DstId": 28,
        "DstName": "probability"
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