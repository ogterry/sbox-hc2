{
  "HeightRange": "32.00,128.00,1",
  "BiomeRange": "0.20,1.00,1",
  "SpawnsInGround": true,
  "Radius": 512,
  "Weight": 0.05,
  "Spawn": {
    "__version": 7,
    "__guid": "a90d1a9e-2440-4e18-b755-2ec381b53967",
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
        "Id": 1,
        "Type": "call",
        "Properties": {
          "_isStatic": false,
          "_name": "SpawnOreSeam",
          "_type": "Voxel.Modifications.VoxelWorldGen"
        },
        "UserData": {
          "Position": "272,0"
        }
      },
      {
        "Id": 3,
        "Type": "resource.ref",
        "Properties": {
          "T": "Voxel.Block",
          "value": "blocks/iron_ore.block"
        },
        "UserData": {
          "Position": "-16,96"
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
        "SrcId": 3,
        "SrcName": "_result",
        "DstId": 1,
        "DstName": "block"
      },
      {
        "Value": "8.00,16.00,1",
        "DstId": 1,
        "DstName": "depthRange"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 1,
        "DstName": "position"
      },
      {
        "Value": "4.00,32.00,1",
        "DstId": 1,
        "DstName": "sizeRange"
      }
    ]
  },
  "__references": [],
  "__version": 0
}