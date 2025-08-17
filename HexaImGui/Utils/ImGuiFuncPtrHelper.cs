using Hexa.NET.ImGui;
using System.Runtime.InteropServices;

namespace ELImGui.Utils;

public delegate uint AdapterIndexToStorageIdDelegate(ImGuiSelectionBasicStoragePtr self, int idx);
public unsafe delegate uint AdapterIndexToStorageIdDelegateUnsafed(ImGuiSelectionBasicStorage* self, int idx);

public delegate object AdapterObjectToStorageIdDelegate(ImGuiSelectionBasicStoragePtr self, object obj);
public unsafe delegate object AdapterObjectToStorageIdDelegateUnsafed(ImGuiSelectionBasicStorage* self, object obj);

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

    public static void SetAdapterObjectToStorageId(
        ref ImGuiSelectionBasicStorage selectionStorage,
        AdapterObjectToStorageIdDelegate indexToStrageIdDelegate)
    {
        unsafe
        {
            AdapterObjectToStorageIdDelegateUnsafed adapterUnsafed = (storage, obj) =>
            {
                return indexToStrageIdDelegate(new ImGuiSelectionBasicStoragePtr { Handle = storage }, obj);
            };

            selectionStorage.AdapterIndexToStorageId =
                Marshal.GetFunctionPointerForDelegate(adapterUnsafed).ToPointer();
        }
    }
}
