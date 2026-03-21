using System;
using System.Windows.Interop;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace FindAncestor.Roc

{
    public class D3DContext : IDisposable
    {
        public ID3D11Device Device { get; private set; }
        public ID3D11DeviceContext DeviceContext { get; private set; }
        private IDXGISwapChain1 _swapChain;
        private ID3D11Texture2D _sharedTexture;

        public D3DImage D3DImage { get; private set; }

        public D3DContext(IntPtr hwnd, int width, int height)
        {
            // FeatureLevel を指定
            FeatureLevel[] featureLevels = new FeatureLevel[]
            {
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            };

            // デバイス作成
            DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport;
#if DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif
            D3D11.D3D11CreateDevice(
                adapter: null,
                driverType: DriverType.Hardware,
                flags: flags,
                featureLevels: featureLevels,
                out ID3D11Device device,
                out FeatureLevel featureLevel,
                out ID3D11DeviceContext context
            );

            Device = device;
            DeviceContext = context;

            // SwapChain 設定
            var swapChainDesc = new SwapChainDescription1
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription(1, 0),
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                AlphaMode = AlphaMode.Premultiplied
            };




            using var dxgiFactory = Device.QueryInterface<IDXGIDevice>().GetAdapter().GetParent<IDXGIFactory2>();
            _swapChain = dxgiFactory.CreateSwapChainForHwnd(Device, hwnd, swapChainDesc);

            // 共有テクスチャ取得
            _sharedTexture = _swapChain.GetBuffer<ID3D11Texture2D>(0);

            // D3DImage 用
            D3DImage = new D3DImage();
            D3DImage.Lock();
            D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _sharedTexture.NativePointer);
            D3DImage.Unlock();
        }

        public void Present() => _swapChain.Present(1, PresentFlags.None);

        public void Dispose()
        {
            _swapChain?.Dispose();
            _sharedTexture?.Dispose();
            DeviceContext?.Dispose();
            Device?.Dispose();
        }

    }
}