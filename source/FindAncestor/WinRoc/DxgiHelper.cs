using Vortice.Direct3D11;
using Vortice.DXGI;

namespace FindAncestor.WinRoc
{
    internal static class DxgiHelper
    {
        public static void CreateDevice(out ID3D11Device device, out ID3D11DeviceContext context)
        {
            D3D11.D3D11CreateDevice(
                null,
                Vortice.Direct3D.DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                null,
                out device,
                out context);
        }

        public static IDXGIOutputDuplication CreateDuplication(ID3D11Device device)
        {
            using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
            using var adapter = dxgiDevice.GetAdapter();

            adapter.EnumOutputs(0, out var output);

            using (output)
            {
                var output1 = output.QueryInterface<IDXGIOutput1>();
                return output1.DuplicateOutput(device);
            }
        }
    }
}