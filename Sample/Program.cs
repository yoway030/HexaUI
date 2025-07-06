using HexaUI;
using HexaUI.D3D11;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        App.Init(Backend.DirectX);
        App.Run(new DX11Window());
    }
}