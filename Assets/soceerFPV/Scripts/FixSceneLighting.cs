using UnityEngine;

public class FixSceneLighting : MonoBehaviour
{
    void Start()
    {
        // 重置环境光
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.ambientIntensity = 1f;

        // 强制刷新
        DynamicGI.UpdateEnvironment();
    }
}