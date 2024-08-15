{
  "HeightRange": "4.00,8.00,1",
  "BiomeRange": "0.00,0.15,1",
  "Radius": 512,
  "Weight": 0.05,
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
        "Type": "input"
      },
      {
        "Id": 2,
        "Type": "resource.ref",
        "Properties": {
          "value": "models/props/bothy/bothy.vmdl",
          "T": "Sandbox.Model"
        },
        "UserData": {
          "Position": "-64,288"
        }
      },
      {
        "Id": 3,
        "Type": "call",
        "Properties": {
          "_name": "SpawnProp",
          "_isStatic": false,
          "_type": "Voxel.Modifications.VoxelWorldGen"
        },
        "UserData": {
          "Position": "336,-0"
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
        "Value": 1,
        "DstId": 3,
        "DstName": "scale"
      }
    ]
  },
  "__references": [],
  "__version": 0
}