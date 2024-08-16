{
  "HeightRange": "40.00,200.00,1",
  "BiomeRange": "0.60,1.00,1",
  "SpawnsInGround": false,
  "Radius": 180,
  "Weight": 1,
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
        "Id": 7,
        "Type": "call",
        "Properties": {
          "_name": "WithZ",
          "_isStatic": false,
          "_type": "Vector3"
        },
        "UserData": {
          "Position": "-0,32"
        }
      },
      {
        "Id": 8,
        "Type": "op.subtract",
        "UserData": {
          "Position": "-368,32"
        }
      },
      {
        "Id": 13,
        "Type": "random.chance",
        "UserData": {
          "Position": "-208,208"
        }
      },
      {
        "Id": 14,
        "Type": "op.conditional",
        "UserData": {
          "Position": "160,288"
        }
      },
      {
        "Id": 32,
        "Type": "input",
        "UserData": {
          "Position": "-544,-160"
        }
      },
      {
        "Id": 33,
        "Type": "property",
        "ParentId": 32,
        "Properties": {
          "_name": "z",
          "_type": "Vector3"
        }
      },
      {
        "Id": 49,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "prefabs/world/spike_mountain.prefab"
        },
        "UserData": {
          "Position": "-288,432"
        }
      },
      {
        "Id": 54,
        "Type": "call",
        "Properties": {
          "_name": "SpawnPrefab",
          "_isStatic": false,
          "_type": "Voxel.Modifications.VoxelWorldGen"
        },
        "UserData": {
          "Position": "400,-128"
        }
      }
    ],
    "Links": [
      {
        "SrcId": 32,
        "SrcName": "origin",
        "DstId": 7,
        "DstName": "_target"
      },
      {
        "SrcId": 8,
        "SrcName": "_result",
        "DstId": 7,
        "DstName": "z"
      },
      {
        "SrcId": 33,
        "SrcName": "_result",
        "DstId": 8,
        "DstName": "a"
      },
      {
        "Value": {
          "$type": "Simple",
          "Type": "System.Single",
          "Value": 200
        },
        "DstId": 8,
        "DstName": "b"
      },
      {
        "Value": 0.1,
        "DstId": 13,
        "DstName": "probability"
      },
      {
        "SrcId": 49,
        "SrcName": "_result",
        "DstId": 14,
        "DstName": "a"
      },
      {
        "SrcId": 49,
        "SrcName": "_result",
        "DstId": 14,
        "DstName": "b"
      },
      {
        "SrcId": 13,
        "SrcName": "_result",
        "DstId": 14,
        "DstName": "x"
      },
      {
        "SrcId": 32,
        "SrcName": "origin",
        "DstId": 33,
        "DstName": "_target"
      },
      {
        "SrcId": 32,
        "SrcName": "_signal",
        "DstId": 54,
        "DstName": "_signal"
      },
      {
        "SrcId": 32,
        "SrcName": "worldGen",
        "DstId": 54,
        "DstName": "_target"
      },
      {
        "SrcId": 7,
        "SrcName": "_result",
        "DstId": 54,
        "DstName": "position"
      },
      {
        "SrcId": 14,
        "SrcName": "_result",
        "DstId": 54,
        "DstName": "prefab"
      }
    ]
  },
  "__references": [],
  "__version": 0
}