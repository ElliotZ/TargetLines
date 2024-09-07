using System;
using System.Runtime.InteropServices;
using DrahsidLib;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Vector3 = System.Numerics.Vector3;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace TargetLines;

public class RawDX11Scene {
    public bool Initialized { get; private set; } = false;
    public Device Device { get; private set; }
    public SwapChain SwapChain { get; private set; }
    public IntPtr WindowHandlePtr { get; private set; }
    
    public ViewportF Viewport { get; internal set; }

    public Matrix ViewMatrix = Matrix.Identity;
    public Matrix ProjectionMatrix = Matrix.Identity;
    public Matrix ViewProjectionMatrix = Matrix.Identity;
    public Vector3 CameraPosition = Vector3.Zero;

    public delegate void NewFrameDelegate();

    public NewFrameDelegate OnNewFrame;

    private DeviceContext deviceContext;
    private RenderTargetView rtv;

    private int targetWidth;
    private int targetHeight;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetEngineCoreSingletonDelegate();

    private IntPtr _engineCoreSingleton;

    public RawDX11Scene(IntPtr nativeSwapChain) {
        SwapChain = new SwapChain(nativeSwapChain);
        Device = SwapChain.GetDevice<Device>();
        deviceContext = Device.ImmediateContext;

        using (var backbuffer = SwapChain.GetBackBuffer<Texture2D>(0))
        {
            rtv = new RenderTargetView(Device, backbuffer);
        }

        targetWidth = SwapChain.Description.ModeDescription.Width;
        targetHeight = SwapChain.Description.ModeDescription.Height;
        Viewport = new ViewportF(0, 0, SwapChain.Description.ModeDescription.Width, SwapChain.Description.ModeDescription.Height, 0, 1.0f);
        WindowHandlePtr = SwapChain.Description.OutputHandle;

        _engineCoreSingleton = Marshal.GetDelegateForFunctionPointer<GetEngineCoreSingletonDelegate>(Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4C 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??"))();

        Initialized = true;
    }

    private void Dispose(bool disposing) {
        rtv?.Dispose();
    }

    public void Dispose() {
        Dispose(true);
    }

    public unsafe Matrix ReadMatrix(IntPtr address) {
        var p = (float*)address;
        Matrix matrix = new();
        for (int index = 0; index < 16; index++) {
            matrix[index] = *p++;
        }
        return matrix;
    }

    public void Render() {
        if (targetWidth <= 0 || targetHeight <= 0) {
            return;
        }

        deviceContext.OutputMerger.SetRenderTargets(rtv);
        deviceContext.Rasterizer.SetViewport(Viewport);

        var blendStateDescription = new BlendStateDescription();
        blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
        blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
        blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
        blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
        blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
        blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
        blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

        var blendState = new BlendState(Device, blendStateDescription);
        deviceContext.OutputMerger.SetBlendState(blendState);

        unsafe
        {
            var control = Control.Instance();
            if (control != null)
            {
                /*
                IntPtr viewProjectionMatrix = (IntPtr)(&control->ViewProjectionMatrix);
                ViewProjectionMatrix = ReadMatrix(viewProjectionMatrix);
                ProjectionMatrix = ReadMatrix(viewProjectionMatrix - 0x40);
                ViewMatrix = ViewProjectionMatrix * Matrix.Invert(ProjectionMatrix);
                */

                var cam = control->CameraManager.GetActiveCamera();
                if (cam != null) {
                    CameraPosition = cam->CameraBase.SceneCamera.Position;
                }

                ViewProjectionMatrix = ReadMatrix(_engineCoreSingleton + 0x1B4);
                ProjectionMatrix = ReadMatrix(_engineCoreSingleton + 0x174);
                ViewMatrix = ReadMatrix(_engineCoreSingleton + 0x134);
            }
        }


        OnNewFrame?.Invoke();
        deviceContext.OutputMerger.SetRenderTargets((RenderTargetView)null);
    }

    public void OnPreResize() {
        deviceContext.OutputMerger.SetRenderTargets((RenderTargetView)null);

        rtv?.Dispose();
        rtv = null;
    }

    public void OnPostResize(int newWidth, int newHeight) {
        using (var backbuffer = SwapChain.GetBackBuffer<Texture2D>(0))
        {
            rtv = new RenderTargetView(Device, backbuffer);
        }

        targetWidth = newWidth;
        targetHeight = newHeight;
        Viewport = new ViewportF(0, 0, newWidth, newHeight, 0, 1.0f);
    }
}
