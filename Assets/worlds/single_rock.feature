{
  "HeightRange": "16.00,64.00,1",
  "BiomeRange": "0.30,1.00,1",
  "Radius": 256,
  "Weight": 0.25,
  "Spawn": {
    "__version": 7,
    "__guid": "196d0739-281f-4a1d-a51f-8f2ee510df78",
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
          "Position": "-16,-64"
        }
      },
      {
        "Id": 1,
        "Type": "call",
        "Properties": {
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false,
          "_name": "SpawnPrefab"
        },
        "UserData": {
          "Position": "288,32"
        }
      },
      {
        "Id": 3,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.PrefabFile",
          "value": "prefabs/resources/node_rock.prefab"
        },
        "UserData": {
          "Position": "-16,144"
        }
      },
      {
        "Id": 6,
        "Type": "call",
        "Properties": {
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false,
          "_name": "SpawnFeatures"
        },
        "UserData": {
          "Position": "624,-128"
        }
      },
      {
        "Id": 16,
        "Type": "resource.ref",
        "Properties": {
          "T": "Voxel.Modifications.WorldGenFeature",
          "value": "worlds/rock_fragment.feature"
        },
        "UserData": {
          "Position": "176,-192"
        }
      }
    ],
    "Links": [
      {
        "SrcId": 0,
        "SrcName": "_signal",
        "DstId": 1,
        "DstName": "_signal"
      },
      {
        "SrcId": 0,
        "SrcName": "worldGen",
        "DstId": 1,
        "DstName": "_target"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 1,
        "DstName": "position"
      },
      {
        "SrcId": 3,
        "SrcName": "_result",
        "DstId": 1,
        "DstName": "prefab"
      },
      {
        "SrcId": 1,
        "SrcName": "_signal",
        "DstId": 6,
        "DstName": "_signal"
      },
      {
        "SrcId": 0,
        "SrcName": "worldGen",
        "DstId": 6,
        "DstName": "_target"
      },
      {
        "SrcId": 16,
        "SrcName": "_result",
        "DstId": 6,
        "DstName": "features",
        "DstIndex": 0
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 6,
        "DstName": "origin"
      },
      {
        "Value": 128,
        "DstId": 6,
        "DstName": "radius"
      }
    ]
  },
  "__references": [],
  "__version": 0
}