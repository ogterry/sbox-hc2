{
  "HeightRange": "4.00,8.00,1",
  "BiomeRange": "0.00,0.15,1",
  "SpawnsInGround": false,
  "Radius": 70,
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
          "value": "models/props/rocks/rock_scatter_01.vmdl",
          "T": "Sandbox.Model"
        },
        "UserData": {
          "Position": "-144,256"
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
          "Position": "432,96"
        }
      },
      {
        "Id": 5,
        "Type": "random.chance",
        "UserData": {
          "Position": "-48,160"
        }
      },
      {
        "Id": 7,
        "Type": "op.conditional",
        "UserData": {
          "Position": "208,240"
        }
      },
      {
        "Id": 8,
        "Type": "resource.ref",
        "Properties": {
          "value": "models/props/rocks/rock_scatter_01.vmdl",
          "T": "Sandbox.Model"
        },
        "UserData": {
          "Position": "-64,480"
        }
      },
      {
        "Id": 9,
        "Type": "random.chance",
        "UserData": {
          "Position": "80,352"
        }
      },
      {
        "Id": 10,
        "Type": "op.conditional",
        "UserData": {
          "Position": "336,432"
        }
      },
      {
        "Id": 12,
        "Type": "resource.ref",
        "Properties": {
          "value": "models/props/rocks/rock_scatter_02.vmdl",
          "T": "Sandbox.Model"
        },
        "UserData": {
          "Position": "144,576"
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
        "SrcId": 7,
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
      },
      {
        "Value": 0.8,
        "DstId": 5,
        "DstName": "probability"
      },
      {
        "SrcId": 2,
        "SrcName": "_result",
        "DstId": 7,
        "DstName": "a"
      },
      {
        "SrcId": 10,
        "SrcName": "_result",
        "DstId": 7,
        "DstName": "b"
      },
      {
        "SrcId": 5,
        "SrcName": "_result",
        "DstId": 7,
        "DstName": "x"
      },
      {
        "Value": 0.7,
        "DstId": 9,
        "DstName": "probability"
      },
      {
        "SrcId": 8,
        "SrcName": "_result",
        "DstId": 10,
        "DstName": "a"
      },
      {
        "SrcId": 12,
        "SrcName": "_result",
        "DstId": 10,
        "DstName": "b"
      },
      {
        "SrcId": 9,
        "SrcName": "_result",
        "DstId": 10,
        "DstName": "x"
      }
    ]
  },
  "__references": [],
  "__version": 0
}