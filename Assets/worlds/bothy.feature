{
  "HeightRange": "6.00,13.00,1",
  "BiomeRange": "0.00,0.15,1",
  "SpawnsInGround": false,
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
        "Type": "input",
        "UserData": {
          "Position": "-288,-224"
        }
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
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false,
          "_name": "SpawnProp"
        },
        "UserData": {
          "Position": "336,-0"
        }
      },
      {
        "Id": 5,
        "Type": "property",
        "ParentId": 0,
        "Properties": {
          "_type": "Vector3",
          "_name": "z"
        }
      },
      {
        "Id": 6,
        "Type": "op.subtract",
        "UserData": {
          "Position": "-224,0"
        }
      },
      {
        "Id": 7,
        "Type": "call",
        "Properties": {
          "_type": "Vector3",
          "_isStatic": false,
          "_name": "WithZ"
        },
        "UserData": {
          "Position": "-32,-32"
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
        "SrcId": 7,
        "SrcName": "_result",
        "DstId": 3,
        "DstName": "position"
      },
      {
        "Value": 1,
        "DstId": 3,
        "DstName": "scale"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 5,
        "DstName": "_target"
      },
      {
        "SrcId": 5,
        "SrcName": "_result",
        "DstId": 6,
        "DstName": "a"
      },
      {
        "Value": {
          "$type": "Simple",
          "Type": "System.Single",
          "Value": 34
        },
        "DstId": 6,
        "DstName": "b"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 7,
        "DstName": "_target"
      },
      {
        "SrcId": 6,
        "SrcName": "_result",
        "DstId": 7,
        "DstName": "z"
      }
    ]
  },
  "__references": [],
  "__version": 0
}