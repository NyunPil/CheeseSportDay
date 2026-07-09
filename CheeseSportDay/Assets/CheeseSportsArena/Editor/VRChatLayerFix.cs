// =============================================================
//  🧀 치즈 운동회 - VRChat 충돌 매트릭스/레이어 설정 (VRChatLayerFix)
// -------------------------------------------------------------
//  VRChat SDK의 UpdateLayers.SetupCollisionLayerMatrix() 를 직접 호출.
//  (SDK 빌드가 IsCollisionLayerMatrixSetup 에서 NullRef로 abort될 때 해결)
//  리플렉션 사용 → SDK 어셈블리 직접참조 없이 안전.
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ ⚙ VRChat 충돌매트릭스 설정
// =============================================================
#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CheeseSports
{
    public static class VRChatLayerFix
    {
        [MenuItem("Tools/🧀 치즈 운동회/⚙ VRChat 충돌매트릭스 설정")]
        static void SetupCollisionMatrix()
        {
            var type = FindType("UpdateLayers");
            if (type == null) { Debug.LogError("❌ VRChat SDK의 UpdateLayers 타입을 못 찾음 (SDK 로드됐는지 확인)"); return; }

            // 레이어도 혹시 몰라 먼저 세팅
            Invoke(type, "SetupEditorLayers");   // 있으면 호출(없으면 조용히 넘김)
            Invoke(type, "SetupLayers");
            bool ok = Invoke(type, "SetupCollisionLayerMatrix");

            if (ok) Debug.Log("✅ VRChat 충돌 매트릭스 설정 완료! 이제 SDK ▸ Build & Test 다시 해보세요. (빨간 NullRef 사라져야 함)");
            else Debug.LogError("❌ SetupCollisionLayerMatrix 메서드 호출 실패 — 메서드명이 바뀌었을 수 있어요.");
        }

        static bool Invoke(Type type, string method)
        {
            var m = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (m == null) return false;
            try { m.Invoke(null, null); return true; }
            catch (Exception e) { Debug.LogWarning($"{method} 호출 중 예외: {e.InnerException?.Message ?? e.Message}"); return false; }
        }

        static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { foreach (var t in asm.GetTypes()) if (t.Name == name) return t; }
                catch { }
            }
            return null;
        }
    }
}
#endif
