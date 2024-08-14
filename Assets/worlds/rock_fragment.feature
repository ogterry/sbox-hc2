{
  "HeightRange": "8.00,96.00,1",
  "BiomeRange": "0.00,1.00,1",
  "Radius": 48,
  "Weight": 0.125,
  "Spawn": {
    "__version": 7,
    "__guid": "156dfd98-7cab-4b4d-ae91-bfaa94d7693b",
    "UserData": {
      "Title": "Spawn",
      "ReferencedComponentTypes": []
    },
    "Variables": [],
    "Nodes": [
      {
        "Id": 0,
        "Type": "input"
      },
      {
        "Id": 2,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.Model",
          "value": "models/props/rocks/rock_fragment.vmdl"
        },
        "UserData": {
          "Position": "-32,128"
        }
      },
      {
        "Id": 3,
        "Type": "call",
        "Properties": {
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false,
          "_name": "SpawnProp"
        },
        "UserData": {
          "Position": "304,112"
        }
      }
    ],
    "Links": [
      {
        "SrcId": 0,
        "SrcName": "_signal",
        "DstId": 3,
        "DstName": "_signal"
      },
      {
        "SrcId": 0,
        "SrcName": "worldGen",
        "DstId": 3,
        "DstName": "_target"
      },
      {
        "SrcId": 2,
        "SrcName": "_result",
        "DstId": 3,
        "DstName": "model"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 3,
        "DstName": "position"
      },
      {
        "Value": 0.5,
        "DstId": 3,
        "DstName": "scale"
      }
    ]
  },
  "__references": [],
  "__version": 0
}