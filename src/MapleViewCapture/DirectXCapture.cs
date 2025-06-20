using System;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.DXGI;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11DeviceContext = SharpDX.Direct3D11.DeviceContext;
using D3D11Texture2D = SharpDX.Direct3D11.Texture2D;
using D3D11ResourceUsage = SharpDX.Direct3D11.ResourceUsage;
using D3D11CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags;
using D3D11MapMode = SharpDX.Direct3D11.MapMode;
using MapMode = SharpDX.Direct3D11.MapMode;

namespace MapleViewCapture
{
    public class DirectXCapture : IDisposable
    {
        private D3D11Device? device;
        private D3D11DeviceContext? deviceContext;
        private Adapter1? adapter;
        private Output? output;
        private Output1? output1;
        private OutputDuplication? outputDuplication;
        private bool disposed = false;

        public DirectXCapture()
        {
            InitializeDirectX();
        }

        private void InitializeDirectX()
        {
            try
            {
                // DirectX 디바이스 생성
                adapter = new Factory1().GetAdapter1(0);
                device = new D3D11Device(adapter);
                deviceContext = device.ImmediateContext;

                // 출력 디바이스 가져오기
                output = adapter.GetOutput(0);
                output1 = output.QueryInterface<Output1>();

                // Desktop Duplication 초기화
                outputDuplication = output1.DuplicateOutput(device);
            }
            catch (Exception ex)
            {
                throw new Exception($"DirectX 초기화 실패: {ex.Message}");
            }
        }

        public Bitmap? CaptureScreen()
        {
            try
            {
                if (outputDuplication == null || deviceContext == null || device == null)
                    return null;

                SharpDX.DXGI.Resource screenResource;
                OutputDuplicateFrameInformation duplicateFrameInformation;

                // 프레임 획득
                var result = outputDuplication.TryAcquireNextFrame(100, out duplicateFrameInformation, out screenResource);
                
                if (!result.Success)
                {
                    return null;
                }

                // 텍스처로 변환
                using (var screenTexture2D = screenResource.QueryInterface<D3D11Texture2D>())
                {
                    var textureDesc = screenTexture2D.Description;
                    textureDesc.CpuAccessFlags = D3D11CpuAccessFlags.Read;
                    textureDesc.Usage = D3D11ResourceUsage.Staging;
                    textureDesc.BindFlags = 0;
                    // MiscFlags 제거 (SharpDX 버전 호환성 문제)

                    using (var stagingTexture = new D3D11Texture2D(device, textureDesc))
                    {
                        deviceContext.CopyResource(screenTexture2D, stagingTexture);

                        // 비트맵으로 변환
                        var mapSource = deviceContext.MapSubresource(stagingTexture, 0, D3D11MapMode.Read, 0);
                        
                        var bitmap = new Bitmap(textureDesc.Width, textureDesc.Height, PixelFormat.Format32bppArgb);
                        var boundsRect = new Rectangle(0, 0, textureDesc.Width, textureDesc.Height);
                        
                        var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var sourcePtr = mapSource.DataPointer;
                        var destPtr = mapDest.Scan0;
                        
                        for (int y = 0; y < textureDesc.Height; y++)
                        {
                            Utilities.CopyMemory(destPtr, sourcePtr, textureDesc.Width * 4);
                            sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                            destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                        }
                        
                        bitmap.UnlockBits(mapDest);
                        deviceContext.UnmapSubresource(stagingTexture, 0);

                        screenResource?.Dispose();
                        outputDuplication.ReleaseFrame();

                        return bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"화면 캡처 실패: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                outputDuplication?.Dispose();
                output1?.Dispose();
                output?.Dispose();
                deviceContext?.Dispose();
                device?.Dispose();
                adapter?.Dispose();
                disposed = true;
            }
        }
    }
}
