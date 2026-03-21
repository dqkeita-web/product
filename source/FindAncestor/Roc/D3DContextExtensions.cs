using Vortice.Direct3D11;
using System;


namespace FindAncestor.Roc
{
    public static class D3DContextExtensions
    {
        // ID3D11DeviceContext に対する拡張メソッド
        public static ID3D11Texture2D CaptureFrame(this ID3D11DeviceContext context, ID3D11Texture2D source)
        {
            // ステージングテクスチャ作成
            var desc = source.Description;
            desc.Usage = ResourceUsage.Staging;
            desc.BindFlags = BindFlags.None;
            desc.CPUAccessFlags = CpuAccessFlags.Read;
            desc.MiscFlags = ResourceOptionFlags.None;

            var staging = context.Device.CreateTexture2D(desc);

            // コピー
            context.CopyResource(staging, source);

            return staging;
        }
    }
}