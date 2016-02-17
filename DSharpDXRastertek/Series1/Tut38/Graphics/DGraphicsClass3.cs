﻿using DSharpDXRastertek.Tut38.System;
using System;
using System.Windows.Forms;

namespace DSharpDXRastertek.Tut38.Graphics
{
    public class DGraphics                  // 104 lines
    {
        // Properties 
        private DDX11 D3D { get; set; }
        private DCamera Camera { get; set; }
        private DModel Model { get; set; }
        private DColorShader ColorShader { get; set; }

        // Constructor
        public DGraphics() { }

        // Mathods
        public bool Initialize(DSystemConfiguration configuration, IntPtr windowHandle)
        {
            try
            {
                // Create the Direct3D object.
                D3D = new DDX11();
                
                // Initialize the Direct3D object.
                if (!D3D.Initialize(configuration, windowHandle))
                    return false;

                // Create the camera object
                Camera = new DCamera();

                // Set the initial position of the camera.
                Camera.SetPosition(0, 0, -3);

                // Create the model object.
                Model = new DModel();

                // Initialize the model object.
                if (!Model.Initialize(D3D.Device))
                    return false;

                // Create the color shader object.
                ColorShader = new DColorShader();

                // Initialize the color shader object.
                if (!ColorShader.Initialize(D3D.Device, windowHandle))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not initialize Direct3D\nError is '" + ex.Message + "'");
                return false;
            }
        }
        public void ShutDown()
        {
            // Release the camera object.
            Camera = null;

            // Release the color shader object.
            ColorShader?.ShutDown();
            ColorShader = null;
            // Release the model object.
            Model?.ShutDown();
            Model = null;
            // Release the Direct3D object.
            D3D?.ShutDown();
            D3D = null;
        }
        public bool Frame()
        {
            // Render the graphics scene.
            return Render();
        }
        private bool Render()
        {
            // Clear the buffer to begin the scene.
            D3D.BeginScene(0f, 0f, 0f, 1f);

            // Generate the view matrix based on the camera position.
            Camera.Render();

            // Get the world, view, and projection matrices from camera and d3d objects.
            var viewMatrix = Camera.ViewMatrix;
            var worldMatrix = D3D.WorldMatrix;
            var projectionMatrix = D3D.ProjectionMatrix;

            // Put the model vertex and index buffers on the graphics pipeline to prepare them for drawing.
            Model.Render(D3D.DeviceContext);

            // Render the model using the color shader.
            if (!ColorShader.Render(D3D.DeviceContext, Model.IndexCount, worldMatrix, viewMatrix, projectionMatrix, 12.0f))
                return false;

            // Present the rendered scene to the screen.
            D3D.EndScene();

            return true;
        }
    }
}