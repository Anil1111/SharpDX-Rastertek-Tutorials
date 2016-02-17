﻿using DSharpDXRastertek.TutTerr06.System;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DSharpDXRastertek.TutTerr06.Graphics.Models
{
    public class DTerrain                   // 442 kubwa
    {
        // Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct DVertexType
        {
            internal Vector3 position;
            internal Vector2 texture;
            internal Vector3 normal;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DHeightMapType
        {
            public float x, y, z;
            public float tu, tv;
            public float nx, ny, nz;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DVectorTypeShareNormal
        {
            public float x, y, z;
        }

        // Variables
        private int m_TerrainWidth, m_TerrainHeight;
        private int TEXTURE_REPEAT = 8;

        // Properties
        public int VertexCount { get; set; }
        public List<DHeightMapType> HeightMap = new List<DHeightMapType>();
        public DTexture Texture { get; set; }
        public DVertexType[] Verticies { get; set; }

        // Constructor
        public DTerrain() { }

        // Methods.
        public bool Initialize(SharpDX.Direct3D11.Device device, string heightMapFileName, string textureFileName)
        {
            // Load in the height map for the terrain.
            if (!LoadHeightMap(heightMapFileName))
                return false;

            // Normalize the height of the height map.
            NormalizeHeightMap();

            // Calculate the normals for the terrain data.
            if (!CalculateNormals())
                return false;

            // Calculate the texture coordinates.
            CalculateTextureCoordinates();

            // Load the texture.
            if (!LoadTexture(device, textureFileName))
                return false;

            // Initialize the vertex and index buffer that hold the geometry for the terrain.
            if (!InitializeBuffers(device))
                return false;
             
            return true;
        }
        private bool LoadTexture(SharpDX.Direct3D11.Device device, string textureFileName)
        {
            textureFileName = DSystemConfiguration.DataFilePath + textureFileName;

            // Create the texture object.
            Texture = new DTexture();

            // Initialize the texture object.
            if (!Texture.Initialize(device, textureFileName))
                return false;

            return true;
        }
        private void CalculateTextureCoordinates()
        {
            // Calculate how much to increment the texture coordinates by.
            float incrementValue = (float)TEXTURE_REPEAT / (float)m_TerrainWidth;

            // Calculate how many times to repeat the texture.
            int incrementCount = m_TerrainWidth / TEXTURE_REPEAT;

            // Initialize the tu and tv coordinate values.
            float tuCoordinate = 0.0f;
            float tvCoordinate = 1.0f;

            // Initialize the tu and tv coordinate indexes.
            int tuCount = 0;
            int tvCount = 0;

            // Loop through the entire height map and calculate the tu and tv texture coordinates for each vertex.
            for (int j = 0; j < m_TerrainHeight; j++)
            {
                for (int i = 0; i < m_TerrainWidth; i++)
                {
                    // Store the texture coordinate in the height map.
                    var tempHeightMap = HeightMap[(m_TerrainHeight * j) + i];
                    tempHeightMap.tu = tuCoordinate;
                    tempHeightMap.tv = tvCoordinate;
                    HeightMap[(m_TerrainHeight * j) + i] = tempHeightMap;

                    // Increment the tu texture coordinate by the increment value and increment the index by one.
                    tuCoordinate += incrementValue;
                    tuCount++;

                    // Check if at the far right end of the texture and if so then start at the beginning again.
                    if (tuCount == incrementCount)
                    {
                        tuCoordinate = 0.0f;
                        tuCount = 0;
                    }
                }

                // Increment the tv texture coordinate by the increment value and increment the index by one.
                tvCoordinate -= incrementValue;
                tvCount++;

                // Check if at the top of the texture and if so then start at the bottom again.
                if (tvCount == incrementCount)
                {
                    tvCoordinate = 1.0f;
                    tvCount = 0;
                }
            }
        }
        private bool CalculateNormals()
        {
            // Create a temporary array to hold the un-normalized normal vectors.
            int index;
            float length;
            Vector3 vertex1, vertex2, vertex3, vector1, vector2, sum;
            DVectorTypeShareNormal[] normals = new DVectorTypeShareNormal[(m_TerrainHeight - 1) * (m_TerrainWidth - 1)];
            
            // Go through all the faces in the mesh and calculate their normals.
            for (int j = 0; j < (m_TerrainHeight - 1); j++)
            {
                for (int i = 0; i < (m_TerrainWidth - 1); i++)
                {
                    int index1 = (j * m_TerrainHeight) + i;
                    int index2 = (j * m_TerrainHeight) + (i + 1);
                    int index3 = ((j + 1) * m_TerrainHeight) + i;

                    // Get three vertices from the face.
                    vertex1.X = HeightMap[index1].x;
                    vertex1.Y = HeightMap[index1].y;
                    vertex1.Z = HeightMap[index1].z;
                    vertex2.X = HeightMap[index2].x;
                    vertex2.Y = HeightMap[index2].y;
                    vertex2.Z = HeightMap[index2].z;
                    vertex3.X = HeightMap[index3].x;
                    vertex3.Y = HeightMap[index3].y;
                    vertex3.Z = HeightMap[index3].z;

                    // Calculate the two vectors for this face.
                    vector1 = vertex1 - vertex3;
                    vector2 = vertex3 - vertex2;
                    index = (j * (m_TerrainHeight - 1)) + i;

                    // Calculate the cross product of those two vectors to get the un-normalized value for this face normal.
                    Vector3 vecTestCrossProduct = Vector3.Cross(vector1, vector2);
                    normals[index].x = vecTestCrossProduct.X;
                    normals[index].y = vecTestCrossProduct.Y;
                    normals[index].z = vecTestCrossProduct.Z;
                }
            }

            // Now go through all the vertices and take an average of each face normal 	
            // that the vertex touches to get the averaged normal for that vertex.
            for (int j = 0; j < m_TerrainHeight; j++)
            {
                for (int i = 0; i < m_TerrainWidth; i++)
                {
                    // Initialize the sum.
                    sum = Vector3.Zero;

                    // Initialize the count.
                    int count = 9;

                    // Bottom left face.
                    if (((i - 1) >= 0) && ((j - 1) >= 0))
                    {
                        index = ((j - 1) * (m_TerrainHeight - 1)) + (i - 1);

                        sum[0] += normals[index].x;
                        sum[1] += normals[index].y;
                        sum[2] += normals[index].z;
                        count++;
                    }
                    // Bottom right face.
                    if ((i < (m_TerrainWidth - 1)) && ((j - 1) >= 0))
                    {
                        index = ((j - 1) * (m_TerrainHeight - 1)) + i;

                        sum[0] += normals[index].x;
                        sum[1] += normals[index].y;
                        sum[2] += normals[index].z;
                        count++;
                    }
                    // Upper left face.
                    if (((i - 1) >= 0) && (j < (m_TerrainHeight - 1)))
                    {
                        index = (j * (m_TerrainHeight - 1)) + (i - 1);

                        sum[0] += normals[index].x;
                        sum[1] += normals[index].y;
                        sum[2] += normals[index].z;
                        count++;
                    }
                    // Upper right face.
                    if ((i < (m_TerrainWidth - 1)) && (j < (m_TerrainHeight - 1)))
                    {
                        index = (j * (m_TerrainHeight - 1)) + i;

                        sum.X += normals[index].x;
                        sum.Y += normals[index].y;
                        sum.Z += normals[index].z;
                        count++;
                    }

                    // Take the average of the faces touching this vertex.
                    sum.X = (sum.X / (float)count);
                    sum.Y = (sum.Y / (float)count);
                    sum.Z = (sum.Z / (float)count);

                    // Calculate the length of this normal.
                    length = (float)Math.Sqrt((sum.X * sum.X) + (sum.Y * sum.Y) + (sum.Z * sum.Z));

                    // Get an index to the vertex location in the height map array.
                    index = (j * m_TerrainHeight) + i;

                    // Normalize the final shared normal for this vertex and store it in the height map array.
                    DHeightMapType editHeightMap = HeightMap[index];
                    editHeightMap.nx = (sum.X / length);
                    editHeightMap.ny = (sum.Y / length);
                    editHeightMap.nz = (sum.Z / length);
                    HeightMap[index] = editHeightMap;
                }
            }

            // Release the temporary normals.
            normals = null;
          
            return true;
        }
        private void NormalizeHeightMap()
        {
            for (var i = 0; i < HeightMap.Count; i++)
			{
				var temp = HeightMap[i];
				temp.y /= 15;
				HeightMap[i] = temp;
			}
        }
        private bool LoadHeightMap(string heightMapFileName)
        {
            Bitmap bitmap;

            try
            {
                // Open the height map file in binary.
                bitmap = new Bitmap(DSystemConfiguration.DataFilePath + heightMapFileName);
            }
            catch
            {
                return false;
            }

            // Save the dimensions of the terrain.
            m_TerrainWidth = bitmap.Width;
            m_TerrainHeight = bitmap.Height;

            // Create the structure to hold the height map data.
            HeightMap = new List<DHeightMapType>(m_TerrainWidth * m_TerrainHeight);

            // Read the image data into the height map
            for (var j = 0; j < m_TerrainHeight; j++)
                for (var i = 0; i < m_TerrainWidth; i++)
                    HeightMap.Add(new DHeightMapType()
                    {
                        x = i,
                        y = bitmap.GetPixel(i, j).R,
                        z = j
                    });

            // Release the bitmap image data.
            bitmap.Dispose();
            bitmap = null;

            return true;
        }
        private bool InitializeBuffers(SharpDX.Direct3D11.Device device)
        {
            try
            {
                /// Calculate the number of vertices in the terrain mesh.
                VertexCount = (m_TerrainWidth - 1) * (m_TerrainHeight - 1) * 6;

                // Create the vertex array.
                Verticies = new DVertexType[VertexCount];

                // Initialize the index to the vertex array.
                int index = 0;
                // Load the vertex and index arrays with the terrain data.
                for (int j = 0; j < (m_TerrainHeight - 1); j++)
                {
                    for (int i = 0; i < (m_TerrainWidth - 1); i++)
                    {
                        int indexBottomLeft1 = (m_TerrainHeight * j) + i;          // Bottom left.
                        int indexBottomRight2 = (m_TerrainHeight * j) + (i + 1);      // Bottom right.
                        int indexUpperLeft3 = (m_TerrainHeight * (j + 1)) + i;      // Upper left.
                        int indexUpperRight4 = (m_TerrainHeight * (j + 1)) + (i + 1);  // Upper right.

                        #region First Triangle
                        // Upper left.
                        float tv = HeightMap[indexUpperLeft3].tv;
                        // Modify the texture coordinates to cover the top edge.
                        if (tv == 1.0f)
                            tv = 0.0f;

                        Verticies[index] = new DVertexType()
                        {
                            position = new Vector3(HeightMap[indexUpperLeft3].x, HeightMap[indexUpperLeft3].y, HeightMap[indexUpperLeft3].z),
                            texture = new Vector2(HeightMap[indexUpperLeft3].tu, tv),
                            normal = new Vector3(HeightMap[indexUpperLeft3].nx, HeightMap[indexUpperLeft3].ny, HeightMap[indexUpperLeft3].nz)
                        };
                        index++;

                        // Upper right.
                        float tu = HeightMap[indexUpperRight4].tu;
                        tv = HeightMap[indexUpperRight4].tv;

                        // Modify the texture coordinates to cover the top and right edge.
                        if (tu == 0.0f)
                            tu = 1.0f;
                        if (tv == 1.0f)
                            tv = 0.0f;

                        Verticies[index] = new DVertexType()
                        {
                            position = new Vector3(HeightMap[indexUpperRight4].x, HeightMap[indexUpperRight4].y, HeightMap[indexUpperRight4].z),
                            texture = new Vector2(tu, tv),
                            normal = new Vector3(HeightMap[indexUpperRight4].nx, HeightMap[indexUpperRight4].ny, HeightMap[indexUpperRight4].nz)
                        };
                        index++;

                        // Bottom left.
                        Verticies[index] = new DVertexType()
                        {
                            position = new Vector3(HeightMap[indexBottomLeft1].x, HeightMap[indexBottomLeft1].y, HeightMap[indexBottomLeft1].z),
                            texture = new Vector2(HeightMap[indexBottomLeft1].tu, HeightMap[indexBottomLeft1].tv),
                            normal = new Vector3(HeightMap[indexBottomLeft1].nx, HeightMap[indexBottomLeft1].ny, HeightMap[indexBottomLeft1].nz)
                        };
                        // indices[index] = index++;
                        index++;
                        #endregion

                        #region Second Triangle
                        // Bottom left.
                        Verticies[index] = new DVertexType()
                        {
                            position = new Vector3(HeightMap[indexBottomLeft1].x, HeightMap[indexBottomLeft1].y, HeightMap[indexBottomLeft1].z),
                            texture = new Vector2(HeightMap[indexBottomLeft1].tu, HeightMap[indexBottomLeft1].tv),
                            normal = new Vector3(HeightMap[indexBottomLeft1].nx, HeightMap[indexBottomLeft1].ny, HeightMap[indexBottomLeft1].nz)
                        };
                        index++;

                        // Upper right.
                        tu = HeightMap[indexUpperRight4].tu;
                        tv = HeightMap[indexUpperRight4].tv;

                        // Modify the texture coordinates to cover the top and right edge.
                        if (tu == 0.0f)
                            tu = 1.0f;
                        if (tv == 1.0f)
                            tv = 0.0f;

                        Verticies[index] = new DVertexType()
                        {
                            position = new Vector3(HeightMap[indexUpperRight4].x, HeightMap[indexUpperRight4].y, HeightMap[indexUpperRight4].z),
                            texture = new Vector2(tu, tv),
                            normal = new Vector3(HeightMap[indexUpperRight4].nx, HeightMap[indexUpperRight4].ny, HeightMap[indexUpperRight4].nz)
                        };
                        index++;

                        // Bottom right.
                        tu = HeightMap[indexBottomRight2].tu;

                        // Modify the texture coordinates to cover the right edge.
                        if (tu == 0.0f)
                            tu = 1.0f;

                        Verticies[index] = new DVertexType()
                        {
                            position = new Vector3(HeightMap[indexBottomRight2].x, HeightMap[indexBottomRight2].y, HeightMap[indexBottomRight2].z),
                            texture = new Vector2(tu, HeightMap[indexBottomRight2].tv),
                            normal = new Vector3(HeightMap[indexBottomRight2].nx, HeightMap[indexBottomRight2].ny, HeightMap[indexBottomRight2].nz)
                        };
                        index++;
                        #endregion
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        public void ShutDown()
        {
            // Release the texture.
            ReleaseTexture();

            // Release the height map data.
            ShutdownHeightMap();
        }
        private void ReleaseTexture()
        {
            // Release the texture object.
            Texture?.ShutDown();
            Texture = null;
        }
        private void ShutdownHeightMap()
        {
            // Release the HeightMap Data loaded from the file.
            HeightMap?.Clear();
            HeightMap = null;
        }
    }
}