﻿using DSharpDXRastertek.Series2.TutTerr08.System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System; 
using System.Globalization;

namespace DSharpDXRastertek.Series2.TutTerr08.Graphics.Models
{
    public class DSkyDome
    {
        // Structs
        [StructLayout(LayoutKind.Sequential)]
        internal struct DVertexType
        {
            internal Vector3 position;
            // internal Vector4 color;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DModelType
        {
            public float x, y, z;
            public float tu, tv;
            public float nx, ny, nz;
        }

        // Variables
        public Vector4 m_apexColor, m_centerColor;

        // Properties
        private SharpDX.Direct3D11.Buffer VertexBuffer { get; set; }
        private SharpDX.Direct3D11.Buffer IndexBuffer { get; set; }
        private int VertexCount { get; set; }
        public int IndexCount { get; private set; }
        public DModelType[] Model { get; private set; }

        //public List<DHeightMapType> HeightMap = new List<DHeightMapType>();

        // Constructor
        public DSkyDome() { }

        // Methods.
        public bool Initialize(SharpDX.Direct3D11.Device device, string setupFilename)
        {
            //// Get the terrain filename, dimensions, and so forth from the setup file.
            //if (!LoadSetupFile(setupFilename))
            //    return false;

            // Load in the sky dome model.
            if (!LoadSkyDomeModel(setupFilename))
                return false;

            // Setup the X and Z coordinates for the height map as well as scale the terrain height by the height scale value.
            // SetTerrainCoordinates();

            // Initialize the vertex and index buffer that hold the geometry for the terrain.
            if (!InitializeBuffers(device))
                return false;

            // Set the color at the top of the sky dome.
            m_apexColor = new Vector4(0.0f, 0.05f, 0.6f, 1.0f);

            // Set the color at the center of the sky dome.
            m_centerColor = new Vector4(0.0f, 0.5f, 0.8f, 1.0f);

            // We can now release the height map since it is no longer needed in memory once the 3D terrain model has been built.
            ShutdownHeightMap();

            return true;
        }

