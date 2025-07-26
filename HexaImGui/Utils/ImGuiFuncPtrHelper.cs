using Hexa.NET.ImGui;
using System.Runtime.InteropServices;

namespace HexaImGui.Utils;

public delegate uint AdapterIndexToStorageIdDelegate(ImGuiSelectionBasicStoragePtr self, int idx);

public unsafe delegate uint AdapterIndexToStorageIdDelegateUnsafed(ImGuiSelectionBasicStorage* self, int idx);

public static class ImGuiFuncPtrHelper
{
    public static void SetAdapterIndexToStorageId(
        ref ImGuiSelectionBasicStorage selectionStorage,
        AdapterIndexToStorageIdDelegate indexToStrageIdDelegate)
    {
        unsafe
        {
            AdapterIndexToStorageIdDelegateUnsafed adapterUnsafed = (storage, idx) =>
            {
                return indexToStrageIdDelegate(new ImGuiSelectionBasicStoragePtr { Handle = storage }, idx);
            };

            selectionStorage.AdapterIndexToStorageId =
                Marshal.GetFunctionPointerForDelegate(adapterUnsafed).ToPointer();
        }
    }
}
