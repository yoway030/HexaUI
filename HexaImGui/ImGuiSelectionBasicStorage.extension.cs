using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HexaImGui;

// 함수 포인터 형식 (unsafe context)
public unsafe delegate uint AdapterIndexToStorageIdDelegate(ImGuiSelectionBasicStorage* self, int idx);
