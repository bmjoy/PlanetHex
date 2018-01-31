﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PlanetGeneration
{


    public class PlanetGenerator : MonoBehaviour
    {
        public int Radius;
        public int TerrainHeight;
        public int NumDivisions;
        public GameObject player;
        public Material[] materials;


        private SimplexNoiseGenerator NoiseGenerator = new SimplexNoiseGenerator();
        private Hexasphere Hexasphere;
        private Dictionary<Region, GameObject> loadedRegions;

        void Start()
        {
            Hexasphere = new Hexasphere(Radius, NumDivisions);
            loadedRegions = new Dictionary<Region, GameObject>();
        }

        void Update()
        {
            var pos = (player.transform.position - this.transform.position).normalized * Radius;

            foreach (var region in Hexasphere.Regions)
            {
                if ((region.Center - pos).magnitude < Radius)
                {
                    if (!loadedRegions.ContainsKey(region))
                    {
                        LoadRegion(region);
                    }
                } else
                {
                    if (loadedRegions.ContainsKey(region))
                    {
                        UnloadRegion(region);
                    }
                }
            }
        }

        void LoadRegion(Region region)
        {
            var regionGameObject = new GameObject("region_" + region.ID);
            regionGameObject.transform.parent = this.transform;
            foreach (Tile tile in region.GetTiles())
            {
                var height = Mathf.Max(TerrainHeight - NoiseGenerator.getDensity(tile.Center.AsVector(), 0, TerrainHeight, octaves: 3, persistence: 0.85f), 0);
                for (var y = 0; y <= height; y++)
                {
                    var block = CreateBlock(tile, y);
                    block.transform.parent = regionGameObject.transform;
                }
            }
            loadedRegions.Add(region, regionGameObject);
        }

        void UnloadRegion(Region region)
        {
            if (loadedRegions.ContainsKey(region))
            {
                Destroy(loadedRegions[region]);
                loadedRegions.Remove(region);
            }
        }

        GameObject CreateBlock(Tile tile, int height)
        {
            var block = new GameObject("Block");
            block.AddComponent<MeshFilter>();
            block.AddComponent<MeshRenderer>();
            block.AddComponent<MeshCollider>();

            var mesh = CreateMesh(tile, height);
            block.GetComponent<MeshFilter>().mesh = mesh;

            var meshCollider = block.GetComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            var meshRenderer = block.GetComponent<MeshRenderer>();
            if (tile.Region != -1)
            {
                meshRenderer.material = materials[tile.Region % materials.Length];
            }

            block.transform.position = tile.Center.Project(Radius + height).AsVector();
            return block;
        }

        Mesh CreateMesh(Tile tile, int height = 0)
        {
            var count = tile.Boundary.Count;
            var vertices = new Vector3[count * 2];
            var tris = new List<int>();
            var pos = tile.Center.Project(Radius + height).AsVector();
            for (var i = 0; i < tile.Boundary.Count; i++)
            {
                vertices[i] = tile.Boundary[i].Project(Radius + height).AsVector() - pos;
                vertices[count + i] = tile.Boundary[i].Project(Radius + height + 1).AsVector() - pos;

                tris.Add(i); tris.Add((i + 1) % count); tris.Add(i + count);
                tris.Add((i + 1) % count); tris.Add((i + 1) % count + count); tris.Add(i + count);
            }

            tris.Add(0); tris.Add(2); tris.Add(1);
            tris.Add(count); tris.Add(count + 1); tris.Add(count + 2);
            tris.Add(2); tris.Add(4); tris.Add(3);
            tris.Add(count + 2); tris.Add(count + 3); tris.Add(count + 4);
            tris.Add(4); tris.Add(2); tris.Add(0);
            tris.Add(count + 4); tris.Add(count); tris.Add(count + 2);

            if (tile.Boundary.Count > 5)
            {
                tris.Add(4); tris.Add(0); tris.Add(5);
                tris.Add(count + 4); tris.Add(count + 5); tris.Add(count);
            }

            var mesh = new Mesh()
            {
                vertices = vertices,
                triangles = tris.ToArray()
            };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}

