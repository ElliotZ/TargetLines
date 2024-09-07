using DrahsidLib;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SwapChain = SharpDX.DXGI.SwapChain;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace TargetLines;

internal class LineActor {
    [StructLayout(LayoutKind.Explicit)]
    internal struct LineVertex
    {
        [FieldOffset(0x00)] public Vector3 Position;
        [FieldOffset(0x0C)] public Vector3 Normal;
        [FieldOffset(0x18)] public Vector2 TexCoord;
    }


    [StructLayout(LayoutKind.Explicit, Size = 0x50)]
    private struct ConstantBuffer
    {
        [FieldOffset(0)] public Matrix ViewProjection;
        [FieldOffset(64)] public Vector3 CameraPosition;
        [FieldOffset(76)] public float RibbonWidth;
    }

    public Vector3 Source;
    public Vector3 Destination;
    public int NumSegments = 7;
    public bool IsQuadratic;

    private Vector3 Middle;

    private Device device;
    private SwapChain swapChain;
    private DeviceContext deviceContext;

    private Buffer vertexBuffer { get; set; }
    private Buffer indexBuffer { get; set; }
    private InputLayout layout { get; set; }
    private VertexBufferBinding vertexBufferBinding { get; set; }

    public Vector3[] linePoints { get; set; }
    private LineVertex[] lineVertices { get; set; }
    private int[] lineIndices { get; set; }

    private ConstantBuffer constantBuffer;
    private Buffer constantBufferBuffer { get; set; }

    private int _numSegments { get; set; }
    private int _frame = 0;

    private bool fail = false;

    private int segmentsPerCylinder = 8;

    private int vertexCount => (_numSegments + 1) * 2;
    private int indexCount => _numSegments * 6;