        private bool LoadSkyDomeModel(string skyDomeModelFileName)
        {
            skyDomeModelFileName = DSystemConfiguration.DataFilePath + skyDomeModelFileName;

            try
            {
                // Open the model file.
                string[] lines = File.ReadAllLines(skyDomeModelFileName);
                byte[] bytesData = File.ReadAllBytes(skyDomeModelFileName);

                ushort ttt = BitConverter.ToUInt16(bytesData, 0);

                // Read up to the value of vertex count.
                int lineIndex = 0;
                bool found = false;
                while (!found)
                {
                    if (lines[lineIndex].StartsWith("Vertex Count:"))
                        found = true;
                    else
                        lineIndex++;
                }

                // Read in the vertex count, the second column after the ':' of the first row in the file.
                string stringVertexCount = lines[lineIndex].Split(':')[1];
                VertexCount = int.Parse(stringVertexCount);

                // Set the number of indices to be the same as the vertex count.
                IndexCount = VertexCount;

                // Create the model using the vertex count that was read in.
                Model = new DModelType[VertexCount];

                // Before continueing with the line parsing ensure we are starting one line after the "Data" portion of the file.
                int lineDataIndex = ++lineIndex;
                found = false;
                while (!found)
                {
                    if (lines[lineDataIndex].Equals("Data:"))
                        found = true;
                    else
                        lineDataIndex++;
                }

                // Procced to the next line for Vertex data.
                lineDataIndex++;

                // Read up to the beginning of the data.
                int vertexWriteIndex = 0;
                for (int i = lineDataIndex; i < lines.Length; i++)
                {
                    // Ignor blank lines.
                    if (string.IsNullOrEmpty(lines[i]))
                        continue;

                    // break out segments of this line.
                    string[] segments = lines[i].Split(' ');

                    // Read in the vertex data, First X, Y & Z positions.
                    Model[vertexWriteIndex].x = float.Parse(segments[0]);
                    Model[vertexWriteIndex].y = float.Parse(segments[1], NumberStyles.Float);
                    Model[vertexWriteIndex].z = float.Parse(segments[2], NumberStyles.Float);

                    // Read in the Tu and Yv tecture coordinate values.
                    Model[vertexWriteIndex].tu = float.Parse(segments[3], NumberStyles.Float);
                    Model[vertexWriteIndex].tv = float.Parse(segments[4], NumberStyles.Float);

                    // Read in the Normals X, Y & Z values.
                    Model[vertexWriteIndex].nx = float.Parse(segments[5], NumberStyles.Float);
                    Model[vertexWriteIndex].ny = float.Parse(segments[6], NumberStyles.Float);
                    Model[vertexWriteIndex].nz = float.Parse(segments[7], NumberStyles.Float, CultureInfo.InvariantCulture);
                    vertexWriteIndex++;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        //private bool LoadSetupFile(string setupFilename)
        //{
        //    // Open the setup file.  If it could not open the file then exit.
        //    setupFilename = DSystemConfiguration.DataFilePath + setupFilename;

        //    // Get all the lines containing the font data.
        //    var setupLines = File.ReadAllLines(setupFilename);

        //    // Read in the terrain file name.
        //    m_TerrainHeightManName = setupLines[0].Trim("Terrain Filename: ".ToCharArray());
        //    // Read in the terrain height & width.
        //    m_TerrainHeight = int.Parse(setupLines[1].Trim("Terrain Height: ".ToCharArray()));
        //    m_TerrainWidth = int.Parse(setupLines[2].Trim("Terrain Width: ".ToCharArray()));
        //    // Read in the terrain height scaling.
        //    m_TerrainScale = float.Parse(setupLines[3].Trim("Terrain Scaling: ".ToCharArray()));

        //    return true;
        //}
        //private void SetTerrainCoordinates()
        //{
        //    for (var i = 0; i < HeightMap.Count; i++)
        //    {
        //        var temp = HeightMap[i];
        //        temp.y /= m_TerrainScale;
        //        HeightMap[i] = temp;
        //    }
        //}
        //private bool LoadHeightMap()
        //{
        //    Bitmap bitmap;

        //    try
        //    {
        //        // Open the height map file in binary.
        //        bitmap = new Bitmap(DSystemConfiguration.TextureFilePath + m_TerrainHeightManName);
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    // Check if the width and height are correct acording to bitmap file.
        //    if (m_TerrainWidth != bitmap.Width || m_TerrainHeight != bitmap.Height)
        //        return false;

        //    // Create the structure to hold the height map data.
        //    HeightMap = new List<DHeightMapType>(m_TerrainWidth * m_TerrainHeight);
        //    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

        //    // Read the image data into the height map
        //    for (var j = 0; j < m_TerrainHeight; j++)
        //        for (var i = 0; i < m_TerrainWidth; i++)
        //            HeightMap.Add(new DHeightMapType()
        //            {
        //                x = i,
        //                y = bitmap.GetPixel(i, j).R,
        //                z = j
        //            });

        //    return true;
        //}
        private bool InitializeBuffers(SharpDX.Direct3D11.Device device)
        {
            try
            {
                // Create the vertex array.
                DVertexType[] vertices = new DVertexType[VertexCount];
                // Create the index array.
                int[] indices = new int[IndexCount];

                // Load the vertex array and index array with data.
                for (int i = 0; i < VertexCount; i++)
                {
                    vertices[i].position = new Vector3(Model[i].x, Model[i].y, Model[i].z);
                    indices[i] = i;
                }

                // Create the vertex buffer.
                VertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, vertices);

                // Create the index buffer.
                IndexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.IndexBuffer, indices);

                // Release the arrays now that the buffers have been created and loaded.
                vertices = null;
                indices = null;

                return true;
            }
            catch
            {
                return false;
            }
        }
        public void ShutDown()
        {
            // Release the vertex and index buffers.
            ShutdownBuffers();
            // Release the height map.
            ShutdownHeightMap();
        }
        private void ShutdownBuffers()
        {
            // Return the index buffer.
            IndexBuffer?.Dispose();
            IndexBuffer = null;
            // Release the vertex buffer.
            VertexBuffer?.Dispose();
            VertexBuffer = null;
        }
        private void ShutdownHeightMap()
        {
            // Release the height map array.
            Model = null;
        }
        public void Render(DeviceContext deviceContext)
        {
            // Put the vertex and index buffers on the graphics pipeline to prepare them for drawing.
            RenderBuffers(deviceContext);
        }
        private void RenderBuffers(DeviceContext deviceContext)
        {
            // Set the vertex buffer to active in the input assembler so it can be rendered.
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(VertexBuffer, Utilities.SizeOf<DVertexType>(), 0));
            // Set the index buffer to active in the input assembler so it can be rendered.
            deviceContext.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
            // Set the type of the primitive that should be rendered from this vertex buffer, in this case triangles.
            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }
    }
}
