﻿using DSharpDXRastertek.Tut21.System;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DSharpDXRastertek.Tut21.Graphics.Shaders
{
    public class DSpecMapShader                 // 359 lines
    {
        // Struct
        [StructLayout(LayoutKind.Sequential)]
        internal struct DMatrixBuffer
        {
            public Matrix world;
            public Matrix view;
            public Matrix projection;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct DCameraBuffer
        {
            public Vector3 cameraPosition;
            public float padding;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct DLightBuffer
        {
            public Vector4 diffuseColor;
            public Vector3 lightDirection;
            public float specularPower;
            public Vector4 specularColor;
        }

        // Properties
        public VertexShader VertexShader { get; set; }
        public PixelShader PixelShader { get; set; }
        public InputLayout Layout { get; set; }
        public SharpDX.Direct3D11.Buffer ConstantMatrixBuffer { get; set; }
        public SharpDX.Direct3D11.Buffer ConstantLightBuffer { get; set; }
        public SharpDX.Direct3D11.Buffer ConstantCameraBuffer { get; set; }
        public SamplerState SamplerState { get; set; }

        // Constructor
        public DSpecMapShader() { }

        // Methods
        public bool Initialize(SharpDX.Direct3D11.Device device, IntPtr windowsHandler)
        {
            // Initialize the vertex and pixel shaders.
            return InitializeShader(device, windowsHandler, "specmap.vs", "specmap.ps");
        }
        private bool InitializeShader(SharpDX.Direct3D11.Device device, IntPtr windowsHandler, string vsFileName, string psFileName)
        {
            try
            {
                // Setup full pathes
                vsFileName = DSystemConfiguration.ShaderFilePath + vsFileName;
                psFileName = DSystemConfiguration.ShaderFilePath + psFileName;

                #region Initialize Shaders
                // Compile the Vertex Shader & Pixel Shader code.
                ShaderBytecode vertexShaderByteCode = ShaderBytecode.CompileFromFile(vsFileName, "SpecMapVertexShader", DSystemConfiguration.VertexShaderProfile, ShaderFlags.None, EffectFlags.None);
                ShaderBytecode pixelShaderByteCode = ShaderBytecode.CompileFromFile(psFileName, "SpecMapPixelShader", DSystemConfiguration.PixelShaderProfile, ShaderFlags.None, EffectFlags.None);

                // Create the Vertex & Pixel Shaders from the buffer.
                VertexShader = new VertexShader(device, vertexShaderByteCode);
                PixelShader = new PixelShader(device, pixelShaderByteCode);
                #endregion

                #region Initialize Input Layouts
                // Now setup the layout of the data that goes into the shader.
                // This setup needs to match the VertexType structure in the Model and in the shader.
                InputElement[] inputElements = new InputElement[] 
                {
                    new InputElement()
                    {
                        SemanticName = "POSITION",
                        SemanticIndex = 0,
                        Format = SharpDX.DXGI.Format.R32G32B32_Float,
                        Slot = 0,
                        AlignedByteOffset = 0,
                        Classification = InputClassification.PerVertexData,
                        InstanceDataStepRate = 0
                    },
                    new InputElement()
                    {
                        SemanticName = "TEXCOORD",
                        SemanticIndex = 0,
                        Format = SharpDX.DXGI.Format.R32G32_Float,
                        Slot = 0,
                        AlignedByteOffset = InputElement.AppendAligned,
                        Classification = InputClassification.PerVertexData,
                        InstanceDataStepRate = 0
                    },
                    new InputElement()
					{
						SemanticName = "NORMAL",
						SemanticIndex = 0,
						Format = Format.R32G32B32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					},
                    new InputElement()
					{
						SemanticName = "TANGENT",
						SemanticIndex = 0,
						Format = Format.R32G32B32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					},
					new InputElement()
					{
						SemanticName = "BINORMAL",
						SemanticIndex = 0,
						Format = Format.R32G32B32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					}
                };

                // Create the vertex input the layout. Kin dof like a Vertex Declaration.
                Layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);
                #endregion

                // Release the vertex and pixel shader buffers, since they are no longer needed.
                vertexShaderByteCode.Dispose();
                pixelShaderByteCode.Dispose();

                #region Initialize Matrix Buffer
                // Setup the description of the dynamic matrix constant Matrix buffer that is in the vertex shader.
                BufferDescription matrixBufferDescription = new BufferDescription()
                {
                    Usage = ResourceUsage.Dynamic,
                    SizeInBytes = Utilities.SizeOf<DMatrixBuffer>(),
                    BindFlags = BindFlags.ConstantBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = 0
                };

                // Create the constant buffer pointer so we can access the vertex shader constant buffer from within this class.
                ConstantMatrixBuffer = new SharpDX.Direct3D11.Buffer(device, matrixBufferDescription);
                #endregion

                #region Initialize Sampler
                // Create a texture sampler state description.
                SamplerStateDescription samplerDesc = new SamplerStateDescription()
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = TextureAddressMode.Wrap,
                    AddressV = TextureAddressMode.Wrap,
                    AddressW = TextureAddressMode.Wrap,
                    MipLodBias = 0,
                    MaximumAnisotropy = 1,
                    ComparisonFunction = Comparison.Always,
                    BorderColor = new Color4(0, 0, 0, 0),  // Black Border.
                    MinimumLod = 0,
                    MaximumLod = float.MaxValue
                };

                // Create the texture sampler state.
                SamplerState = new SamplerState(device, samplerDesc);
                #endregion

                #region Initialize Light Buffer
                // Setup the description of the light dynamic constant bufffer that is in the pixel shader.
				BufferDescription lightBufferDesc = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = Utilities.SizeOf<DLightBuffer>(),
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				// Create the constant buffer pointer so we can access the vertex shader constant buffer from within this class.
				ConstantLightBuffer = new SharpDX.Direct3D11.Buffer(device, lightBufferDesc);
                #endregion

                #region Initialize Camera Buffer
                // Setup the description of the camera dynamic constant buffer that is in the vertex shader.
                BufferDescription cameraBufferDesc = new BufferDescription()
                {
                    Usage = ResourceUsage.Dynamic,
                    SizeInBytes = Utilities.SizeOf<DCameraBuffer>(),
                    BindFlags = BindFlags.ConstantBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = 0
                };

                // Create the constant buffer pointer so we can access the vertex shader constant buffer from within this class.
                ConstantCameraBuffer = new SharpDX.Direct3D11.Buffer(device, cameraBufferDesc);
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing shader. Error is " + ex.Message);
                return false;
            }
        }
        public void ShutDown()
        {
            // Shutdown the vertex and pixel shaders as well as the related objects.
            ShuddownShader();
        }
        private void ShuddownShader()
        {
            // Release the sampler state.
            SamplerState?.Dispose();
            SamplerState = null;
            // Release the camera constant buffer.
            ConstantCameraBuffer?.Dispose();
            ConstantCameraBuffer = null;
            // Release the light constant buffer.
            ConstantLightBuffer?.Dispose();
            ConstantLightBuffer = null;
            // Release the matrix constant buffer.
            ConstantMatrixBuffer?.Dispose();
            ConstantMatrixBuffer = null;
            // Release the layout.
            Layout?.Dispose();
            Layout = null;
            // Release the pixel shader.
            PixelShader?.Dispose();
            PixelShader = null;
            // Release the vertex shader.
            VertexShader?.Dispose();
            VertexShader = null;
        }
        public bool Render(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView[] textures, Vector3 lightDirection, Vector4 diffuseColor, Vector3 cameraPosition, Vector4 specularColor, float specularPower)
        {
            // Set the shader parameters that it will use for rendering.
            if (!SetShaderParameters(deviceContext, worldMatrix, viewMatrix, projectionMatrix, textures, lightDirection, diffuseColor, cameraPosition, specularColor, specularPower))
                return false;

            // Now render the prepared buffers with the shader.
            RenderShader(deviceContext, indexCount);

            return true;
        }
        private bool SetShaderParameters(DeviceContext deviceContext, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView[] textures, Vector3 lightDirection, Vector4 diffuseColor, Vector3 cameraPosition, Vector4 specularColor, float specularPower)
        {
            try
            {
                #region Constant Matrix Buffer
                // Transpose the matrices to prepare them for shader.
                worldMatrix.Transpose();
                viewMatrix.Transpose();
                projectionMatrix.Transpose();

                // Lock the constant buffer so it can be written to.
                DataStream mappedResource;
                deviceContext.MapSubresource(ConstantMatrixBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out mappedResource);

                // Copy the passed in matrices into the constant buffer.
                DMatrixBuffer matrixBuffer = new DMatrixBuffer()
                {
                    world = worldMatrix,
                    view = viewMatrix,
                    projection = projectionMatrix
                };
                mappedResource.Write(matrixBuffer);

                // Unlock the constant buffer.
                deviceContext.UnmapSubresource(ConstantMatrixBuffer, 0);

                // Set the position of the constant buffer in the vertex shader.
                int bufferPositionNumber = 0;

                // Finally set the constant buffer in the vertex shader with the updated values.
                deviceContext.VertexShader.SetConstantBuffer(bufferPositionNumber, ConstantMatrixBuffer);

                // Set shader resource in the pixel shader.
                deviceContext.PixelShader.SetShaderResources(0, textures);
                #endregion

                #region Constant Light Buffer
                // Lock the light constant buffer so it can be written to.
                deviceContext.MapSubresource(ConstantLightBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out mappedResource);

                // Copy the lighting variables into the constant buffer.
                DLightBuffer lightBuffer = new DLightBuffer()
                {
                    diffuseColor = diffuseColor,
                    lightDirection = lightDirection,
                    specularColor = specularColor,
                    specularPower = specularPower,
                };
                mappedResource.Write(lightBuffer);

                // Unlock the constant buffer.
                deviceContext.UnmapSubresource(ConstantLightBuffer, 0);

                // Set the position of the light constant buffer in the pixel shader.
                bufferPositionNumber = 0;

                // Finally set the light constant buffer in the pixel shader with the updated values.
                deviceContext.PixelShader.SetConstantBuffer(bufferPositionNumber, ConstantLightBuffer);
                #endregion

                #region Constant Camera Buffer
                // Lock the camera constant buffer so it can be written to.
                deviceContext.MapSubresource(ConstantCameraBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out mappedResource);

                // Copy the lighting variables into the constant buffer.
                var cameraBuffer = new DCameraBuffer()
                {
                    cameraPosition = cameraPosition,
                    padding = 0.0f
                };
                mappedResource.Write(cameraBuffer);

                // Unlock the constant buffer.
                deviceContext.UnmapSubresource(ConstantCameraBuffer, 0);

                // Set the position of the light constant buffer in the pixel shader.
                bufferPositionNumber = 1;

                // Now set the camera constant buffer in the vertex shader with the updated values.
                deviceContext.VertexShader.SetConstantBuffer(bufferPositionNumber, ConstantCameraBuffer);
                #endregion

                return true;
            }
            catch
            {
                return false;
            }
        }
        private void RenderShader(DeviceContext deviceContext, int indexCount)
        {
            // Set the vertex input layout.
            deviceContext.InputAssembler.InputLayout = Layout;

            // Set the vertex and pixel shaders that will be used to render this triangle.
            deviceContext.VertexShader.Set(VertexShader);
            deviceContext.PixelShader.Set(PixelShader);

            // Set the sampler state in the pixel shader.
            deviceContext.PixelShader.SetSampler(0, SamplerState);

            // Render the triangle.
            deviceContext.DrawIndexed(indexCount, 0, 0);
        }
    }
}