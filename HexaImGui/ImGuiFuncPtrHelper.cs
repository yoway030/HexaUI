using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HexaImGui;

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
