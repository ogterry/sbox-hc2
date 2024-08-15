{
  "HeightRange": "4.00,32.00,1",
  "BiomeRange": "0.00,0.50,1",
  "Radius": 384,
  "Weight": 0.05,
  "Spawn": {
    "__version": 7,
    "__guid": "b00142c2-c109-41af-8b85-df3c272a4bd8",
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
          "Position": "336,-144"
        }
      },
      {
        "Id": 1,
        "Type": "random.chance",
        "UserData": {
          "Position": "112,112"
        }
      },
      {
        "Id": 5,
        "Type": "random.chance",
        "UserData": {
          "Position": "-112,304"
        }
      },
      {
        "Id": 6,
        "Type": "op.conditional",
        "UserData": {
          "Position": "384,176"
        }
      },
      {
        "Id": 7,
        "Type": "op.conditional",
        "UserData": {
          "Position": "192,352"
        }
      },
      {
        "Id": 9,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.Model",
          "value": "models/props/standing_stones/standing_stone_1.vmdl"
        },
        "UserData": {
          "Position": "128,192"
        }
      },
      {
        "Id": 11,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.Model",
          "value": "models/props/standing_stones/standing_stone_2.vmdl"
        },
        "UserData": {
          "Position": "-112,432"
        }
      },
      {
        "Id": 14,
        "Type": "call",
        "Properties": {
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false,
          "_name": "SpawnProp"
        },
        "UserData": {
          "Position": "864,-112"
        }
      },
      {
        "Id": 20,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.Model",
          "value": "models/props/standing_stones/standing_stone_3.vmdl"
        },
        "UserData": {
          "Position": "-96,576"
        }
      },
      {
        "Id": 21,
        "Type": "resource.ref",
        "Properties": {
          "T": "Sandbox.Model",
          "value": "models/props/standing_stones/standing_stone_4.vmdl"
        },
        "UserData": {
          "Position": "224,608"
        }
      },
      {
        "Id": 22,
        "Type": "random.chance",
        "UserData": {
          "Position": "384,336"
        }
      },
      {
        "Id": 23,
        "Type": "op.conditional",
        "UserData": {
          "Position": "656,400"
        }
      },
      {
        "Id": 35,
        "Type": "property",
        "ParentId": 0,
        "Properties": {
          "_type": "Vector3",
          "_name": "z"
        }
      },
      {
        "Id": 36,
        "Type": "call",
        "Properties": {
          "_type": "Vector3",
          "_isStatic": false,
          "_name": "WithZ"
        },
        "UserData": {
          "Position": "576,-16"
        }
      },
      {
        "Id": 37,
        "Type": "op.subtract",
        "UserData": {
          "Position": "400,32"
        }
      }
    ],
    "Links": [
      {
        "Value": 0.33333334,
        "DstId": 1,
        "DstName": "probability"
      },
      {
        "SrcId": 9,
        "SrcName": "_result",
        "DstId": 6,
        "DstName": "a"
      },
      {
        "SrcId": 7,
        "SrcName": "_result",
        "DstId": 6,
        "DstName": "b"
      },
      {
        "SrcId": 1,
        "SrcName": "_result",
        "DstId": 6,
        "DstName": "x"
      },
      {
        "SrcId": 11,
        "SrcName": "_result",
        "DstId": 7,
        "DstName": "a"
      },
      {
        "SrcId": 20,
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
        "SrcId": 0,
        "SrcName": "_signal",
        "DstId": 14,
        "DstName": "_signal"
      },
      {
        "SrcId": 0,
        "SrcName": "worldGen",
        "DstId": 14,
        "DstName": "_target"
      },
      {
        "SrcId": 23,
        "SrcName": "_result",
        "DstId": 14,
        "DstName": "model"
      },
      {
        "SrcId": 36,
        "SrcName": "_result",
        "DstId": 14,
        "DstName": "position"
      },
      {
        "Value": 1,
        "DstId": 14,
        "DstName": "scale"
      },
      {
        "Value": 0.33333334,
        "DstId": 22,
        "DstName": "probability"
      },
      {
        "SrcId": 21,
        "SrcName": "_result",
        "DstId": 23,
        "DstName": "a"
      },
      {
        "SrcId": 6,
        "SrcName": "_result",
        "DstId": 23,
        "DstName": "b"
      },
      {
        "SrcId": 22,
        "SrcName": "_result",
        "DstId": 23,
        "DstName": "x"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 35,
        "DstName": "_target"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 36,
        "DstName": "_target"
      },
      {
        "SrcId": 37,
        "SrcName": "_result",
        "DstId": 36,
        "DstName": "z"
      },
      {
        "SrcId": 35,
        "SrcName": "_result",
        "DstId": 37,
        "DstName": "a"
      },
      {
        "Value": {
          "$type": "Simple",
          "Type": "System.Single",
          "Value": 32
        },
        "DstId": 37,
        "DstName": "b"
      }
    ]
  },
  "__references": [],
  "__version": 0
}