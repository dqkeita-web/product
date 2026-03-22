using Vortice.Direct3D11;
using Vortice.DXGI;

namespace FindAncestor.WinRoc
{
    internal class DxgiDuplicator : IDisposable
    {
        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private IDXGIOutputDuplication _duplication;

        private ID3D11Texture2D? _staging;

        public DxgiDuplicator()
        {
            DxgiHelper.CreateDevice(out _device, out _context);
            _duplication = DxgiHelper.CreateDuplication(_device);
        }

        public DxgiFrame? Capture(WinRocRegion region)
        {
            try
            {
                var result = _duplication.AcquireNextFrame(100, out var _, out var resource);
                if (result.Failure) return null;

                using var tex = resource.QueryInterface<ID3D11Texture2D>();
                var desc = tex.Description;

                if (_staging == null)
                {
                    desc.Usage = ResourceUsage.Staging;
                    desc.BindFlags = BindFlags.None;
                    desc.CPUAccessFlags = CpuAccessFlags.Read;
                    desc.MiscFlags = ResourceOptionFlags.None;

                    _staging = _device.CreateTexture2D(desc);
                }

                _context.CopyResource(_staging, tex);

                var map = _context.Map(_staging, 0, MapMode.Read);

                int rowPitch = (int)map.RowPitch;
                int stride = region.Width * 4;

                var buffer = new byte[stride * region.Height];

                for (int y = 0; y < region.Height; y++)
                {
                    int srcOffset = (region.Y + y) * rowPitch + region.X * 4;
                    int dstOffset = y * stride;

                    System.Runtime.InteropServices.Marshal.Copy(
                        map.DataPointer + srcOffset,
                        buffer,
                        dstOffset,
                        stride);
                }

                _context.Unmap(_staging, 0);
                _duplication.ReleaseFrame();

                return new DxgiFrame
                {
                    Buffer = buffer,
                    Width = region.Width,
                    Height = region.Height,
                    Stride = stride
                };
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _staging?.Dispose();
            _duplication.Dispose();
            _context.Dispose();
            _device.Dispose();
        }
    }
}