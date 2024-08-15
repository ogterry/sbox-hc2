{
  "HeightRange": "3.00,4.00,1",
  "BiomeRange": "0.00,0.15,1",
  "Radius": 64,
  "Weight": 0.01,
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
        "Id": 5,
        "Type": "call",
        "Properties": {
          "_type": "Voxel.Modifications.VoxelWorldGen",
          "_isStatic": false,
          "_name": "SpawnSpawnPoint"
        },
        "UserData": {
          "Position": "240,32"
        }
      }
    ],
    "Links": [
      {
        "SrcId": 0,
        "SrcName": "_signal",
        "DstId": 5,
        "DstName": "_signal"
      },
      {
        "SrcId": 0,
        "SrcName": "worldGen",
        "DstId": 5,
        "DstName": "_target"
      },
      {
        "SrcId": 0,
        "SrcName": "origin",
        "DstId": 5,
        "DstName": "position"
      }
    ]
  },
  "__references": [],
  "__version": 0
}