    public LineActor(Device _device, SwapChain _swapChain) {
        device = _device;
        swapChain = _swapChain;
        deviceContext = _device.ImmediateContext;

        _numSegments = NumSegments = 7;
        IsQuadratic = false;
        constantBuffer = new ConstantBuffer();

        InitializeMemory();

        layout = new InputLayout(
            device,
            ShaderSingleton.GetVertexShaderBytecode(ShaderSingleton.Shader.Line).Data,
            new InputElement[] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24)
            }
        );

        if (SwapChainHook.Renderer != null) {
            SwapChainHook.Renderer.OnFrameEvent += OnFrame;
        }
    }

    public void Dispose() {
        layout?.Dispose();
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        constantBufferBuffer?.Dispose();
        layout = null;
        vertexBuffer = null;
        indexBuffer = null;
        if (SwapChainHook.Renderer != null) {
            SwapChainHook.Renderer.OnFrameEvent -= OnFrame;
        }
    }

    private void InitializeMemory() {
        _numSegments = NumSegments;

        linePoints = new Vector3[NumSegments + 1];  // +1 because NumSegments is number of segments, not points
        lineVertices = new LineVertex[vertexCount];
        lineIndices = new int[indexCount];

        var vertexBufferDesc = new BufferDescription() {
            SizeInBytes = Utilities.SizeOf<LineVertex>() * vertexCount,
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.VertexBuffer,
            CpuAccessFlags = CpuAccessFlags.Write,
            OptionFlags = ResourceOptionFlags.None,
            StructureByteStride = Utilities.SizeOf<LineVertex>()
        };
        vertexBuffer = new Buffer(device, vertexBufferDesc);

        var indexBufferDesc = new BufferDescription() {
            SizeInBytes = Utilities.SizeOf<int>() * indexCount,
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.IndexBuffer,
            CpuAccessFlags = CpuAccessFlags.Write,
            OptionFlags = ResourceOptionFlags.None,
            StructureByteStride = Utilities.SizeOf<int>()
        };
        indexBuffer = new Buffer(device, indexBufferDesc);
        vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<LineVertex>(), 0);

        constantBufferBuffer = new Buffer(device, Utilities.SizeOf<ConstantBuffer>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
    }

    private void CreateLinePoints() {
        Vector3 point = Vector3.Zero;
        for (int index = 0; index < linePoints.Length; index++) {
            float time = (float)index / ((float)linePoints.Length - 1);

            if (IsQuadratic) {
                point = MathUtils.EvaluateQuadratic(Source, Middle, Destination, time);
            }
            else {
                point = MathUtils.EvaluateCubic(Source, Middle, Middle, Destination, time);
            }

            linePoints[index] = point;
        }
    }

    Vector2 ScreenCoordsToNDC(Vector2 coords) {
        float screenWidth = SwapChainHook.Scene.Viewport.Width;
        float screenHeight = SwapChainHook.Scene.Viewport.Height;

        return new Vector2(
            (coords.X / screenWidth) * 2.0f - 1.0f,
            1.0f - (coords.Y / screenHeight) * 2.0f
        );
    }

    private void CreateMesh()
    {
        lineVertices = new LineVertex[vertexCount];
        lineIndices = new int[indexCount];

        for (int i = 0; i <= _numSegments; i++)
        {
            Vector3 position = linePoints[i];
            Vector3 direction = Vector3.UnitZ; // Default direction
            if (i < _numSegments)
            {
                direction = Vector3.Normalize(linePoints[i + 1] - position);
            }
            else if (i > 0)
            {
                direction = Vector3.Normalize(position - linePoints[i - 1]);
            }

            float t = (float)i / _numSegments;

            lineVertices[i * 2] = new LineVertex
            {
                Position = position,
                Normal = direction,
                TexCoord = new Vector2(-0.5f, t)
            };

            lineVertices[i * 2 + 1] = new LineVertex
            {
                Position = position,
                Normal = direction,
                TexCoord = new Vector2(0.5f, t)
            };

            if (i < _numSegments)
            {
                int baseIndex = i * 6;
                int vertexIndex = i * 2;
                lineIndices[baseIndex] = vertexIndex;
                lineIndices[baseIndex + 1] = vertexIndex + 1;
                lineIndices[baseIndex + 2] = vertexIndex + 2;
                lineIndices[baseIndex + 3] = vertexIndex + 2;
                lineIndices[baseIndex + 4] = vertexIndex + 1;
                lineIndices[baseIndex + 5] = vertexIndex + 3;
            }
        }
    }

    private void UpdateBufferContents() {
        DataStream stream;

        // Update vertex buffer
        var vertexBox = deviceContext.MapSubresource(vertexBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
        Utilities.Write(vertexBox.DataPointer, lineVertices, 0, lineVertices.Length);
        deviceContext.UnmapSubresource(vertexBuffer, 0);

        // Update index buffer
        var indexBox = deviceContext.MapSubresource(indexBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
        Utilities.Write(indexBox.DataPointer, lineIndices, 0, lineIndices.Length);
        deviceContext.UnmapSubresource(indexBuffer, 0);
    }

    private unsafe void Render() {
        // if the segment count changes, we need to reallocate the relevant memory
        if (_numSegments != NumSegments) {
            _numSegments = NumSegments;
            InitializeMemory();
        }

        if (linePoints.Length < 2 || lineVertices.Length <= 0) {
            Service.Logger.Error($"Line is invalid");
            fail = true;
            return;
        }

        CreateLinePoints();
        CreateMesh();
        UpdateBufferContents();

        constantBuffer.ViewProjection = SwapChainHook.Scene.ViewProjectionMatrix;
        constantBuffer.ViewProjection.Transpose();
        constantBuffer.CameraPosition = SwapChainHook.Scene.CameraPosition;
        constantBuffer.RibbonWidth = 0.1f;
        deviceContext.UpdateSubresource(ref constantBuffer, constantBufferBuffer);

        vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<LineVertex>(), 0);

        deviceContext.InputAssembler.InputLayout = layout;
        deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

        deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
        deviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

        deviceContext.VertexShader.Set(ShaderSingleton.GetVertexShader(ShaderSingleton.Shader.Line));
        deviceContext.PixelShader.Set(ShaderSingleton.GetPixelShader(ShaderSingleton.Shader.Line));

        deviceContext.VertexShader.SetConstantBuffer(0, constantBufferBuffer);

        //deviceContext.DrawIndexed(indexCount, 0, 0);

        var blendStateDesc = new BlendStateDescription();
        blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
        blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
        blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
        blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
        blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
        blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
        blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

        using (var blendState = new BlendState(device, blendStateDesc))
        {
            deviceContext.OutputMerger.SetBlendState(blendState);
            deviceContext.DrawIndexed(indexCount, 0, 0);
        }
    }

    public void OnFrame(double _time) {
        if (fail) { return; }
        try {
            Middle = (Source + Destination) * 0.5f;
            Middle.Y += 0.5f;

            Render();
        }
        catch (Exception ex) {
            Service.Logger.Error($"Line error?\n{ex.ToString()}");
            fail = true;
        }
    }
}
